#!/usr/bin/env python3
"""Stitch images into one labelled grid PNG.

Usage:
  contact_sheet.py OUT.png IMG [IMG ...] [--labels a,b,c] [--cols N] [--cell 512]

Why: one image per review step keeps token use low and puts candidates side by side.
Cut-outs are composited on mid grey so transparent edges stay visible.
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("images", nargs="+")
    ap.add_argument("--labels", default="")
    ap.add_argument("--cols", type=int, default=0)
    ap.add_argument("--cell", type=int, default=512)
    a = ap.parse_args()

    labels = a.labels.split(",") if a.labels else [os.path.splitext(os.path.basename(p))[0] for p in a.images]
    n = len(a.images)
    cols = a.cols or min(n, 4)
    rows = -(-n // cols)
    cell, pad, lab = a.cell, 8, 28

    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + lab + pad) + pad), (40, 40, 44))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 18)
    except Exception:
        font = ImageFont.load_default()

    for i, path in enumerate(a.images):
        im = Image.open(path).convert("RGBA")
        im.thumbnail((cell, cell))
        tile = Image.new("RGBA", (cell, cell), (96, 96, 102, 255))
        tile.paste(im, ((cell - im.width) // 2, (cell - im.height) // 2), im)
        x = pad + (i % cols) * (cell + pad)
        y = pad + (i // cols) * (cell + lab + pad)
        sheet.paste(tile.convert("RGB"), (x, y))
        label = labels[i] if i < len(labels) else ""
        draw.text((x + 6, y + cell + 4), f"{i + 1}. {label}", fill=(235, 235, 235), font=font)

    sheet.save(a.out)
    print(a.out, sheet.size)


if __name__ == "__main__":
    main()
