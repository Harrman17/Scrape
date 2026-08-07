# Listing Health Feature

## Overview
The **Listing Health** feature allows users to sync their eBay listing statuses with their local inventory by uploading a "Price and Stock Update" CSV file downloaded from eBay.

## Features Created

### Backend (C# / .NET)

1. **Model**: `ListingHealthJob.cs`
   - Tracks each listing health check job
   - Fields: id, user_id, total_listings, processed_listings, updated_listings, failed_listings, job_complete, created_at, completed_at

2. **Repository**: `ListingHealthJobsRepository.cs`
   - `GetByUserAsync()` - Retrieve all jobs for a user
   - `CreateAsync()` - Create a new job
   - `UpdateCompletedAsync()` - Mark job as complete with statistics

3. **Controller**: `ListingHealthJobsController.cs`
   - `GET /api/listing-health-jobs` - Get all jobs for current user
   - `POST /api/listing-health-jobs` - Create a new job (CSV processing placeholder)

4. **Database Migration**: `create_listing_health_jobs_table.sql`
   - Creates `listing_health_jobs` table
   - Indexes for performance
   - Foreign key to users table

### Frontend (React)

1. **Page**: `ListingHealth.jsx`
   - Displays job history table
   - Upload button for eBay CSV files
   - Shows job statistics (total, processed, updated, failed)
   - Real-time status updates

2. **Navigation**: Updated `App.jsx`
   - Added "Listing Health" link in nav bar
   - Added route `/listing-health`

## How It Works (Current)

1. User navigates to **Listing Health** page
2. User uploads an eBay "Price and Stock Update" CSV
3. System creates a job record
4. Job is displayed in the history table

## Next Steps (To Be Implemented)

### CSV Processing

When you provide the example CSV, I'll implement:

1. **CSV Parser** - Parse the eBay CSV file
2. **Status Mapper** - Map eBay statuses to inventory statuses:
   - Active listings → "Active"
   - Out of stock → "Issues"
   - Ended listings → "Ended on eBay"
   - Unlisted items → "Unpaired"

3. **Inventory Updater** - Update `user_inventory.status` and `user_inventory.ebay_item_id`
4. **Job Statistics** - Track processed, updated, and failed listings

### Expected CSV Columns (Typical eBay Format)

Common columns in eBay's "Price and Stock Update" CSV:
- Item ID (eBay Item ID)
- Custom Label (could contain ASIN)
- Title
- Current Price
- Quantity Available
- Status (Active, Out of Stock, Ended, etc.)

## Database Schema

```sql
CREATE TABLE listing_health_jobs (
    id                  BIGSERIAL PRIMARY KEY,
    user_id             BIGINT NOT NULL REFERENCES users(id),
    total_listings      INT NOT NULL DEFAULT 0,
    processed_listings  INT NOT NULL DEFAULT 0,
    updated_listings    INT NOT NULL DEFAULT 0,
    failed_listings     INT NOT NULL DEFAULT 0,
    job_complete        BOOLEAN NOT NULL DEFAULT false,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);
```

## API Endpoints

### GET /api/listing-health-jobs
Get all listing health jobs for the authenticated user.

**Response:**
```json
[
  {
    "id": 1,
    "userId": 123,
    "totalListings": 50,
    "processedListings": 50,
    "updatedListings": 48,
    "failedListings": 2,
    "jobComplete": true,
    "createdAt": "2026-08-06T10:30:00Z",
    "completedAt": "2026-08-06T10:30:15Z"
  }
]
```

### POST /api/listing-health-jobs
Create a new listing health job.

**Request Body:**
```json
{
  "totalListings": 50
}
```

**Response:**
```json
{
  "id": 2,
  "userId": 123,
  "totalListings": 50,
  "processedListings": 0,
  "updatedListings": 0,
  "failedListings": 0,
  "jobComplete": false,
  "createdAt": "2026-08-06T11:00:00Z",
  "completedAt": null
}
```

## Running the Migration

To create the database table, run:

```bash
# Connect to your PostgreSQL database
psql -h your-host -U your-user -d your-database

# Run the migration
\i backend/migrations/create_listing_health_jobs_table.sql
```

Or using a PostgreSQL client:
```sql
-- Copy and execute the contents of:
-- backend/migrations/create_listing_health_jobs_table.sql
```

## Testing

1. Start the backend: `cd backend/api && dotnet run`
2. Start the frontend: `npm run dev`
3. Navigate to `/listing-health`
4. Upload a CSV file (processing not yet implemented)
5. View the job in the history table

## Files Modified

**Backend:**
- `/backend/api/Models/ListingHealthJob.cs` (new)
- `/backend/api/Services/ListingHealthJobsRepository.cs` (new)
- `/backend/api/Controllers/ListingHealthJobsController.cs` (new)
- `/backend/api/Program.cs` (added repository registration)
- `/backend/migrations/create_listing_health_jobs_table.sql` (new)

**Frontend:**
- `/src/pages/ListingHealth.jsx` (new)
- `/src/App.jsx` (added route and navigation)

## Ready for CSV Processing

Once you provide the example eBay CSV, I can implement:
- CSV parsing logic
- Status mapping and update logic
- Proper job tracking and statistics
- Error handling for invalid/missing data
