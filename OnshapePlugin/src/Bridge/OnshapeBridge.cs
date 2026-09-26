// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    // Why a command did not take effect. Falling back to a keystroke is right when
    // the page could not be reached, and wrong when it heard the command and
    // refused it - the key would then do whatever it means in that context.
    public enum BridgeResult
    {
        Unreachable,
        Rejected,
        Ok,
    }

    // The loopback WebSocket server the "Onshape MX Bridge" Chrome extension
    // connects to.
    //
    // The roles are the reverse of the Blender bridge: a browser extension cannot
    // listen on a port, so the plugin is the server and the extension's service
    // worker dials in. Only one extension connection is kept; a new one replaces
    // the old, which is what a reloaded extension looks like.
    //
    // Messages are JSON objects with a "type". The extension sends "state"
    // whenever the toolbar changes, "icons" when asked, and "result" for every
    // request that carried an "id".

    public sealed class OnshapeBridge : IDisposable
    {
        public const Int32 DefaultPort = 47810;

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(2500);

        // A Manifest V3 service worker is shut down after 30 s without events;
        // traffic on its WebSocket counts as one.
        private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(20);

        private readonly ConcurrentDictionary<Int32, TaskCompletionSource<JsonElement>> _pending =
            new ConcurrentDictionary<Int32, TaskCompletionSource<JsonElement>>();

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private WebSocket _socket;
        private Timer _pingTimer;
        private Int32 _nextRequestId;

        public Int32 Port { get; set; } = DefaultPort;

        public Boolean IsExtensionConnected => this._socket?.State == WebSocketState.Open;

        // The extension is there and it sees an Onshape tab.
        public Boolean IsConnected => this.IsExtensionConnected && this.State.HasPage;

        public OnshapeState State { get; private set; } = new OnshapeState();

        // Raised when the state or the connection status actually changed.
        public event EventHandler Changed;

        // Raised when a fresh extension connection is up - the moment to ask for icons.
        public event EventHandler Connected;

        // Sprite symbols by icon name, as the page had them.
        public event EventHandler<IReadOnlyDictionary<String, String>> IconsReceived;

        public void Start()
        {
            if (this._listener != null)
            {
                return;
            }

            this._cts = new CancellationTokenSource();
            this._listener = new HttpListener();
            this._listener.Prefixes.Add($"http://127.0.0.1:{this.Port}/");

            try
            {
                this._listener.Start();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Could not listen on 127.0.0.1:{this.Port}");
                this._listener = null;
                return;
            }

            this._pingTimer = new Timer(_ => this.Post(new JsonObject { ["type"] = "ping" }), null, PingInterval, PingInterval);
            _ = this.AcceptLoop(this._cts.Token);
            PluginLog.Info($"Waiting for the Onshape extension on ws://127.0.0.1:{this.Port}");
        }

        public void Stop()
        {
            this._pingTimer?.Dispose();
            this._pingTimer = null;

            this._cts?.Cancel();
            this._socket?.Abort();
            this._socket = null;

            try
            {
                this._listener?.Stop();
                this._listener?.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            this._listener = null;
            this.SetState(new OnshapeState());
        }

        public void Dispose() => this.Stop();

        // Sends a request to the page and waits for its answer.
        public BridgeResult Invoke(String type, JsonObject payload = null)
        {
            if (!this.IsConnected)
            {
                return BridgeResult.Unreachable;
            }

            var id = Interlocked.Increment(ref this._nextRequestId);
            var message = payload ?? new JsonObject();
            message["type"] = type;
            message["id"] = id;

            var waiter = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            this._pending[id] = waiter;

            try
            {
                if (!this.Send(message))
                {
                    return BridgeResult.Unreachable;
                }

                // A timeout is reported as a refusal, not as unreachable: the page may
                // still act on the request late, and a fallback keystroke on top of it
                // would do the thing twice.
                if (!waiter.Task.Wait(RequestTimeout))
                {
                    PluginLog.Warning($"Onshape did not answer '{type}' in time");
                    return BridgeResult.Rejected;
                }

                var response = waiter.Task.Result;
                if (response.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
                {
                    return BridgeResult.Ok;
                }

                var error = response.TryGetProperty("error", out var e) ? e.GetString() : "unknown error";
                PluginLog.Info($"Onshape refused '{type}': {error}");
                return BridgeResult.Rejected;
            }
            finally
            {
                this._pending.TryRemove(id, out _);
            }
        }

        // Fire-and-forget; for requests whose answer arrives as its own message.
        public void Post(JsonObject message)
        {
            if (this.IsExtensionConnected)
            {
                this.Send(message);
            }
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await this._listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "Accepting a connection failed");
                    continue;
                }

                _ = this.HandleConnection(context, token);
            }
        }

        private async Task HandleConnection(HttpListenerContext context, CancellationToken token)
        {
            // Any web page can open a WebSocket to localhost. Only the extension may
            // drive Onshape, so anything that is not an extension origin is turned away.
            var origin = context.Request.Headers["Origin"] ?? "";
            if (!context.Request.IsWebSocketRequest || !origin.StartsWith("chrome-extension://", StringComparison.Ordinal))
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            WebSocket socket;
            try
            {
                socket = (await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false)).WebSocket;
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "WebSocket handshake failed");
                return;
            }

            var previous = Interlocked.Exchange(ref this._socket, socket);
            previous?.Abort();

            PluginLog.Info($"Onshape extension connected from {origin}");
            this.Changed?.Invoke(this, EventArgs.Empty);
            this.Connected?.Invoke(this, EventArgs.Empty);

            try
            {
                await this.ReceiveLoop(socket, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException || ex is IOException)
            {
                PluginLog.Verbose(ex, "Extension connection dropped");
            }
            finally
            {
                if (Interlocked.CompareExchange(ref this._socket, null, socket) == socket)
                {
                    PluginLog.Info("Onshape extension disconnected");
                    this.SetState(new OnshapeState());
                    this.Changed?.Invoke(this, EventArgs.Empty);
                }

                socket.Dispose();
            }
        }

        private async Task ReceiveLoop(WebSocket socket, CancellationToken token)
        {
            var buffer = new Byte[64 * 1024];
            using (var message = new MemoryStream())
            {
                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<Byte>(buffer), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                    {
                        continue;
                    }

                    var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (Int32)message.Length);
                    message.SetLength(0);

                    try
                    {
                        this.Dispatch(JsonDocument.Parse(text).RootElement.Clone());
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Warning(ex, "Could not handle a message from the extension");
                    }
                }
            }
        }

        private void Dispatch(JsonElement message)
        {
            var type = message.TryGetProperty("type", out var t) ? t.GetString() : null;
            switch (type)
            {
                case "state":
                    if (message.TryGetProperty("state", out var state))
                    {
                        this.SetState(OnshapeState.Parse(state));
                    }

                    break;

                case "result":
                    if (message.TryGetProperty("id", out var id) && id.TryGetInt32(out var requestId)
                        && this._pending.TryGetValue(requestId, out var waiter))
                    {
                        waiter.TrySetResult(message);
                    }

                    break;

                case "icons":
                    if (message.TryGetProperty("icons", out var icons) && icons.ValueKind == JsonValueKind.Object)
                    {
                        var map = new Dictionary<String, String>();
                        foreach (var icon in icons.EnumerateObject())
                        {
                            map[icon.Name] = icon.Value.GetString();
                        }

                        this.IconsReceived?.Invoke(this, map);
                    }

                    break;

                case "hello":
                    PluginLog.Info($"Extension says hello: {message}");
                    break;

                case "log":
                    PluginLog.Info($"[extension] {(message.TryGetProperty("text", out var text) ? text.GetString() : "")}");
                    break;
            }
        }

        private void SetState(OnshapeState state)
        {
            if (state.SameAs(this.State))
            {
                return;
            }

            this.State = state;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        private Boolean Send(JsonObject message)
        {
            var socket = this._socket;
            if (socket == null || socket.State != WebSocketState.Open)
            {
                return false;
            }

            var bytes = Encoding.UTF8.GetBytes(message.ToJsonString());

            // WebSocket allows one outstanding send at a time.
            this._sendLock.Wait();
            try
            {
                socket.SendAsync(new ArraySegment<Byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
                      .Wait(RequestTimeout);
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Verbose(ex, "Sending to the extension failed");
                return false;
            }
            finally
            {
                this._sendLock.Release();
            }
        }
    }
}
