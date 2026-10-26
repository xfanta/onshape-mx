#!/usr/bin/env python3
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

"""Render the Chrome Web Store images into store/.

  screenshot-1.png   1280x800, the keypad as Options+ shows it, with the pitch
  promo-small.png    440x280, the small promotional tile
  icon-128.png       128x128 store icon: the artwork at 96x96 with 16 px of
                     transparent margin, as the store's image guidelines ask

Needs Pillow and rsvg-convert (Homebrew librsvg). The keypad picture is
docs/keypad.png, a screenshot of Logi Options+; the icon is extension/icon.svg.
"""

import pathlib
import subprocess
import tempfile

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "store"

# Onshape's green (#64BC4F, from its logo), shading gently darker.
LIGHT = (0x64, 0xBC, 0x4F)
DARK = (0x44, 0x96, 0x3A)
INK = (0x1A, 0x1A, 0x1A)
WHITE = (0xFF, 0xFF, 0xFF)
GREY = (0xB4, 0xB4, 0xB4)

FONT = "/System/Library/Fonts/HelveticaNeue.ttc"


def font(size, bold=False):
    # HelveticaNeue.ttc: 0 regular, 1 bold
    return ImageFont.truetype(FONT, size, index=1 if bold else 0)


def gradient(size):
    """A diagonal gradient across Onshape's green."""
    w, h = size
    img = Image.new("RGB", size)
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = (x / w + y / h) / 2
            px[x, y] = tuple(round(a + (b - a) * t) for a, b in zip(LIGHT, DARK))
    return img


def glyph(size):
    """The icon's white keypad without its gradient square, for use on the gradient."""
    svg = (ROOT / "extension" / "icon.svg").read_text()
    svg = "\n".join(line for line in svg.splitlines() if 'fill="url(#g)"' not in line)
    with tempfile.NamedTemporaryFile(suffix=".svg", mode="w") as src, \
            tempfile.NamedTemporaryFile(suffix=".png") as out:
        src.write(svg)
        src.flush()
        subprocess.run(["rsvg-convert", "-w", str(size), "-h", str(size), src.name, "-o", out.name], check=True)
        return Image.open(out.name).convert("RGBA").copy()


def wrap(draw, text, fnt, width):
    lines, line = [], ""
    for word in text.split():
        trial = f"{line} {word}".strip()
        if draw.textlength(trial, font=fnt) <= width:
            line = trial
        else:
            lines.append(line)
            line = word
    return lines + [line]


def screenshot():
    img = Image.new("RGB", (1280, 800), INK)
    img.paste(gradient((1280, 8)), (0, 0))

    keypad = Image.open(ROOT / "docs" / "keypad.png").convert("RGBA")
    keypad.thumbnail((600, 680))
    img.paste(keypad, (80, (800 - keypad.height) // 2 + 4), keypad)

    draw = ImageDraw.Draw(img)
    x, y, width = 720, 190, 500
    for line in wrap(draw, "Every Onshape tool on your MX Creative Console", font(46, bold=True), width):
        draw.text((x, y), line, font=font(46, bold=True), fill=WHITE)
        y += 58

    y += 26
    points = [
        "Chamfer, Shell, Loft, patterns, sheet metal — even tools Onshape has no shortcut for",
        "Open a sketch and the keypad switches to sketch tools",
        "Open a feature and its keys become that dialog's options",
        "Keys light up for the tool that is running",
    ]
    for point in points:
        lines = wrap(draw, point, font(25), width - 28)
        draw.ellipse((x, y + 11, x + 9, y + 20), fill=LIGHT)
        for line in lines:
            draw.text((x + 28, y), line, font=font(25), fill=GREY)
            y += 34
        y += 14

    img.save(OUT / "screenshot-1.png")


def promo_small():
    img = gradient((440, 280))
    mark = glyph(150)
    img.paste(mark, (30, 65), mark)

    draw = ImageDraw.Draw(img)
    draw.text((181, 96), "MX Console", font=font(34, bold=True), fill=WHITE)
    draw.text((181, 136), "Bridge for Onshape", font=font(26), fill=WHITE)
    img.save(OUT / "promo-small.png")


def store_icon():
    with tempfile.NamedTemporaryFile(suffix=".png") as f:
        subprocess.run(["rsvg-convert", "-w", "96", "-h", "96",
                        str(ROOT / "extension" / "icon.svg"), "-o", f.name], check=True)
        art = Image.open(f.name).convert("RGBA")
        img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        img.paste(art, (16, 16), art)
        img.save(OUT / "icon-128.png")


def main():
    OUT.mkdir(exist_ok=True)
    screenshot()
    promo_small()
    store_icon()
    print("wrote store/screenshot-1.png, store/promo-small.png and store/icon-128.png")


if __name__ == "__main__":
    main()
