// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    // Undo, redo and finishing a dialog - things every context needs.

    public class EditCommand : OnshapeCommand
    {
        public EditCommand() : base("Edit")
        {
            this.Tool("UNDO_A_CHANGE", "Undo", "undo-button", "cmd+z", shortLabel: null, parameter: "undo");
            this.Tool("REDO_A_CHANGE", "Redo", "redo-button", "cmd+shift+z", shortLabel: null, parameter: "redo");

            // Enter and Escape mean the same on every layout, and Onshape only reacts
            // to them with focus in the page, so they go out as real keystrokes.
            this.Add(new Item
            {
                Parameter = "accept",
                DisplayName = "Accept",
                Icon = "checkmark-button",
                Shortcut = Shortcut.Parse("enter"),
                AlwaysRealKey = true,
                IsActive = s => s.HasDialog,
            });

            this.Add(new Item
            {
                Parameter = "cancel",
                DisplayName = "Cancel / Exit Tool",
                ShortLabel = "Cancel",
                Icon = "close-button",
                Shortcut = Shortcut.Parse("escape"),
                AlwaysRealKey = true,
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
            this.Key("front", "Front View", "shift+1", shortLabel: "Front");
            this.Key("back", "Back View", "shift+2", shortLabel: "Back");
            this.Key("left", "Left View", "shift+3", shortLabel: "Left");
            this.Key("right", "Right View", "shift+4", shortLabel: "Right");
            this.Key("top", "Top View", "shift+5", shortLabel: "Top");
            this.Key("bottom", "Bottom View", "shift+6", shortLabel: "Bottom");
            this.Key("iso", "Isometric View", "shift+7", shortLabel: "Iso");
            this.Key("normal", "Normal To", "n", shortLabel: "Normal To");
            this.Key("fit", "Zoom to Fit", "f", shortLabel: "Fit");
            this.Key("zoom_selection", "Zoom to Selection", "w", shortLabel: "Zoom Sel.");
            this.Key("section", "Section View", "shift+x", shortLabel: "Section");
            this.Key("transparent", "Make Transparent", "shift+t", shortLabel: "Transparent");
            this.Key("hide", "Hide Selected", "y", shortLabel: "Hide");
            this.Key("show_hidden", "Show Hidden", "shift+y", shortLabel: "Show All");
            this.Key("isolate", "Isolate Selected", "shift+i", shortLabel: "Isolate");
            this.Key("sketches", "Show/Hide Sketches", "shift+h", shortLabel: "Sketches");
            this.Key("planes", "Show/Hide Planes", "p", shortLabel: "Planes");
            this.Key("measure", "Measure", "[", shortLabel: "Measure");
            this.Key("search", "Search Tools", "alt+c", shortLabel: "Search");
            this.Key("shortcut_toolbar", "Shortcut Toolbar", "s", shortLabel: "Shortcuts");
        }
    }
}
