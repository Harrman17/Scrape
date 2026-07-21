import { useState } from 'react'

function ImportProducts() {
  const [asins, setAsins] = useState('')
  const [results, setResults] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const token = localStorage.getItem('authToken')

  async function handleSubmit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setResults([])

    try {
      const response = await fetch('http://localhost:5211/api/scrape', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          asins: asins
            .split(/\r?\n/)
            .map((value) => value.trim())
            .filter(Boolean),
        }),
      })

      const payload = await response.json()
      if (!response.ok) {
        throw new Error(payload.error || 'Scraping failed')
      }

      setResults(payload.saved || [])
      if (payload.errors && payload.errors.length > 0) {
        setError(`Imported ${payload.saved.length} products with ${payload.errors.length} errors`)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Scraping failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="grid min-h-screen place-items-center p-8 dark:bg-slate-950">
      <div className="w-full max-w-[760px] rounded-3xl border border-gray-200 bg-white p-8 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        <h1 className="mt-2 text-3xl font-bold dark:text-slate-100">Pull product details from multiple ASINs</h1>

        <p className="mb-6 mt-2 text-gray-600 dark:text-slate-400">
          Paste one ASIN per line and the app will return the title, price and ASIN for each product.
        </p>

        <form onSubmit={handleSubmit} className="flex flex-wrap gap-3">
          <textarea
            value={asins}
            onChange={(event) => setAsins(event.target.value)}
            className="min-h-40 min-w-[320px] flex-1 rounded-xl border border-slate-300 px-4 py-3 text-base outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
            placeholder={"B09H39M36G\nB09H39M37H\nB091234ASDF"}
            aria-label="Amazon ASINs"
          />

          <button
            type="submit"
            disabled={loading}
            className="cursor-pointer rounded-xl bg-blue-600 px-4 py-3 text-white transition hover:bg-blue-700 disabled:cursor-wait disabled:opacity-70"
          >
            {loading ? 'Scraping…' : 'Scrape Product'}
          </button>
        </form>

        {error ? <p className="mt-4 text-red-700 dark:text-red-400">{error}</p> : null}

        {results.length > 0 && (
          <div className="mt-6">
            <h2 className="mb-3 text-lg font-semibold text-slate-900 dark:text-slate-100">
              Imported Products ({results.length})
            </h2>
            <div className="space-y-2 rounded-lg border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-800">
              {results.map((product) => (
                <div key={product.asin} className="rounded bg-white p-3 dark:bg-slate-700">
                  <div className="flex items-start gap-3">
                    {product.imageUrl && (
                      <img
                        src={product.imageUrl}
                        alt={product.asin}
                        className="h-12 w-12 rounded object-contain"
                      />
                    )}
                    <div className="flex-1">
                      <h3 className="line-clamp-2 font-medium text-slate-900 dark:text-slate-100">
                        {product.title}
                      </h3>
                      <div className="mt-1 flex gap-4 text-sm text-slate-600 dark:text-slate-400">
                        <span>
                          Amazon: <span className="font-semibold">{product.currency}{product.amazonPrice ?? '—'}</span>
                        </span>
                        <span>
                          Selling: <span className="font-semibold text-green-600 dark:text-green-400">
                            {product.sellingPrice != null
                              ? `${product.currency}${product.sellingPrice.toFixed(2)}`
                              : '—'}
                          </span>
                        </span>
                        <span>
                          Qty: <span className="font-semibold">{product.qty}</span>
                        </span>
                        <span className="font-mono text-xs">{product.asin}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </section>
  )
}

export default ImportProducts
