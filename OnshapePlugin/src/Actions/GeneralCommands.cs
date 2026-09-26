// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System.Text.Json.Nodes;

    // Undo, redo and finishing a dialog - things every context needs.

    public class EditCommand : OnshapeCommand
    {
        public EditCommand() : base("Edit")
        {
            this.Tool("UNDO_A_CHANGE", "Undo", "undo-button", "cmd+z", shortLabel: null, parameter: "undo");
            this.Tool("REDO_A_CHANGE", "Redo", "redo-button", "cmd+shift+z", shortLabel: null, parameter: "redo");

            // The extension presses the dialog's own green check and red cross, which
            // needs no keyboard focus. Enter and Escape stand in without it.
            this.Add(new Item
            {
                Parameter = "accept",
                DisplayName = "Accept",
                Icon = "ok-button",
                Request = new JsonObject { ["type"] = "dialog", ["action"] = "ok" },
                Shortcut = Shortcut.Parse("enter"),
                IsActive = s => s.HasDialog,
            });

            // Out of the running tool first, then out of the dialog - but never out
            // of a sketch, whose red cross discards it.
            this.Add(new Item
            {
                Parameter = "cancel",
                DisplayName = "Cancel / Exit Tool",
                ShortLabel = "Cancel",
                Icon = "cancel-button",
                Request = new JsonObject { ["type"] = "dialog", ["action"] = "cancel" },
                Shortcut = Shortcut.Parse("escape"),
                IsActive = s => s.HasDialog,
            });

            this.Key("deselect", "Deselect All", "space", shortLabel: "Deselect");
            this.Key("delete", "Delete", "delete", realKey: true);
        }
    }

    // Onshape's view shortcuts, replayed inside the page.

    public class ViewCommand : OnshapeCommand
    {
        public ViewCommand() : base("View")
        {
            this.Key("front", "Front View", "shift+1", "mx-view-front", shortLabel: "Front");
            this.Key("back", "Back View", "shift+2", "mx-view-back", shortLabel: "Back");
            this.Key("left", "Left View", "shift+3", "mx-view-left", shortLabel: "Left");
            this.Key("right", "Right View", "shift+4", "mx-view-right", shortLabel: "Right");
            this.Key("top", "Top View", "shift+5", "mx-view-top", shortLabel: "Top");
            this.Key("bottom", "Bottom View", "shift+6", "mx-view-bottom", shortLabel: "Bottom");
            this.Key("iso", "Isometric View", "shift+7", "mx-view-iso", shortLabel: "Iso");
            this.Key("normal", "Normal To", "n", "mx-view-normal", shortLabel: "Normal To");
            this.Key("fit", "Zoom to Fit", "f", "lui-zoom-to-fit", shortLabel: "Fit");
            this.Key("zoom_selection", "Zoom to Selection", "w", "lui-zoom-window", shortLabel: "Zoom Sel.");
            this.Key("section", "Section View", "shift+x", "lui-section-view", shortLabel: "Section");
            this.Key("transparent", "Make Transparent", "shift+t", "make-transparent", shortLabel: "Transparent");
            this.Key("hide", "Hide Selected", "y", shortLabel: "Hide");
            this.Key("show_hidden", "Show Hidden", "shift+y", shortLabel: "Show All");
            this.Key("isolate", "Isolate Selected", "shift+i", "isolate-button", shortLabel: "Isolate");
            this.Key("sketches", "Show/Hide Sketches", "shift+h", shortLabel: "Sketches");
            this.Key("planes", "Show/Hide Planes", "p", "onshape-default-planes", shortLabel: "Planes");
            this.Key("named_views", "Named Views", "shift+v", "named-view-button", shortLabel: "Named");
            this.Key("measure", "Measure", "[", "lui-measure", shortLabel: "Measure");
            this.Key("search", "Search Tools", "alt+c", "search-button", shortLabel: "Search");
            this.Key("shortcut_toolbar", "Shortcut Toolbar", "s", shortLabel: "Shortcuts");
        }
    }
}
