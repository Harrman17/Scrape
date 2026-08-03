#!/usr/bin/env python3
"""
Amazon Price and Stock Update Script
Lightweight script to check price and stock status for existing products.
Designed for frequent AWS Lambda/scheduled execution.
"""

import json
import re
import sys
from typing import Any

from playwright.sync_api import sync_playwright
from bs4 import BeautifulSoup


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


def check_price_and_stock(asin: str) -> dict[str, Any]:
    """
    Check price and stock status for a single ASIN.
    Returns minimal data optimized for frequent updates.
    """
    url = f"https://www.amazon.co.uk/dp/{asin}"
    
    result = {
        "asin": asin,
        "price": None,
        "currency": "GBP",
        "in_stock": False,
        "error": None
    }

    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            context = browser.new_context(
                user_agent="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
                viewport={"width": 1280, "height": 720}
            )
            page = context.new_page()
            
            # Navigate with reduced timeout for faster execution
            try:
                page.goto(url, wait_until="domcontentloaded", timeout=15000)
                page.wait_for_timeout(2000)  # Reduced wait time
            except Exception as e:
                result["error"] = f"Page load failed: {str(e)}"
                browser.close()
                return result

            html = page.content()
            browser.close()

        soup = BeautifulSoup(html, "html.parser")

        # Extract price - use same selectors as full scraper
        price = None
        price_text = None
        price_selectors = [
            "#priceblock_ourprice",
            "#priceblock_saleprice",
            "#priceblock_dealprice",
            "#corePriceDisplay_desktop_feature_div .a-offscreen",
            ".a-price .a-offscreen",
            ".a-offscreen",
            ".a-price",
        ]
        
        for selector in price_selectors:
            element = soup.select_one(selector)
            if element:
                price_text = element.get_text(" ", strip=True)
                if price_text:
                    price = parse_price_decimal(price_text)
                    if price:
                        # Debug output to stderr
                        import sys
                        print(f"[DEBUG] ASIN {asin}: Found price {price} using selector '{selector}', text: '{price_text}'", file=sys.stderr)
                        break

        result["price"] = price
        
        # If no price found, log the selectors we tried
        if price is None:
            import sys
            print(f"[DEBUG] ASIN {asin}: No price found. Tried selectors: {price_selectors}", file=sys.stderr)

        # Check stock status - look for out of stock indicators
        page_text = soup.get_text().lower()
        
        # Out of stock indicators
        out_of_stock_phrases = [
            "currently unavailable",
            "out of stock",
            "not available",
            "temporarily out of stock",
        ]
        
        is_out_of_stock = any(phrase in page_text for phrase in out_of_stock_phrases)
        
        # In stock indicators
        in_stock_indicators = [
            soup.select_one("#add-to-cart-button"),
            soup.select_one("#buy-now-button"),
            "in stock" in page_text,
            "available" in page_text and not is_out_of_stock,
        ]
        
        result["in_stock"] = any(in_stock_indicators) and not is_out_of_stock

    except Exception as e:
        result["error"] = str(e)

    return result


def main():
    """
    Main entry point.
    Accepts ASINs as command line arguments.
    Outputs JSON array of price/stock updates.
    """
    if len(sys.argv) < 2:
        print(json.dumps({"error": "No ASINs provided"}), file=sys.stderr)
        sys.exit(1)

    asins = sys.argv[1:]
    results = []

    for asin in asins:
        asin = asin.strip().upper()
        if not re.fullmatch(r"[A-Z0-9]{10}", asin):
            results.append({
                "asin": asin,
                "error": "Invalid ASIN format"
            })
            continue

        result = check_price_and_stock(asin)
        results.append(result)

    # Output results as JSON
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
