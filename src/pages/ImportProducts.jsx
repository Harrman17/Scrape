import { useState } from 'react'

function ImportProducts() {
  const [asins, setAsins] = useState('')
  const [results, setResults] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setResults([])

    try {
      const response = await fetch('http://localhost:5211/api/scrape', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
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

      setResults(Array.isArray(payload) ? payload : [payload])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Scraping failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="grid min-h-screen place-items-center p-8">
      <div className="w-full max-w-[760px] rounded-3xl border border-gray-200 bg-white p-8 shadow-[0_18px_50px_rgba(15,23,42,0.08)]">
        <p className="text-xs font-bold uppercase tracking-[0.24em] text-blue-600">Amazon Scraper</p>

        <h1 className="mt-2 text-3xl font-bold">Pull product details from multiple ASINs</h1>

        <p className="mb-6 mt-2 text-gray-600">
          Paste one ASIN per line and the app will return the title, price and ASIN for each product.
        </p>

        <form onSubmit={handleSubmit} className="flex flex-wrap gap-3">
          <textarea
            value={asins}
            onChange={(event) => setAsins(event.target.value)}
            className="min-h-40 min-w-[320px] flex-1 rounded-xl border border-slate-300 px-4 py-3 text-base outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
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

        {error ? <p className="mt-4 text-red-700">{error}</p> : null}

        {results.length > 0 ? (
          <div className="mt-6 grid gap-4">
            {results.map((result) => (
              <div key={result.asin} className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-left">
                <h2 className="text-xl font-semibold">{result.title}</h2>
                <p>
                  <strong>ASIN:</strong> {result.asin}
                </p>
                <p>
                  <strong>Price:</strong> {result.price}
                </p>
                <a href={result.url} target="_blank" rel="noreferrer" className="font-semibold text-blue-600 hover:underline">
                  Open product page
                </a>
              </div>
            ))}
          </div>
        ) : null}
      </div>
    </section>
  )
}

export default ImportProducts
