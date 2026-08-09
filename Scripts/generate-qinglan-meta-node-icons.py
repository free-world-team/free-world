#!/usr/bin/env python3
"""Generate deterministic first-party vector-raster icons for twelve Qinglan meta nodes."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageDraw


SOURCE_SIZE = 1024
FINAL_SIZE = 128
ALPHA_THRESHOLD = 16
SOURCE_SAFE_MARGIN = 80
FINAL_SAFE_MARGIN = 10
BACKGROUND = (17, 45, 53, 238)
INNER = (24, 74, 82, 235)
IVORY = (244, 239, 216, 255)
MUTED = (152, 177, 171, 255)
TERMINAL = (232, 197, 106, 255)
BRANCH_COLORS = {
    "innate": (66, 200, 194, 255),
    "movement": (120, 183, 208, 255),
    "mind": (214, 165, 83, 255),
}
NODES = tuple((branch, number) for branch in ("innate", "movement", "mind") for number in range(1, 5))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def regular_polygon(cx: float, cy: float, radius: float, count: int, rotation: float = 0.0) -> list[tuple[int, int]]:
    return [
        (
            round(cx + math.cos(rotation + index * math.tau / count) * radius),
            round(cy + math.sin(rotation + index * math.tau / count) * radius),
        )
        for index in range(count)
    ]


def closed_line(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: tuple[int, int, int, int], width: int) -> None:
    draw.line(points + [points[0]], fill=fill, width=width, joint="curve")


def draw_terminal_rays(draw: ImageDraw.ImageDraw) -> None:
    rays = (
        [(512, 96), (558, 162), (512, 200), (466, 162)],
        [(928, 512), (862, 558), (824, 512), (862, 466)],
        [(512, 928), (466, 862), (512, 824), (558, 862)],
        [(96, 512), (162, 466), (200, 512), (162, 558)],
    )
    for ray in rays:
        draw.polygon(ray, fill=TERMINAL)


def draw_frame(draw: ImageDraw.ImageDraw, branch: str, terminal: bool) -> None:
    accent = BRANCH_COLORS[branch]
    if branch == "innate":
        outer = regular_polygon(512, 512, 402, 8, math.pi / 8)
        inner = regular_polygon(512, 512, 330, 8, math.pi / 8)
        draw.polygon(outer, fill=BACKGROUND)
        closed_line(draw, outer, IVORY, 28)
        closed_line(draw, inner, accent, 34)
        for x, y in ((512, 144), (880, 512), (512, 880), (144, 512)):
            draw.ellipse((x - 34, y - 34, x + 34, y + 34), fill=accent, outline=IVORY, width=12)
    elif branch == "movement":
        outer = [(512, 110), (900, 512), (512, 914), (124, 512)]
        inner = [(512, 176), (832, 512), (512, 848), (192, 512)]
        draw.polygon(outer, fill=BACKGROUND)
        closed_line(draw, outer, IVORY, 28)
        closed_line(draw, inner, accent, 34)
        anchors = (
            [(512, 118), (550, 180), (474, 180)],
            [(906, 512), (844, 550), (844, 474)],
            [(512, 906), (474, 844), (550, 844)],
            [(118, 512), (180, 474), (180, 550)],
        )
        for anchor in anchors:
            draw.polygon(anchor, fill=accent, outline=IVORY)
    else:
        draw.rounded_rectangle((104, 104, 920, 920), radius=190, fill=BACKGROUND, outline=IVORY, width=28)
        draw.rounded_rectangle((176, 176, 848, 848), radius=142, outline=accent, width=34)
        for x, y in ((176, 176), (848, 176), (848, 848), (176, 848)):
            draw.rectangle((x - 30, y - 30, x + 30, y + 30), fill=accent, outline=IVORY, width=10)
    if terminal:
        draw_terminal_rays(draw)
        draw.ellipse((242, 242, 782, 782), outline=TERMINAL, width=22)


def draw_sword(draw: ImageDraw.ImageDraw, cx: int, top: int, bottom: int, color: tuple[int, int, int, int], width: int = 74) -> None:
    half = width // 2
    draw.polygon([(cx, top), (cx + half, bottom - 120), (cx, bottom - 62), (cx - half, bottom - 120)], fill=color, outline=IVORY)
    draw.line((cx - 100, bottom - 70, cx + 100, bottom - 70), fill=IVORY, width=28)
    draw.line((cx, bottom - 70, cx, bottom + 34), fill=color, width=34)
    draw.ellipse((cx - 28, bottom + 8, cx + 28, bottom + 64), fill=IVORY)


def draw_innate(draw: ImageDraw.ImageDraw, number: int) -> None:
    accent = BRANCH_COLORS["innate"]
    if number == 1:
        draw_sword(draw, 512, 286, 690, accent)
        draw.arc((250, 316, 530, 700), 105, 260, fill=IVORY, width=34)
        draw.arc((494, 316, 774, 700), 280, 75, fill=IVORY, width=34)
    elif number == 2:
        for inset, width in ((290, 30), (346, 26), (404, 22)):
            draw.arc((inset, inset, 1024 - inset, 1024 - inset), 205, 520, fill=accent, width=width)
        for angle in (-42, 0, 42):
            radians = math.radians(angle - 90)
            x1 = 512 + math.cos(radians) * 120
            y1 = 512 + math.sin(radians) * 120
            x2 = 512 + math.cos(radians) * 230
            y2 = 512 + math.sin(radians) * 230
            draw.line((x1, y1, x2, y2), fill=IVORY, width=30)
        draw.ellipse((462, 462, 562, 562), fill=IVORY)
    elif number == 3:
        lens = [(512, 300), (742, 512), (512, 724), (282, 512)]
        closed_line(draw, lens, accent, 42)
        draw.ellipse((398, 398, 626, 626), outline=IVORY, width=30)
        draw_sword(draw, 512, 414, 592, accent, 42)
    else:
        for offset in (-150, 0, 150):
            blade = [(512 + offset, 268), (566 + offset, 552), (512 + offset, 660), (458 + offset, 552)]
            draw.polygon(blade, fill=accent, outline=IVORY)
        draw.arc((282, 330, 742, 790), 190, 350, fill=TERMINAL, width=36)
        draw.polygon([(512, 278), (566, 362), (512, 416), (458, 362)], fill=TERMINAL)


def draw_movement(draw: ImageDraw.ImageDraw, number: int) -> None:
    accent = BRANCH_COLORS["movement"]
    if number == 1:
        boot = [(370, 300), (548, 320), (538, 540), (700, 614), (682, 716), (398, 704), (336, 610)]
        draw.polygon(boot, fill=accent, outline=IVORY)
        draw.arc((292, 560, 730, 816), 190, 350, fill=IVORY, width=28)
        draw.arc((346, 606, 684, 804), 190, 350, fill=accent, width=24)
    elif number == 2:
        draw.arc((286, 286, 738, 738), 35, 325, fill=accent, width=42)
        draw.polygon([(690, 266), (770, 310), (698, 356)], fill=IVORY)
        draw.polygon([(402, 330), (622, 330), (550, 512), (622, 694), (402, 694), (474, 512)], fill=INNER, outline=IVORY)
        draw.ellipse((474, 464, 550, 540), fill=accent)
    elif number == 3:
        draw.line((512, 742, 512, 544), fill=IVORY, width=44)
        draw.line((512, 566, 330, 360), fill=accent, width=44)
        draw.line((512, 566, 512, 304), fill=accent, width=44)
        draw.line((512, 566, 694, 360), fill=accent, width=44)
        for x, y in ((330, 340), (512, 284), (694, 340)):
            draw.ellipse((x - 48, y - 48, x + 48, y + 48), fill=accent, outline=IVORY, width=16)
    else:
        for y, scale in ((652, 1.0), (500, 0.82), (366, 0.64)):
            half = round(176 * scale)
            height = round(98 * scale)
            chevron = [(512 - half, y), (512, y - height), (512 + half, y), (512, y + 52), (512, y - height + 72), (512 - half, y + 52)]
            draw.polygon(chevron, fill=accent, outline=IVORY)


def draw_mind(draw: ImageDraw.ImageDraw, number: int) -> None:
    accent = BRANCH_COLORS["mind"]
    if number == 1:
        draw.ellipse((384, 384, 640, 640), fill=INNER, outline=IVORY, width=30)
        draw.ellipse((468, 468, 556, 556), fill=accent)
        for x, y in ((512, 286), (318, 632), (706, 632)):
            diamond = [(x, y - 64), (x + 54, y), (x, y + 64), (x - 54, y)]
            draw.polygon(diamond, fill=accent, outline=IVORY)
            draw.line((512, 512, x, y), fill=MUTED, width=22)
    elif number == 2:
        for x in (360, 512, 664):
            draw.ellipse((x - 58, 454, x + 58, 570), fill=accent, outline=IVORY, width=18)
        draw.arc((278, 316, 746, 708), 195, 340, fill=IVORY, width=28)
        draw.polygon([(686, 310), (766, 344), (704, 402)], fill=accent)
        draw.arc((278, 316, 746, 708), 15, 160, fill=accent, width=28)
        draw.polygon([(338, 714), (258, 680), (320, 622)], fill=IVORY)
    elif number == 3:
        points = [(312, 704), (400, 624), (466, 662), (532, 548), (612, 574), (682, 442)]
        draw.line(points, fill=accent, width=24)
        for x, y in points[:-1]:
            draw.ellipse((x - 18, y - 18, x + 18, y + 18), fill=IVORY)
        stele = [(626, 322), (738, 322), (764, 628), (600, 628)]
        draw.polygon(stele, fill=INNER, outline=IVORY)
        draw.line((636, 548, 728, 548), fill=accent, width=22)
    else:
        draw.line((512, 726, 512, 512), fill=IVORY, width=38)
        draw.line((512, 532, 340, 350), fill=accent, width=38)
        draw.line((512, 532, 684, 350), fill=TERMINAL, width=38)
        draw.ellipse((278, 260, 402, 384), fill=accent, outline=IVORY, width=18)
        risk = regular_polygon(684, 322, 82, 8, math.pi / 8)
        draw.polygon(risk, fill=TERMINAL, outline=IVORY)
        draw.polygon([(684, 280), (718, 350), (650, 350)], fill=BACKGROUND)


def bounds_metrics(image: Image.Image) -> tuple[int, float]:
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError("generated icon is empty")
    margin = min(bounds[0], bounds[1], image.width - bounds[2], image.height - bounds[3])
    covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
    return margin, covered / (image.width * image.height)


def render(branch: str, number: int) -> Image.Image:
    image = Image.new("RGBA", (SOURCE_SIZE, SOURCE_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw_frame(draw, branch, number == 4)
    if branch == "innate":
        draw_innate(draw, number)
    elif branch == "movement":
        draw_movement(draw, number)
    else:
        draw_mind(draw, number)
    return image


def main() -> None:
    args = parse_args()
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    source_hashes = set()
    final_hashes = set()
    for branch, number in NODES:
        slug = f"{branch}-{number:02d}"
        source = render(branch, number)
        final = source.resize((FINAL_SIZE, FINAL_SIZE), Image.Resampling.LANCZOS)
        source_margin, source_coverage = bounds_metrics(source)
        final_margin, final_coverage = bounds_metrics(final)
        if source_margin < SOURCE_SAFE_MARGIN or final_margin < FINAL_SAFE_MARGIN:
            raise ValueError(f"{slug} violates safe margin budget")
        source_hash = hashlib.sha256(source.tobytes()).hexdigest()
        final_hash = hashlib.sha256(final.tobytes()).hexdigest()
        if source_hash in source_hashes or final_hash in final_hashes:
            raise ValueError(f"{slug} duplicates another node icon")
        source_hashes.add(source_hash)
        final_hashes.add(final_hash)
        source_path = args.source_dir / f"{slug}-source.png"
        final_path = args.final_dir / f"{slug}-icon.png"
        source.save(source_path, optimize=True)
        final.save(final_path, optimize=True)
        print(
            f"node={branch}.{number:02d} source={source.size} source_margin={source_margin} "
            f"source_coverage={source_coverage:.4f} final={final.size} final_margin={final_margin} "
            f"final_coverage={final_coverage:.4f} output={final_path}"
        )

    print(f"unique_source={len(source_hashes)} unique_final={len(final_hashes)}")


if __name__ == "__main__":
    main()
