import os
import re
from PIL import Image, ImageDraw

sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'

if not os.path.exists(image_dir):
    os.makedirs(image_dir)

with open(sql_file, 'r', encoding='utf-8') as f:
    content = f.read()

# Find all local image URLs inserted previously to figure out which images to generate
matches = list(set(re.findall(r"'/image/products/([^']+)'", content)))
print(f"Found {len(matches)} images to generate.")

for filename in matches:
    filepath = os.path.join(image_dir, filename)
    
    # Determine text from filename: remove extension and replace underscores
    text = filename.replace('.png', '').replace('_', ' ')
    
    # Create the image
    img = Image.new('RGB', (300, 300), color = (51, 51, 51))
    d = ImageDraw.Draw(img)
    
    # Try using default font, scale it roughly by placing in center
    # Pillow default font is small, so we just place it.
    # Text length * 6 is roughly the width
    tw = len(text) * 6
    x = (300 - tw) / 2
    if x < 10: x = 10
    
    d.text((x, 140), text, fill=(255,255,255))
    
    img.save(filepath)

print("All images generated successfully!")
