import os
import re
import urllib.request
import urllib.parse
import sys

sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
image_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\image\products'

if not os.path.exists(image_dir):
    os.makedirs(image_dir)

with open(sql_file, 'r', encoding='utf-8') as f:
    content = f.read()

# Find all image URLs
matches = re.finditer(r"'https://via\.placeholder\.com/([^\']+)'", content)

new_content = content
downloaded = 0
for match in matches:
    url_part = match.group(1)
    full_url = f'https://via.placeholder.com/{url_part}'
    
    # Extract intended filename from the text parameter
    if '?text=' in url_part:
        text_part = url_part.split('?text=')[1]
        filename = text_part.replace('+', '_').replace('%20', '_') + '.png'
    else:
        # Fallback
        filename = url_part.replace('/', '_').replace('?', '_') + '.png'
    
    # Clean up filename
    filename = "".join([c for c in filename if c.isalpha() or c.isdigit() or c=='_' or c=='.' or c=='-'])

    filepath = os.path.join(image_dir, filename)
    
    # Update SQL content to use relative path for wwwroot
    local_url = f'/image/products/{filename}'
    new_content = new_content.replace(f"'{full_url}'", f"'{local_url}'")

    if not os.path.exists(filepath):
        print(f"Downloading {full_url} to {filepath}...")
        try:
            req = urllib.request.Request(full_url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req) as response, open(filepath, 'wb') as out_file:
                data = response.read()
                out_file.write(data)
            downloaded += 1
        except Exception as e:
            print(f"Failed to download {full_url}: {e}")
            # we can try to create a dummy image if placehold fails
            pass

with open(sql_file, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Done! Downloaded {downloaded} images. Updated SQL file.")
