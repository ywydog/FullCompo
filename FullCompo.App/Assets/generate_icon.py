#!/usr/bin/env python3
"""生成一个蓝紫色渐变风格的默认 ICO 图标"""

from PIL import Image, ImageDraw
import math

SIZE = 256

img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# 圆角矩形背景
radius = 56
x1, y1 = 8, 8
x2, y2 = SIZE - 8, SIZE - 8

# 绘制圆角矩形（模拟渐变）
for y in range(y1, y2):
    ratio = (y - y1) / (y2 - y1)
    r = int(100 + ratio * 80)
    g = int(150 + ratio * 60)
    b = int(255)
    a = int(230)
    draw.line([(x1 + radius, y), (x2 - radius, y)], fill=(r, g, b, a))

# 绘制圆角部分（简化处理：用椭圆填充四个角）
for cx, cy in [(x1 + radius, y1 + radius), (x2 - radius, y1 + radius),
               (x1 + radius, y2 - radius), (x2 - radius, y2 - radius)]:
    draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius],
                 fill=(120, 160, 255, 230))

# 绘制波浪装饰线
for i in range(3):
    y_base = 70 + i * 60
    points = []
    for x in range(x1 + 20, x2 - 20, 4):
        wave = math.sin((x / SIZE) * math.pi * 3 + i) * 12
        points.append((x, y_base + wave))
    if len(points) > 1:
        for j in range(len(points) - 1):
            draw.line([points[j], points[j + 1]], fill=(255, 255, 255, 180), width=4)

# 绘制加号
plus_x, plus_y = x2 - 50, y1 + 50
plus_size = 24
draw.rectangle([plus_x - plus_size, plus_y - 4, plus_x + plus_size, plus_y + 4], fill=(255, 255, 255, 220))
draw.rectangle([plus_x - 4, plus_y - plus_size, plus_x + 4, plus_y + plus_size], fill=(255, 255, 255, 220))

# 绘制两个矩形块（模拟组件）
# 左上矩形
r1_x1, r1_y1 = x1 + 40, y1 + 80
r1_x2, r1_y2 = r1_x1 + 80, r1_y1 + 50
draw.rounded_rectangle([r1_x1, r1_y1, r1_x2, r1_y2], radius=12, fill=(255, 255, 255, 200))

# 左下矩形
r2_x1, r2_y1 = x1 + 40, r1_y2 + 20
r2_x2, r2_y2 = r2_x1 + 80, r2_y1 + 50
draw.rounded_rectangle([r2_x1, r2_y1, r2_x2, r2_y2], radius=12, fill=(255, 255, 255, 200))

# 右侧大矩形
r3_x1, r3_y1 = r1_x2 + 20, r1_y1
r3_x2, r3_y2 = x2 - 40, r2_y2
draw.rounded_rectangle([r3_x1, r3_y1, r3_x2, r3_y2], radius=16, fill=(255, 200, 220, 200))

# 保存为多尺寸 ICO
sizes = [256, 128, 64, 48, 32, 16]
ico_images = []
for s in sizes:
    ico_img = img.resize((s, s), Image.LANCZOS)
    ico_images.append(ico_img)

ico_images[0].save(
    "/workspace/FullCompo.App/Assets/logo.ico",
    format="ICO",
    sizes=[(s, s) for s in sizes],
    append_images=ico_images[1:]
)

print("Generated /workspace/FullCompo.App/Assets/logo.ico")
