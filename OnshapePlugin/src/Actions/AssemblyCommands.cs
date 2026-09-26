// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    // The Assembly toolbar. Every mate button carries the command-id "mate" and
    // every relation "mateRelation"; only the icon tells them apart, so those are
    // addressed as "mate#revolute" and the like.

    public class AssemblyCommand : OnshapeCommand
    {
        private static readonly string[] AssemblyOnly = { OnshapeState.Assembly };

        public AssemblyCommand() : base("Assembly")
        {
            this.Tool("insertPartOrAssembly", "Insert", "assembly-insert-button", "i", AssemblyOnly);

            this.Tool("mate#fastened", "Fastened Mate", "fastened", "m", AssemblyOnly, "Fastened", "fastened");
            this.Tool("mate#revolute", "Revolute Mate", "revolute", null, AssemblyOnly, "Revolute", "revolute");
            this.Tool("mate#slider", "Slider Mate", "slider", null, AssemblyOnly, "Slider", "slider");
            this.Tool("mate#planar", "Planar Mate", "planar", null, AssemblyOnly, "Planar", "planar");
            this.Tool("mate#cylindrical", "Cylindrical Mate", "cylindrical", null, AssemblyOnly, "Cylindrical", "cylindrical");
            this.Tool("mate#pin_slot", "Pin Slot Mate", "pin_slot", null, AssemblyOnly, "Pin Slot", "pin_slot");
            this.Tool("mate#ball", "Ball Mate", "ball", null, AssemblyOnly, "Ball", "ball");
            this.Tool("mate#parallel", "Parallel Mate", "parallel", null, AssemblyOnly, "Parallel", "parallel");
            this.Tool("geometryMate", "Tangent Mate", "tangent", null, AssemblyOnly, "Tangent");
            this.Tool("widthMate", "Width Mate", "width", null, AssemblyOnly, "Width");
            this.Tool("mateGroup", "Group", "mateGroup", null, AssemblyOnly);

            this.Tool("mateRelation#gear", "Gear Relation", "gear", null, AssemblyOnly, "Gear", "gear");
            this.Tool("mateRelation#rack_and_pinion", "Rack and Pinion Relation", "rack_and_pinion", null, AssemblyOnly, "Rack & Pinion", "rack_and_pinion");
            this.Tool("mateRelation#screw", "Screw Relation", "screw", null, AssemblyOnly, "Screw", "screw");
            this.Tool("mateRelation#linear", "Linear Relation", "linear", null, AssemblyOnly, "Linear Rel.", "linear_relation");

            this.Tool("TOGGLE_ASSEMBLY_SNAP_MODE", "Snap Mode", "SnapMate", "shift+s", AssemblyOnly, "Snap");
            this.Tool("TOGGLE_SHOW_MATES_ON_SELECTION_MODE", "Show Mates Mode", "HoverMate", "h", AssemblyOnly, "Show Mates");

            this.Tool("replicate", "Replicate", "AssemblyReplicate", null, AssemblyOnly);
            this.Tool("replaceAssemblyPart", "Replace Instance", "replace-part", null, AssemblyOnly, "Replace");
            this.Tool("assemblyLinearPattern", "Assembly Linear Pattern", "linear-pattern-button", null, AssemblyOnly, "Lin. Pattern");
            this.Tool("assemblyCircularPattern", "Assembly Circular Pattern", "circular-pattern-button", null, AssemblyOnly, "Circ. Pattern");
            this.Tool("assemblyMirror", "Assembly Mirror", "mirror-button", null, AssemblyOnly, "Mirror");
            this.Tool("assemblyDisplayStates", "Display States", "display-states-button", null, AssemblyOnly, "Display St.");
            this.Tool("createInContextPartStudio", "Create Part Studio in Context", "create-partstudio-incontext", null, AssemblyOnly, "In Context");
            this.Tool("updateAllExternalReferences", "Update All References", "linked-document-update", null, AssemblyOnly, "Update Refs");
        }
    }
}
