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
    'VATPercent',
  ]

  const rows = products.map((product) => {
    const description = buildDescriptionHtml(product.title, product.description)

    const fields = [
      'Add',
      product.ebayCategory ?? '',   // *Category — populated automatically from eBay API
      product.asin,
      product.title,
      '',                           // Subtitle
      description,
      '1000',                       // *ConditionID — New
      product.imageUrl ?? '',
      String(product.qty),
      'FixedPrice',
      product.sellingPrice != null ? String(product.sellingPrice) : '',
      '',                           // SaleTemplateName
      'Does Not Apply',             // Product:UPC
      'Does Not Apply',             // Product:ISBN
      'Does Not Apply',             // Product:EAN
      'Does Not Apply',             // Product:MPN
      'Does Not Apply',             // C:MPN
      'Does Not Apply',             // Product:Brand
      'Does Not Apply',             // C:Brand
      '1',                          // Product:IncludePrefilledItemInformation
      '1',                          // Product:IncludeStockPhotoURL
      '0',                          // Product:UseStockPhotoURLAsGallery
      '0',                          // Product:ReturnSearchResultsOnDuplicates
      'GBP',
      'GTC',
      '1',                          // ImmediatePayRequired
      '0',                          // ClickAndCollect
      '0',                          // CashOnPickup
      '0',                          // CCAccepted
      '0',                          // MOCashiers
      '0',                          // PaymentSeeDescription
      '1',                          // PaymentStatus
      '0',                          // HolidayReturns
      '1',                          // PayPalAccepted
      location,
      '',                           // PayPalEmailAddress
      '0',                          // PayUponPickup
      '0',                          // PersonalCheck
      '1',                          // *DispatchTimeMax
      '',                           // PaymentInstructions
      'ReturnsAccepted',
      'Days_30',
      'Buyer',
      '',                           // StoreCategory
      '',                           // StoreCategory2
      '',                           // Relationship
      '',                           // RelationshipDetails
      'Flat',
      'UK_OtherCourier48',
      '',                           // ShippingService-1:Cost (free)
      '1',                          // ShippingService-1:FreeShipping
      '1',                          // ShippingService-1:Priority
      'UK_OtherCourier24',
      '4.99',                       // ShippingService-2:Cost
      '0',                          // ShippingService-2:FreeShipping
      '2',                          // ShippingService-2:Priority
      'TRUE',                       // OutOfStockControl
      '1',                          // GlobalShipping
      '1',                          // GetItFast
      '0',                          // VATPercent
    ]

    return fields.map(csvEscape).join(',')
  })

  return [headers.map(csvEscape).join(','), ...rows].join('\r\n')
}

/**
 * Wraps a CSV field value in double quotes and escapes internal double quotes.
 */
function csvEscape(value) {
  const str = String(value ?? '')
  // Always quote — simplest safe approach
  return '"' + str.replace(/"/g, '""') + '"'
}

/**
 * Builds the eBay HTML description from the product title and raw description text.
 * Uses single-quoted HTML attributes to avoid CSV escaping issues.
 */
function buildDescriptionHtml(title, description) {
  // Sanitise inputs: strip double quotes so they don't break CSV quoting
  const safeTitle = (title ?? '').replace(/"/g, '&quot;')
  const safeDesc = description
    ? description.replace(/"/g, '&quot;').replace(/\n/g, '<br/>')
    : ''

  const descBlock = safeDesc
    ? `<p>${safeDesc}</p>`
    : ''

  return (
    `<!DOCTYPE html>` +
    `<html lang='en'>` +
    `<head>` +
    `<meta charset='utf-8'>` +
    `<meta http-equiv='X-UA-Compatible' content='IE=edge'>` +
    `<meta name='viewport' content='width=device-width, initial-scale=1'>` +
    `<style type='text/css'>` +
    `body{font-family:Arial,Helvetica,sans-serif;font-size:16px;color:#333;margin:0;padding:0;}` +
    `.header{background:##969696;padding:40px 20px;text-align:center;}` +
    `.header h1{color:#fff;font-size:26px;margin:0;}` +
    `.container{max-width:840px;margin:0 auto;padding:30px 20px;}` +
    `h3{font-size:20px;color:#333;margin-top:36px;}` +
    `p,li{line-height:1.7;}` +
    `</style>` +
    `</head>` +
    `<body>` +
    `<div class='header'><h1>${safeTitle}</h1></div>` +
    `<div class='container'>` +
    (descBlock ? `<h3>Description</h3>${descBlock}` : '') +
    `<h3>Why buy from us?</h3>` +
    `<p>We know waiting for products to arrive can be frustrating! So we aim to give you not only the best products but also deliver them as fast as possible.</p>` +
    `<h3>Delivery</h3>` +
    `<p>3-5 days depending on what you ordered.</p>` +
    `<h3>Return Policy</h3>` +
    `<p>You may return your item if you&apos;re not happy with it within 30 days of purchase.</p>` +
    `<h3>Feedback</h3>` +
    `<p>Once you have received your items please leave some feedback as it really helps us grow. Thank you!</p>` +
    `</div>` +
    `</body>` +
    `</html>`
  )
}
