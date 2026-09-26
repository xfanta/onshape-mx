// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Linq;
    using System.Text.Json.Nodes;

    // A dial for the numbers in the open feature dialog: Extrude's depth, Fillet's
    // radius. Each detent is one unit of what the field shows (1 mm, 1 deg), and
    // the preview follows as the dial turns. Pressing the dial moves on to the
    // dialog's next number.

    public class DialogValueAdjustment : PluginDynamicAdjustment
    {
        private Int32 _index;
        private String _formTitle;

        public DialogValueAdjustment()
            : base("Dialog Value", "Change the open dialog's value - depth, radius, angle; press for the next one", "Dialog", hasReset: true)
        {
        }

        protected OnshapePlugin OnshapePlugin => this.Plugin as OnshapePlugin;

        private DialogParam Current()
        {
            var bridge = this.OnshapePlugin?.Bridge;
            var form = bridge != null && bridge.IsConnected ? bridge.State.Form : null;
            var numbers = form?.Params.Where(p => p.IsNumber).ToArray();
            if (numbers == null || numbers.Length == 0)
            {
                return null;
            }

            // A different dialog starts again from its first number.
            if (form.Title != this._formTitle)
            {
                this._formTitle = form.Title;
                this._index = 0;
            }

            return numbers[Math.Min(this._index, numbers.Length - 1)];
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var param = this.Current();
            if (param != null)
            {
                this.OnshapePlugin.Bridge.Invoke("param", new JsonObject { ["param"] = param.Id, ["op"] = "step", ["delta"] = diff });
            }
        }

        // Press: the next number in the dialog.
        protected override void RunCommand(String actionParameter)
        {
            var form = this.OnshapePlugin?.Bridge.State.Form;
            var count = form?.Params.Count(p => p.IsNumber) ?? 0;
            this._index = count == 0 ? 0 : (this._index + 1) % count;
            this.AdjustmentValueChanged();
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var param = this.Current();
            return param == null ? null : $"{param.Label} {param.Value}".Trim();
        }

        protected override Boolean OnLoad()
        {
            var bridge = this.OnshapePlugin?.Bridge;
            if (bridge != null)
            {
                bridge.Changed += this.OnChanged;
            }

            return true;
        }

        protected override Boolean OnUnload()
        {
            var bridge = this.OnshapePlugin?.Bridge;
            if (bridge != null)
            {
                bridge.Changed -= this.OnChanged;
            }

            return true;
        }

        private void OnChanged(Object sender, EventArgs e) => this.AdjustmentValueChanged();
    }
}
