// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json.Nodes;

    // Drives Onshape from the MX Creative Console.
    //
    // Actions go through the "Onshape MX Bridge" Chrome extension, which clicks
    // Onshape's toolbar and reports back what the toolbar shows. When the extension
    // is not running, actions that have a keyboard shortcut fall back to sending it.
    //
    // The console follows the toolbar: opening a sketch switches the plugin to its
    // Sketch mode, which the default profile gives a layout of its own.

    public class OnshapePlugin : Plugin
    {
        // Mode names. "main" is built in and stands for the Part Studio.
        public const String MainMode = "main";
        public const String SketchMode = "Sketch";
        public const String AssemblyMode = "Assembly";
        public const String DialogMode = "Dialog";

        // The plugin sends keyboard shortcuts, so it is not API-only.
        public override Boolean UsesApplicationApiOnly => false;

        // This is an application plugin: its actions target Onshape.
        public override Boolean HasNoApplication => false;

        public OnshapeBridge Bridge { get; } = new OnshapeBridge();

        // Created on first use: the service loads the actions - whose OnLoad
        // subscribes to Icons.Changed - before it calls Load().
        internal IconStore Icons => this._icons ??= new IconStore(Path.Combine(this.GetPluginDataDirectory(), "icons"));

        internal IconRenderer IconRenderer => this._iconRenderer ??= new IconRenderer(this.Icons);

        private IconStore _icons;
        private IconRenderer _iconRenderer;

        private String _mode = MainMode;

        public OnshapePlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            this.AddModes();

            this.Bridge.Connected += this.OnExtensionConnected;
            this.Bridge.IconsReceived += this.OnIconsReceived;
            this.Bridge.Changed += this.OnBridgeChanged;

            // The server runs for as long as the plugin does: the extension decides
            // when there is an Onshape page to talk about.
            this.Bridge.Start();
        }

        public override void Unload()
        {
            this.Bridge.Connected -= this.OnExtensionConnected;
            this.Bridge.IconsReceived -= this.OnIconsReceived;
            this.Bridge.Changed -= this.OnBridgeChanged;
            this.Bridge.Dispose();
        }

        // The base implementation does nothing but return false: switching is the
        // plugin's job. OnModeChanged stores the new mode and raises ModeChanged,
        // which is what makes the service load that mode's layout.
        public override Boolean SetCurrentMode(String modeName)
        {
            var mode = this.GetMode(modeName);
            if (mode == null)
            {
                return false;
            }

            this.OnModeChanged(mode, null);
            return true;
        }

        private void AddModes()
        {
            foreach (var (name, description) in new[]
            {
                (SketchMode, "A sketch is open"),
                (AssemblyMode, "An Assembly tab is open"),
                (DialogMode, "A feature dialog is open"),
            })
            {
                try
                {
                    this.AddMode(new PluginMode(this.Name, name, name, description, true, null, Array.Empty<PluginMode>()));
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, $"Could not add the {name} mode");
                }
            }

            PluginLog.Info($"Plugin modes: {String.Join(", ", this.GetPluginModeNames() ?? Array.Empty<String>())}");
        }

        // Ask for every icon a button draws, whether cached or not: a cached copy
        // may be older than the Onshape the page is running.
        private void OnExtensionConnected(Object sender, EventArgs e)
        {
            // A reloaded page has forgotten what it was asked for.
            this._requestedIcons.Clear();

            var names = new JsonArray(OnshapeCommand.AllIcons.OrderBy(n => n).Select(n => (JsonNode)n).ToArray());
            this.Bridge.Post(new JsonObject { ["type"] = "getIcons", ["names"] = names });
        }

        private readonly System.Collections.Generic.HashSet<String> _requestedIcons =
            new System.Collections.Generic.HashSet<String>();

        // For icons only known once the page shows them, like a dialog's direction
        // arrow. Asked for once each; the page keeps looking until it has them.
        internal void RequestIcons(params String[] names)
        {
            var fresh = names.Where(n => n != null && this._requestedIcons.Add(n)).ToArray();
            if (fresh.Length > 0)
            {
                this.Bridge.Post(new JsonObject
                {
                    ["type"] = "getIcons",
                    ["names"] = new JsonArray(fresh.Select(n => (JsonNode)n).ToArray()),
                });
            }
        }

        private void OnIconsReceived(Object sender, System.Collections.Generic.IReadOnlyDictionary<String, String> icons)
        {
            this.Icons.Save(icons);

            var missing = OnshapeCommand.AllIcons.Where(n => !this.Icons.Has(n)).OrderBy(n => n).ToArray();
            if (missing.Length > 0)
            {
                PluginLog.Info($"Icons the page does not have: {String.Join(", ", missing)}");
            }
        }

        private void OnBridgeChanged(Object sender, EventArgs e)
        {
            var state = this.Bridge.State;
            // A feature dialog takes over the keys for as long as it is open, in a
            // Part Studio and an Assembly alike; a sketch keeps its own layout.
            var mode = !state.IsSketch && state.HasForm ? DialogMode : state.Context switch
            {
                OnshapeState.Sketch => SketchMode,
                OnshapeState.Assembly => AssemblyMode,
                OnshapeState.PartStudio => MainMode,

                // Keep the layout as it is while nothing better is known: a page that
                // is still loading should not flick the console back to the start.
                _ => this._mode,
            };

            if (mode == this._mode)
            {
                return;
            }

            this._mode = mode;
            try
            {
                var switched = this.SetCurrentMode(mode);
                PluginLog.Info($"Switched to the {mode} mode ({(switched ? "ok" : "refused")})");
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, $"Could not switch to the {mode} mode");
            }
        }
    }
}
