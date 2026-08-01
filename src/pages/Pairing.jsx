import { useState, useRef } from 'react'

const API = 'http://localhost:5211'

/**
 * Parse the eBay pairing CSV downloaded from eBay Seller Hub / File Exchange.
 * Returns an array of { itemNumber, customLabel } objects.
 */
function parsePairingCsv(text) {
  const lines = text.split(/\r?\n/)
  if (lines.length < 2) return []

  // Find header row (first non-empty line)
  const headerLine = lines[0]
  const headers = headerLine.split(',').map((h) => h.replace(/^"|"$/g, '').trim())

  const itemNumberIdx = headers.findIndex((h) => h.toLowerCase() === 'item number')
  const customLabelIdx = headers.findIndex(
    (h) => h.toLowerCase().includes('custom label') || h.toLowerCase().includes('sku')
  )

  if (itemNumberIdx === -1 || customLabelIdx === -1) return null

  const rows = []
  for (let i = 1; i < lines.length; i++) {
    const line = lines[i].trim()
    if (!line) continue
    const cols = splitCsvLine(line)
    const itemNumber = cols[itemNumberIdx]?.replace(/^"|"$/g, '').trim()
    const customLabel = cols[customLabelIdx]?.replace(/^"|"$/g, '').trim()
    if (itemNumber && customLabel) {
      rows.push({ itemNumber, customLabel })
    }
  }
  return rows
}

/** Splits a single CSV line respecting quoted fields. */
function splitCsvLine(line) {
  const result = []
  let current = ''
  let inQuotes = false
  for (let i = 0; i < line.length; i++) {
    const ch = line[i]
    if (ch === '"') {
      if (inQuotes && line[i + 1] === '"') {
        current += '"'
        i++
      } else {
        inQuotes = !inQuotes
      }
    } else if (ch === ',' && !inQuotes) {
      result.push(current)
      current = ''
    } else {
      current += ch
    }
  }
  result.push(current)
  return result
}

