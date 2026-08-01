/**
 * Generates an eBay "Product Creation CSV" in the File Exchange format.
 * @param {Array} products - Selected inventory items (UserInventoryDto shape)
 * @param {Object} settings - User settings { itemLocationPostcode, itemLocationCity }
 * @returns {string} CSV content ready for download
 */
export function generateEbayCsv(products, settings) {
  const location = [settings.itemLocationPostcode, settings.itemLocationCity]
    .filter(Boolean)
    .join(', ')

  const headers = [
    '*Action(SiteID=UK|Country=GB|Currency=GBP|Version=745|CC=UTF-8)',
    '*Category',
    'CustomLabel',
    'Title',
    'Subtitle',
    '*Description',
    '*ConditionID',
    'PicURL',
    '*Quantity',
    'Format',
    '*StartPrice',
    'SaleTemplateName',
    'Product:UPC',
    'Product:ISBN',
    'Product:EAN',
    'Product:MPN',
    'C:MPN',
    'Product:Brand',
    'C:Brand',
    'Product:IncludePrefilledItemInformation',
    'Product:IncludeStockPhotoURL',
    'Product:UseStockPhotoURLAsGallery',
    'Product:ReturnSearchResultsOnDuplicates',
    'Currency',
    '*Duration',
    'ImmediatePayRequired',
    'ClickAndCollect',
    'CashOnPickup',
    'CCAccepted',
    'MOCashiers',
    'PaymentSeeDescription',
    'PaymentStatus',
    'HolidayReturns',
    'PayPalAccepted',
    '*Location',
    'PayPalEmailAddress',
    'PayUponPickup',
    'PersonalCheck',
    '*DispatchTimeMax',
    'PaymentInstructions',
    '*ReturnsAcceptedOption',
    'ReturnsWithinOption',
    'ShippingCostPaidByOption',
    'StoreCategory',
    'StoreCategory2',
    'Relationship',
    'RelationshipDetails',
    '*ShippingType',
    'ShippingService-1:Option',
    'ShippingService-1:Cost',
    'ShippingService-1:FreeShipping',
    'ShippingService-1:Priority',
    'ShippingService-2:Option',
    'ShippingService-2:Cost',
    'ShippingService-2:FreeShipping',
    'ShippingService-2:Priority',
    'OutOfStockControl',
    'GlobalShipping',
    'GetItFast',
    'C:Type',
    'C:Size',
    'C:Colour',
    'C:Product',
    'C:Department',
    'C:Item Length',
    'C:Material',
    'C:Item Width',
    'C:Wireless Technology',
    'C:Storage Capacity',
    'C:Screen Size',
    'C:Number of Earpieces',
    'C:Microphone Type',
    'C:Memory Card(s) Supported',
    'C:Items Included',
    'C:Compatible Brand',
    'C:Compatible Model',
    'C:Chipset/GPU Model',
    'C:Chipset Manufacturer',
    'C:Case Size',
    'C:Band Material',
    'C:EAN',
    'C:Connectivity',
    'C:Format',
    'C:Model',
    'C:Sport/Activity',
    'C:Part Type',
    'C:ISBN',
    'C:Manufacturer Part Number',
    'C:Height',
    'C:Form Factor',
    'C:Power Source',
    'C:Processor',
    'VATPercent',
  ]

  const rows = products.map((product) => {
    // Build description HTML with features
    const description = buildDescriptionHtml(
      product.title, 
      product.description,
      product.features || []
    )

    // Handle multiple images - join with " | " separator (space-pipe-space)
    const imageUrls = [product.imageUrl, ...(product.imageUrls || [])]
      .filter(Boolean)
      .filter((url, index, arr) => arr.indexOf(url) === index)
    
    const picUrlField = imageUrls.join(' | ')

    const metadata = inferProductMetadata(product)
    const quantity = String(product.qty || 3)
    const startPrice = product.sellingPrice != null
      ? String(product.sellingPrice)
      : (product.amazonPrice != null ? String(product.amazonPrice) : '')

    // Extract brand, MPN, model from product or metadata
    const brand = product.brand || metadata.brand || 'Does Not Apply'
    const mpn = product.mpn || metadata.mpn || 'Does Not Apply'
    const model = product.model || metadata.model || 'Does Not Apply'
    const color = product.color || metadata.color || 'Does Not Apply'
    const size = product.size || metadata.size || 'Does Not Apply'
    const ean = product.ean || 'Does Not Apply'
    const upc = product.upc || 'Does Not Apply'
    const isbn = product.isbn || 'Does Not Apply'
    const height = product.height || metadata.height || ''
    const width = product.width || metadata.width || ''
    const length = product.length || metadata.length || ''

    const fields = [
      'Add',
      product.ebayCategory ?? '',
      product.asin ?? '',
      product.title ?? '',
      '',
      description,
      '1000',
      picUrlField,
      quantity,
      'FixedPrice',
      startPrice,
      '',
      upc,
      isbn,
      ean,
      mpn,
      mpn,
      brand,
      brand,
      '1',
      '1',
      '0',
      '0',
      'GBP',
      'GTC',
      '1',
      '0',
      '0',
      '0',
      '0',
      '0',
      '1',
      '0',
      '1',
      location,
      '',
      '0',
      '0',
      '1',
      '',
      'ReturnsAccepted',
      'Days_30',
      'Buyer',
      '',
      '',
      '',
      '',
      'Flat',
      'UK_OtherCourier48',
      '',
      '1',
      '1',
      'UK_OtherCourier24',
      '4.99',
      '0',
      '2',
      'TRUE',
      '1',
      '1',
      metadata.productType || 'Does Not Apply',
      size,
      color,
      'Does Not Apply',
      metadata.department || 'Does Not Apply',
      length,
      'Does Not Apply',
      width,
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      ean,
      'Does Not Apply',
      'Does Not Apply',
      model,
      'Does Not Apply',
      'Does Not Apply',
      isbn,
      mpn,
      height,
      'Does Not Apply',
      'Does Not Apply',
      'Does Not Apply',
      '0',
    ]

    const normalizedFields = fields.slice(0, headers.length)
    const paddedFields = normalizedFields.concat(
      Array(Math.max(0, headers.length - normalizedFields.length)).fill('')
    )

    return paddedFields.map(csvEscape).join(',')
  })

  return [headers.map(csvEscape).join(','), ...rows].join('\r\n')
}

