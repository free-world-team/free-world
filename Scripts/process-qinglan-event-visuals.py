#!/usr/bin/env python3
"""Compose deterministic four-frame Qinglan event VFX from approved AI-assisted cores."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageDraw


EVENTS = (
    "wind-vein-riot",
    "herb-garden-revival",
    "old-sword-resonance",
)

FRAME_COUNT = 4
MASTER_CELL_SIZE = 512
FINAL_CELL_SIZE = 256
SOURCE_MASTER_SIZE = 2048
ALPHA_THRESHOLD = 16
FINAL_SAFE_MARGIN = 14

TEAL = (72, 184, 187, 150)
PALE_CYAN = (181, 236, 226, 195)
PALE_IVORY = (238, 235, 207, 180)
CELADON = (116, 169, 115, 180)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def crop_visible(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
    bounds = thresholded.getbbox()
    if bounds is None:
        raise ValueError("event core has no visible pixels")
    return image.crop(bounds)


def render_core(core: Image.Image, maximum: int) -> Image.Image:
    scale = min(maximum / core.width, maximum / core.height)
    size = (max(1, round(core.width * scale)), max(1, round(core.height * scale)))
    return core.resize(size, Image.Resampling.LANCZOS)


def point(center: tuple[float, float], radius: float, degrees: float) -> tuple[int, int]:
    radians = math.radians(degrees)
    return (
        round(center[0] + math.cos(radians) * radius),
        round(center[1] + math.sin(radians) * radius),
    )


def diamond(draw: ImageDraw.ImageDraw, center: tuple[int, int], radius: int, fill: tuple[int, ...]) -> None:
    x, y = center
    draw.polygon(((x, y - radius), (x + radius, y), (x, y + radius), (x - radius, y)), fill=fill)


def draw_wind_overlay(overlay: Image.Image, phase: int) -> None:
    draw = ImageDraw.Draw(overlay, "RGBA")
    center = (256, 256)
    rotation = phase * 22.5
    for ring, radius in enumerate((148, 190, 222)):
        bounds = (256 - radius, 256 - radius * 0.58, 256 + radius, 256 + radius * 0.58)
        start = 18 + rotation + ring * 31
        draw.arc(bounds, start=start, end=start + 116, fill=TEAL, width=13 - ring * 2)
        draw.arc(bounds, start=start + 180, end=start + 296, fill=PALE_CYAN, width=8 - ring)
    for bead in range(6):
        x, y = point(center, 214, rotation + bead * 60)
        radius = 7 if bead % 2 == phase % 2 else 4
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=PALE_IVORY)


def draw_herb_overlay(overlay: Image.Image, phase: int) -> None:
    draw = ImageDraw.Draw(overlay, "RGBA")
    center = (256, 276)
    expansion = phase * 5
    for ring, base in enumerate((136, 176, 206)):
        rx = base + expansion - ring * 4
        ry = round(rx * 0.42)
        bounds = (center[0] - rx, center[1] - ry, center[0] + rx, center[1] + ry)
        draw.arc(bounds, start=8 + phase * 18, end=170 + phase * 18, fill=CELADON, width=11 - ring * 2)
        draw.arc(bounds, start=188 + phase * 18, end=350 + phase * 18, fill=PALE_CYAN, width=7 - ring)
    for ray in range(6):
        angle = 15 + phase * 9 + ray * 60
        inner = point(center, 64, angle)
        outer = point(center, 180, angle)
        draw.line((inner, outer), fill=(166, 210, 156, 125), width=5)
        diamond(draw, outer, 9 if ray % 2 == phase % 2 else 6, PALE_IVORY)


def draw_sword_overlay(overlay: Image.Image, phase: int) -> None:
    draw = ImageDraw.Draw(overlay, "RGBA")
    center = (256, 256)
    rotation = phase * 17
    for ring, radius in enumerate((158, 202, 226)):
        bounds = (256 - radius, 256 - radius, 256 + radius, 256 + radius)
        start = 25 + rotation + ring * 43
        draw.arc(bounds, start=start, end=start + 82, fill=PALE_CYAN, width=11 - ring * 2)
        draw.arc(bounds, start=start + 120, end=start + 202, fill=TEAL, width=8 - ring)
        draw.arc(bounds, start=start + 240, end=start + 322, fill=PALE_IVORY, width=6)
    vertices = [point(center, 206, rotation + index * 120 - 90) for index in range(3)]
    draw.line((vertices[0], vertices[1], vertices[2], vertices[0]), fill=(129, 210, 205, 110), width=5)
    for index, vertex in enumerate(vertices):
        radius = 8 if index == phase % 3 else 5
        draw.ellipse(
            (vertex[0] - radius, vertex[1] - radius, vertex[0] + radius, vertex[1] + radius),
            fill=PALE_IVORY,
        )


def build_master_frame(event: str, core: Image.Image, phase: int) -> Image.Image:
    overlay = Image.new("RGBA", (MASTER_CELL_SIZE, MASTER_CELL_SIZE), (0, 0, 0, 0))
    if event == "wind-vein-riot":
        draw_wind_overlay(overlay, phase)
    elif event == "herb-garden-revival":
        draw_herb_overlay(overlay, phase)
    else:
        draw_sword_overlay(overlay, phase)

    rendered = render_core(core, 338 if event != "herb-garden-revival" else 352)
    overlay.alpha_composite(
        rendered,
        ((MASTER_CELL_SIZE - rendered.width) // 2, (MASTER_CELL_SIZE - rendered.height) // 2),
    )
    return overlay


def build_atlases(event: str, core: Image.Image) -> tuple[Image.Image, Image.Image]:
    master = Image.new("RGBA", (FRAME_COUNT * MASTER_CELL_SIZE, MASTER_CELL_SIZE), (0, 0, 0, 0))
    final = Image.new("RGBA", (FRAME_COUNT * FINAL_CELL_SIZE, FINAL_CELL_SIZE), (0, 0, 0, 0))
    for phase in range(FRAME_COUNT):
        frame = build_master_frame(event, core, phase)
        master.alpha_composite(frame, (phase * MASTER_CELL_SIZE, 0))
        reduced = frame.resize((FINAL_CELL_SIZE, FINAL_CELL_SIZE), Image.Resampling.LANCZOS)
        final.alpha_composite(reduced, (phase * FINAL_CELL_SIZE, 0))
    return master, final


def visible_magenta(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red >= 220 and blue >= 180 and green <= 70
    )


def danger_pixels(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red == 0xE4 and green == 0x5D and blue == 0x45
    )


def cell_metrics(atlas: Image.Image) -> tuple[int, int, float, float]:
    hashes = set()
    minimum_margin = FINAL_CELL_SIZE
    minimum_coverage = 1.0
    maximum_coverage = 0.0
    for index in range(FRAME_COUNT):
        cell = atlas.crop(
            (index * FINAL_CELL_SIZE, 0, (index + 1) * FINAL_CELL_SIZE, FINAL_CELL_SIZE)
        )
        hashes.add(hashlib.sha256(cell.tobytes()).digest())
        alpha = cell.getchannel("A")
        thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
        bounds = thresholded.getbbox()
        if bounds is None:
            raise ValueError(f"empty output phase {index}")
        minimum_margin = min(
            minimum_margin,
            bounds[0],
            bounds[1],
            FINAL_CELL_SIZE - bounds[2],
            FINAL_CELL_SIZE - bounds[3],
        )
        covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
        coverage = covered / (FINAL_CELL_SIZE * FINAL_CELL_SIZE)
        minimum_coverage = min(minimum_coverage, coverage)
        maximum_coverage = max(maximum_coverage, coverage)
    return len(hashes), minimum_margin, minimum_coverage, maximum_coverage


def main() -> None:
    args = parse_args()
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for event in EVENTS:
        transparent_path = args.working_dir / f"{event}-core-transparent.png"
        transparent = Image.open(transparent_path).convert("RGBA")
        source_master = transparent.resize(
            (SOURCE_MASTER_SIZE, SOURCE_MASTER_SIZE), Image.Resampling.LANCZOS
        )
        core = crop_visible(transparent)
        master, final = build_atlases(event, core)
        unique, margin, minimum_coverage, maximum_coverage = cell_metrics(final)
        if unique != FRAME_COUNT:
            raise ValueError(f"{event} must contain four unique phase frames, got {unique}")
        if margin < FINAL_SAFE_MARGIN:
            raise ValueError(f"{event} safe margin {margin} is below {FINAL_SAFE_MARGIN}")
        magenta = visible_magenta(final)
        danger = danger_pixels(final)
        if magenta:
            raise ValueError(f"{event} contains {magenta} visible magenta pixels")
        if danger:
            raise ValueError(f"{event} contains {danger} exact P0 danger-red pixels")

        source_master_path = args.working_dir / f"{event}-source-master.png"
        atlas_master_path = args.working_dir / f"{event}-phase-atlas-master.png"
        final_path = args.final_dir / f"{event}-phase-atlas.png"
        source_master.save(source_master_path, optimize=True)
        master.save(atlas_master_path, optimize=True)
        final.save(final_path, optimize=True)
        print(
            f"event={event} source={transparent.size} source_master={source_master.size} "
            f"atlas_master={master.size} final={final.size} frames={FRAME_COUNT} unique={unique} "
            f"margin={margin} coverage={minimum_coverage:.4f}-{maximum_coverage:.4f} "
            f"magenta={magenta} danger={danger} output={final_path}"
        )


if __name__ == "__main__":
    main()
