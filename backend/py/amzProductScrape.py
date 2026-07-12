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


def parse_product_html(html: str, url: str) -> dict[str, Any]:

    soup = BeautifulSoup(html, "html.parser")

    title = None

    for tag in [
        soup.select_one("#productTitle"),
        soup.find("h1"),
    ]:

        if tag is None:
            continue

        text = tag.get_text(" ", strip=True)

        if text:
            title = text
            break


    price = None

    for selector in [
        ".a-price .a-offscreen",
        ".a-offscreen",
        ".a-price",
        "#priceblock_ourprice",
        "#priceblock_saleprice",
    ]:

        element = soup.select_one(selector)

        if element:
            price = element.get_text(" ", strip=True)
            break


    asin = extract_asin(url)


    return {
        "title": title or "Unknown product",
        "price": price or "N/A",
        "asin": asin or "",
        "url": url,
    }


def scrape_product(url: str) -> dict[str, Any]:

    if not url.startswith("http"):
        raise ValueError("Invalid URL")


    with sync_playwright() as p:

        browser = p.chromium.launch(
            headless=True
        )

        page = browser.new_page(
            user_agent=(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) "
                "AppleWebKit/537.36 "
                "Chrome/124.0 Safari/537.36"
            ),
            locale="en-GB"
        )


        page.goto(
            url,
            wait_until="domcontentloaded",
            timeout=30000
        )


        # Wait for Amazon product title
        try:
            page.wait_for_selector(
                "#productTitle",
                timeout=10000
            )

        except:
            pass


        html = page.content()


        with open(
            "amazon.html",
            "w",
            encoding="utf-8"
        ) as f:
            f.write(html)


        browser.close()


    return parse_product_html(html, url)

def main():

    if len(sys.argv) < 2:
        print(
            json.dumps(
                {"error": "A product URL is required"}
            )
        )
        sys.exit(1)


    try:
        result = scrape_product(sys.argv[1])

    except Exception as exc:
        print(
            json.dumps(
                {"error": str(exc)}
            )
        )

        sys.exit(1)


    print(json.dumps(result))


if __name__ == "__main__":
    main()