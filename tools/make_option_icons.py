#!/usr/bin/env python3
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

"""Draw the icons for dialog options Onshape shows as plain text.

Onshape's feature dialogs offer Solid | Surface | Thin, New | Add | Remove |
Intersect and end types like Blind or Through all as text only, so there is no
icon in its sprite to borrow. These are drawn here in the same 20x20 grid and
with the same colour roles as Onshape's own icons (outline, face, shading,
accent), so IconRenderer recolours them alike.

Writes OnshapePlugin/src/Icons/mx-opt-*.svg. The file names are what
DialogCommand.OptionIcon maps option values and labels to.
"""

import pathlib

OUT = pathlib.Path(__file__).resolve().parent.parent / "OnshapePlugin" / "src" / "Icons"

O = "var(--os-icon-outline-primary, #333333)"
F1 = "var(--os-icon-fill-primary, #FFFFFF)"
F2 = "var(--os-icon-fill-secondary, #999999)"
A = "var(--os-icon-accent-primary, #1651B0)"

STROKE = f'stroke="{O}" stroke-width="1.2" stroke-linejoin="round"'
DASHED = f'stroke="{O}" stroke-width="1" stroke-dasharray="1.4 1.2"'


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">{body}</svg>\n'


def rect(x0, y0, x1, y1, fill, extra=STROKE):
    return f'<rect x="{x0}" y="{y0}" width="{x1 - x0}" height="{y1 - y0}" fill="{fill}" {extra}/>'


# ---------------------------------------------------------------- body type

def solid():
    return (f'<polygon points="10,2 17,6 10,10 3,6" fill="{F1}" {STROKE}/>'
            f'<polygon points="3,6 10,10 10,18 3,14" fill="{A}" {STROKE}/>'
            f'<polygon points="10,10 17,6 17,14 10,18" fill="{F2}" {STROKE}/>')


def surface():
    # a single curved sheet, no thickness
    return (f'<path d="M2 11 C5 6 8 6 10 9 S15 12 18 7 L18 9 C15 14 12 12 10 11 S5 8 2 13 Z" '
            f'fill="{A}" {STROKE}/>')


def thin():
    # a wall of constant thickness: a hollow profile
    return (f'<path d="M3 3 H17 V17 H3 Z M6 6 V14 H14 V6 Z" fill="{A}" fill-rule="evenodd" {STROKE}/>')


# ---------------------------------------------------------------- operation

# The existing part is the square top left, the new material the one bottom right.
PART = (2, 3, 12, 13)
TOOL = (8, 7, 18, 17)


def new():
    return rect(2, 2, 10, 10, F2) + rect(11, 10, 18, 17, A)


def add():
    union = "M2 3 H12 V7 H18 V17 H8 V13 H2 Z"
    return f'<path d="{union}" fill="{A}" {STROKE}/>'


def remove():
    cut = "M2 3 H12 V7 H8 V13 H2 Z"
    return f'<path d="{cut}" fill="{F2}" {STROKE}/>' + rect(*TOOL, "none", DASHED)


def intersect():
    return rect(*PART, "none", DASHED) + rect(*TOOL, "none", DASHED) + rect(8, 7, 12, 13, A)


# ---------------------------------------------------------------- end type

def arrow(tip):
    """An arrow from the sketch at the bottom up to y = tip."""
    return (f'<line x1="10" y1="16" x2="10" y2="{tip + 3}" stroke="{O}" stroke-width="1.6"/>'
            f'<polygon points="7,{tip + 4} 13,{tip + 4} 10,{tip}" fill="{O}"/>')


SKETCH = rect(4, 16.5, 16, 18.5, F2)


def blind():
    # a fixed distance: the arrow stops at a dimension tick
    return SKETCH + arrow(6) + f'<line x1="5" y1="5" x2="15" y2="5" stroke="{A}" stroke-width="1.6"/>'


def up_to_next():
    return SKETCH + rect(3, 1.5, 17, 5.5, A) + arrow(6)


def up_to_face():
    return SKETCH + f'<polygon points="3,5 12,2 17,4 8,7" fill="{A}" {STROKE}/>' + arrow(7.5)


def up_to_part():
    return (SKETCH
            + f'<polygon points="10,0.8 14,3 10,5.2 6,3" fill="{F1}" {STROKE}/>'
            + f'<polygon points="6,3 10,5.2 10,8 6,5.8" fill="{A}" {STROKE}/>'
            + f'<polygon points="10,5.2 14,3 14,5.8 10,8" fill="{F2}" {STROKE}/>'
            + arrow(8.5))


def up_to_vertex():
    return SKETCH + f'<circle cx="10" cy="3.2" r="2.2" fill="{A}" {STROKE}/>' + arrow(6)


def through_all():
    # through the part and out the other side
    return SKETCH + rect(3, 7, 17, 11, F2) + arrow(1.5)


ICONS = {
    "solid": solid, "surface": surface, "thin": thin,
    "new": new, "add": add, "remove": remove, "intersect": intersect,
    "blind": blind, "up-to-next": up_to_next, "up-to-face": up_to_face,
    "up-to-part": up_to_part, "up-to-vertex": up_to_vertex, "through-all": through_all,
}


def main():
    for name, draw in ICONS.items():
        (OUT / f"mx-opt-{name}.svg").write_text(svg(draw()))
    print(f"wrote {len(ICONS)} icons to {OUT.relative_to(OUT.parent.parent.parent)}")


if __name__ == "__main__":
    main()
