// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;

    // Keys that take on the options of whatever feature dialog is open.
    //
    // Slot 1 is the first option of the dialog, slot 2 the second and so on, read
    // off the dialog by the extension: Extrude gives Flip, Solid, New, Blind,
    // Direction, Starting offset, Symmetric, Draft, Second end position. Pressing a
    // slot moves its option on - the next tab, the next dropdown entry, the other
    // state of a checkbox or of the direction arrow. The direction arrow always
    // comes first, since it is what a dialog is most often opened again for.
    //
    // Numbers are not slots; the Dialpad dial steps those (DialogValueAdjustment).

    public class DialogCommand : PluginDynamicCommand
    {
        public const Int32 SlotCount = 16;

        private const String CycleIcon = "mx-cycle";
        private const String CheckOnIcon = "mx-check-on";
        private const String CheckOffIcon = "mx-check-off";

        public DialogCommand()
        {
            for (var i = 1; i <= SlotCount; i++)
            {
                this.AddParameter(Slot(i), $"Dialog Option {i}", "Dialog");
            }
        }

        public static String Slot(Int32 number) => $"slot{number}";

        protected OnshapePlugin OnshapePlugin => this.Plugin as OnshapePlugin;

        // The dialog's options in slot order: direction arrows first, then the rest
        // as the dialog lists them.
        internal static IReadOnlyList<DialogParam> Options(OnshapeState state) =>
            state.Form == null
                ? Array.Empty<DialogParam>()
                : state.Form.Params.Where(p => p.Kind == "flip")
                       .Concat(state.Form.Params.Where(p => p.Kind != "flip" && !p.IsNumber))
                       .ToArray();

        private DialogParam ParamFor(String actionParameter)
        {
            var bridge = this.OnshapePlugin?.Bridge;
            if (bridge == null || !bridge.IsConnected || actionParameter == null
                || !actionParameter.StartsWith("slot", StringComparison.Ordinal)
                || !Int32.TryParse(actionParameter.Substring(4), out var number))
            {
                return null;
            }

            var options = Options(bridge.State);
            return number >= 1 && number <= options.Count ? options[number - 1] : null;
        }

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
            var param = this.ParamFor(actionParameter);
            if (param != null)
            {
                this.OnshapePlugin.Bridge.Invoke("param", new JsonObject { ["id"] = param.Id, ["op"] = "next" });
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var param = this.ParamFor(actionParameter);
            if (param == null)
            {
                return "";
            }

            return param.Kind == "flip" ? (param.Label.Length > 0 ? param.Label : "Flip") : param.Label;
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = this.OnshapePlugin;
            var param = this.ParamFor(actionParameter);

            var (icon, tone) = param?.Kind switch
            {
                "flip" => (param.Icon, IconTone.Active),
                "check" => param.IsOn ? (CheckOnIcon, IconTone.Active) : (CheckOffIcon, IconTone.Idle),
                "tabs" or "select" => (CycleIcon, IconTone.Active),
                _ => (null, IconTone.Idle),
            };

            try
            {
                using (var builder = new BitmapBuilder(imageSize))
                {
                    // An empty slot is a blank key, not the action's name.
                    builder.Clear(BitmapColor.Transparent);

                    if (icon != null && plugin != null)
                    {
                        // The arrow's icon only turns up when a dialog shows it.
                        if (!plugin.Icons.Has(icon))
                        {
                            plugin.RequestIcons(icon);
                        }

                        var side = (Int32)(Math.Min(builder.Width, builder.Height) * 0.74);
                        var image = plugin.IconRenderer.Render(icon, tone, side);
                        if (image != null)
                        {
                            builder.DrawImage(image, (builder.Width - side) / 2, (builder.Height - side) / 2, side, side);
                        }
                    }

                    return builder.ToImage();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Could not draw dialog {actionParameter}");
                return null;
            }
        }
    }
}
