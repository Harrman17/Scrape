# Amazon Price and Stock Update System

## Overview

The price and stock update system provides automated monitoring of Amazon product prices and availability. It's designed for frequent execution (multiple times per day) to keep eBay listings synchronized with Amazon's current prices and stock status.

## Components

### 1. Python Script: `amzPriceStockUpdate.py`

A lightweight scraper optimized for price and stock checks only. Unlike the full product scraper, this script:
- Only fetches price and availability data
- Has reduced timeouts for faster execution
- Minimal data processing
- Returns JSON output for easy parsing

**Usage:**
```bash
python3 amzPriceStockUpdate.py ASIN1 ASIN2 ASIN3
```

**Output:**
```json
[
  {
    "asin": "B08N5WRWNW",
    "price": 29.99,
    "currency": "GBP",
    "in_stock": true,
    "error": null
  }
]
```

### 2. API Endpoints

#### POST `/api/scrape/update-prices`
Updates prices and stock for all products in the user's inventory.

**Response:**
```json
{
  "totalChecked": 50,
  "updated": 5,
  "errors": 0,
  "changes": [
    {
      "asin": "B08N5WRWNW",
      "title": "Product Name",
      "oldPrice": 29.99,
      "newPrice": 24.99,
      "priceChanged": true,
      "oldStock": true,
      "newStock": true,
      "stockChanged": false
    }
  ]
}
```

#### GET `/api/scrape/ebay-update-csv`
Generates a CSV file for bulk updating eBay listings with new prices and stock quantities.

**CSV Format:**
eBay File Exchange format with columns:
- CustomLabel (eBay Item ID)
- Action (Revise)
- Quantity (0 if out of stock)
- StartPrice (New calculated price with markup)
- SiteID (3 = UK)
- Format (FixedPriceItem)

## AWS Deployment

### Option 1: AWS Lambda (Recommended for Production)

**Setup:**
1. Create a Lambda function with Python 3.11+ runtime
2. Install Playwright in Lambda layer:
   ```bash
   pip install playwright -t python/
   python -m playwright install chromium
   ```
3. Package the script with dependencies
4. Set up EventBridge (CloudWatch Events) for scheduling
5. Configure environment variables for database connection

**Schedule Examples:**
- Every 4 hours: `rate(4 hours)`
- Three times daily: `cron(0 8,14,20 * * ? *)`
- Every 2 hours during business hours: `cron(0 8-18/2 * * ? *)`

**Benefits:**
- Serverless (no server management)
- Pay per execution
- Automatic scaling
- Easy scheduling with EventBridge

### Option 2: EC2 with Cron Jobs

**Setup:**
1. Launch EC2 instance (t3.small or larger)
2. Install dependencies:
   ```bash
   sudo apt update
   sudo apt install python3-pip
   pip3 install playwright beautifulsoup4
   python3 -m playwright install
   ```
3. Set up cron job:
   ```bash
   crontab -e
   # Run every 4 hours
   0 */4 * * * cd /path/to/app && /usr/bin/python3 backend/py/amzPriceStockUpdate.py
   ```

**Benefits:**
- More control over environment
- Easier debugging
- Can run full API alongside

### Option 3: ECS/Fargate Scheduled Tasks

**Setup:**
1. Create Docker container with script
2. Push to ECR
3. Create ECS scheduled task
4. Configure schedule expression

**Benefits:**
- Container-based (consistent environment)
- No server management
- Easy to version and deploy

## Configuration

### API Settings (appsettings.json)

```json
{
  "Scraper": {
    "PythonExecutable": "python3",
    "ScriptPath": "/path/to/amzProductScrape.py",
    "PriceUpdateScriptPath": "/path/to/amzPriceStockUpdate.py"
  }
}
```

### Environment Variables (for AWS)

- `DATABASE_CONNECTION_STRING`: PostgreSQL connection string
- `SCRAPER_SCRIPT_PATH`: Path to price update script in Lambda/container
- `PYTHON_EXECUTABLE`: Python binary path (default: python3)

## Performance Optimization

### For Frequent Updates

1. **Batch Processing**: Process products in chunks
   ```python
   # Process 10 products at a time
   batch_size = 10
   for i in range(0, len(asins), batch_size):
       batch = asins[i:i+batch_size]
       # Process batch
   ```

2. **Caching**: Use Redis/ElastiCache to cache recent checks
   - Cache results for 1-2 hours
   - Reduce redundant scraping

3. **Rate Limiting**: Avoid Amazon blocks
   - Add delays between requests
   - Rotate user agents
   - Consider using proxies

### Cost Optimization

- **Lambda**: ~$0.20 per 1M requests + compute time
- **EC2**: Fixed monthly cost (~$15/month for t3.small)
- **Scheduled execution**: Only run during business hours to reduce costs

## Monitoring

### CloudWatch Metrics (AWS)

Track:
- Execution count
- Error rate
- Duration
- Products updated per run

### Alerts

Set up alerts for:
- High error rates (>10%)
- Execution failures
- Long execution times (>5 minutes)

## Best Practices

1. **Error Handling**: Always log errors but continue processing other products
2. **Retries**: Implement exponential backoff for failed requests
3. **Validation**: Verify price changes aren't anomalous before updating
4. **Notifications**: Send email/Slack notifications for significant price changes
5. **Audit Log**: Keep history of all price changes for analysis

## Frontend Integration

The Inventory page includes:
- **Update Prices** button: Triggers manual price check
- **eBay Update CSV** button: Downloads CSV for bulk eBay updates
- Price change notifications with before/after comparison
- Stock status changes highlighted

## Troubleshooting

### Script fails with "playwright not found"
```bash
python3 -m playwright install
```

### Timeout errors
Increase timeout in script:
```python
page.goto(url, wait_until="domcontentloaded", timeout=30000)
```

### Amazon blocking requests
- Use residential proxies
- Rotate user agents
- Add random delays
- Reduce frequency

### Database connection issues
Check:
- Connection string format
- Database credentials
- Network security groups (AWS)
- VPC configuration (if using private subnets)

## Future Enhancements

1. **Parallel Processing**: Use asyncio for concurrent scraping
2. **Price History**: Track historical prices for trend analysis
3. **Smart Scheduling**: Only check products that frequently change
4. **Notifications**: Alert users to significant price drops
5. **Auto-adjust eBay prices**: Automatically update eBay listings when Amazon prices change
