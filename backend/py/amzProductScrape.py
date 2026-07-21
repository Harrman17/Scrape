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

    asin = extract_asin(url)

    return {
        "title": title or "Unknown product",
        "price": price_text or "N/A",
        "amazon_price": parse_price_decimal(price_text),
        "image_url": image_url,
        "in_stock": in_stock,
        "currency": "GBP",
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