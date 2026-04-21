import os
import re
import urllib.request
from ddgs import DDGS
import time

sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'

with open(sql_file, 'r', encoding='utf-8') as f:
    content = f.read()

matches = list(set(re.findall(r"'/image/products/([^']+)'", content)))
print(f"Found {len(matches)} images to search and download.", flush=True)

for i, filename in enumerate(matches):
    filepath = os.path.join(image_dir, filename)
    search_term = filename.replace('.png', '').replace('_', ' ')
    print(f"[{i+1}/{len(matches)}] Searching: {search_term}", flush=True)
    
    try:
        with DDGS() as ddgs:
            # We get top 3 images
            results = list(ddgs.images(search_term, max_results=3))
            success = False
            for res in results:
                # We prioritize thumbnail because it downloads instantly and won't be 403 Forbidden
                url = res.get('thumbnail') 
                if not url: url = res.get('image')
                if not url: continue
                
                try:
                    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
                    with urllib.request.urlopen(req, timeout=10) as response:
                        data = response.read()
                        if len(data) > 1024:
                            with open(filepath, 'wb') as out_file:
                                out_file.write(data)
                            print(f"  -> Downloaded OK.", flush=True)
                            success = True
                            break
                except :
                    pass
                    
            if not success:
                print(f"  !!! Failed to download.", flush=True)
                
    except Exception as e:
        print(f"Search failed: {e}", flush=True)

print("Done!", flush=True)
