import re
import os

files = [
    r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\Views\Shop\Index.cshtml',
    r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\Views\Shop\Products.cshtml'
]

replacement_card = """                        <div class="col-lg-3 col-md-4 col-sm-6 mb-4">
                            <div class="product-card fpt-style">
                                <div class="badge-installment">Trả góp 0%</div>
                                
                                <div class="product-img-wrap">
                                    <a asp-controller="Shop" asp-action="Product" asp-route-id="@product.ProductId">
                                        <img src="@(string.IsNullOrEmpty(product.ImageUrl) ? "/images/products/placeholder.svg" : product.ImageUrl)" alt="@product.Name" class="product-main-img" />
                                    </a>
                                    
                                    <!-- side specs -->
                                    <div class="product-side-specs">
                                        <div class="spec-icon"><i class="fas fa-microchip"></i><br>Snapdragon</div>
                                        <div class="spec-icon"><i class="fas fa-battery-full"></i><br>Pin lớn</div>
                                        <div class="spec-icon"><i class="fas fa-bolt"></i><br>Sạc nhanh</div>
                                        <div class="spec-icon"><i class="fas fa-mobile-alt"></i><br>AMOLED 2X</div>
                                    </div>
                                </div>
                                
                                <div class="product-info-wrap">
                                    <div class="price-block">
                                        <div class="price-original-wrap">
                                            <del class="price-old">@((product.Price * 1.15m).ToString("N0"))đ</del>
                                            <span class="discount-badge">-15%</span>
                                        </div>
                                        <div class="price-current">@product.Price.ToString("N0")đ</div>
                                        <div class="countdown-timer">Còn 00 ngày 13:36:50</div>
                                    </div>

                                    <h3 class="product-name">
                                        <a asp-controller="Shop" asp-action="Product" asp-route-id="@product.ProductId">@product.Name</a>
                                    </h3>

                                    <div class="product-variants">
                                        <div class="color-swatches">
                                            <span class="swatch color-gray active"></span>
                                            <span class="swatch color-blue"></span>
                                            <span class="swatch color-white"></span>
                                        </div>
                                        <div class="storage-options">
                                            <div class="storage-pill active">256 GB<i class="fas fa-check tick"></i></div>
                                            <div class="storage-pill">512 GB</div>
                                        </div>
                                    </div>

                                    <div class="payment-methods">
                                        <span class="pay-badge zalopay">ZaloPay</span>
                                        <span class="pay-badge kredivo">Kredivo</span>
                                        <span class="pay-badge visascb">SCB</span>
                                    </div>
                                    
                                    <div class="promo-text">
                                        Giảm đến 500,000đ khi thanh toán Visa SCB.
                                    </div>

                                    <div class="product-actions mt-3">
                                        <a href="javascript:void(0)" class="compare-link" onclick="addToCart(@product.ProductId)">
                                            <i class="fas fa-plus-circle me-1"></i>Thêm vào so sánh
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </div>"""

for filepath in files:
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # We will match from <div class="col-lg-3... or col-lg-4... down to the closing </div> of the column.
    # A robust regex: find @foreach (...) { ... } and replace the inner div
    
    # Let's find: @foreach (var product in ...)\s*\{\s*<div class="col-lg-[^>]+>\s*(<div class="product-card">.*?)</div>\s*</div>
    # Using re.sub with a custom function to handle nested divs is tricky.
    # Instead, we just replace the whole `<div class="col-lg-...` chunk inside foreach.
    
    def replacer(match):
        prefix = match.group(1) # @foreach... {
        return prefix + "\n" + replacement_card
    
    # Match `@foreach (var product in XXX) {` followed by the column div
    pattern = r"(@foreach\s*\([^)]+\)\s*\{)[\s\S]*?(?:<div class=\"col-lg-(?:3|4).*?>[\s\S]*?<!-- end of card -->.*?</div>\s*|.*?<div class=\"product-card\">[\s\S]*?</button>\s*</div>\s*</div>\s*</div>\s*)"
    # Wait! Simple regex won't balance tags perfectly.
    
    # Better approach: string replacement via finding bounds.
    pass

# Using a simpler string manipulation logic:
def replace_blocks(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
        
    start_token = "                    @foreach (var product in "
    out = []
    lines = content.split('\n')
    i = 0
    while i < len(lines):
        line = lines[i]
        out.append(line)
        if line.startswith("                    @foreach (var product in "):
            # Next line is '{'
            i += 1
            out.append(lines[i])
            i += 1
            # We skip lines until we hit the '                    }'
            while i < len(lines) and not lines[i].startswith("                    }"):
                i += 1
            # Now we insert our replacement block
            out.append(replacement_card)
            # Add the '}' line
            out.append(lines[i])
        i += 1
        
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(out))

for f in files:
    replace_blocks(f)
    print(f"Updated {f}")
