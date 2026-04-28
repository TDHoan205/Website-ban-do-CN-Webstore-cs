import os
import re
import urllib.request
from duckduckgo_search import DDGS
import time

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

# Find all local image URLs inserted previously to figure out which images to download
matches = list(set(re.findall(r"'/image/products/([^']+)'", content)))
print(f"Found {len(matches)} images to search and download.")

ddgs = DDGS()

for filename in matches:
    filepath = os.path.join(image_dir, filename)
    base = os.path.splitext(filename)[0]
    
    # Determine search term from filename: remove extension, replace underscores with space
    search_term = filename.replace('.png', '').replace('_', ' ')
    print(f"Searching for: {search_term}")
    
    try:
        results = list(ddgs.images(search_term + " product", max_results=MAX_RESULTS))
        success = False
        for res in results:
            url = res.get('image')
            if not url: continue
            # Avoid downloading webp if possible, though DDG might return any extensions
            # the product just needs a working image format. We save it as .png anyway (ASP.NET Core / HTML handles mimetype internally matching the content bytes mostly, or browsers just infer from signature).
            
            print(f"  Trying URL: {url}")
            try:
                # Need user-agent otherwise some image hosts block it
                req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'})
                with urllib.request.urlopen(req, timeout=10) as response:
                    data = response.read()
                    
                    # only save if data has substantial length
                    if len(data) > MIN_BYTES:
                        with open(filepath, 'wb') as out_file:
                            out_file.write(data)
                        print(f"  -> Successfully downloaded {filename}")
                        success = True
                        break
                    else:
                        print(f"  -> Image too small, skipping.")
            except Exception as e:
                print(f"  -> Failed: {e}")
                
        if not success:
            print(f"  !!! Could not download any image for {search_term}")

        # Optional extra gallery images
        if PER_PRODUCT > 1:
            slot = 1
            for res in results:
                if slot > PER_PRODUCT:
                    break
                url = res.get('image')
                if not url:
                    continue
                extra_path = os.path.join(image_dir, f"{base}__{slot:02d}.jpg")
                if os.path.exists(extra_path) and os.path.getsize(extra_path) > 0:
                    slot += 1
                    continue
                try:
                    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'})
                    with urllib.request.urlopen(req, timeout=10) as response:
                        data = response.read()
                        if len(data) > MIN_BYTES:
                            with open(extra_path, 'wb') as out_file:
                                out_file.write(data)
                            print(f"  -> Extra {slot}/{PER_PRODUCT} saved: {os.path.basename(extra_path)}")
                            slot += 1
                except Exception as e:
                    continue
            
        time.sleep(SLEEP_SEC) # Be polite
    except Exception as e:
        print(f"Search failed for {search_term}: {e}")

print("Done downloading all real images!")
