import argparse
import hashlib
import os
import re
import time
import urllib.request
from pathlib import Path

from ddgs import DDGS

try:
    from PIL import Image
    from io import BytesIO
except Exception:
    Image = None
    BytesIO = None


def parse_products_from_seed(seed_path: Path):
    content = seed_path.read_text(encoding="utf-8")
    pattern = re.compile(r"new\s+Product\s*\{(?P<body>.*?)\}", re.DOTALL)
    name_re = re.compile(r"\bName\s*=\s*\"(?P<name>[^\"]+)\"")
    img_re = re.compile(r"\bImageUrl\s*=\s*\"(?P<url>/images/products/[^\"]+)\"")

    products = []
    for m in pattern.finditer(content):
        body = m.group("body")
        nm = name_re.search(body)
        im = img_re.search(body)
        if not im: continue
        name = nm.group("name").strip() if nm else ""
        url = im.group("url").strip()
        products.append({"name": name, "image_url": url})

    seen = set()
    unique = []
    for p in products:
        if p["image_url"] in seen: continue
        seen.add(p["image_url"])
        unique.append(p)
    return unique


def sha1(data: bytes) -> str:
    return hashlib.sha1(data).hexdigest()


def http_get(url: str, timeout: int = 20) -> bytes | None:
    try:
        req = urllib.request.Request(url, headers={
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        })
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return resp.read()
    except Exception: return None


def save_as_jpg(target: Path, data: bytes) -> bool:
    if Image is None or BytesIO is None:
        target.write_bytes(data)
        return True
    try:
        img = Image.open(BytesIO(data))
        img = img.convert("RGB")
        target.parent.mkdir(parents=True, exist_ok=True)
        img.save(target, format="JPEG", quality=85, optimize=True)
        return True
    except Exception:
        target.write_bytes(data)
        return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--seed", default=r"Data/Seed/SeedData.cs")
    parser.add_argument("--out", default=r"wwwroot/images/products")
    parser.add_argument("--per-product", type=int, default=5)
    args = parser.parse_args()

    seed_path = Path(args.seed)
    image_dir = Path(args.out)
    image_dir.mkdir(parents=True, exist_ok=True)

    products = parse_products_from_seed(seed_path)
    print(f"Found {len(products)} products to process.")

    for idx, p in enumerate(products, start=1):
        image_url = p["image_url"]
        filename = image_url.split("/")[-1]
        base = os.path.splitext(filename)[0]
        
        # 1. Main Image
        main_path = image_dir / filename
        product_name = p["name"] or base.replace("_", " ")
        
        if not main_path.exists() or main_path.stat().st_size == 0:
            print(f"[{idx}] Downloading main for: {product_name}")
            with DDGS() as ddgs:
                results = list(ddgs.images(product_name + " product white background", max_results=5))
                for r in results:
                    data = http_get(r.get("image") or r.get("thumbnail"))
                    if data and len(data) > 5000:
                        save_as_jpg(main_path, data)
                        break

        # 2. Extra Images for Gallery
        for s in range(1, args.per_product):
            extra_path = image_dir / f"{base}__{s:02d}.jpg"
            if not extra_path.exists() or extra_path.stat().st_size == 0:
                print(f"  -> Downloading extra {s} for: {product_name}")
                with DDGS() as ddgs:
                    results = list(ddgs.images(f"{product_name} angle {s}", max_results=10))
                    for r in results:
                        data = http_get(r.get("image") or r.get("thumbnail"))
                        if data and len(data) > 5000:
                            save_as_jpg(extra_path, data)
                            break
                time.sleep(0.5)

if __name__ == "__main__":
    main()
