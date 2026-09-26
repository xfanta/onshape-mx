// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    // What the Onshape page looks like right now, as reported by the extension.
    //
    // Everything here is read off the toolbar: which tools it holds says whether
    // a sketch is open, and a tool's "is-active" class says whether it is running.

    public sealed class OnshapeState
    {
        public const String Sketch = "sketch";
        public const String PartStudio = "partStudio";
        public const String Assembly = "assembly";
        public const String Drawing = "drawing";

        // "none" when no Onshape tab is open, "other" for a tab without a toolbar.
        public String Context { get; private set; } = "none";

        public IReadOnlyCollection<String> Active { get; private set; } = Array.Empty<String>();

        public IReadOnlyCollection<String> Disabled { get; private set; } = Array.Empty<String>();

        // A feature or sketch dialog is open, so Enter and Escape mean something.
        public Boolean HasDialog { get; private set; }

        public Boolean HasPage => this.Context != "none";

        public Boolean IsSketch => this.Context == Sketch;

        public Boolean IsActive(String commandId) => this.Active.Contains(commandId);

        public Boolean IsDisabled(String commandId) => this.Disabled.Contains(commandId);

        // The open feature dialog's parameters; null when there is none, and in a
        // sketch, which has its own layout.
        public DialogForm Form { get; private set; }

        public Boolean HasForm => this.Form != null && this.Form.Params.Count > 0;

        // The same text as last time means the same form; cheaper than comparing
        // parameter by parameter.
        private String _formJson = "";

        public static OnshapeState Parse(JsonElement element)
        {
            var state = new OnshapeState();

            if (element.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.String)
            {
                state.Context = context.GetString();
            }

            state.Active = ReadSet(element, "active");
            state.Disabled = ReadSet(element, "disabled");
            state.HasDialog = element.TryGetProperty("dialog", out var dialog) && dialog.ValueKind == JsonValueKind.True;

            if (element.TryGetProperty("form", out var form) && form.ValueKind == JsonValueKind.Object)
            {
                state.Form = DialogForm.Parse(form);
                state._formJson = form.GetRawText();
            }

            return state;
        }

        public Boolean SameAs(OnshapeState other) =>
            other != null
            && this.Context == other.Context
            && this.HasDialog == other.HasDialog
            && this._formJson == other._formJson
            && this.Active.SetEquals(other.Active)
            && this.Disabled.SetEquals(other.Disabled);

        private static IReadOnlyCollection<String> ReadSet(JsonElement element, String name) =>
            element.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
                ? new HashSet<String>(array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()))
                : new HashSet<String>();
    }

    // A feature dialog's parameters, as the extension describes them. Kind is one
    // of "tabs", "select", "flip", "check" and "number"; see content.js.
    public sealed class DialogForm
    {
        public String Title { get; private set; } = "";

        public IReadOnlyList<DialogParam> Params { get; private set; } = Array.Empty<DialogParam>();

        public static DialogForm Parse(JsonElement element)
        {
            var form = new DialogForm
            {
                Title = element.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            };

            if (element.TryGetProperty("params", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                form.Params = items.EnumerateArray().Select(DialogParam.Parse).Where(p => p.Id != null).ToArray();
            }

            return form;
        }
    }

    public sealed class DialogParam
    {
        public String Id { get; private set; }

        public String Kind { get; private set; }

        public String Label { get; private set; }

        public String Value { get; private set; }

        public String Icon { get; private set; }

        public Boolean IsNumber => this.Kind == "number";

        public Boolean IsOn => this.Value == "True" || this.Value == "true";

        public static DialogParam Parse(JsonElement element)
        {
            String Text(String name) =>
                element.TryGetProperty(name, out var v)
                    ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()
                    : null;

            return new DialogParam
            {
                Id = Text("id"),
                Kind = Text("kind"),
                Label = Text("label") ?? "",
                Value = Text("value") ?? "",
                Icon = Text("icon"),
            };
        }
    }

    internal static class SetExtensions
    {
        public static Boolean SetEquals(this IReadOnlyCollection<String> a, IReadOnlyCollection<String> b) =>
            a.Count == b.Count && a.All(b.Contains);
    }
}
