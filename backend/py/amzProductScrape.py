import json
import re
import sys
from typing import Any

from playwright.sync_api import sync_playwright
from bs4 import BeautifulSoup


def extract_asin(url: str) -> str | None:
    patterns = [
        r"/dp/([A-Z0-9]{10})",
        r"/gp/product/([A-Z0-9]{10})",
        r"[?&]asin=([A-Z0-9]{10})",
        r"/([A-Z0-9]{10})/?$",
    ]

    for pattern in patterns:
        match = re.search(pattern, url, re.IGNORECASE)

        if match:
            return match.group(1).upper()

    return None


def normalize_asin(value: str) -> str | None:
    cleaned = value.strip().upper()
    match = re.fullmatch(r"[A-Z0-9]{10}", cleaned)
    return cleaned if match else None


def parse_price_decimal(price_text: str | None) -> float | None:
    """Extract a numeric price value from text like '£9.99' or '£1,234.56'."""
    if not price_text or price_text == "N/A":
        return None
    cleaned = price_text.replace(",", "")
    match = re.search(r"\d+\.?\d*", cleaned)
    if match:
        try:
            return float(match.group())
        except ValueError:
            return None
    return None


def extract_attribute_from_text(text: str, patterns: list[str]) -> str | None:
    for pattern in patterns:
        match = re.search(pattern, text, re.IGNORECASE | re.DOTALL)
        if match and match.group(1):
            return re.sub(r"\s+", " ", match.group(1)).strip(" -:")
    return None


def extract_color_from_text(text: str) -> str | None:
    match = re.search(
        r"\b(black|white|blue|red|green|silver|gold|grey|gray|pink|purple|orange|yellow|brown)\b",
        text,
        re.IGNORECASE,
    )
    return match.group(1).strip().title() if match else None


def extract_size_from_text(text: str) -> str | None:
    match = re.search(
        r"\b(\d+(?:\.\d+)?(?:cm|mm|in|inch|inches|kg|g|lb|lbs|x\d+(?:cm|mm|in|inch|inches)))\b",
        text,
        re.IGNORECASE,
    )
    return match.group(1).strip() if match else None


def is_video_url(url: str) -> bool:
    """Check if a URL is likely a video rather than an image."""
    if not url:
        return False
    
    video_indicators = [
        '/video/',
        'video-',
        '.mp4',
        '.webm',
        '.mov',
        'vnd.amazonvideo',
        'amazon-video'
    ]
    url_lower = url.lower()
    return any(indicator in url_lower for indicator in video_indicators)


def clean_amazon_image_url(url: str) -> str:
    """
    Remove size parameters from Amazon image URLs to match Dilato format.
    Converts: https://m.media-amazon.com/images/I/71vY18ciJsL._AC_SL1500_.jpg
    To: https://m.media-amazon.com/images/I/71vY18ciJsL.jpg
    """
    if not url:
        return url
    
    # Remove size/format suffixes like _AC_SL1500_, _SL500_, _AC_UL1500_, etc.
    # Pattern: ._AC_SL1500_. becomes just .
    # The URL structure is: filename._AC_SL1500_.jpg (two dots!)
    cleaned = re.sub(r'\._[A-Z]+(_[A-Z]+)?\d+_\.', '.', url)
    
    return cleaned


