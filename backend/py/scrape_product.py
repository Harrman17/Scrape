import json
import re
import sys
from typing import Any

import requests
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


def parse_product_html(html: str, url: str) -> dict[str, Any]:
    soup = BeautifulSoup(html, "html.parser")

    title = None
    for tag in [
        soup.find("h1"),
        "#productTitle",
        ".a-size-large"
    ]:
        if tag is None:
            continue
        text = tag.get("content")
        if text:
            title = text
            break

    price = None
    for selector in [
        "#priceblock_ourprice",
        "#priceblock_saleprice",
        "#priceblock_dealprice",
        ".a-price",
        ".a-price .a-offscreen",
        ".a-offscreen",
    ]:
        element = soup.select_one(selector)
        if element is not None:
            price = element.get_text(" ", strip=True)
            if price:
                break

    if not price:
        for text in soup.stripped_strings:
            if "$" in text:
                price = text.strip()
                break

    asin = extract_asin(url)

    return {
        "title": title or "Unknown product",
        "price": price or "N/A",
        "asin": asin or "",
        "url": url,
    }


def scrape_product(url: str) -> dict[str, Any]:
    if not url or not url.startswith("http"):
        raise ValueError("Please provide a valid Amazon product URL.")

    response = requests.get(
        url,
        headers={
            "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            "Accept-Language": "en-US,en;q=0.9",
        },
        timeout=20,
    )
    print(response.status_code)
    print(response.url)

    with open("amazon.html", "w", encoding="utf-8") as f:
        f.write(response.text)

    print(response.text[:2000])
    response.raise_for_status()
    return parse_product_html(response.text, response.url)


def main() -> None:
    if len(sys.argv) < 2:
        print(json.dumps({"error": "A product URL is required."}))
        sys.exit(1)

    try:
        result = scrape_product(sys.argv[1])
    except Exception as exc:  # noqa: BLE001
        print(json.dumps({"error": str(exc)}))
        sys.exit(1)

    print(json.dumps(result))


if __name__ == "__main__":
    main()
