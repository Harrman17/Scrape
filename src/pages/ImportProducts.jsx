import { useState } from 'react'
import { Link } from 'react-router-dom'
import Loading from '../components/Loading'

function ImportProducts() {
  const [asins, setAsins] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(false)
  const token = localStorage.getItem('authToken')

  async function handleSubmit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setSuccess('')

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

      const saved = payload.saved?.length ?? 0
      const errors = payload.errors?.length ?? 0
      const blocked = payload.blocked?.length ?? 0
      
      let message = `Successfully imported ${saved} product${saved !== 1 ? 's' : ''}.`
      
      if (blocked > 0) {
        message += ` ${blocked} duplicate${blocked !== 1 ? 's' : ''} blocked.`
      }
      
      if (errors > 0) {
        message += ` ${errors} error${errors !== 1 ? 's' : ''}.`
      }
      
      setSuccess(message)
      setAsins('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Scraping failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-[560px] rounded-3xl border border-gray-200 bg-white p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        <div className="mb-5 flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold dark:text-slate-100">Import Products</h1>
            <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">
              Paste one ASIN per line.
            </p>
          </div>
          <Link
            to="/import-history"
            className="shrink-0 text-sm font-medium text-blue-600 hover:underline dark:text-blue-400"
          >
            Import History →
          </Link>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <textarea
            value={asins}
            onChange={(event) => setAsins(event.target.value)}
            className="h-32 w-full rounded-xl border border-slate-300 px-4 py-3 text-base outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
            placeholder={"B09H39M36G\nB09H39M37H\nB091234ASDF"}
            aria-label="Amazon ASINs"
          />

          {loading && (
            <div className="flex justify-center py-4">
              <Loading message="Scraping products..." />
            </div>
          )}
          <button
            type="submit"
            disabled={loading}
            className="cursor-pointer rounded-xl bg-blue-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-wait disabled:opacity-70"
          >
            {loading ? 'Scraping…' : 'Scrape Products'}
          </button>
        </form>

        {error ? <p className="mt-3 text-sm text-red-700 dark:text-red-400">{error}</p> : null}
        {success ? <p className="mt-3 text-sm text-green-700 dark:text-green-400">{success}</p> : null}
      </div>
    </section>
  )
}

export default ImportProducts
