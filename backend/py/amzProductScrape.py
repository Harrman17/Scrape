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

    price = None
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
            price = element.get_text(" ", strip=True)
            if price:
                break

    asin = extract_asin(url)

    return {
        "title": title or "Unknown product",
        "price": price or "N/A",
        "asin": asin or "",
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