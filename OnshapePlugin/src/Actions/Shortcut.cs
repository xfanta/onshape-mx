// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Text.Json.Nodes;

    // An Onshape keyboard shortcut, written the way Onshape's own tooltips write
    // them: "shift+e", "shift+1", "cmd+z", "escape".
    //
    // It is sent two ways. With the extension running it becomes a KeyboardEvent
    // dispatched inside the page, spelled as a US keyboard would spell it - so
    // Shift+1 arrives as key "!" and code "Digit1" whatever the system layout.
    // That matters on a Czech QWERTZ keyboard, where Shift+1 types "1" and a real
    // keystroke would never reach Onshape's handler. Without the extension it
    // falls back to a real keystroke.

    public sealed class Shortcut
    {
        private const String ShiftedDigits = ")!@#$%^&*(";

        public String Key { get; private set; }

        public String Code { get; private set; }

        public Int32 KeyCode { get; private set; }

        public Boolean Shift { get; private set; }

        public Boolean Control { get; private set; }

        public Boolean Alt { get; private set; }

        public Boolean Meta { get; private set; }

        public VirtualKeyCode VirtualKey { get; private set; }

        public ModifierKey Modifiers { get; private set; } = ModifierKey.None;

        public static Shortcut Parse(String text)
        {
            var shortcut = new Shortcut();
            var parts = text.ToLowerInvariant().Split('+');

            for (var i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i])
                {
                    case "shift":
                        shortcut.Shift = true;
                        shortcut.Modifiers |= ModifierKey.Shift;
                        break;
                    case "ctrl":
                        shortcut.Control = true;
                        shortcut.Modifiers |= ModifierKey.Control;
                        break;
                    case "alt":
                        shortcut.Alt = true;
                        shortcut.Modifiers |= ModifierKey.Alt;
                        break;
                    case "cmd":
                        shortcut.Meta = true;
                        shortcut.Modifiers |= ModifierKey.Command;
                        break;
                    default:
                        throw new ArgumentException($"Unknown modifier in '{text}'");
                }
            }

            shortcut.SetKey(parts[parts.Length - 1], text);
            return shortcut;
        }

        private void SetKey(String key, String text)
        {
            if (key.Length == 1 && key[0] >= 'a' && key[0] <= 'z')
            {
                this.Key = this.Shift ? key.ToUpperInvariant() : key;
                this.Code = "Key" + key.ToUpperInvariant();
                this.KeyCode = Char.ToUpperInvariant(key[0]);
                this.VirtualKey = Named("Key" + key.ToUpperInvariant());
                return;
            }

            if (key.Length == 1 && key[0] >= '0' && key[0] <= '9')
            {
                var digit = key[0] - '0';
                this.Key = this.Shift ? ShiftedDigits[digit].ToString() : key;
                this.Code = "Digit" + key;
                this.KeyCode = key[0];
                this.VirtualKey = Named("Key" + key);
                return;
            }

            (this.Key, this.Code, this.KeyCode, this.VirtualKey) = key switch
            {
                "enter" => ("Enter", "Enter", 13, VirtualKeyCode.Return),
                "escape" => ("Escape", "Escape", 27, VirtualKeyCode.Escape),
                "space" => (" ", "Space", 32, VirtualKeyCode.Space),
                "delete" => ("Delete", "Delete", 46, VirtualKeyCode.Delete),
                "left" => ("ArrowLeft", "ArrowLeft", 37, VirtualKeyCode.ArrowLeft),
                "up" => ("ArrowUp", "ArrowUp", 38, VirtualKeyCode.ArrowUp),
                "right" => ("ArrowRight", "ArrowRight", 39, VirtualKeyCode.ArrowRight),
                "down" => ("ArrowDown", "ArrowDown", 40, VirtualKeyCode.ArrowDown),
                "[" => ("[", "BracketLeft", 219, VirtualKeyCode.Oem4),
                "`" => ("`", "Backquote", 192, VirtualKeyCode.Oem3),
                _ => throw new ArgumentException($"Unknown key in '{text}'"),
            };
        }

        // VirtualKeyCode is a class of named constants rather than an enum, so there
        // is no arithmetic from 'a' to KeyA; look the constant up by name instead.
        private static VirtualKeyCode Named(String name)
        {
            var member = (System.Reflection.MemberInfo)typeof(VirtualKeyCode).GetField(name)
                         ?? typeof(VirtualKeyCode).GetProperty(name);
            return member switch
            {
                System.Reflection.FieldInfo f => (VirtualKeyCode)f.GetValue(null),
                System.Reflection.PropertyInfo p => (VirtualKeyCode)p.GetValue(null),
                _ => throw new ArgumentException($"No VirtualKeyCode.{name}"),
            };
        }

        public JsonObject ToJson() => new JsonObject
        {
            ["key"] = this.Key,
            ["code"] = this.Code,
            ["keyCode"] = this.KeyCode,
            ["shift"] = this.Shift,
            ["ctrl"] = this.Control,
            ["alt"] = this.Alt,
            ["meta"] = this.Meta,
        };
    }
}