def parse_product_html(html: str, url: str) -> dict[str, Any]:
    soup = BeautifulSoup(html, "html.parser")

    title = None
    for selector in ["#productTitle", "h1", "meta[property='og:title']"]:
        if selector.startswith("meta"):
            element = soup.select_one(selector)
            if element is not None:
                title = (element.get("content") or "").strip()
        else:
            element = soup.select_one(selector)
            if element is not None:
                title = element.get_text(" ", strip=True)
        if title:
            break

    # eBay listing titles are capped at 80 characters — truncate at word boundary
    if title and len(title) > 80:
        title = title[:80].rsplit(" ", 1)[0].rstrip()

    price_text = None
    for selector in [
        "#priceblock_ourprice",
        "#priceblock_saleprice",
        "#priceblock_dealprice",
        "#corePriceDisplay_desktop_feature_div .a-offscreen",
        ".a-price .a-offscreen",
        ".a-offscreen",
        ".a-price",
    ]:
        element = soup.select_one(selector)
        if element is not None:
            price_text = element.get_text(" ", strip=True)
            if price_text:
                break

    image_url = None
    for selector in [
        "#landingImage",
        "#imgTagWrapperId img",
        "#main-image",
        "meta[property='og:image']",
    ]:
        if selector.startswith("meta"):
            element = soup.select_one(selector)
            if element is not None:
                image_url = (element.get("content") or "").strip() or None
        else:
            element = soup.select_one(selector)
            if element is not None:
                image_url = (
                    element.get("data-old-hires")
                    or element.get("src")
                    or ""
                ).strip() or None
        if image_url:
            image_url = clean_amazon_image_url(image_url)
            break

    # Collect all gallery images (max 5, no duplicates, no videos)
    image_urls = []
    
    # Add main image first if found
    if image_url:
        image_urls.append(image_url)  # Already cleaned above
    
    # Find additional gallery images from alt images section
    # Skip the first image (index 0) as it's usually the main image
    gallery_images = soup.select("#altImages img")
    for idx, img_elem in enumerate(gallery_images):
        # Skip first gallery image to avoid duplicating main image
        if idx == 0:
            continue
            
        if len(image_urls) >= 5:
            break
            
        raw_url = (
            img_elem.get("data-old-hires")
            or img_elem.get("src")
            or ""
        ).strip()
        
        if not raw_url or is_video_url(raw_url):
            continue
            
        cleaned_url = clean_amazon_image_url(raw_url)
        
        # Check for duplicates (compare cleaned URLs)
        if cleaned_url and cleaned_url not in image_urls:
            image_urls.append(cleaned_url)
    
    # Fallback: check for images in the main image container if we have no images
    if not image_urls:
        for img_elem in soup.select("#imgTagWrapperId img, #main-image img"):
            if len(image_urls) >= 5:
                break
                
            raw_url = (
                img_elem.get("data-old-hires")
                or img_elem.get("src")
                or ""
            ).strip()
            
            if not raw_url or is_video_url(raw_url):
                continue
                
            cleaned_url = clean_amazon_image_url(raw_url)
            
            if cleaned_url and cleaned_url not in image_urls:
                image_urls.append(cleaned_url)

    # Extract feature bullets/description
    features = []
    feature_list = soup.select_one("#feature-bullets ul, #aplus-feature-bullets .a-unordered-list")
    if feature_list:
        for li in feature_list.select("li"):
            feature_text = li.get_text(" ", strip=True)
            if feature_text and not feature_text.lower().startswith("see more"):
                features.append(feature_text)
    
    # Get general description
    description = None
    for selector in [
        "#productDescription p",
        "#productDescription",
        "meta[name='description']",
    ]:
        if selector.startswith("meta"):
            element = soup.select_one(selector)
            if element is not None:
                description = (element.get("content") or "").strip() or None
        else:
            element = soup.select_one(selector)
            if element is not None:
                description = element.get_text(" ", strip=True) or None
        if description:
            break

    # Availability
    in_stock = False
    availability_el = soup.select_one("#availability span, #availability")
    if availability_el:
        availability_text = availability_el.get_text(" ", strip=True).lower()
        in_stock = (
            "in stock" in availability_text
            or "only" in availability_text
        )
    else:
        # Fallback: add-to-cart button present means purchasable
        in_stock = soup.select_one("#add-to-cart-button") is not None

    full_text = soup.get_text(" ", strip=True)
    
    # Extract product details from detail table
    product_details = {}
    for table in soup.select("#productDetails_detailBullets_sections1 tr, #productDetails_techSpec_section_1 tr, .prodDetTable tr"):
        th = table.select_one("th")
        td = table.select_one("td")
        if th and td:
            key = th.get_text(" ", strip=True).lower().rstrip(": ")
            value = td.get_text(" ", strip=True)
            product_details[key] = value
    
    # Extract brand, MPN, model, etc.
    brand = (
        product_details.get("brand")
        or product_details.get("manufacturer")
        or extract_attribute_from_text(
            full_text,
            [
                r"\bbrand\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)",
                r"\bmanufacturer\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)",
            ],
        )
    )
    mpn = (
        product_details.get("part number")
        or product_details.get("manufacturer part number")
        or product_details.get("mpn")
        or extract_attribute_from_text(
            full_text,
            [
                r"\bpart\s+number\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)",
                r"\bmpn\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)",
                r"\bmanufacturer\s+part\s+number\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)",
            ],
        )
    )
    model = (
        product_details.get("model")
        or product_details.get("model number")
        or extract_attribute_from_text(full_text, [r"\bmodel\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)"])
    )
    
    # Extract identifiers
    ean = product_details.get("ean") or None
    upc = product_details.get("upc") or None
    isbn = product_details.get("isbn") or product_details.get("isbn-13") or product_details.get("isbn-10") or None
    
    # Extract dimensions
    dimensions = product_details.get("product dimensions") or product_details.get("package dimensions") or ""
    item_weight = product_details.get("item weight") or product_details.get("weight") or ""
    
    # Parse dimensions to extract height, width, length
    height = width = length = ""
    dim_match = re.search(r"(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)", dimensions)
    if dim_match:
        length, width, height = dim_match.groups()
        # Standardize to mm (assuming cm in Amazon listings)
        try:
            length = str(int(float(length) * 10))
            width = str(int(float(width) * 10))
            height = str(int(float(height) * 10))
        except ValueError:
            pass
    
    # Color and size
    color = extract_color_from_text(full_text) or extract_attribute_from_text(
        full_text,
        [r"\bcolour\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)", r"\bcolor\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)"],
    )
    size = extract_size_from_text(full_text) or extract_attribute_from_text(
        full_text,
        [r"\bsize\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)"],
    )
    
    # Product type and department
    product_type = extract_attribute_from_text(
        full_text,
        [r"\btype\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)", r"\bcategory\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)"],
    )
    department = product_details.get("department") or extract_attribute_from_text(
        full_text, [r"\bdepartment\b\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)"]
    )

    asin = extract_asin(url)

    return {
        "title": title or "Unknown product",
        "price": price_text or "N/A",
        "amazon_price": parse_price_decimal(price_text),
        "image_url": image_url,
        "image_urls": image_urls,
        "description": description,
        "features": features,
        "in_stock": in_stock,
        "currency": "GBP",
        "asin": asin or "",
        "brand": brand or "",
        "mpn": mpn or "",
        "model": model or "",
        "color": color or "",
        "size": size or "",
        "product_type": product_type or "",
        "department": department or "",
        "ean": ean or "",
        "upc": upc or "",
        "isbn": isbn or "",
        "height": height,
        "width": width,
        "length": length,
        "weight": item_weight,
    }


