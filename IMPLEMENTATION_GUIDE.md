# CSV Generation Improvements - Implementation Guide

## Overview
This update aligns our CSV generation with the Dilato format to optimize eBay listings and prevent upload failures.

## Database Migration Required

**⚠️ IMPORTANT**: Run the migration SQL file before starting the backend:

```bash
# Connect to your PostgreSQL database and run:
psql -U your_username -d your_database -f backend/api/migrations/002_add_product_metadata.sql
```

This migration adds:
- Product metadata fields (brand, mpn, model, ean, upc, isbn, color, size, etc.)
- Features JSON storage  
- Dimensions fields (height, width, length, weight)
- PayPal email to user settings

## Changes Summary

### 1. Python Scraper Enhancements
**File**: `backend/py/amzProductScrape.py`
- ✅ Extracts feature bullets from Amazon listings
- ✅ Parses product details table for Brand, MPN, Model, EAN, UPC, ISBN
- ✅ Extracts dimensions (height, width, length) from product specs
- ✅ Better color, size, department extraction

### 2. Backend Models Updated
**Files**: 
- `backend/api/Models/ScrapedProduct.cs` - Added all new fields from scraper
- `backend/api/Models/InventoryItem.cs` - Added product metadata storage
- `backend/api/Models/Dtos.cs` - Added fields to UserInventoryDto
- `backend/api/Models/UserSettings.cs` - Added PayPalEmail field

### 3. Repository Layer Updated
**Files**:
- `backend/api/Services/InventoryRepository.cs` - Saves/loads all metadata
- `backend/api/Services/UserInventoryRepository.cs` - Includes metadata in queries
- `backend/api/Services/UserSettingsRepository.cs` - Handles PayPal email

### 4. Controllers Updated
**Files**:
- `backend/api/Controllers/ScrapeController.cs` - Maps all fields to DTOs
- `backend/api/Controllers/UserInventoryController.cs` - PayPal email in settings

### 5. Frontend CSV Generation
**File**: `src/utils/generateEbayCsv.js`
- ✅ Multi-image support with " | " separator
- ✅ Features embedded in HTML description as `<li>` bullets
- ✅ Dilato HTML template with branding
- ✅ All product identifiers (EAN, UPC, ISBN, MPN, Brand, Model)
- ✅ Dimensions populated (C:Item Length, C:Item Width, C:Height)
- ✅ PayPal email from settings
- ✅ Actual product quantity (not hardcoded)

## Key Improvements vs Dilato CSV

### ✅ Now Matching Dilato:
1. **Product Identifiers**: Brand, MPN, Model, EAN all populated from Amazon data
2. **Features in Description**: Bullet points embedded in HTML like Dilato
3. **Multi-Image Support**: All gallery images with proper " | " separator  
4. **Dimensions**: Height, width, length in custom fields
5. **Quantity**: Uses actual inventory qty instead of hardcoded 1
6. **PayPal Email**: Configurable in user settings

### ✅ Better eBay Optimization:
- Complete product metadata for better search visibility
- Rich HTML descriptions with features
- Proper product categorization with all custom fields
- Professional Dilato-style template with branding

## Testing After Migration

1. **Run the database migration** (see above)
2. **Rebuild and restart the backend**:
   ```bash
   cd backend/api
   dotnet build
   dotnet run
   ```
3. **Test scraping a product**:
   - Scrape an Amazon product (e.g., B09H39M36G)
   - Verify all metadata is saved (brand, mpn, features, etc.)
4. **Generate CSV**:
   - Go to Inventory page
   - Select products
   - Generate CSV
   - Verify all fields are populated (not "Does Not Apply")
5. **Upload to eBay**:
   - Test upload to eBay File Exchange
   - Should have zero errors

## Rollback Instructions

If you need to rollback the database changes:

```sql
ALTER TABLE inventory 
DROP COLUMN IF EXISTS features_json,
DROP COLUMN IF EXISTS brand,
DROP COLUMN IF EXISTS mpn,
DROP COLUMN IF EXISTS model,
DROP COLUMN IF EXISTS color,
DROP COLUMN IF EXISTS size,
DROP COLUMN IF EXISTS product_type,
DROP COLUMN IF EXISTS department,
DROP COLUMN IF EXISTS ean,
DROP COLUMN IF EXISTS upc,
DROP COLUMN IF EXISTS isbn,
DROP COLUMN IF EXISTS height,
DROP COLUMN IF EXISTS width,
DROP COLUMN IF EXISTS length,
DROP COLUMN IF EXISTS weight;

ALTER TABLE user_settings
DROP COLUMN IF EXISTS paypal_email;
```

## Next Steps

1. ✅ Apply database migration
2. ✅ Test with real products
3. ✅ Upload test CSV to eBay
4. ✅ Monitor for any errors
5. Configure PayPal email in Settings page (needs frontend UI update)

