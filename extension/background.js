// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.
//
// Service worker: the link between the Onshape tabs and the Options+ plugin.
//
// The plugin listens on a loopback WebSocket; this worker dials in and relays.
// Requests from the plugin go to the Onshape tab that last had focus, and that
// tab's toolbar state goes back to the plugin. A page cannot open the socket
// itself without Chrome asking for local network access, so it goes through here.

const PLUGIN_URL = "ws://127.0.0.1:47810/";

let socket = null;
const ports = new Map();   // tabId -> Port of that tab's content script
const states = new Map();  // tabId -> last reported toolbar state
let activeTab = null;      // the Onshape tab the plugin talks to

function connect() {
  if (ports.size === 0) {
    return;
  }

  if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
    return;
  }

  try {
    socket = new WebSocket(PLUGIN_URL);
  } catch {
    socket = null;
    return;
  }

  socket.onopen = () => {
    send({ type: "hello", version: chrome.runtime.getManifest().version });
    pushState();
  };

  socket.onmessage = (event) => {
    try {
      fromPlugin(JSON.parse(event.data));
    } catch (error) {
      console.warn("Bad message from the plugin", error);
    }
  };

  // Options+ not running, or the plugin reloading. The alarm below retries.
  socket.onclose = () => {
    socket = null;
  };
  socket.onerror = () => {};
}

function send(message) {
  if (socket?.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  }
}

function pushState() {
  const state = activeTab != null ? states.get(activeTab) : null;
  send({ type: "state", state: state ?? { context: "none" } });
}

function fromPlugin(message) {
  if (message.type === "ping") {
    send({ type: "pong" });
    return;
  }

  const port = ports.get(activeTab);
  if (!port) {
    if (message.id != null) {
      send({ type: "result", id: message.id, ok: false, error: "no Onshape tab" });
    }
    return;
  }

  port.postMessage(message);
}

function setActive(tabId) {
  if (activeTab !== tabId && ports.has(tabId)) {
    activeTab = tabId;
    pushState();
  }
}

chrome.runtime.onConnect.addListener((port) => {
  const tabId = port.sender?.tab?.id;
  if (port.name !== "onshape" || tabId == null) {
    return;
  }

  ports.set(tabId, port);
  if (activeTab == null) {
    activeTab = tabId;
  }
  connect();

  port.onMessage.addListener((message) => {
    switch (message.type) {
      case "state":
        states.set(tabId, message.state);
        if (message.state.focused) {
          activeTab = tabId;
        }
        if (tabId === activeTab) {
          pushState();
        }
        break;

      case "focus":
        setActive(tabId);
        break;

      default:
        // "result", "icons", "log" - for the plugin as they are.
        send(message);
    }
  });

  port.onDisconnect.addListener(() => {
    ports.delete(tabId);
    states.delete(tabId);
    if (activeTab === tabId) {
      activeTab = [...ports.keys()].pop() ?? null;
      pushState();
    }
  });
});

// Switching tabs within a window fires no focus event in the page itself.
chrome.tabs.onActivated.addListener(({ tabId }) => setActive(tabId));

// Keep trying while Onshape is open and the plugin is not. The plugin pings every
// 20 s once connected, which keeps this worker from being shut down.
chrome.alarms.create("connect", { periodInMinutes: 0.5 });
chrome.alarms.onAlarm.addListener(connect);
