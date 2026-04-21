import os
from PIL import Image

image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'

count = 0
for filename in os.listdir(image_dir):
    if filename.endswith(".png"):
        filepath = os.path.join(image_dir, filename)
        try:
            with Image.open(filepath) as img:
                # convert to RGB to ensure jpeg/webp can be saved as png cleanly
                rgb_im = img.convert('RGB')
                rgb_im.save(filepath, format='PNG')
                count += 1
        except Exception as e:
            print(f"Failed to convert {filename}: {e}")

print(f"Successfully ensured {count} files are valid PNGs.")
