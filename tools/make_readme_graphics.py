#!/usr/bin/env python3
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

"""Render the README's graphics into docs/.

  banner.png         the header: name, pitch and the keypad on Onshape's green
  how-it-works.svg   console -> plugin -> extension -> Onshape
  modes.svg          the four keypad layouts and their pages
  drawn-icons.png    the icons drawn for this project, as the keys show them
  social-preview.png 1280x640 for the repository's Settings > Social preview

The SVGs carry their own backgrounds, so they read in GitHub's light and dark
themes alike. Needs Pillow and rsvg-convert; reuses make_store_assets.py.
"""

import pathlib
import re
import subprocess
import tempfile
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw

import make_store_assets as store

ROOT = pathlib.Path(__file__).resolve().parent.parent
DOCS = ROOT / "docs"
ICONS = ROOT / "OnshapePlugin" / "src" / "Icons"

GREEN = "#64BC4F"
GREEN_DARK = "#44963A"
CARD = "#F6F8FA"
INK = "#1F2328"
MUTED = "#59636E"
FONT = "-apple-system, BlinkMacSystemFont, 'Helvetica Neue', 'Segoe UI', Arial, sans-serif"


# ---------------------------------------------------------------- banner

def banner():
    w, h = 1600, 500
    img = store.gradient((w, h)).convert("RGBA")

    keypad = store.keypad_cutout()
    keypad.thumbnail((440, 430))
    left = w - keypad.width - 120
    img.alpha_composite(keypad, (left, (h - keypad.height) // 2))

    mark = store.glyph(120)
    img.alpha_composite(mark, (96, 70))

    draw = ImageDraw.Draw(img)
    x = 120
    draw.text((x, 200), "Onshape MX Creative", font=store.font(64, bold=True), fill=store.WHITE)
    draw.text((x, 284), "Every Onshape tool on your Logitech MX Creative Console.",
              font=store.font(32), fill=(255, 255, 255, 235))
    draw.text((x, 330), "The keypad follows your sketches, features and dialogs.",
              font=store.font(32), fill=(255, 255, 255, 235))

    img.convert("RGB").save(DOCS / "banner.png", optimize=True)


def social_preview():
    """What a link to the repository unfurls into. GitHub takes it by upload only."""
    w, h = 1280, 640
    img = store.gradient((w, h)).convert("RGBA")

    keypad = store.keypad_cutout()
    keypad.thumbnail((470, 520))
    left = w - keypad.width - 80
    img.alpha_composite(keypad, (left, (h - keypad.height) // 2))

    mark = store.glyph(130)
    img.alpha_composite(mark, (64, 120))

    draw = ImageDraw.Draw(img)
    x = 90
    draw.text((x, 270), "Onshape", font=store.font(72, bold=True), fill=store.WHITE)
    draw.text((x, 352), "MX Creative", font=store.font(72, bold=True), fill=store.WHITE)
    draw.text((x, 452), "Every Onshape tool on your", font=store.font(30), fill=(255, 255, 255, 235))
    draw.text((x, 492), "Logitech MX Creative Console", font=store.font(30), fill=(255, 255, 255, 235))

    img.convert("RGB").save(DOCS / "social-preview.png", optimize=True)


# ---------------------------------------------------------------- SVG helpers

def svg(width, height, body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
            f'viewBox="0 0 {width} {height}" font-family="{FONT}">{body}</svg>\n')


def text(x, y, content, size, fill, weight="normal", anchor="middle"):
    content = escape(content)
    return (f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{weight}" fill="{fill}" '
            f'text-anchor="{anchor}">{content}</text>')


# ---------------------------------------------------------------- how it works

def how_it_works():
    boxes = [
        ("MX Creative", "Console", "Keypad and Dialpad"),
        ("Onshape plugin", "for Logi Options+", "C#, in Logi Plugin Service"),
        ("MX Console Bridge", "Chrome extension", "service worker + page script"),
        ("Onshape", "cad.onshape.com", "toolbar and dialogs"),
    ]
    links = ["key presses", "ws://127.0.0.1:47810", "clicks & state"]

    bw, bh, gap, top = 200, 112, 140, 20
    width = len(boxes) * bw + (len(boxes) - 1) * gap + 40
    height = top + bh + 56
    body = [f'<rect width="{width}" height="{height}" rx="16" fill="{CARD}"/>']

    for i, (title, sub, note) in enumerate(boxes):
        x = 20 + i * (bw + gap)
        body.append(f'<rect x="{x}" y="{top}" width="{bw}" height="{bh}" rx="12" fill="{GREEN}"/>')
        body.append(text(x + bw / 2, top + 44, title, 19, "#FFFFFF", "600"))
        body.append(text(x + bw / 2, top + 68, sub, 15, "#FFFFFF"))
        body.append(text(x + bw / 2, top + bh + 26, note, 13, MUTED))

    for i, label in enumerate(links):
        x0 = 20 + i * (bw + gap) + bw + 6
        x1 = x0 + gap - 12
        y = top + bh / 2
        body.append(f'<line x1="{x0}" y1="{y}" x2="{x1 - 6}" y2="{y}" stroke="{GREEN_DARK}" stroke-width="2.5"/>')
        body.append(f'<polygon points="{x1},{y} {x1 - 9},{y - 5} {x1 - 9},{y + 5}" fill="{GREEN_DARK}"/>')
        if i == 1:
            # the WebSocket goes both ways
            body.append(f'<polygon points="{x0},{y} {x0 + 9},{y - 5} {x0 + 9},{y + 5}" fill="{GREEN_DARK}"/>')
        body.append(text((x0 + x1) / 2, y - 12, label, 12, MUTED))

    (DOCS / "how-it-works.svg").write_text(svg(width, height, "".join(body)))


# ---------------------------------------------------------------- modes

def modes():
    cards = [
        ("Part Studio", "a Part Studio is open", ["Create", "Modify", "View"]),
        ("Sketch", "a sketch is open", ["Draw", "Modify", "Constrain", "More"]),
        ("Dialog", "a feature dialog is open", ["Options", "More"]),
        ("Assembly", "an Assembly is open", ["Mate", "More", "View"]),
    ]
    cw, ch, gap = 210, 196, 16
    width = len(cards) * cw + (len(cards) - 1) * gap
    body = []

    for i, (title, when, pages) in enumerate(cards):
        x = i * (cw + gap)
        body.append(f'<rect x="{x}" y="0" width="{cw}" height="{ch}" rx="12" fill="{CARD}" '
                    f'stroke="{GREEN}" stroke-width="2"/>')
        body.append(f'<path d="M{x} 12 a12 12 0 0 1 12 -12 h{cw - 24} a12 12 0 0 1 12 12 v38 h-{cw} z" fill="{GREEN}"/>')
        body.append(text(x + cw / 2, 32, title, 19, "#FFFFFF", "600"))
        body.append(text(x + cw / 2, 74, when, 13, MUTED))
        for j, page in enumerate(pages):
            py = 92 + j * 25
            body.append(f'<rect x="{x + 24}" y="{py}" width="{cw - 48}" height="20" rx="10" fill="#FFFFFF" '
                        f'stroke="#D0D7DE"/>')
            body.append(text(x + cw / 2, py + 14.5, page, 13, INK))

    (DOCS / "modes.svg").write_text(svg(width, ch, "".join(body)))


# ---------------------------------------------------------------- drawn icons

# IconRenderer's dark palette, at the brightness of an idle key.
PALETTE = {"outline": "#f2f2f2", "accent": "#5c9dff", "fill-secondary": "#8f8f8f", "fill": "#343434"}
IDLE = 0.62
COLOUR = re.compile(r'var\(\s*--([\w-]+)\s*(?:,\s*(#[0-9A-Fa-f]{3,8}|[a-z]+))?\s*\)|#[0-9A-Fa-f]{6}\b|#[0-9A-Fa-f]{3}\b')


def _scale(hex_colour, factor):
    h = hex_colour.lstrip("#")
    h = "".join(c * 2 for c in h) if len(h) == 3 else h[:6]
    return "#" + "".join(f"{round(int(h[i:i + 2], 16) * factor):02x}" for i in (0, 2, 4))


def _flip(hex_colour):
    h = hex_colour.lstrip("#")
    h = "".join(c * 2 for c in h) if len(h) == 3 else h[:6]
    r, g, b = (int(h[i:i + 2], 16) for i in (0, 2, 4))
    if max(r, g, b) - min(r, g, b) > 24:
        return "#" + h
    v = 255 - (r + g + b) // 3
    return f"#{v:02x}{v:02x}{v:02x}"


def keyed(svg_text, bright):
    keep = "data-keep-colours" in svg_text

    def recolour(m):
        if keep:
            colour = m.group(0)
        elif not m.group(1):
            colour = _flip(m.group(0))
        else:
            colour = next((v for k, v in PALETTE.items() if k in m.group(1)), None) or _flip(m.group(2) or "#333333")
        return colour if bright else _scale(colour, IDLE)

    return COLOUR.sub(recolour, svg_text)


def drawn_icons():
    rows = [
        ["mx-view-front", "mx-view-top", "mx-view-right", "mx-view-iso", "mx-view-back",
         "mx-view-bottom", "mx-view-left", "mx-view-normal", "mx-accept", "mx-cancel"],
        ["mx-opt-solid", "mx-opt-surface", "mx-opt-thin", "mx-opt-new", "mx-opt-add",
         "mx-opt-remove", "mx-opt-intersect", "mx-opt-blind", "mx-opt-up-to-next", "mx-opt-through-all"],
    ]
    key, gap, pad = 96, 14, 22
    cols = max(len(r) for r in rows)
    w = pad * 2 + cols * key + (cols - 1) * gap
    h = pad * 2 + len(rows) * key + (len(rows) - 1) * gap

    sheet = store.gradient((w, h)).convert("RGBA")
    draw = ImageDraw.Draw(sheet)
    for r, names in enumerate(rows):
        for c, name in enumerate(names):
            x = pad + c * (key + gap)
            y = pad + r * (key + gap)
            draw.rounded_rectangle((x, y, x + key, y + key), radius=14, fill=(8, 8, 8, 255))
            bright = name in ("mx-accept", "mx-cancel")
            src = keyed((ICONS / f"{name}.svg").read_text(), bright)
            with tempfile.NamedTemporaryFile(suffix=".svg", mode="w") as f, \
                    tempfile.NamedTemporaryFile(suffix=".png") as out:
                f.write(src)
                f.flush()
                side = round(key * 0.7)
                subprocess.run(["rsvg-convert", "-w", str(side), "-h", str(side), f.name, "-o", out.name], check=True)
                glyph = Image.open(out.name).convert("RGBA")
            sheet.alpha_composite(glyph, (x + (key - side) // 2, y + (key - side) // 2))

    sheet.convert("RGB").save(DOCS / "drawn-icons.png", optimize=True)


def main():
    DOCS.mkdir(exist_ok=True)
    banner()
    how_it_works()
    modes()
    drawn_icons()
    social_preview()
    print("wrote banner.png, how-it-works.svg, modes.svg, drawn-icons.png and social-preview.png in docs/")


if __name__ == "__main__":
    main()
