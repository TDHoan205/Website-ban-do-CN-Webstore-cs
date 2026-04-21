import re
import os

sql_input = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database_v2.sql'
sql_output = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\database\create_database_v3.sql'

html_template = """<div class="fpt-desc mt-4">
    <div class="fpt-desc-header text-center mb-4">
        <h4 class="fw-bold" style="color: #CB1C22; text-transform: uppercase;">Đặc điểm nổi bật của {name}</h4>
    </div>
    <div class="fpt-desc-content" style="font-size: 0.95rem; line-height: 1.7; color: #333;">
        <p><strong>{name}</strong> {desc} Đây hứa hẹn sẽ là phiên bản mang lại trải nghiệm đỉnh cao cho người dùng với hàng loạt nâng cấp đáng giá từ thiết kế đến hiệu năng.</p>
        
        <div class="fpt-desc-image my-4 text-center">
            <img src="{image_url}" class="img-fluid rounded" alt="{name}" style="max-height: 350px; object-fit: contain;" />
            <p class="text-muted mt-2 mb-0" style="font-size: 0.8rem;"><em>Hình ảnh thực tế cực kỳ sắc nét của {name}.</em></p>
        </div>

        <h5 class="fw-bold mt-4 text-dark">Thiết kế thời thượng, màn hình hiển thị xuất sắc</h5>
        <p>Thế hệ mới này đem lại cảm giác sang trọng, các đường nét được trau chuốt vô cùng tỉ mỉ. Chất liệu cao cấp không chỉ giúp tăng độ bền mà còn toát lên vẻ ngoài vô cùng thời thượng. Màn hình của máy được trang bị công nghệ mới nhất, cho dải màu siêu rộng, độ tương phản tuyệt vời, giúp mọi trải nghiệm giải trí như lướt web, xem phim trở nên chân thực hơn bao giờ hết.</p>

        <h5 class="fw-bold mt-4 text-dark">Hiệu năng đột phá, đáp ứng mọi nhu cầu</h5>
        <p>Nhờ mang trong mình cấu hình mạnh mẽ, thiết bị có thể xử lý mượt mà mọi tác vụ từ cơ bản đến nâng cao. Dù bạn làm việc với tần suất cao hay sử dụng đa nhiệm, máy vẫn duy trì được hiệu suất ổn định và nhiệt độ luôn mát mẻ. Thiết bị còn được tối ưu hóa hệ điều hành, kéo dài thời lượng pin vô cùng ấn tượng.</p>
        
        <div class="p-3 my-4 rounded border-start border-4" style="background-color: #f8f9fa; border-color: #CB1C22 !important;">
            <p class="fw-bold text-danger mb-2">Đánh giá chung:</p>
            <ul class="mb-0 text-dark" style="padding-left: 20px;">
                <li>Công nghệ vượt trội, đem lại hiệu năng ổn định.</li>
                <li>Thiết kế tối ưu, bắt kịp xu hướng thị trường.</li>
                <li>Thời lượng sử dụng duy trì lâu dài, hỗ trợ công nghệ sạc hiện đại.</li>
            </ul>
        </div>
        
        <p>Hiện tại, sản phẩm đang được phân phối với mức giá cực kỳ ưu đãi tại hệ thống. Nếu bạn đang tìm kiếm một thiết bị toàn diện, <strong>{name}</strong> chắc chắn là một sự lựa chọn tuyệt vời.</p>
    </div>
</div>"""

with open(sql_input, 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern for INSERT INTO Products
# We look for tuples like (N'Name', N'Desc', N'Image', Price, Cat, Sup)
pattern = r"\(N'([^']+)',\s*N'([^']+)',\s*N'([^']+)',\s*(\d+),\s*(\d+),\s*(\d+)\)"

def replacer(match):
    name = match.group(1)
    desc = match.group(2)
    image_url = match.group(3)
    price = match.group(4)
    cat = match.group(5)
    sup = match.group(6)
    
    # Generate HTML
    raw_html = html_template.format(name=name, desc=desc, image_url=image_url)
    # Escape single quotes for SQL
    safe_html = raw_html.replace("'", "''")
    safe_name = name.replace("'", "''")
    
    return f"(N'{safe_name}', N'{safe_html}', N'{image_url}', {price}, {cat}, {sup})"

# Execute replacement
new_content = re.sub(pattern, replacer, content)

with open(sql_output, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Enhanced descriptions and created {sql_output}")
