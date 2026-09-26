// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;

    // Dialpad dials that turn and zoom the view by repeating Onshape's own view
    // keys, one keypress per detent.

    public abstract class ViewAdjustment : PluginDynamicAdjustment
    {
        private readonly Shortcut _forward;
        private readonly Shortcut _backward;
        private readonly Shortcut _press;

        protected ViewAdjustment(String displayName, String description, String forward, String backward, String press)
            : base(displayName, description, "View", hasReset: press != null)
        {
            this._forward = Shortcut.Parse(forward);
            this._backward = Shortcut.Parse(backward);
            this._press = press == null ? null : Shortcut.Parse(press);
        }

        protected OnshapePlugin OnshapePlugin => this.Plugin as OnshapePlugin;

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var shortcut = diff > 0 ? this._forward : this._backward;
            for (var i = 0; i < Math.Abs(diff); i++)
            {
                this.Send(shortcut);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this._press != null)
            {
                this.Send(this._press);
            }
        }

        protected override String GetAdjustmentValue(String actionParameter) => null;

        // In-page when the extension is there - a real "z" is "y" on a QWERTZ
        // keyboard, and "y" hides the selection in Onshape.
        private void Send(Shortcut shortcut)
        {
            var bridge = this.OnshapePlugin?.Bridge;
            if (bridge != null && bridge.Invoke("key", shortcut.ToJson()) != BridgeResult.Unreachable)
            {
                return;
            }

            this.Plugin.ClientApplication.SendKeyboardShortcut(shortcut.VirtualKey, shortcut.Modifiers);
        }
    }

    public class OrbitAdjustment : ViewAdjustment
    {
        // An arrow key turns the model 15 degrees. Onshape's finer Ctrl+arrow is out:
        // should it ever go out as a real keystroke, macOS takes Ctrl+arrow for
        // switching Spaces.
        public OrbitAdjustment()
            : base("Orbit", "Turn the view left and right; press for isometric", "right", "left", "shift+7")
        {
        }
    }

    public class TiltAdjustment : ViewAdjustment
    {
        public TiltAdjustment()
            : base("Tilt", "Tilt the view up and down; press for isometric", "down", "up", "shift+7")
        {
        }
    }

    public class ZoomAdjustment : ViewAdjustment
    {
        public ZoomAdjustment()
            : base("Zoom", "Zoom in and out; press to fit", "shift+z", "z", "f")
        {
        }
    }
}
