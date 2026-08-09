#!/usr/bin/env python3
"""Generate deterministic first-party ART-UI-004 telegraph/accessibility masks."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageDraw


SOURCE_SIZE = 2048
FINAL_SIZE = 1024
GRID = 4
SEED = 31026
ALPHA_THRESHOLD = 16
NAMES = (
    "shape.area-circle", "shape.directional-line", "shape.fan-cone", "shape.point-impact",
    "shape.cross-lanes", "shape.hazard-ring", "shape.sweep-arc", "shape.direction-arrow",
    "texture.diagonal-stripes", "texture.crosshatch", "texture.dots", "texture.chevrons",
    "texture.radial-spokes", "texture.grid", "texture.broken-bars", "texture.concentric",
)

DANGER = (228, 93, 69, 255)
DANGER_FILL = (228, 93, 69, 92)
DANGER_EDGE = (255, 208, 163, 255)
DEEP = (12, 37, 44, 245)
IVORY = (244, 239, 216, 240)
TEAL = (89, 199, 193, 230)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def polygon(cx: int, cy: int, radius: int, count: int, rotation: float = 0.0) -> list[tuple[int, int]]:
    return [
        (
            round(cx + math.cos(rotation + index * math.tau / count) * radius),
            round(cy + math.sin(rotation + index * math.tau / count) * radius),
        )
        for index in range(count)
    ]


def arrow(draw: ImageDraw.ImageDraw, x: int, y: int, scale: int, fill) -> None:
    draw.polygon(
        [
            (x - scale, y - scale // 3),
            (x + scale // 4, y - scale // 3),
            (x + scale // 4, y - scale),
            (x + scale, y),
            (x + scale // 4, y + scale),
            (x + scale // 4, y + scale // 3),
            (x - scale, y + scale // 3),
        ],
        fill=fill,
    )


def draw_shape(kind: int) -> Image.Image:
    image = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == 0:  # circular area with an explicit bounded interior
        draw.ellipse((66, 66, 446, 446), fill=DANGER_FILL, outline=DANGER_EDGE, width=16)
        draw.ellipse((94, 94, 418, 418), outline=DANGER, width=20)
        for angle in (0, 90, 180, 270):
            radians = math.radians(angle)
            x = round(256 + math.cos(radians) * 176)
            y = round(256 + math.sin(radians) * 176)
            draw.polygon(polygon(x, y, 24, 3, radians), fill=DANGER, outline=DEEP)
    elif kind == 1:  # directional lane
        draw.rounded_rectangle((52, 178, 460, 334), radius=48, fill=DANGER_FILL, outline=DANGER_EDGE, width=16)
        draw.line((74, 210, 428, 210), fill=DANGER, width=18)
        draw.line((74, 302, 428, 302), fill=DEEP, width=12)
        for x in (158, 260, 362):
            draw.line((x - 34, 272, x, 238, x + 34, 272), fill=DANGER, width=18, joint="curve")
    elif kind == 2:  # fan / cone
        origin = (90, 256)
        outer = [(90, 256)]
        for angle in range(-38, 39, 4):
            radians = math.radians(angle)
            outer.append((round(90 + math.cos(radians) * 330), round(256 + math.sin(radians) * 330)))
        draw.polygon(outer, fill=DANGER_FILL)
        draw.line(outer + [origin], fill=DANGER_EDGE, width=16, joint="curve")
        for radius in (132, 220, 300):
            box = (90 - radius, 256 - radius, 90 + radius, 256 + radius)
            draw.arc(box, -38, 38, fill=DANGER, width=18)
    elif kind == 3:  # point impact / target
        draw.ellipse((80, 80, 432, 432), outline=DANGER_EDGE, width=14)
        draw.ellipse((130, 130, 382, 382), outline=DANGER, width=20)
        draw.polygon(polygon(256, 256, 104, 4, math.pi / 4), fill=DANGER_FILL, outline=DANGER)
        for x1, y1, x2, y2 in ((256, 54, 256, 146), (256, 366, 256, 458), (54, 256, 146, 256), (366, 256, 458, 256)):
            draw.line((x1, y1, x2, y2), fill=DEEP, width=18)
    elif kind == 4:  # crossing lanes
        draw.rounded_rectangle((62, 194, 450, 318), radius=36, fill=DANGER_FILL, outline=DANGER_EDGE, width=14)
        draw.rounded_rectangle((194, 62, 318, 450), radius=36, fill=DANGER_FILL, outline=DANGER_EDGE, width=14)
        draw.polygon(polygon(256, 256, 68, 4, math.pi / 4), fill=DANGER, outline=DEEP)
        for x, y, rotation in ((108, 256, math.pi), (404, 256, 0), (256, 108, -math.pi / 2), (256, 404, math.pi / 2)):
            draw.polygon(polygon(x, y, 22, 3, rotation), fill=DANGER)
    elif kind == 5:  # annular hazard with safe center
        draw.ellipse((60, 60, 452, 452), fill=DANGER_FILL, outline=DANGER_EDGE, width=16)
        draw.ellipse((154, 154, 358, 358), fill=(0, 0, 0, 0), outline=DEEP, width=18)
        for start in range(0, 360, 60):
            draw.arc((88, 88, 424, 424), start + 8, start + 45, fill=DANGER, width=22)
    elif kind == 6:  # clockwise sweep arc
        draw.arc((66, 66, 446, 446), 42, 326, fill=DANGER_EDGE, width=54)
        draw.arc((92, 92, 420, 420), 46, 322, fill=DANGER, width=24)
        arrow(draw, 398, 338, 42, DANGER)
        draw.arc((168, 168, 344, 344), 42, 326, fill=DEEP, width=16)
    else:  # explicit travel direction arrow
        arrow(draw, 256, 256, 188, DANGER_FILL)
        draw.line((82, 214, 292, 214, 292, 128, 442, 256, 292, 384, 292, 298, 82, 298, 82, 214),
                  fill=DANGER_EDGE, width=16, joint="curve")
        for x in (148, 218, 288):
            draw.line((x - 24, 270, x, 246, x + 24, 270), fill=DANGER, width=14, joint="curve")
    return image


def clipped_pattern(draw_fn) -> Image.Image:
    pattern = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(pattern))
    mask = Image.new("L", (512, 512), 0)
    ImageDraw.Draw(mask).rounded_rectangle((58, 58, 454, 454), radius=82, fill=255)
    pattern.putalpha(Image.composite(pattern.getchannel("A"), Image.new("L", (512, 512), 0), mask))
    return pattern


def draw_texture(kind: int) -> Image.Image:
    def render(draw: ImageDraw.ImageDraw) -> None:
        if kind == 0:  # single-direction hatch
            for offset in range(-320, 640, 64):
                draw.line((offset, 454, offset + 396, 58), fill=IVORY, width=24)
        elif kind == 1:  # crosshatch
            for offset in range(-320, 640, 92):
                draw.line((offset, 454, offset + 396, 58), fill=IVORY, width=18)
                draw.line((offset, 58, offset + 396, 454), fill=TEAL, width=18)
        elif kind == 2:  # dot field with alternating scale
            for row, y in enumerate(range(98, 430, 66)):
                for column, x in enumerate(range(98, 430, 66)):
                    radius = 13 if (row + column) % 2 == 0 else 8
                    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=IVORY)
        elif kind == 3:  # repeated directional chevrons
            for y in (132, 256, 380):
                for x in (134, 256, 378):
                    draw.line((x - 34, y - 28, x, y, x - 34, y + 28), fill=IVORY, width=18, joint="curve")
        elif kind == 4:  # radial spokes
            for angle in range(0, 360, 30):
                radians = math.radians(angle)
                inner = (round(256 + math.cos(radians) * 74), round(256 + math.sin(radians) * 74))
                outer = (round(256 + math.cos(radians) * 190), round(256 + math.sin(radians) * 190))
                draw.line((inner, outer), fill=IVORY if angle % 60 == 0 else TEAL, width=18)
            draw.ellipse((214, 214, 298, 298), outline=IVORY, width=16)
        elif kind == 5:  # orthogonal grid
            for value in (112, 184, 256, 328, 400):
                draw.line((76, value, 436, value), fill=IVORY, width=14)
                draw.line((value, 76, value, 436), fill=TEAL, width=14)
        elif kind == 6:  # staggered broken bars
            for row, y in enumerate((116, 186, 256, 326, 396)):
                start = 80 if row % 2 == 0 else 116
                for x in range(start, 420, 96):
                    draw.rounded_rectangle((x, y - 13, min(x + 58, 438), y + 13), radius=10, fill=IVORY)
        else:  # concentric low-flash boundary texture
            for inset, width, color in ((74, 18, IVORY), (126, 16, TEAL), (180, 14, IVORY)):
                draw.rounded_rectangle((inset, inset, 512 - inset, 512 - inset), radius=72 - inset // 6,
                                       outline=color, width=width)
            for x, y in ((98, 98), (414, 98), (414, 414), (98, 414)):
                draw.polygon(polygon(x, y, 18, 4, math.pi / 4), fill=IVORY)

    return clipped_pattern(render)


def render_source() -> Image.Image:
    atlas = Image.new("RGBA", (SOURCE_SIZE, SOURCE_SIZE), (0, 0, 0, 0))
    for index in range(16):
        row, column = divmod(index, GRID)
        cell = draw_shape(index) if index < 8 else draw_texture(index - 8)
        atlas.alpha_composite(cell, (column * 512, row * 512))
    return atlas


def pixel_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def grayscale_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.convert("LA").tobytes()).hexdigest()


def metrics(cell: Image.Image) -> tuple[int, float, int, int]:
    pixels = list(cell.get_flattened_data())
    alpha = cell.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError("atlas cell is empty")
    margin = min(bounds[0], bounds[1], cell.width - bounds[2], cell.height - bounds[3])
    covered = sum(1 for _, _, _, a in pixels if a >= ALPHA_THRESHOLD)
    magenta = sum(1 for r, g, b, a in pixels if a >= 16 and r >= 220 and b >= 180 and g <= 70)
    danger = sum(1 for r, g, b, a in pixels if a >= 16 and (r, g, b) == DANGER[:3])
    return margin, covered / (cell.width * cell.height), magenta, danger


def validate(atlas: Image.Image, expected_size: int, label: str) -> None:
    if atlas.size != (expected_size, expected_size):
        raise ValueError(f"{label} has unexpected size {atlas.size}")
    cell_size = expected_size // GRID
    minimum_margin = 20 if expected_size == FINAL_SIZE else 40
    color_hashes: set[str] = set()
    gray_hashes: set[str] = set()
    for index, name in enumerate(NAMES):
        row, column = divmod(index, GRID)
        cell = atlas.crop((column * cell_size, row * cell_size, (column + 1) * cell_size, (row + 1) * cell_size))
        margin, coverage, magenta, danger = metrics(cell)
        if margin < minimum_margin:
            raise ValueError(f"{label} {name} margin {margin} is below {minimum_margin}")
        if not 0.035 <= coverage <= 0.48:
            raise ValueError(f"{label} {name} coverage {coverage:.4f} is outside 0.035..0.48")
        if magenta:
            raise ValueError(f"{label} {name} contains chroma-magenta")
        if index < 8 and danger == 0:
            raise ValueError(f"{label} {name} lacks the reserved danger core")
        if index >= 8 and danger:
            raise ValueError(f"{label} {name} texture channel must remain neutral/tintable")
        if cell.getpixel((0, 0))[3] or cell.getpixel((cell_size - 1, cell_size - 1))[3]:
            raise ValueError(f"{label} {name} has a nontransparent corner")
        color_hashes.add(pixel_hash(cell))
        gray_hashes.add(grayscale_hash(cell))
        print(f"{label} sprite={name} margin={margin} coverage={coverage:.4f} danger_pixels={danger}")
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
    source_path = args.source_dir / "telegraph-accessibility-atlas-source.png"
    final_path = args.final_dir / "telegraph-accessibility-atlas.png"
    source.save(source_path, optimize=True)
    final.save(final_path, optimize=True)
    print(f"source_pixel_sha256={pixel_hash(source)}")
    print(f"final_pixel_sha256={pixel_hash(final)}")
    print(f"output_source={source_path} output_final={final_path}")


if __name__ == "__main__":
    main()
