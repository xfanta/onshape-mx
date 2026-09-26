// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    // The sketch toolbar: what replaces the Part Studio toolbar while a sketch is
    // open. Command-ids and icon names are as Onshape's own toolbar has them.

    public class SketchDrawCommand : OnshapeCommand
    {
        public SketchDrawCommand() : base("Sketch · Draw")
        {
            this.Tool("LINESEGMENT", "Line", "sketch-line-segment-button", "l", SketchOnly);
            this.Tool("MIDPOINTLINE", "Midpoint Line", "sketch-midpoint-line-button", null, SketchOnly, "Mid Line");
            this.Tool("RECTANGLE_TWO_CORNERS", "Corner Rectangle", "sketch-rectangle-button", "g", SketchOnly, "Rectangle");
            this.Tool("RECTANGLE_CENTER", "Center Point Rectangle", "sketch-center-rectangle-button", "r", SketchOnly, "Ctr Rect");
            this.Tool("RECTANGLE_ALIGNED", "Aligned Rectangle", "sketch-aligned-rectangle-button", null, SketchOnly, "Aligned");
            this.Tool("CIRCLE_CENTER_RADIUS", "Center Point Circle", "sketch-circle-button", "c", SketchOnly, "Circle");
            this.Tool("CIRCLE_PERIMETER", "3 Point Circle", "sketch-perimeter-circle-button", null, SketchOnly, "3pt Circle");
            this.Tool("ELLIPSE", "Ellipse", "sketch-ellipse-button", null, SketchOnly);
            this.Tool("ARC_START_END_RADIUS", "3 Point Arc", "sketch-arc-button", "a", SketchOnly, "Arc");
            this.Tool("ARC_TANGENT", "Tangent Arc", "sketch-tangent-arc-button", null, SketchOnly, "Tan Arc");
            this.Tool("CENTER_ARC", "Center Point Arc", "sketch-center-arc-button", null, SketchOnly, "Ctr Arc");
            this.Tool("ELLIPTICAL_ARC", "Elliptical Arc", "elliptical-arc-button", null, SketchOnly, "Ell. Arc");
            this.Tool("CONIC", "Conic", "sketch-conic-button", null, SketchOnly);
            this.Tool("INSCRIBED_POLYGON", "Inscribed Polygon", "sketch-inscribed-polygon-button", null, SketchOnly, "Polygon");
            this.Tool("CIRCUMSCRIBED_POLYGON", "Circumscribed Polygon", "sketch-circumscribed-polygon-button", null, SketchOnly, "Circ. Poly");
            this.Tool("SLOT", "Slot", "sketch-slot-button", null, SketchOnly);
            this.Tool("SPLINE", "Spline", "sketch-spline-button", null, SketchOnly);
            this.Tool("BEZIER", "Bezier", "sketch-bezier-button", null, SketchOnly);
            this.Tool("ADD_SPLINE_POINT", "Spline Control Point", "add-spline-point-button", null, SketchOnly, "Spl. Point");
            this.Tool("POINT", "Point", "sketch-point-button", "shift+s", SketchOnly);
            this.Tool("TEXT_RECTANGLE_TWO_CORNERS", "Text", "sketch-text-rectangle-button", null, SketchOnly);
            this.Tool("IMAGE_RECTANGLE_TWO_CORNERS", "Insert Image", "sketch-image-rectangle-button", null, SketchOnly, "Image");
            this.Tool("TOGGLE_CONSTRUCTION", "Construction", "sketch-construction-button", "q", SketchOnly);
        }
    }

    public class SketchModifyCommand : OnshapeCommand
    {
        public SketchModifyCommand() : base("Sketch · Modify")
        {
            this.Tool("DIMENSION", "Dimension", "sketch-dimension-button", "d", SketchOnly);
            this.Tool("USE", "Use (Project/Convert)", "sketch-use-button", "u", SketchOnly, "Use");
            this.Tool("INTERSECTION", "Intersection", "sketch-intersection-button", null, SketchOnly);
            this.Tool("FILLET", "Sketch Fillet", "sketch-fillet-button", "shift+f", SketchOnly, "Fillet");
            this.Tool("CHAMFER", "Sketch Chamfer", "sketch-chamfer-button", null, SketchOnly, "Chamfer");
            this.Tool("TRIM", "Trim", "sketch-trim-button", "m", SketchOnly);
            this.Tool("EXTEND", "Extend", "sketch-extend-button", "x", SketchOnly);
            this.Tool("SPLIT", "Split", "sketch-split-button", null, SketchOnly);
            this.Tool("OFFSET", "Offset", "sketch-offset-button", "o", SketchOnly);
            this.Tool("SKETCHMIRROR", "Sketch Mirror", "sketch-mirror-button", null, SketchOnly, "Mirror");
            this.Tool("SKETCHLPATTERN", "Sketch Linear Pattern", "sketch-lpattern-button", null, SketchOnly, "Lin. Pattern");
            this.Tool("SKETCHCPATTERN", "Sketch Circular Pattern", "sketch-cpattern-button", null, SketchOnly, "Circ. Pattern");
            this.Tool("TRANSFORM", "Sketch Transform", "sketch-transform-button", null, SketchOnly, "Transform");
            this.Tool("INSERT_DXFDWG", "Insert DXF or DWG", "150715_Sketch_Icons_30_InsertDXFDWG", null, SketchOnly, "DXF/DWG");
        }
    }

    public class SketchConstraintCommand : OnshapeCommand
    {
        public SketchConstraintCommand() : base("Sketch · Constraints")
        {
            this.Tool("COINCIDENT", "Coincident", "sketch-coincident-button", "i", SketchOnly);
            this.Tool("CONCENTRIC", "Concentric", "sketch-concentric-button", "shift+o", SketchOnly);
            this.Tool("PARALLEL", "Parallel", "sketch-parallel-button", "b", SketchOnly);
            this.Tool("TANGENT", "Tangent", "sketch-tangent-button", "t", SketchOnly);
            this.Tool("HORIZONTAL", "Horizontal", "sketch-horizontal-button", "h", SketchOnly);
            this.Tool("VERTICAL", "Vertical", "sketch-vertical-button", "v", SketchOnly);
            this.Tool("PERPENDICULAR", "Perpendicular", "sketch-perpendicular-button", "shift+l", SketchOnly, "Perpend.");
            this.Tool("EQUAL", "Equal", "sketch-equal-button", "e", SketchOnly);
            this.Tool("MIDPOINT", "Midpoint", "sketch-midpoint-button", "shift+m", SketchOnly);
            this.Tool("NORMAL", "Normal", "sketch-normal-button", "shift+k", SketchOnly);
            this.Tool("PIERCE", "Pierce", "sketch-pierce-button", "shift+g", SketchOnly);
            this.Tool("MIRROR", "Symmetric", "sketch-symmetric-button", "shift+q", SketchOnly);
            this.Tool("FIX", "Fix", "sketch-fix-button", "shift+j", SketchOnly);
            this.Tool("EQUAL_CURVATURE", "Curvature", "equal-curvature-button", "shift+u", SketchOnly);
        }
    }
}
