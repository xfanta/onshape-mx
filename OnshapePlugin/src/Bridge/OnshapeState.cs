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

            return state;
        }

        public Boolean SameAs(OnshapeState other) =>
            other != null
            && this.Context == other.Context
            && this.HasDialog == other.HasDialog
            && this.Active.SetEquals(other.Active)
            && this.Disabled.SetEquals(other.Disabled);

        private static IReadOnlyCollection<String> ReadSet(JsonElement element, String name) =>
            element.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
                ? new HashSet<String>(array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()))
                : new HashSet<String>();
    }

    internal static class SetExtensions
    {
        public static Boolean SetEquals(this IReadOnlyCollection<String> a, IReadOnlyCollection<String> b) =>
            a.Count == b.Count && a.All(b.Contains);
    }
}
