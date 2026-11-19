#!/usr/bin/env python3
"""Simple 256x256 RGBA placeholder parts, aligned to one head template. Tintable
layers are neutral grey or white so a multiply tint works. Real art comes later."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
SIZE = 256
CX, CY = 128, 108  # head centre
GREY = (200, 200, 200, 255)
WHITE = (240, 240, 240, 255)
INK = (40, 40, 40, 255)
CLEAR = (0, 0, 0, 0)


def canvas():
    return Image.new("RGBA", (SIZE, SIZE), CLEAR)


def save(img, folder, name):
    dest = ROOT / folder
    dest.mkdir(parents=True, exist_ok=True)
    path = dest / f"{name}.png"
    img.save(path, "PNG")
    print(path.relative_to(ROOT))


def background(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    if variant == "01":
        d.rectangle((0, 0, 255, 255), fill=(210, 214, 220, 255))
    else:
        d.ellipse((8, 8, 247, 247), fill=(186, 196, 206, 255))
        d.rectangle((0, 200, 255, 255), fill=(170, 178, 186, 255))
    save(img, "background", variant)


def back_hair(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    if variant == "01":
        d.ellipse((CX - 70, CY - 80, CX + 70, CY + 50), fill=GREY)
    else:
        d.pieslice((CX - 80, CY - 40, CX - 10, CY + 90), 90, 270, fill=GREY)
        d.pieslice((CX + 10, CY - 40, CX + 80, CY + 90), 270, 90, fill=GREY)
    save(img, "back-hair", variant)


def neck(_variant="01"):
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rectangle((CX - 22, CY + 50, CX + 22, CY + 110), fill=GREY)
    save(img, "neck", "01")


def uniform(role, mark):
    img = canvas()
    d = ImageDraw.Draw(img)
    # shoulders / torso
    d.polygon([(30, 255), (70, 170), (186, 170), (226, 255)], fill=GREY)
    # collar
    d.polygon([(88, 170), (CX, 200), (168, 170)], fill=WHITE)
    # a small role mark so files differ
    x0, y0 = 118 + mark, 220
    d.rectangle((x0, y0, x0 + 18, y0 + 18), fill=WHITE)
    save(img, "uniform", role)


def head(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    if variant == "01":
        d.ellipse((CX - 52, CY - 64, CX + 52, CY + 64), fill=GREY)
    else:
        d.ellipse((CX - 48, CY - 70, CX + 48, CY + 58), fill=GREY)
    save(img, "head", variant)


def eyes(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    dy = 0 if variant == "01" else 6
    gap = 22 if variant == "01" else 18
    for sign in (-1, 1):
        x = CX + sign * gap
        d.ellipse((x - 10, CY - 8 + dy, x + 10, CY + 8 + dy), fill=GREY)
        d.ellipse((x - 3, CY - 3 + dy, x + 3, CY + 3 + dy), fill=INK)
    save(img, "eyes", variant)


def nose_mouth(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    if variant == "01":
        d.line((CX, CY + 4, CX, CY + 22), fill=INK, width=3)
        d.arc((CX - 16, CY + 16, CX + 16, CY + 36), 20, 160, fill=INK, width=3)
    else:
        d.polygon([(CX, CY + 2), (CX - 6, CY + 20), (CX + 6, CY + 20)], outline=INK)
        d.line((CX - 14, CY + 30, CX + 14, CY + 30), fill=INK, width=3)
    save(img, "nose-mouth", variant)


def hair(variant):
    img = canvas()
    d = ImageDraw.Draw(img)
    if variant == "01":
        d.pieslice((CX - 54, CY - 80, CX + 54, CY + 10), 200, 340, fill=GREY)
    else:
        d.ellipse((CX - 58, CY - 78, CX + 58, CY - 10), fill=GREY)
        d.rectangle((CX + 30, CY - 20, CX + 58, CY + 40), fill=GREY)
    save(img, "hair", variant)


def extras(_="01"):
    img = canvas()
    d = ImageDraw.Draw(img)
    d.arc((CX - 22 - 22, CY - 12, CX - 22 + 12, CY + 10), 0, 360, fill=INK, width=3)
    d.arc((CX + 22 - 12, CY - 12, CX + 22 + 22, CY + 10), 0, 360, fill=INK, width=3)
    d.line((CX - 10, CY - 1, CX + 10, CY - 1), fill=INK, width=3)
    save(img, "extras", "01")


def insignia(_="01"):
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(CX, 200), (CX + 16, 216), (CX, 232), (CX - 16, 216)], fill=WHITE)
    save(img, "insignia", "01")


def badge(_="01"):
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse((178, 200, 210, 232), fill=(180, 160, 60, 255), outline=INK, width=2)
    save(img, "badge", "01")


def main():
    background("01")
    background("02")
    back_hair("01")
    back_hair("02")
    neck()
    for i, role in enumerate([
        "captain", "administrator", "industry-head", "scientist",
        "lab-technician", "civilian", "candidate",
    ]):
        uniform(role, i)
    head("01")
    head("02")
    eyes("01")
    eyes("02")
    nose_mouth("01")
    nose_mouth("02")
    hair("01")
    hair("02")
    extras()
    insignia()
    badge()


if __name__ == "__main__":
    main()
