import os
import re
import urllib.request
from ddgs import DDGS
import time

"""Download product images from DuckDuckGo.

This script was adapted for the current Webstore project.
It supports downloading multiple images per product (for the Product Image Gallery)
by saving files with a shared base prefix: BaseName__01.jpg ... BaseName__05.jpg.

Default behavior remains backward compatible: it will ensure the original filename exists.
"""

sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\SQL\setup_database.sql'
image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\images\products'

PER_PRODUCT = int(os.environ.get('PER_PRODUCT', '1'))  # set to 5 to download gallery images
MAX_RESULTS = int(os.environ.get('MAX_RESULTS', '20'))
SLEEP_SEC = float(os.environ.get('SLEEP_SEC', '1.0'))
MIN_BYTES = int(os.environ.get('MIN_BYTES', '25000'))

if not os.path.exists(image_dir):
    os.makedirs(image_dir)

with open(sql_file, 'r', encoding='utf-8') as f:
    content = f.read()

matches = list(set(re.findall(r"'/image/products/([^']+)'", content)))
print(f"Found {len(matches)} images to search and download.", flush=True)

for i, filename in enumerate(matches):
    filepath = os.path.join(image_dir, filename)
    base = os.path.splitext(filename)[0]
    search_term = base.replace('_', ' ')
    print(f"[{i+1}/{len(matches)}] Searching: {search_term}", flush=True)
    
    try:
        with DDGS() as ddgs:
            results = list(ddgs.images(search_term + " product", max_results=MAX_RESULTS))

        def download_to(path, url):
            try:
                req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
                with urllib.request.urlopen(req, timeout=15) as response:
                    data = response.read()
                    if len(data) < MIN_BYTES:
                        return False
                    with open(path, 'wb') as out_file:
                        out_file.write(data)
                    return True
            except Exception:
                return False

        # 1) Ensure the original filename exists (important for Product.ImageUrl in DB)
        if (not os.path.exists(filepath)) or os.path.getsize(filepath) == 0:
            success_main = False
            for res in results:
                url = res.get('image') or res.get('thumbnail')
                if not url:
                    continue
                if download_to(filepath, url):
                    print(f"  -> Downloaded main: {filename}", flush=True)
                    success_main = True
                    break
            if not success_main:
                print(f"  !!! Failed to download main image.", flush=True)

        # 2) Optionally download extra gallery images with same base prefix
        if PER_PRODUCT > 1:
            saved = 0
            for n in range(1, PER_PRODUCT + 1):
                extra_name = f"{base}__{n:02d}.jpg"
                extra_path = os.path.join(image_dir, extra_name)
                if os.path.exists(extra_path) and os.path.getsize(extra_path) > 0:
                    saved += 1
            if saved >= PER_PRODUCT:
                time.sleep(SLEEP_SEC)
                continue

            target_slot = 1
            for res in results:
                if target_slot > PER_PRODUCT:
                    break

                extra_name = f"{base}__{target_slot:02d}.jpg"
                extra_path = os.path.join(image_dir, extra_name)
                if os.path.exists(extra_path) and os.path.getsize(extra_path) > 0:
                    target_slot += 1
                    continue

                url = res.get('image') or res.get('thumbnail')
                if not url:
                    continue
                if download_to(extra_path, url):
                    print(f"  -> Downloaded extra: {extra_name}", flush=True)
                    target_slot += 1
                
    except Exception as e:
        print(f"Search failed: {e}", flush=True)

    time.sleep(SLEEP_SEC)

print("Done!", flush=True)
