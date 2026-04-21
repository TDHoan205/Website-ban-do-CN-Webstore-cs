import os

css_file = r'c:\Code full\CURSOR-JG-DEV\Website-ban-do-CN-Webstore-cs\wwwroot\css\site.css'

css_content = """
/* ====================================================
   FPT STYLE PRODUCT CARD 
==================================================== */
.product-card.fpt-style {
    position: relative;
    background: #fff;
    border-radius: 12px;
    padding: 16px;
    border: 1px solid #e5e7eb;
    transition: all 0.3s ease;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
    height: 100%;
    display: flex;
    flex-direction: column;
}

.product-card.fpt-style:hover {
    box-shadow: 0 10px 20px -5px rgba(0, 0, 0, 0.1);
    transform: translateY(-4px);
    border-color: #cbd5e1;
}

.badge-installment {
    position: absolute;
    top: 12px;
    left: 12px;
    background: #f1f5f9;
    color: #475569;
    font-size: 0.7rem;
    font-weight: 700;
    padding: 4px 10px;
    border-radius: 20px;
    z-index: 10;
}

.product-img-wrap {
    position: relative;
    padding: 20px 0;
    text-align: center;
    margin-bottom: 8px;
    height: 220px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.product-main-img {
    max-width: 80%;
    max-height: 100%;
    object-fit: contain;
    transition: transform 0.3s ease;
}

.product-card.fpt-style:hover .product-main-img {
    transform: scale(1.05);
}

.product-side-specs {
    position: absolute;
    right: 0;
    top: 0;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.spec-icon {
    text-align: center;
    font-size: 0.6rem;
    color: #64748b;
    font-weight: 600;
}

.spec-icon i {
    font-size: 1rem;
    color: #94a3b8;
    margin-bottom: 2px;
}

.product-info-wrap {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
}

.price-block {
    margin-bottom: 12px;
}

.price-original-wrap {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
}

.price-old {
    font-size: 0.85rem;
    color: #94a3b8;
    text-decoration: line-through;
}

.discount-badge {
    color: #ef4444;
    background: #fef2f2;
    font-size: 0.75rem;
    font-weight: 700;
    padding: 2px 6px;
    border-radius: 4px;
}

.price-current {
    font-size: 1.25rem;
    font-weight: 800;
    color: #0f172a;
    line-height: 1;
    margin-bottom: 6px;
}

.countdown-timer {
    font-size: 0.75rem;
    color: #10b981;
    font-weight: 600;
}

.product-name {
    font-size: 0.95rem;
    font-weight: 700;
    color: #1e293b;
    line-height: 1.4;
    margin-bottom: 12px;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.product-name a {
    color: inherit;
    text-decoration: none;
}

.product-name a:hover {
    color: #3b82f6;
}

.product-variants {
    margin-bottom: 12px;
}

.color-swatches {
    display: flex;
    gap: 6px;
    margin-bottom: 8px;
}

.swatch {
    width: 16px;
    height: 16px;
    border-radius: 50%;
    border: 1px solid #e2e8f0;
    cursor: pointer;
    box-shadow: inset 0 1px 2px rgba(0,0,0,0.1);
}

.swatch.color-gray { background: #64748b; }
.swatch.color-blue { background: #60a5fa; }
.swatch.color-white { background: #f8fafc; }

.swatch.active {
    box-shadow: 0 0 0 2px #fff, 0 0 0 3px #ef4444;
}

.storage-options {
    display: flex;
    gap: 8px;
}

.storage-pill {
    position: relative;
    font-size: 0.7rem;
    padding: 4px 10px;
    border: 1px solid #cbd5e1;
    border-radius: 6px;
    color: #475569;
    font-weight: 600;
    cursor: pointer;
}

.storage-pill.active {
    color: #ef4444;
    border-color: #ef4444;
}

.storage-pill .tick {
    position: absolute;
    bottom: -1px;
    right: -1px;
    background: #ef4444;
    color: white;
    font-size: 0.5rem;
    padding: 2px;
    border-top-left-radius: 6px;
    border-bottom-right-radius: 4px;
    display: none;
}

.storage-pill.active .tick {
    display: block;
}

.payment-methods {
    display: flex;
    gap: 6px;
    margin-bottom: 8px;
    flex-wrap: wrap;
}

.pay-badge {
    font-size: 0.65rem;
    font-weight: 700;
    padding: 2px 8px;
    border-radius: 4px;
    border: 1px solid transparent;
}

.pay-badge.zalopay {
    color: #0068ff;
    border-color: #bfdbfe;
    background: #eff6ff;
}

.pay-badge.kredivo {
    color: #f97316;
    border-color: #fed7aa;
    background: #fff7ed;
}

.pay-badge.visascb {
    color: #e11d48;
    border-color: #fecdd3;
    background: #fff1f2;
}

.promo-text {
    font-size: 0.75rem;
    color: #64748b;
    line-height: 1.4;
    margin-bottom: auto;
}

.compare-link {
    display: inline-block;
    font-size: 0.8rem;
    color: #3b82f6;
    font-weight: 600;
    text-decoration: none;
    margin-top: 8px;
}

.compare-link:hover {
    color: #2563eb;
    text-decoration: underline;
}
"""

with open(css_file, 'a', encoding='utf-8') as f:
    f.write(css_content)

print("Appended new CSS styles for FPT product card.")
