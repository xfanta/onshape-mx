// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;

    // Shared base for every button this plugin offers.
    //
    // A button is one of three things:
    //
    //   * a toolbar tool, named by the command-id Onshape gives its toolbar button
    //     ("extrude", "CIRCLE_CENTER_RADIUS"). The extension clicks that button,
    //     opening its dropdown first if the tool lives in one, so tools without a
    //     keyboard shortcut work exactly like the ones with one;
    //   * a keyboard shortcut Onshape handles in the page (views, show/hide),
    //     replayed inside the page by the extension;
    //   * a real keystroke (Enter, Escape, arrows), for keys that behave the same
    //     on every layout.
    //
    // Tools and page shortcuts fall back to a real keystroke when the extension is
    // not running, if Onshape has a shortcut for them at all.

    public abstract class OnshapeCommand : PluginDynamicCommand
    {
        protected static readonly String[] PartStudioOnly = { OnshapeState.PartStudio };
        protected static readonly String[] SketchOnly = { OnshapeState.Sketch };
        protected static readonly String[] Modelling = { OnshapeState.PartStudio, OnshapeState.Sketch };

        // Every icon any button draws, so the plugin can fetch them in one go.
        internal static readonly HashSet<String> AllIcons = new HashSet<String>();

        protected sealed class Item
        {
            // The action parameter; must be unique within the command.
            public String Parameter { get; set; }

            public String DisplayName { get; set; }

            // Drawn on the key when DisplayName is too wide for it. The full name
            // is still what the action picker shows.
            public String ShortLabel { get; set; }

            // Sprite name of Onshape's icon, without the "svg-icon-" prefix.
            public String Icon { get; set; }

            // The toolbar button's command-id, or null for a shortcut-only item.
            public String CommandId { get; set; }

            public Shortcut Shortcut { get; set; }

            // Send Shortcut as a real keystroke even when the extension is there.
            public Boolean AlwaysRealKey { get; set; }

            // Contexts in which the item does anything; null means everywhere.
            public String[] Contexts { get; set; }

            // Overrides the default "is this tool running" test.
            public Func<OnshapeState, Boolean> IsActive { get; set; }
        }

        private readonly Dictionary<String, Item> _items = new Dictionary<String, Item>();
        private readonly String _groupName;

        protected OnshapeCommand(String groupName) => this._groupName = groupName;

        protected OnshapePlugin OnshapePlugin => this.Plugin as OnshapePlugin;

        protected OnshapeState State => this.OnshapePlugin?.Bridge.State ?? new OnshapeState();

        // Without the extension there is no state to draw from, and guessing would
        // be worse than saying nothing: shortcuts still work, so buttons must not
        // be greyed out or wrongly highlighted just because it is missing.
        protected Boolean IsStateKnown => this.OnshapePlugin?.Bridge.IsConnected == true;

        // A toolbar tool.
        protected void Tool(String commandId, String name, String icon, String shortcut = null,
                            String[] contexts = null, String shortLabel = null, String parameter = null) =>
            this.Add(new Item
            {
                Parameter = parameter ?? commandId,
                DisplayName = name,
                ShortLabel = shortLabel,
                Icon = icon ?? IconFor(commandId),
                CommandId = commandId,
                Shortcut = shortcut == null ? null : Shortcut.Parse(shortcut),
                Contexts = contexts,
            });

        // Part Studio command-ids are camelCase and their sprite names are the same
        // words in kebab case: "booleanBodies" draws "svg-icon-boolean-bodies-button".
        // Sketch tools do not follow that and name their icons explicitly.
        private static String IconFor(String commandId) =>
            System.Text.RegularExpressions.Regex.Replace(commandId, "(?<=[a-z0-9])([A-Z])", "-$1").ToLowerInvariant() + "-button";

        // A keyboard shortcut with no toolbar button behind it.
        protected void Key(String parameter, String name, String shortcut, String icon = null,
                           String[] contexts = null, String shortLabel = null, Boolean realKey = false) =>
            this.Add(new Item
            {
                Parameter = parameter,
                DisplayName = name,
                ShortLabel = shortLabel,
                Icon = icon,
                Shortcut = Shortcut.Parse(shortcut),
                AlwaysRealKey = realKey,
                Contexts = contexts,
            });

        protected void Add(Item item)
        {
            this._items[item.Parameter] = item;
            this.AddParameter(item.Parameter, item.DisplayName, this._groupName);

            if (item.Icon != null)
            {
                AllIcons.Add(item.Icon);
            }
        }

        internal IEnumerable<String> Parameters => this._items.Keys;

        protected override Boolean OnLoad()
        {
            var plugin = this.OnshapePlugin;
            if (plugin != null)
            {
                plugin.Bridge.Changed += this.OnRedraw;
                plugin.Icons.Changed += this.OnRedraw;
            }

            return true;
        }

        protected override Boolean OnUnload()
        {
            var plugin = this.OnshapePlugin;
            if (plugin != null)
            {
                plugin.Bridge.Changed -= this.OnRedraw;
                plugin.Icons.Changed -= this.OnRedraw;
            }

            return true;
        }

        private void OnRedraw(Object sender, EventArgs e) => this.ActionImageChanged(null);

        protected override void RunCommand(String actionParameter)
        {
            if (actionParameter == null || !this._items.TryGetValue(actionParameter, out var item))
            {
                return;
            }

            var bridge = this.OnshapePlugin?.Bridge;
            var result = BridgeResult.Unreachable;

            if (!item.AlwaysRealKey && bridge != null)
            {
                if (item.CommandId != null)
                {
                    result = bridge.Invoke("run", new JsonObject { ["command"] = item.CommandId });
                }
                else if (item.Shortcut != null)
                {
                    result = bridge.Invoke("key", item.Shortcut.ToJson());
                }
            }

            // Only stand in for an extension that is not there. If the page heard the
            // request and refused it - a tool that is greyed out, usually - the key
            // would do whatever it happens to mean in that context instead.
            if (result == BridgeResult.Unreachable && item.Shortcut != null)
            {
                this.Plugin.ClientApplication.SendKeyboardShortcut(item.Shortcut.VirtualKey, item.Shortcut.Modifiers);
            }

            this.ActionImageChanged(null);
        }

        // Logi Options+ draws this under the icon itself, so the images stay text-free.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            actionParameter != null && this._items.TryGetValue(actionParameter, out var item)
                ? item.ShortLabel ?? item.DisplayName
                : null;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null || !this._items.TryGetValue(actionParameter, out var item) || item.Icon == null)
            {
                return null;
            }

            var plugin = this.OnshapePlugin;
            if (plugin == null)
            {
                return null;
            }

            var state = this.State;
            var known = this.IsStateKnown;
            var active = known && (item.IsActive?.Invoke(state) ?? (item.CommandId != null && state.IsActive(item.CommandId)));
            var available = !known
                            || ((item.Contexts == null || item.Contexts.Contains(state.Context))
                                && (item.CommandId == null || !state.IsDisabled(item.CommandId)));

            var tone = !available ? IconTone.Disabled : active || !known ? IconTone.Active : IconTone.Idle;

            try
            {
                using (var builder = new BitmapBuilder(imageSize))
                {
                    // Transparent: the key background and the label belong to Options+.
                    builder.Clear(BitmapColor.Transparent);

                    // This image *is* the key's icon element, and where that element
                    // sits comes from DefaultIconTemplate.ict - so the pictogram is
                    // simply centred here.
                    var side = (Int32)(Math.Min(builder.Width, builder.Height) * 0.74);
                    var icon = plugin.IconRenderer.Render(item.Icon, tone, side);
                    if (icon == null)
                    {
                        // Not fetched yet: let Options+ fall back to the name.
                        return null;
                    }

                    // An SVG-backed BitmapImage reports its size as -1x-1, and the
                    // three-argument DrawImage throws on one. The width and height
                    // here are what actually rasterises the vector.
                    builder.DrawImage(icon, (builder.Width - side) / 2, (builder.Height - side) / 2, side, side);
                    return builder.ToImage();
                }
            }
            catch (Exception ex)
            {
                // The service swallows anything thrown here and silently falls back
                // to drawing the action name, which is a miserable thing to debug.
                PluginLog.Error(ex, $"Could not draw '{item.Icon}' for {actionParameter}");
                return null;
            }
        }
    }
}
