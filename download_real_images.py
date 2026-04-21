import os
import re
import urllib.request
from duckduckgo_search import DDGS
import time

sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'

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
    
    # Determine search term from filename: remove extension, replace underscores with space
    search_term = filename.replace('.png', '').replace('_', ' ')
    print(f"Searching for: {search_term}")
    
    try:
        results = list(ddgs.images(search_term, max_results=5))
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
                    if len(data) > 1024:
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
            
        time.sleep(1) # Be polite
    except Exception as e:
        print(f"Search failed for {search_term}: {e}")

print("Done downloading all real images!")