export default function Pairing() {
  const [file, setFile] = useState(null)
  const [preview, setPreview] = useState(null) // parsed CSV rows before applying
  const [parseError, setParseError] = useState('')
  const [inventory, setInventory] = useState(null)
  const [applying, setApplying] = useState(false)
  const [results, setResults] = useState(null) // { matched, unmatched, errors }
  const fileInputRef = useRef(null)
  const token = localStorage.getItem('authToken')

  async function handleFileChange(e) {
    const selected = e.target.files?.[0]
    if (!selected) return
    setFile(selected)
    setPreview(null)
    setParseError('')
    setResults(null)

    const text = await selected.text()
    const rows = parsePairingCsv(text)
    if (rows === null) {
      setParseError('Could not find "Item number" or "Custom label (SKU)" columns. Make sure you are uploading an eBay pairing CSV.')
      return
    }
    if (rows.length === 0) {
      setParseError('No rows found in the CSV file.')
      return
    }
    setPreview(rows)
  }

  async function loadInventory() {
    const res = await fetch(`${API}/api/inventory`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (!res.ok) throw new Error('Failed to load inventory')
    return await res.json()
  }

  async function handleApply() {
    if (!preview || preview.length === 0) return
    setApplying(true)
    setResults(null)

    try {
      const inv = inventory ?? (await loadInventory())
      setInventory(inv)

      // Build a map: ASIN → inventory item
      const asinMap = new Map()
      for (const item of inv) {
        asinMap.set(item.asin?.toUpperCase(), item)
      }

      const matched = []
      const unmatched = []

      for (const row of preview) {
        const item = asinMap.get(row.customLabel.toUpperCase())
        if (item) {
          matched.push({ row, item })
        } else {
          unmatched.push(row)
        }
      }

      const errors = []
      let successCount = 0

      for (const { row, item } of matched) {
        try {
          const res = await fetch(`${API}/api/inventory/${item.userInventoryId}`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/json',
              Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify({
              qty: item.qty,
              status: 1, // Active
              ebayItemId: row.itemNumber,
            }),
          })
          if (!res.ok) {
            const body = await res.text()
            errors.push({ asin: item.asin, error: body })
          } else {
            successCount++
          }
        } catch (err) {
          errors.push({ asin: item.asin, error: err.message })
        }
      }

      setResults({ matched: successCount, unmatched, errors })
      // Refresh inventory cache
      const refreshed = await loadInventory()
      setInventory(refreshed)
    } catch (err) {
      setParseError(err.message)
    } finally {
      setApplying(false)
    }
  }

  function handleReset() {
    setFile(null)
    setPreview(null)
    setParseError('')
    setResults(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-3xl rounded-3xl border border-gray-200 bg-white p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        <h1 className="mb-1 text-2xl font-bold dark:text-slate-100">eBay Pairing</h1>
        <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">
          Upload the CSV downloaded from eBay Seller Hub to pair your inventory items with their eBay listings.
          Products are matched by <span className="font-medium text-slate-700 dark:text-slate-300">Custom Label (SKU / ASIN)</span> and their status will be set to <span className="font-medium text-green-600">Active</span>.
        </p>

        {/* Upload area */}
        <div className="mb-4">
          <label
            htmlFor="pairing-csv"
            className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-300 bg-slate-50 p-10 text-sm text-slate-500 transition hover:border-blue-400 hover:bg-blue-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400 dark:hover:border-blue-500 dark:hover:bg-slate-700/50"
          >
            <svg className="h-8 w-8 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
            </svg>
            <span>{file ? file.name : 'Click to upload eBay pairing CSV'}</span>
            <span className="text-xs opacity-60">.csv files only</span>
            <input
              id="pairing-csv"
              ref={fileInputRef}
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={handleFileChange}
            />
          </label>
        </div>

        {parseError && (
          <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
            {parseError}
          </div>
        )}

        {/* Preview table */}
        {preview && !results && (
          <div className="mb-4">
            <p className="mb-2 text-sm text-slate-600 dark:text-slate-400">
              Found <span className="font-semibold text-slate-800 dark:text-slate-200">{preview.length}</span> listing{preview.length !== 1 ? 's' : ''} in the pairing file.
            </p>
            <div className="max-h-64 overflow-y-auto rounded-xl border border-slate-200 dark:border-slate-700">
              <table className="w-full text-left text-xs">
                <thead className="sticky top-0 bg-slate-50 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                  <tr>
                    <th className="px-4 py-2 font-medium">eBay Item Number</th>
                    <th className="px-4 py-2 font-medium">Custom Label (ASIN)</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
                  {preview.map((row, i) => (
                    <tr key={i} className="dark:text-slate-300">
                      <td className="px-4 py-2 font-mono">{row.itemNumber}</td>
                      <td className="px-4 py-2 font-mono">{row.customLabel}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-4 flex gap-2">
              <button
                type="button"
                onClick={handleApply}
                disabled={applying}
                className="cursor-pointer rounded-md bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {applying ? 'Applying…' : `Apply Pairing (${preview.length})`}
              </button>
              <button
                type="button"
                onClick={handleReset}
                className="cursor-pointer rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800"
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        {/* Results */}
        {results && (
          <div className="space-y-3">
            <div className="flex flex-wrap gap-3">
              <div className="flex-1 rounded-xl border border-green-200 bg-green-50 p-4 dark:border-green-800 dark:bg-green-900/20">
                <p className="text-2xl font-bold text-green-700 dark:text-green-400">{results.matched}</p>
                <p className="text-sm text-green-600 dark:text-green-500">Paired successfully</p>
              </div>
              <div className="flex-1 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-900/20">
                <p className="text-2xl font-bold text-amber-700 dark:text-amber-400">{results.unmatched.length}</p>
                <p className="text-sm text-amber-600 dark:text-amber-500">Not found in inventory</p>
              </div>
              {results.errors.length > 0 && (
                <div className="flex-1 rounded-xl border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-900/20">
                  <p className="text-2xl font-bold text-red-700 dark:text-red-400">{results.errors.length}</p>
                  <p className="text-sm text-red-600 dark:text-red-500">API errors</p>
                </div>
              )}
            </div>

            {results.unmatched.length > 0 && (
              <details className="rounded-xl border border-slate-200 dark:border-slate-700">
                <summary className="cursor-pointer select-none px-4 py-3 text-sm font-medium text-slate-700 dark:text-slate-300">
                  Unmatched listings ({results.unmatched.length})
                </summary>
                <ul className="divide-y divide-slate-100 px-4 pb-3 text-xs dark:divide-slate-700/60 dark:text-slate-400">
                  {results.unmatched.map((row, i) => (
                    <li key={i} className="flex gap-4 py-1.5 font-mono">
                      <span className="text-slate-400">{row.itemNumber}</span>
                      <span>{row.customLabel}</span>
                    </li>
                  ))}
                </ul>
              </details>
            )}

            {results.errors.length > 0 && (
              <details className="rounded-xl border border-red-200 dark:border-red-800">
                <summary className="cursor-pointer select-none px-4 py-3 text-sm font-medium text-red-700 dark:text-red-400">
                  Errors ({results.errors.length})
                </summary>
                <ul className="divide-y divide-red-100 px-4 pb-3 text-xs dark:divide-red-900/40 dark:text-red-300">
                  {results.errors.map((e, i) => (
                    <li key={i} className="py-1.5">
                      <span className="font-mono font-medium">{e.asin}</span>: {e.error}
                    </li>
                  ))}
                </ul>
              </details>
            )}

            <button
              type="button"
              onClick={handleReset}
              className="cursor-pointer rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              Upload another file
            </button>
          </div>
        )}
      </div>
    </section>
  )
}
