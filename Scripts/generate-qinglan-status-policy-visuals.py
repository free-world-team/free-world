#!/usr/bin/env python3
"""Generate nine deterministic Qinglan status and damage-policy visuals."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image, ImageDraw, ImageFilter


Point = tuple[float, float]
Color = tuple[int, int, int, int]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def rgba(value: str, alpha: int = 255) -> Color:
    value = value.lstrip("#")
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4)) + (alpha,)


def polar(center: Point, radii: Point, angle: float) -> Point:
    radians = math.radians(angle)
    return center[0] + math.cos(radians) * radii[0], center[1] + math.sin(radians) * radii[1]


def rotate(center: Point, points: Sequence[Point], angle: float) -> list[Point]:
    radians = math.radians(angle)
    cosine, sine = math.cos(radians), math.sin(radians)
    return [(center[0] + x * cosine - y * sine, center[1] + x * sine + y * cosine) for x, y in points]


class Canvas:
    def __init__(self, size: int, palette: dict[str, str]) -> None:
        self.scale = size / 1024
        self.image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.g = ImageDraw.Draw(self.glow)
        self.c = ImageDraw.Draw(self.core)
        self.primary = rgba(palette["primary"])
        self.highlight = rgba(palette["highlight"])
        self.deep = rgba(palette["deep"])
        self.secondary = rgba(palette["secondary"])

    def p(self, point: Point) -> Point:
        return point[0] * self.scale, point[1] * self.scale

    def line(self, points: Iterable[Point], width: int, ink: Color | None = None) -> None:
        points = [self.p(point) for point in points]
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        self.g.line(points, fill=ink[:3] + (90,), width=width + round(26 * self.scale), joint="curve")
        self.c.line(points, fill=self.deep, width=width + round(9 * self.scale), joint="curve")
        self.c.line(points, fill=ink, width=width, joint="curve")

    def arc(self, center: Point, radii: Point, start: float, end: float, width: int, ink: Color | None = None) -> None:
        center, radii = self.p(center), self.p(radii)
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        box = (center[0] - radii[0], center[1] - radii[1], center[0] + radii[0], center[1] + radii[1])
        self.g.arc(box, start, end, fill=ink[:3] + (90,), width=width + round(26 * self.scale))
        self.c.arc(box, start, end, fill=self.deep, width=width + round(9 * self.scale))
        self.c.arc(box, start, end, fill=ink, width=width)

    def polygon(self, points: Iterable[Point], ink: Color | None = None) -> None:
        points = [self.p(point) for point in points]
        ink = ink or self.primary
        center = (sum(x for x, _ in points) / len(points), sum(y for _, y in points) / len(points))
        expanded = [(center[0] + (x - center[0]) * 1.14, center[1] + (y - center[1]) * 1.14) for x, y in points]
        self.g.polygon(expanded, fill=ink[:3] + (96,))
        self.c.polygon(expanded, fill=self.deep)
        self.c.polygon(points, fill=ink)

    def ellipse(self, box: tuple[float, float, float, float], ink: Color | None = None) -> None:
        box = tuple(value * self.scale for value in box)
        ink = ink or self.primary
        grow = 12 * self.scale
        glow_box = (box[0] - grow, box[1] - grow, box[2] + grow, box[3] + grow)
        self.g.ellipse(glow_box, fill=ink[:3] + (96,))
        self.c.ellipse(glow_box, fill=self.deep)
        self.c.ellipse(box, fill=ink)

    def diamond(self, center: Point, radius: float, ink: Color | None = None) -> None:
        self.polygon([(center[0], center[1] - radius), (center[0] + radius * .72, center[1]),
                      (center[0], center[1] + radius), (center[0] - radius * .72, center[1])], ink or self.highlight)

    def finish(self) -> Image.Image:
        self.image.alpha_composite(self.glow.filter(ImageFilter.GaussianBlur(round(14 * self.scale))))
        self.image.alpha_composite(self.core)
        return self.image


def burning(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    c.polygon([(512, 168), (620, 350), (596, 462), (704, 392), (752, 584), (676, 790),
               (512, 858), (348, 790), (272, 584), (356, 398), (420, 492), (402, 338)], c.primary)
    c.polygon([(512, 388), (588, 520), (556, 604), (628, 574), (616, 706), (512, 776),
               (408, 706), (414, 582), (468, 628), (452, 510)], c.highlight)
    for x, y in ((290, 322), (704, 284), (774, 468)):
        c.diamond((x, y), 18, c.secondary)
    return c.finish()


def poisoned(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    drops = [((512, 480), 190), ((320, 660), 105), ((704, 660), 105)]
    for (x, y), radius in drops:
        c.polygon([(x, y - radius), (x + radius * .72, y + radius * .16), (x + radius * .52, y + radius * .7),
                   (x, y + radius), (x - radius * .52, y + radius * .7), (x - radius * .72, y + radius * .16)],
                  c.primary if radius > 150 else c.secondary)
    c.arc((512, 540), (92, 92), 12 + phase, 286 + phase, 18, c.highlight)
    for x, y, r in ((330, 300, 30), (690, 330, 42), (770, 500, 24)):
        c.ellipse((x - r, y - r, x + r, y + r), c.highlight)
    return c.finish()


def slowed(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    center = (512, 512)
    for ring, radii in enumerate(((340, 286), (260, 210), (178, 142))):
        for start in (20 + ring * 12, 200 + ring * 12):
            c.arc(center, radii, start + phase, start + 128 + phase, 22 - ring * 4,
                  c.primary if ring != 1 else c.secondary)
    for y in (390, 512, 634):
        c.line([(690, y), (500, y)], 18, c.highlight)
        c.polygon([(500, y), (560, y - 42), (560, y + 42)], c.highlight)
    return c.finish()


def rooted(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    c.ellipse((432, 232, 592, 404), c.highlight)
    c.line([(512, 360), (512, 650)], 34, c.primary)
    roots = [[(512, 560), (400, 650), (288, 796)], [(512, 590), (470, 700), (426, 850)],
             [(512, 590), (554, 700), (598, 850)], [(512, 560), (624, 650), (736, 796)]]
    for index, points in enumerate(roots):
        c.line(points, 22 - index % 2 * 4, c.primary if index % 2 == 0 else c.secondary)
        c.polygon(rotate(points[-1], [(-20, 12), (0, -44), (20, 12)], -20 + index * 15), c.highlight)
    c.line([(344, 470), (680, 470)], 18, c.secondary)
    c.line([(374, 530), (650, 530)], 12, c.secondary)
    return c.finish()


def armor_broken(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    shield = [(512, 158), (746, 252), (712, 630), (624, 778), (512, 858), (400, 778), (312, 630), (278, 252)]
    c.polygon(shield, c.primary)
    inner = [(512, 230), (676, 296), (648, 598), (584, 704), (512, 758), (440, 704), (376, 598), (348, 296)]
    c.polygon(inner, c.secondary)
    c.line([(548, 250), (466, 414), (554, 492), (448, 640), (522, 758)], 32, c.highlight)
    c.line([(466, 414), (392, 372)], 18, c.highlight)
    c.line([(554, 492), (640, 438)], 18, c.highlight)
    return c.finish()


def marked(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    center = (512, 512)
    for ring, radii in enumerate(((330, 280), (248, 204), (162, 128))):
        for start in (8, 98, 188, 278):
            c.arc(center, radii, start + phase + ring * 6, start + 58 + phase + ring * 6, 16 - ring * 3,
                  c.primary if ring != 1 else c.secondary)
    c.diamond(center, 94, c.highlight)
    for angle in range(0, 360, 60):
        c.diamond(polar(center, (370, 312), angle), 18, c.secondary)
    return c.finish()


def damage_immunity(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    center = (512, 512)
    for side in range(6):
        start = -90 + side * 60 + 5
        end = -90 + (side + 1) * 60 - 5
        c.line([polar(center, (342, 342), start), polar(center, (342, 342), end)], 28, c.primary)
        c.diamond(polar(center, (386, 386), (start + end) * .5), 19, c.highlight)
    c.ellipse((376, 376, 648, 648), c.secondary)
    c.ellipse((424, 424, 600, 600), c.highlight)
    c.line([(512, 318), (512, 706)], 18, c.deep)
    c.line([(350, 512), (674, 512)], 18, c.deep)
    return c.finish()


def contact_protection(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    center = (512, 512)
    for start in (18, 108, 198, 288):
        c.arc(center, (334, 280), start + phase, start + 58 + phase, 18, c.primary)
    c.polygon([(248, 390), (420, 390), (466, 512), (420, 634), (248, 634), (318, 512)], c.secondary)
    c.polygon([(776, 390), (604, 390), (558, 512), (604, 634), (776, 634), (706, 512)], c.secondary)
    c.line([(466, 512), (558, 512)], 22, c.highlight)
    for angle in (60, 120, 240, 300):
        c.diamond(polar(center, (382, 320), angle), 16, c.highlight)
    return c.finish()


def boss_hazard(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    c = Canvas(size, palette)
    c.polygon([(512, 132), (852, 792), (172, 792)], c.primary)
    c.polygon([(512, 242), (754, 724), (270, 724)], c.secondary)
    c.polygon([(512, 330), (606, 630), (512, 702), (418, 630)], c.highlight)
    for angle in (210, 270, 330):
        point = polar((512, 512), (374, 374), angle)
        c.polygon(rotate(point, [(-22, 24), (0, -54), (22, 24)], angle + 90), c.highlight)
    c.line([(340, 820), (684, 820)], 18, c.deep)
    return c.finish()


BUILDERS = {
    "burning": burning,
    "poisoned": poisoned,
    "slowed": slowed,
    "rooted": rooted,
    "armor-broken": armor_broken,
    "marked": marked,
    "damage-immunity": damage_immunity,
    "contact-protection": contact_protection,
    "boss-hazard": boss_hazard,
}


def main() -> None:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    source_size, runtime_size = int(spec["sourceSize"]), int(spec["runtimeSize"])
    phase = float(int(spec["seed"]) % 7)
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for visual in spec["visuals"]:
        image = BUILDERS[visual["name"]](source_size, visual["palette"], phase)
        source_path, final_path = args.source_dir / visual["sourceFile"], args.final_dir / visual["finalFile"]
        image.save(source_path, optimize=True)
        runtime = image.resize((runtime_size, runtime_size), Image.Resampling.LANCZOS)
        runtime.save(final_path, optimize=True)
        coverage = sum(runtime.getchannel("A").histogram()[16:]) / (runtime_size * runtime_size)
        print(f"visual={visual['name']} source={source_path} final={final_path} coverage={coverage:.4f}")


if __name__ == "__main__":
    main()
