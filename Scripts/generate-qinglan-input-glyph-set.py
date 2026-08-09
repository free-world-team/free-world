#!/usr/bin/env python3
"""Generate deterministic first-party ART-UI-005 cursor, focus, and input glyphs."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageDraw


SOURCE_SIZE = 512
FINAL_SIZE = 128
SEED = 31027
ALPHA_THRESHOLD = 16
NAMES = (
    "cursor.pointer",
    "focus.ring",
    "keyboard.wasd",
    "keyboard.arrows",
    "keyboard.enter",
    "keyboard.escape",
    "keyboard.key-e",
    "keyboard.key-m",
    "keyboard.key-q",
    "keyboard.page-axis",
    "mouse.left-button",
    "mouse.right-button",
    "mouse.scroll",
    "gamepad.left-stick",
    "gamepad.dpad",
    "gamepad.button-south",
    "gamepad.button-east",
    "gamepad.button-north",
    "gamepad.start",
    "gamepad.select",
    "gamepad.left-shoulder",
    "gamepad.right-shoulder",
    "gamepad.left-trigger",
    "gamepad.right-trigger",
)

DEEP = (12, 37, 44, 250)
INK = (22, 61, 69, 255)
TEAL = (89, 199, 193, 255)
PALE_TEAL = (167, 230, 221, 255)
IVORY = (244, 239, 216, 255)
GOLD = (215, 184, 92, 255)
WARM_STONE = (182, 155, 120, 255)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def filename(name: str) -> str:
    return name.replace(".", "-") + ".png"


def keycap(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], border=IVORY) -> None:
    draw.rounded_rectangle(box, radius=34, fill=DEEP, outline=border, width=18)
    x0, y0, x1, y1 = box
    draw.line((x0 + 24, y1 - 28, x1 - 24, y1 - 28), fill=TEAL, width=10)


def symbol_points(letter: str) -> list[list[tuple[float, float]]]:
    symbols = {
        "W": [[(0.05, 0.08), (0.23, 0.92), (0.5, 0.48), (0.77, 0.92), (0.95, 0.08)]],
        "A": [[(0.08, 0.92), (0.5, 0.08), (0.92, 0.92)], [(0.24, 0.62), (0.76, 0.62)]],
        "S": [[(0.88, 0.18), (0.68, 0.08), (0.22, 0.08), (0.08, 0.38), (0.78, 0.58), (0.9, 0.84), (0.68, 0.92), (0.14, 0.92)]],
        "D": [[(0.12, 0.08), (0.12, 0.92), (0.55, 0.92), (0.9, 0.72), (0.9, 0.28), (0.55, 0.08), (0.12, 0.08)]],
        "E": [[(0.82, 0.08), (0.16, 0.08), (0.16, 0.92), (0.82, 0.92)], [(0.16, 0.5), (0.68, 0.5)]],
        "M": [[(0.1, 0.92), (0.1, 0.08), (0.5, 0.52), (0.9, 0.08), (0.9, 0.92)]],
        "Q": [[(0.5, 0.08), (0.22, 0.14), (0.08, 0.5), (0.22, 0.86), (0.5, 0.92), (0.78, 0.86), (0.92, 0.5), (0.78, 0.14), (0.5, 0.08)], [(0.58, 0.62), (0.94, 0.98)]],
    }
    return symbols[letter]


def letter(draw: ImageDraw.ImageDraw, value: str, box: tuple[int, int, int, int], color=IVORY, width: int = 22) -> None:
    x0, y0, x1, y1 = box
    for stroke in symbol_points(value):
        points = [(round(x0 + x * (x1 - x0)), round(y0 + y * (y1 - y0))) for x, y in stroke]
        draw.line(points, fill=color, width=width, joint="curve")


def arrow(draw: ImageDraw.ImageDraw, center: tuple[int, int], direction: str, size: int, color=IVORY) -> None:
    cx, cy = center
    if direction == "up":
        points = [(cx, cy - size), (cx + size, cy + size // 3), (cx + size // 3, cy + size // 3),
                  (cx + size // 3, cy + size), (cx - size // 3, cy + size), (cx - size // 3, cy + size // 3),
                  (cx - size, cy + size // 3)]
    elif direction == "down":
        points = [(cx, cy + size), (cx + size, cy - size // 3), (cx + size // 3, cy - size // 3),
                  (cx + size // 3, cy - size), (cx - size // 3, cy - size), (cx - size // 3, cy - size // 3),
                  (cx - size, cy - size // 3)]
    elif direction == "left":
        points = [(cx - size, cy), (cx + size // 3, cy - size), (cx + size // 3, cy - size // 3),
                  (cx + size, cy - size // 3), (cx + size, cy + size // 3), (cx + size // 3, cy + size // 3),
                  (cx + size // 3, cy + size)]
    else:
        points = [(cx + size, cy), (cx - size // 3, cy - size), (cx - size // 3, cy - size // 3),
                  (cx - size, cy - size // 3), (cx - size, cy + size // 3), (cx - size // 3, cy + size // 3),
                  (cx - size // 3, cy + size)]
    draw.polygon(points, fill=color)


def draw_cursor_pointer(draw: ImageDraw.ImageDraw) -> None:
    points = [(98, 54), (104, 410), (188, 326), (248, 458), (318, 424), (258, 300), (380, 300)]
    draw.polygon(points, fill=IVORY, outline=INK)
    draw.line(points + [points[0]], fill=INK, width=18, joint="curve")
    draw.line((122, 104, 130, 360), fill=TEAL, width=16)


def draw_focus_ring(draw: ImageDraw.ImageDraw) -> None:
    for x, y, sx, sy in ((78, 78, 1, 1), (434, 78, -1, 1), (434, 434, -1, -1), (78, 434, 1, -1)):
        draw.line((x, y + sy * 112, x, y, x + sx * 112, y), fill=IVORY, width=26, joint="curve")
        draw.polygon([(x + sx * 28, y + sy * 28), (x + sx * 72, y + sy * 28), (x + sx * 28, y + sy * 72)], fill=TEAL)
    draw.ellipse((222, 222, 290, 290), fill=GOLD, outline=INK, width=12)


def draw_key_cluster(draw: ImageDraw.ImageDraw, arrows: bool) -> None:
    boxes = ((196, 52, 316, 172), (60, 224, 180, 344), (196, 224, 316, 344), (332, 224, 452, 344))
    values = ("W", "A", "S", "D")
    directions = ("up", "left", "down", "right")
    for index, box in enumerate(boxes):
        keycap(draw, box, GOLD if index == 0 else IVORY)
        if arrows:
            arrow(draw, ((box[0] + box[2]) // 2, (box[1] + box[3]) // 2 - 2), directions[index], 25, IVORY)
        else:
            letter(draw, values[index], (box[0] + 34, box[1] + 30, box[2] - 34, box[3] - 30), IVORY, 13)


def draw_single_key(draw: ImageDraw.ImageDraw, kind: str) -> None:
    box = (70, 104, 442, 408)
    keycap(draw, box)
    if kind in ("E", "M", "Q"):
        letter(draw, kind, (174, 158, 338, 342), GOLD, 30)
    elif kind == "enter":
        draw.line((354, 174, 354, 254, 184, 254), fill=IVORY, width=28, joint="curve")
        arrow(draw, (178, 254), "left", 48, GOLD)
    elif kind == "escape":
        draw.arc((148, 148, 364, 364), 35, 300, fill=IVORY, width=28)
        arrow(draw, (160, 174), "left", 36, GOLD)
        draw.line((220, 220, 310, 310), fill=TEAL, width=18)
    else:  # paired PageUp/PageDown axis
        draw.line((256, 150, 256, 362), fill=TEAL, width=16)
        arrow(draw, (184, 218), "up", 42, IVORY)
        arrow(draw, (328, 294), "down", 42, GOLD)
        draw.line((132, 142, 236, 142), fill=IVORY, width=18)
        draw.line((276, 370, 380, 370), fill=GOLD, width=18)


def mouse_shell(draw: ImageDraw.ImageDraw) -> None:
    draw.rounded_rectangle((142, 50, 370, 462), radius=112, fill=DEEP, outline=IVORY, width=18)
    draw.line((256, 62, 256, 238), fill=INK, width=14)
    draw.line((154, 238, 358, 238), fill=INK, width=14)
    draw.rounded_rectangle((232, 90, 280, 180), radius=22, fill=TEAL, outline=IVORY, width=8)


def draw_mouse(draw: ImageDraw.ImageDraw, kind: str) -> None:
    mouse_shell(draw)
    if kind == "left":
        draw.pieslice((154, 62, 256, 228), 180, 360, fill=GOLD)
        arrow(draw, (205, 144), "down", 22, DEEP)
    elif kind == "right":
        draw.pieslice((256, 62, 358, 228), 180, 360, fill=GOLD)
        arrow(draw, (307, 144), "down", 22, DEEP)
    else:
        draw.rounded_rectangle((220, 76, 292, 198), radius=30, fill=GOLD, outline=IVORY, width=10)
        arrow(draw, (256, 100), "up", 16, DEEP)
        arrow(draw, (256, 174), "down", 16, DEEP)


def draw_left_stick(draw: ImageDraw.ImageDraw) -> None:
    draw.ellipse((74, 74, 438, 438), fill=DEEP, outline=IVORY, width=20)
    draw.ellipse((154, 154, 358, 358), fill=TEAL, outline=INK, width=18)
    draw.ellipse((200, 200, 312, 312), fill=GOLD, outline=IVORY, width=14)
    for direction, center in (("up", (256, 112)), ("down", (256, 400)), ("left", (112, 256)), ("right", (400, 256))):
        arrow(draw, center, direction, 18, IVORY)


def draw_dpad(draw: ImageDraw.ImageDraw) -> None:
    points = [(190, 58), (322, 58), (322, 190), (454, 190), (454, 322), (322, 322),
              (322, 454), (190, 454), (190, 322), (58, 322), (58, 190), (190, 190)]
    draw.polygon(points, fill=DEEP, outline=IVORY)
    draw.line(points + [points[0]], fill=IVORY, width=18, joint="curve")
    for direction, center in (("up", (256, 124)), ("down", (256, 388)), ("left", (124, 256)), ("right", (388, 256))):
        arrow(draw, center, direction, 24, TEAL if direction in ("left", "right") else GOLD)


def draw_face_cluster(draw: ImageDraw.ImageDraw, selected: str) -> None:
    positions = {"north": (256, 104), "east": (408, 256), "south": (256, 408), "west": (104, 256)}
    for name, (x, y) in positions.items():
        fill = GOLD if name == selected else DEEP
        outline = IVORY if name == selected else TEAL
        radius = 54 if name == selected else 42
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill, outline=outline, width=16)
        if name == selected:
            direction = "up" if name == "north" else "down" if name == "south" else "right" if name == "east" else "left"
            arrow(draw, (x, y), direction, 18, DEEP)
    draw.ellipse((224, 224, 288, 288), outline=IVORY, width=12)


def controller_body(draw: ImageDraw.ImageDraw) -> None:
    draw.rounded_rectangle((62, 142, 450, 398), radius=112, fill=DEEP, outline=IVORY, width=18)
    draw.ellipse((136, 222, 224, 310), outline=TEAL, width=16)
    draw.ellipse((306, 214, 350, 258), fill=TEAL)
    draw.ellipse((358, 266, 402, 310), fill=GOLD)


def draw_menu_control(draw: ImageDraw.ImageDraw, kind: str) -> None:
    controller_body(draw)
    if kind == "start":
        draw.rounded_rectangle((222, 212, 292, 284), radius=18, outline=IVORY, width=12)
        arrow(draw, (257, 248), "right", 18, GOLD)
    else:
        draw.rounded_rectangle((204, 222, 252, 270), radius=10, fill=GOLD, outline=IVORY, width=8)
        draw.rounded_rectangle((266, 238, 314, 286), radius=10, fill=TEAL, outline=IVORY, width=8)


def draw_top_control(draw: ImageDraw.ImageDraw, side: str, trigger: bool) -> None:
    draw.arc((62, 176, 450, 500), 185, 355, fill=IVORY, width=22)
    draw.line((116, 330, 84, 430), fill=TEAL, width=24)
    draw.line((396, 330, 428, 430), fill=TEAL, width=24)
    selected = (78, 78, 244, 188) if side == "left" else (268, 78, 434, 188)
    other = (268, 90, 434, 174) if side == "left" else (78, 90, 244, 174)
    draw.rounded_rectangle(other, radius=28, fill=DEEP, outline=TEAL, width=14)
    if trigger:
        x0, y0, x1, y1 = selected
        draw.polygon([(x0 + 24, y0), (x1 - 18, y0), (x1 - 38, y1), (x0 + 50, y1 - 18)],
                     fill=GOLD, outline=IVORY)
        draw.line((x0 + 58, y0 + 38, x1 - 54, y1 - 42), fill=DEEP, width=14)
    else:
        draw.rounded_rectangle(selected, radius=34, fill=GOLD, outline=IVORY, width=16)
        x0, y0, x1, y1 = selected
        draw.line((x0 + 36, y1 - 36, x1 - 36, y1 - 36), fill=DEEP, width=14)
    arrow(draw, (142 if side == "left" else 370, 284), "left" if side == "left" else "right", 24, IVORY)


def render(name: str) -> Image.Image:
    image = Image.new("RGBA", (SOURCE_SIZE, SOURCE_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if name == "cursor.pointer": draw_cursor_pointer(draw)
    elif name == "focus.ring": draw_focus_ring(draw)
    elif name == "keyboard.wasd": draw_key_cluster(draw, False)
    elif name == "keyboard.arrows": draw_key_cluster(draw, True)
    elif name.startswith("keyboard."):
        token = name.split(".", 1)[1]
        draw_single_key(draw, {"key-e": "E", "key-m": "M", "key-q": "Q", "page-axis": "page"}.get(token, token))
    elif name.startswith("mouse."): draw_mouse(draw, name.split(".", 1)[1].replace("-button", ""))
    elif name == "gamepad.left-stick": draw_left_stick(draw)
    elif name == "gamepad.dpad": draw_dpad(draw)
    elif name.startswith("gamepad.button-"): draw_face_cluster(draw, name.rsplit("-", 1)[1])
    elif name in ("gamepad.start", "gamepad.select"): draw_menu_control(draw, name.split(".", 1)[1])
    elif name.endswith("shoulder"): draw_top_control(draw, "left" if ".left-" in name else "right", False)
    else: draw_top_control(draw, "left" if ".left-" in name else "right", True)
    return image


def pixel_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def grayscale_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.convert("LA").tobytes()).hexdigest()


def metrics(image: Image.Image) -> tuple[int, float, int, int]:
    pixels = list(image.get_flattened_data())
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError("glyph is empty")
    margin = min(bounds[0], bounds[1], image.width - bounds[2], image.height - bounds[3])
    covered = sum(1 for _, _, _, a in pixels if a >= ALPHA_THRESHOLD)
    magenta = sum(1 for r, g, b, a in pixels if a >= 16 and r >= 220 and b >= 180 and g <= 70)
    danger = sum(1 for r, g, b, a in pixels if a >= 16 and (r, g, b) == (228, 93, 69))
    return margin, covered / (image.width * image.height), magenta, danger


def validate(images: list[Image.Image], expected_size: int, label: str) -> None:
    minimum_margin = 8 if expected_size == FINAL_SIZE else 32
    color_hashes: set[str] = set()
    gray_hashes: set[str] = set()
    for name, image in zip(NAMES, images):
        if image.size != (expected_size, expected_size):
            raise ValueError(f"{label} {name} has unexpected size {image.size}")
        margin, coverage, magenta, danger = metrics(image)
        if margin < minimum_margin:
            raise ValueError(f"{label} {name} margin {margin} is below {minimum_margin}")
        if not 0.035 <= coverage <= 0.62:
            raise ValueError(f"{label} {name} coverage {coverage:.4f} is outside 0.035..0.62")
        if magenta or danger:
            raise ValueError(f"{label} {name} contains a reserved forbidden color")
        if image.getpixel((0, 0))[3] or image.getpixel((expected_size - 1, expected_size - 1))[3]:
            raise ValueError(f"{label} {name} has a nontransparent corner")
        color_hashes.add(pixel_hash(image))
        gray_hashes.add(grayscale_hash(image))
        print(f"{label} glyph={name} margin={margin} coverage={coverage:.4f}")
    if len(color_hashes) != len(NAMES) or len(gray_hashes) != len(NAMES):
        raise ValueError(f"{label} glyphs are not unique in color and grayscale")


def main() -> None:
    args = parse_args()
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    sources = [render(name) for name in NAMES]
    finals = [source.resize((FINAL_SIZE, FINAL_SIZE), Image.Resampling.LANCZOS) for source in sources]
    validate(sources, SOURCE_SIZE, "source")
    validate(finals, FINAL_SIZE, "final")
    for name, source, final in zip(NAMES, sources, finals):
        source.save(args.source_dir / filename(name), optimize=True)
        final.save(args.final_dir / filename(name), optimize=True)
    print(f"source_set_sha256={hashlib.sha256(''.join(pixel_hash(image) for image in sources).encode()).hexdigest()}")
    print(f"final_set_sha256={hashlib.sha256(''.join(pixel_hash(image) for image in finals).encode()).hexdigest()}")
    print(f"glyph_count={len(NAMES)}")


if __name__ == "__main__":
    main()