/**
 * Wraps a CSV field value in double quotes and escapes internal double quotes.
 */
function csvEscape(value) {
  const str = String(value ?? '')
  return '"' + str.replace(/"/g, '""') + '"'
}

function inferProductMetadata(product) {
  const title = String(product?.title ?? '')
  const description = String(product?.description ?? '')
  const text = `${title} ${description}`.trim()

  return {
    brand: product?.brand || firstMatch(text, [
      /brand\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
      /manufacturer\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
    ]),
    mpn: product?.mpn || firstMatch(text, [
      /part\s+number\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
      /mpn\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
      /manufacturer\s+part\s+number\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
    ]),
    model: product?.model || firstMatch(text, [
      /model\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
    ]),
    color: product?.color || firstMatch(text, [
      /\b(black|white|blue|red|green|silver|gold|grey|gray|pink|purple|orange|yellow|brown)\b/i,
    ]),
    size: product?.size || firstMatch(text, [
      /\b(\d+(?:\.\d+)?(?:cm|mm|in|inch|inches|kg|g|lb|lbs|x\d+(?:cm|mm|in|inch|inches)))\b/i,
    ]),
    productType: product?.productType || firstMatch(text, [
      /\btype\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
      /\bcategory\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
    ]),
    department: product?.department || firstMatch(text, [
      /\bdepartment\s*[:\-]\s*([A-Za-z0-9&/().\s-]+)/i,
    ]),
    height: product?.height || '',
    width: product?.width || '',
    length: product?.length || '',
  }
}

function firstMatch(text, patterns) {
  for (const pattern of patterns) {
    const match = text.match(pattern)
    if (match && match[1]) {
      return match[1].trim()
    }
  }
  return ''
}

/**
 * Builds the eBay HTML description from the product title and raw description text.
 * Uses single-quoted HTML attributes to avoid CSV escaping issues.
 * Matches the Dilato template format with features as bullet points.
 */
function buildDescriptionHtml(title, description, features = []) {
  const safeTitle = (title ?? '').replace(/"/g, '&quot;')
  const safeDesc = description
    ? description.replace(/"/g, '&quot;').replace(/\n/g, '<br/>')
    : ''

  // Build features list if available
  const featuresHtml = features.length > 0
    ? `<br/><strong>Features: </strong><br/>` + 
      features.map(f => `<li>${f.replace(/"/g, '&quot;')}</li>`).join('')
    : ''

  const descBlock = safeDesc
    ? `<p>${safeDesc}${featuresHtml}</p>`
    : (featuresHtml ? `<p>${featuresHtml}</p>` : '')

  return (
    `<!DOCTYPE html>` +
    `<html lang='en'>` +
    `<head>` +
    `<meta charset='utf-8'>` +
    `<meta http-equiv='X-UA-Compatible' content='IE=edge'>` +
    `<meta name='viewport' content='width=device-width, initial-scale=1'>` +
    `<link href='https://fonts.googleapis.com/css2?family=Roboto:wght@300&display=swap' rel='stylesheet'>` +
    `<style type='text/css'>` +
    `#wrapper { font-family: 'Roboto', sans-serif; font-size: 19px; color: #333; background-color: #FFF;}` +
    `.container { width: 80%; margin: 0 auto; background-color: #FFF; border-style:solid; border-width:0px;}` +
    `li { margin-bottom: 8px;}` +
    `ul.list-unstyled li { list-style: none; margin-bottom: 0;}` +
    `ul.list-unstyled { margin: 0; padding: 0;}` +
    `.header { text-align: center; color: #FFF; font-size: 24px;}` +
    `.box { padding: 60px; background: #F2F2F2;}` +
    `h1 { font-size: 36px;}` +
    `h3 { font-size: 24px; color:#000;}` +
    `.section-header { margin-top: 50px;}` +
    `</style>` +
    `</head>` +
    `<body>` +
    `<div id='wrapper'>` +
    `<div class='header box'> ` +
    `<h1 style='color: #181818;'>${safeTitle}</h1>` +
    `</div>` +
    `<div class='container'> ` +
    `<h3>Description</h3> ` +
    descBlock +
    ` <ul class='list-unstyled'> ` +
    `<li> ` +
    `<h3 class='section-header'>Why buy from us?</h3> ` +
    `<p>We know waiting for products to arrive can be frustrating! So we aim to give you not only the best products but also deliver them as fast as possible.</p> ` +
    `</li> ` +
    `<li> ` +
    `<h3 class='section-header'>Delivery</h3> ` +
    `3-5 days depending on what you ordered. ` +
    `</li> ` +
    `<li> ` +
    `<h3 class='section-header'>Return Policy</h3> ` +
    `You may return your item if you're not happy with it within 30 days of purchase ` +
    `</li> ` +
    `<li> ` +
    `<h3 class='section-header'>Feedback</h3> ` +
    `<p> Once you have received your items please leave some feedback as it really helps us grow. Thank you! </p> ` +
    `<br> ` +
    `</li> ` +
    `</ul>` +
    `</div>` +
    `<div class='box'></div>` +
    `</div>` +
    `</body>` +
    `</html>`
  )
}
