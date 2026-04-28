import os
import shutil

src_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'
dst_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\images\products'

if not os.path.exists(dst_dir):
    os.makedirs(dst_dir)

count = 0
for filename in os.listdir(src_dir):
    if filename.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
        src_file = os.path.join(src_dir, filename)
        dst_file = os.path.join(dst_dir, filename)
        shutil.copy2(src_file, dst_file)
        count += 1

print(f"Copied {count} files to {dst_dir}")
