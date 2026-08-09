#!/usr/bin/env python3
"""Generate deterministic first-party ART-UI-002 frame, panel, and icon atlas."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


SOURCE_SIZE = 4096
FINAL_SIZE = 2048
GRID = 4
SEED = 31024
ALPHA_THRESHOLD = 16
NAMES = (
    "frame.standard", "frame.focused", "frame.disabled", "frame.danger",
    "panel.solid", "panel.translucent", "panel.card", "panel.tooltip",
    "icon.health", "icon.shield", "icon.experience", "icon.level",
    "icon.time", "icon.objective", "icon.map", "icon.lock",
)

INK = (22, 61, 69, 255)
DEEP = (12, 37, 44, 245)
TEAL = (89, 199, 193, 255)
PALE_TEAL = (167, 230, 221, 255)
IVORY = (244, 239, 216, 255)
WARM_STONE = (182, 155, 120, 255)
GOLD = (215, 184, 92, 255)
MUTED = (130, 151, 147, 220)
DANGER = (228, 93, 69, 255)
DANGER_EDGE = (255, 208, 163, 255)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def regular_polygon(cx: int, cy: int, radius: int, count: int, rotation: float = 0.0) -> list[tuple[int, int]]:
    return [
        (
            round(cx + math.cos(rotation + index * math.tau / count) * radius),
            round(cy + math.sin(rotation + index * math.tau / count) * radius),
        )
        for index in range(count)
    ]


def closed_line(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill, width: int) -> None:
    draw.line(points + [points[0]], fill=fill, width=width, joint="curve")


def draw_corner_leaves(draw: ImageDraw.ImageDraw, color, focused: bool = False) -> None:
    corners = ((154, 154, 1, 1), (870, 154, -1, 1), (154, 870, 1, -1), (870, 870, -1, -1))
    for x, y, sx, sy in corners:
        draw.line((x, y, x + sx * 116, y, x + sx * 168, y + sy * 52), fill=color, width=28, joint="curve")
        draw.line((x, y, x, y + sy * 116, x + sx * 52, y + sy * 168), fill=color, width=28, joint="curve")
        if focused:
            draw.polygon(
                [(x + sx * 58, y + sy * 20), (x + sx * 116, y + sy * 58), (x + sx * 38, y + sy * 92)],
                fill=GOLD,
            )


def draw_frame(kind: int) -> Image.Image:
    image = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == 0:
        draw.rounded_rectangle((78, 78, 946, 946), radius=112, outline=IVORY, width=34)
        draw.rounded_rectangle((126, 126, 898, 898), radius=82, outline=TEAL, width=22)
        draw_corner_leaves(draw, WARM_STONE)
    elif kind == 1:
        draw.rounded_rectangle((66, 66, 958, 958), radius=124, outline=GOLD, width=30)
        draw.rounded_rectangle((110, 110, 914, 914), radius=94, outline=PALE_TEAL, width=38)
        draw_corner_leaves(draw, IVORY, True)
        for x, y in ((512, 82), (942, 512), (512, 942), (82, 512)):
            draw.ellipse((x - 24, y - 24, x + 24, y + 24), fill=TEAL, outline=IVORY, width=8)
    elif kind == 2:
        for inset in (88, 142):
            for offset in range(0, 700, 112):
                draw.line((inset + offset, inset, min(936 - inset + 88, inset + offset + 64), inset), fill=MUTED, width=24)
                draw.line((inset + offset, 1024 - inset, min(936 - inset + 88, inset + offset + 64), 1024 - inset), fill=MUTED, width=24)
                draw.line((inset, inset + offset, inset, min(936 - inset + 88, inset + offset + 64)), fill=MUTED, width=24)
                draw.line((1024 - inset, inset + offset, 1024 - inset, min(936 - inset + 88, inset + offset + 64)), fill=MUTED, width=24)
        for x, y in ((142, 142), (882, 142), (142, 882), (882, 882)):
            draw.rectangle((x - 42, y - 42, x + 42, y + 42), outline=IVORY, width=18)
    else:
        draw.rounded_rectangle((70, 70, 954, 954), radius=112, outline=DANGER_EDGE, width=30)
        for inset in (112, 156):
            draw.rounded_rectangle((inset, inset, 1024 - inset, 1024 - inset), radius=82, outline=DANGER, width=28)
        for x, y, rotation in ((132, 132, -0.75), (892, 132, 0.75), (892, 892, 2.35), (132, 892, 3.95)):
            draw.polygon(regular_polygon(x, y, 70, 3, rotation), fill=DANGER, outline=DANGER_EDGE)
    return image


def draw_panel(kind: int) -> Image.Image:
    image = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == 0:
        draw.rounded_rectangle((64, 64, 960, 960), radius=104, fill=DEEP, outline=IVORY, width=26)
        draw.rounded_rectangle((116, 116, 908, 908), radius=70, outline=TEAL, width=16)
    elif kind == 1:
        draw.rounded_rectangle((64, 64, 960, 960), radius=104, fill=(22, 61, 69, 210), outline=PALE_TEAL, width=24)
        draw_corner_leaves(draw, TEAL)
        draw.line((220, 112, 804, 112), fill=GOLD, width=16)
    elif kind == 2:
        draw.rounded_rectangle((64, 64, 960, 960), radius=92, fill=(232, 225, 199, 242), outline=INK, width=32)
        draw.rounded_rectangle((112, 112, 912, 912), radius=62, outline=WARM_STONE, width=18)
        draw.polygon([(418, 78), (512, 126), (606, 78)], fill=GOLD)
        draw.polygon([(418, 946), (512, 898), (606, 946)], fill=TEAL)
    else:
        draw.rounded_rectangle((64, 64, 960, 960), radius=76, fill=(12, 37, 44, 246), outline=GOLD, width=24)
        draw.rounded_rectangle((110, 110, 914, 914), radius=46, outline=MUTED, width=14)
        for x, y in ((128, 128), (896, 128), (128, 896), (896, 896)):
            draw.polygon(regular_polygon(x, y, 30, 4, math.pi / 4), fill=PALE_TEAL)
    return image


def draw_icon(kind: int) -> Image.Image:
    image = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == 0:  # health: leaf-heart, not danger red
        draw.polygon([(512, 780), (274, 520), (294, 334), (414, 272), (512, 374), (610, 272), (730, 334), (750, 520)], fill=GOLD, outline=IVORY)
        draw.line((512, 374, 512, 690), fill=INK, width=34)
        draw.arc((380, 410, 644, 674), 198, 342, fill=TEAL, width=42)
    elif kind == 1:  # shield
        shield = [(512, 244), (730, 332), (696, 626), (512, 790), (328, 626), (294, 332)]
        draw.polygon(shield, fill=TEAL, outline=IVORY)
        draw.line((512, 302, 512, 710), fill=INK, width=34)
        draw.arc((376, 382, 648, 654), 205, 335, fill=GOLD, width=34)
    elif kind == 2:  # experience crystal
        outer = [(512, 224), (730, 400), (640, 748), (384, 748), (294, 400)]
        draw.polygon(outer, fill=PALE_TEAL, outline=IVORY)
        draw.polygon([(512, 260), (630, 420), (512, 696), (394, 420)], fill=TEAL, outline=INK)
        draw.line((314, 404, 710, 404), fill=GOLD, width=28)
    elif kind == 3:  # level chevrons
        for y, half in ((710, 240), (534, 194), (374, 146)):
            draw.polygon([(512 - half, y), (512, y - 132), (512 + half, y), (512 + half - 58, y + 58), (512, y - 50), (512 - half + 58, y + 58)], fill=TEAL, outline=IVORY)
    elif kind == 4:  # time hourglass
        draw.line((330, 260, 694, 260), fill=IVORY, width=46)
        draw.line((330, 764, 694, 764), fill=IVORY, width=46)
        draw.polygon([(366, 300), (658, 300), (566, 494), (658, 724), (366, 724), (458, 494)], fill=(22, 61, 69, 230), outline=TEAL)
        draw.polygon([(430, 354), (594, 354), (512, 474)], fill=GOLD)
        draw.polygon([(512, 528), (594, 674), (430, 674)], fill=GOLD)
    elif kind == 5:  # objective altar
        draw.ellipse((360, 264, 664, 568), outline=PALE_TEAL, width=38)
        draw.ellipse((420, 324, 604, 508), outline=GOLD, width=30)
        draw.polygon([(512, 336), (576, 416), (512, 496), (448, 416)], fill=TEAL)
        draw.polygon([(332, 716), (692, 716), (642, 566), (382, 566)], fill=WARM_STONE, outline=IVORY)
    elif kind == 6:  # folded map
        draw.polygon([(270, 326), (430, 260), (594, 330), (754, 264), (754, 704), (594, 770), (430, 700), (270, 766)], fill=(232, 225, 199, 250), outline=INK)
        draw.line((430, 260, 430, 700), fill=TEAL, width=26)
        draw.line((594, 330, 594, 770), fill=GOLD, width=26)
        draw.line((324, 600, 400, 514, 510, 570, 676, 430), fill=WARM_STONE, width=26, joint="curve")
        draw.ellipse((646, 396, 706, 456), fill=TEAL)
    else:  # lock
        draw.rounded_rectangle((302, 430, 722, 772), radius=68, fill=INK, outline=IVORY, width=34)
        draw.arc((360, 224, 664, 548), 180, 360, fill=GOLD, width=58)
        draw.ellipse((474, 536, 550, 612), fill=TEAL)
        draw.polygon([(486, 598), (538, 598), (566, 698), (458, 698)], fill=TEAL)
    return image


def render_source() -> Image.Image:
    atlas = Image.new("RGBA", (SOURCE_SIZE, SOURCE_SIZE), (0, 0, 0, 0))
    for index in range(16):
        row, column = divmod(index, GRID)
        if row == 0:
            cell = draw_frame(column)
        elif row == 1:
            cell = draw_panel(column)
        else:
            cell = draw_icon(index - 8)
        atlas.alpha_composite(cell, (column * 1024, row * 1024))
    return atlas


def pixel_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def grayscale_hash(image: Image.Image) -> str:
    rgba = image.convert("RGBA")
    gray = rgba.convert("LA")
    return hashlib.sha256(gray.tobytes()).hexdigest()


def cell_metrics(cell: Image.Image) -> tuple[int, float]:
    alpha = cell.getchannel("A")
    mask = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("atlas cell is empty")
    margin = min(bounds[0], bounds[1], cell.width - bounds[2], cell.height - bounds[3])
    covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
    return margin, covered / (cell.width * cell.height)


def count_magenta(cell: Image.Image) -> int:
    return sum(1 for r, g, b, a in cell.get_flattened_data() if a >= 16 and r >= 220 and b >= 180 and g <= 70)


def count_danger(cell: Image.Image) -> int:
    return sum(1 for r, g, b, a in cell.get_flattened_data() if a >= 16 and (r, g, b) == DANGER[:3])


def validate(atlas: Image.Image, expected_size: int, label: str) -> None:
    if atlas.size != (expected_size, expected_size):
        raise ValueError(f"{label} has unexpected size {atlas.size}")
    cell_size = expected_size // GRID
    color_hashes = set()
    gray_hashes = set()
    for index, name in enumerate(NAMES):
        row, column = divmod(index, GRID)
        cell = atlas.crop((column * cell_size, row * cell_size, (column + 1) * cell_size, (row + 1) * cell_size))
        margin, coverage = cell_metrics(cell)
        minimum_margin = 28 if expected_size == FINAL_SIZE else 56
        if row >= 2:
            minimum_margin = 80 if expected_size == FINAL_SIZE else 160
        if margin < minimum_margin:
            raise ValueError(f"{label} {name} margin {margin} is below {minimum_margin}")
        low, high = ((0.04, 0.36) if row == 0 else (0.58, 0.88) if row == 1 else (0.05, 0.42))
        if not low <= coverage <= high:
            raise ValueError(f"{label} {name} coverage {coverage:.4f} is outside {low}..{high}")
        if cell.getpixel((0, 0))[3] or cell.getpixel((cell_size - 1, cell_size - 1))[3]:
            raise ValueError(f"{label} {name} has nontransparent corner")
        if count_magenta(cell):
            raise ValueError(f"{label} {name} contains chroma-magenta")
        danger_count = count_danger(cell)
        if name == "frame.danger" and danger_count == 0:
            raise ValueError(f"{label} danger frame lacks reserved danger red")
        if name != "frame.danger" and danger_count:
            raise ValueError(f"{label} {name} improperly uses reserved danger red")
        center_alpha = cell.getpixel((cell_size // 2, cell_size // 2))[3]
        if row == 0 and center_alpha:
            raise ValueError(f"{label} {name} frame center must remain transparent")
        if row == 1 and center_alpha < 180:
            raise ValueError(f"{label} {name} panel center must remain readable")
        color_hashes.add(pixel_hash(cell))
        gray_hashes.add(grayscale_hash(cell))
        print(f"{label} sprite={name} margin={margin} coverage={coverage:.4f} danger_pixels={danger_count}")
    if len(color_hashes) != 16 or len(gray_hashes) != 16:
        raise ValueError(f"{label} atlas sprites are not unique in color and grayscale")


def main() -> None:
    args = parse_args()
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    source = render_source()
    final = source.resize((FINAL_SIZE, FINAL_SIZE), Image.Resampling.LANCZOS)
    validate(source, SOURCE_SIZE, "source")
    validate(final, FINAL_SIZE, "final")
    source_path = args.source_dir / "ui-framework-atlas-source.png"
    final_path = args.final_dir / "ui-framework-atlas.png"
    source.save(source_path, optimize=True)
    final.save(final_path, optimize=True)
    print(f"source_pixel_sha256={pixel_hash(source)}")
    print(f"final_pixel_sha256={pixel_hash(final)}")
    print(f"output_source={source_path} output_final={final_path}")


if __name__ == "__main__":
    main()
