// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    // The Part Studio toolbar, including everything tucked into its dropdowns.
    // Only a handful of these have a keyboard shortcut; the rest work only with
    // the extension running, which clicks them in the toolbar.

    public class FeatureCommand : OnshapeCommand
    {
        public FeatureCommand() : base("Part Studio · Create")
        {
            this.Tool("newSketch", "Sketch", null, "shift+s", PartStudioOnly);

            // Extrude and revolve stay on the toolbar while a sketch is open.
            this.Tool("extrude", "Extrude", null, "shift+e", Modelling);
            this.Tool("revolve", "Revolve", null, "shift+w", Modelling);
            this.Tool("sweep", "Sweep", null, null, PartStudioOnly);
            this.Tool("loft", "Loft", null, null, PartStudioOnly);
            this.Tool("thicken", "Thicken", null, null, PartStudioOnly);
            this.Tool("enclose", "Enclose", null, null, PartStudioOnly);
            this.Tool("rib", "Rib", null, null, PartStudioOnly);
            this.Tool("hole", "Hole", null, null, PartStudioOnly);
            this.Tool("externalThread", "External Thread", null, null, PartStudioOnly, "Thread");
            this.Tool("cPlane", "Plane", null, null, PartStudioOnly);
            this.Tool("mateConnector", "Mate Connector", null, "ctrl+m", PartStudioOnly, "Mate Conn.");
        }
    }

    public class ModifyCommand : OnshapeCommand
    {
        public ModifyCommand() : base("Part Studio · Modify")
        {
            this.Tool("fillet", "Fillet", null, "shift+f", PartStudioOnly);
            this.Tool("faceBlend", "Face Blend", null, null, PartStudioOnly);
            this.Tool("chamfer", "Chamfer", null, null, PartStudioOnly);
            this.Tool("draft", "Draft", null, null, PartStudioOnly);
            this.Tool("bodyDraft", "Body Draft", null, null, PartStudioOnly);
            this.Tool("shell", "Shell", null, null, PartStudioOnly);
            this.Tool("modifyFillet", "Modify Fillet", null, null, PartStudioOnly, "Mod. Fillet");
            this.Tool("deleteFace", "Delete Face", null, null, PartStudioOnly, "Del. Face");
            this.Tool("moveFace", "Move Face", null, null, PartStudioOnly);
            this.Tool("replaceFace", "Replace Face", null, null, PartStudioOnly, "Repl. Face");
            this.Tool("booleanBodies", "Boolean", null, null, PartStudioOnly);
            this.Tool("splitPart", "Split", null, null, PartStudioOnly);
            this.Tool("deleteBodies", "Delete Part", null, null, PartStudioOnly, "Del. Part");
        }
    }

    public class PatternCommand : OnshapeCommand
    {
        public PatternCommand() : base("Part Studio · Pattern & Transform")
        {
            this.Tool("linearPattern", "Linear Pattern", null, null, PartStudioOnly, "Lin. Pattern");
            this.Tool("circularPattern", "Circular Pattern", null, null, PartStudioOnly, "Circ. Pattern");
            this.Tool("curvePattern", "Curve Pattern", null, null, PartStudioOnly, "Crv. Pattern");
            this.Tool("mirror", "Mirror", null, null, PartStudioOnly);
            this.Tool("transform", "Transform", null, null, PartStudioOnly);
            this.Tool("wrap", "Wrap", null, null, PartStudioOnly);
            this.Tool("decal", "Decal", null, null, PartStudioOnly);
        }
    }

    public class SurfaceCommand : OnshapeCommand
    {
        public SurfaceCommand() : base("Part Studio · Surfaces")
        {
            this.Tool("offsetSurface", "Offset Surface", null, null, PartStudioOnly, "Offset Srf");
            this.Tool("boundarySurface", "Boundary Surface", null, null, PartStudioOnly, "Boundary");
            this.Tool("fill", "Fill", null, null, PartStudioOnly);
            this.Tool("extendSurface", "Move Boundary", null, null, PartStudioOnly, "Move Bound.");
            this.Tool("ruledSurface", "Ruled Surface", null, null, PartStudioOnly, "Ruled");
            this.Tool("mutualTrim", "Mutual Trim", null, null, PartStudioOnly);
            this.Tool("constrainedSurface", "Constrained Surface", null, null, PartStudioOnly, "Constrained");
        }
    }

    public class CurveCommand : OnshapeCommand
    {
        public CurveCommand() : base("Part Studio · Curves")
        {
            this.Tool("helix", "Helix", null, null, PartStudioOnly);
            this.Tool("spline3D", "3D Spline", "spline-3d-button", null, PartStudioOnly);
            this.Tool("fitSpline", "3D Fit Spline", null, null, PartStudioOnly, "Fit Spline");
            this.Tool("projectCurves", "Projected Curve", null, null, PartStudioOnly, "Projected");
            this.Tool("bridgingCurve", "Bridging Curve", null, null, PartStudioOnly, "Bridging");
            this.Tool("compositeCurve", "Composite Curve", null, null, PartStudioOnly, "Composite");
            this.Tool("intersectionCurve", "Intersection Curve", null, null, PartStudioOnly, "Intersect.");
            this.Tool("trimCurve", "Trim Curve", null, null, PartStudioOnly);
            this.Tool("isocline", "Isocline", null, null, PartStudioOnly);
            this.Tool("offsetCurveOnFace", "Offset Curve", null, null, PartStudioOnly, "Offset Crv");
            this.Tool("isoparametricCurve", "Isoparametric Curve", null, null, PartStudioOnly, "Isoparam.");
            this.Tool("editCurve", "Edit Curve", null, null, PartStudioOnly);
            this.Tool("routingCurve", "Routing Curve", null, null, PartStudioOnly, "Routing");
        }
    }

    public class SheetMetalCommand : OnshapeCommand
    {
        public SheetMetalCommand() : base("Part Studio · Sheet Metal")
        {
            this.Tool("sheetMetalStart", "Sheet Metal Model", null, null, PartStudioOnly, "SM Model");
            this.Tool("sheetMetalFlange", "Flange", null, null, PartStudioOnly);
            this.Tool("sheetMetalHem", "Hem", null, null, PartStudioOnly);
            this.Tool("sheetMetalTab", "Tab", null, null, PartStudioOnly);
            this.Tool("sheetMetalBend", "Bend", null, null, PartStudioOnly);
            this.Tool("sheetMetalJog", "Jog", null, null, PartStudioOnly);
            this.Tool("sheetMetalFormed", "Form", null, null, PartStudioOnly);
            this.Tool("sheetMetalLoft", "Sheet Metal Loft", null, null, PartStudioOnly, "SM Loft");
            this.Tool("sheetMetalMakeJoint", "Make Joint", null, null, PartStudioOnly);
            this.Tool("sheetMetalCorner", "Corner", null, null, PartStudioOnly);
            this.Tool("sheetMetalBendRelief", "Bend Relief", null, null, PartStudioOnly);
            this.Tool("sheetMetalCornerBreak", "Corner Break", null, null, PartStudioOnly);
            this.Tool("sheetMetalEnd", "Finish Sheet Metal Model", null, null, PartStudioOnly, "Finish SM");
        }
    }

    public class FrameCommand : OnshapeCommand
    {
        public FrameCommand() : base("Part Studio · Frames")
        {
            this.Tool("frame", "Frame", null, null, PartStudioOnly);
            this.Tool("frameTrim", "Frame Trim", null, null, PartStudioOnly);
            this.Tool("gusset", "Gusset", null, null, PartStudioOnly);
            this.Tool("endcap", "End Cap", null, null, PartStudioOnly);
            this.Tool("cutlist", "Cut List", null, null, PartStudioOnly);
        }
    }

    public class UtilityCommand : OnshapeCommand
    {
        public UtilityCommand() : base("Part Studio · Utilities")
        {
            this.Tool("assignVariable", "Variable", null, null, PartStudioOnly);
            this.Tool("queryVariable", "Query Variable", null, null, PartStudioOnly, "Query Var.");
            this.Tool("importDerived", "Derived", null, null, PartStudioOnly);
            this.Tool("compositePart", "Composite Part", null, null, PartStudioOnly, "Composite");
            this.Tool("tag", "Tag", null, null, PartStudioOnly);
        }
    }
}
