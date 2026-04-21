import os
sql_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database.sql'
with open(sql_file, 'r', encoding='utf-8') as f:
    content = f.read()

new_content = content.replace("'/image/products/", "'/images/products/")

with open(sql_file, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Updated create_database.sql to use /images/products/")