def scrape_page(page, url: str) -> dict[str, Any]:
    if not url.startswith("http"):
        raise ValueError("Invalid URL")

    page.goto(
        url,
        wait_until="domcontentloaded",
        timeout=30000
    )

    # Wait for Amazon product title
    try:
        page.wait_for_selector(
            "#productTitle",
            timeout=5000
        )
    except Exception:
        pass

    html = page.content()
    return parse_product_html(html, url)


def scrape_asins(asins: list[str]) -> list[dict[str, Any]]:
    results = []

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(
            user_agent=(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) "
                "AppleWebKit/537.36 "
                "Chrome/124.0 Safari/537.36"
            ),
            locale="en-GB"
        )
        page = context.new_page()

        try:
            for asin in asins:
                url = f"https://www.amazon.co.uk/dp/{asin}"
                try:
                    result = scrape_page(page, url)
                except Exception as exc:  # noqa: BLE001
                    result = {"title": "Unknown product", "price": "N/A", "asin": asin, "error": str(exc)}
                result["asin"] = asin
                result["url"] = url
                results.append(result)
        finally:
            browser.close()

    return results


def main():

    if len(sys.argv) < 2:
        print(
            json.dumps(
                {"error": "A product URL is required"}
            )
        )
        sys.exit(1)


    try:
        asin_inputs = [normalize_asin(argument) for argument in sys.argv[1:]]
        asins = [asin for asin in asin_inputs if asin is not None]

        if not asins:
            raise ValueError("At least one valid ASIN is required")

        result = scrape_asins(asins)
    except Exception as exc:
        print(json.dumps({"error": str(exc)}))
        sys.exit(1)

    print(json.dumps(result))


if __name__ == "__main__":
    main()