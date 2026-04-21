import os
import re

views_dir = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\Views'

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # We want to replace `@Html.Encode(expression)` with `@(expression)`
    new_content = re.sub(r'@Html\.Encode\((.*?)\)', r'@(\1)', content)

    if new_content != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {filepath}")

for root, _, files in os.walk(views_dir):
    for f in files:
        if f.endswith('.cshtml'):
            process_file(os.path.join(root, f))

print("Done removing @Html.Encode")
