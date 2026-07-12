import React from 'react'
import { useState } from 'react'


function ImportProducts() {


  const [url, setUrl] = useState('')
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setResult(null)

    try {
      const response = await fetch('http://localhost:5211/api/scrape', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url }),
      })

      const payload = await response.json()
      if (!response.ok) {
        throw new Error(payload.error || 'Scraping failed')
      }

      setResult(payload)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }



  return (
    <div>
     <section className="hero-card">
        <p className="eyebrow">Amazon scraper</p>
        <h1>Pull product details from an Amazon URL</h1>
        <p className="subtitle">
          Paste a product page URL with an ASIN and the app will return the title, price, and ASIN from the backend scraper.
        </p>

        <form onSubmit={handleSubmit} className="scrape-form">
          <input
            value={url}
            onChange={(event) => setUrl(event.target.value)}
            placeholder="https://www.amazon.com/dp/B0ABC12345"
            aria-label="Amazon product URL"
          />
          <button type="submit" disabled={loading}>
            {loading ? 'Scraping…' : 'Scrape product'}
          </button>
        </form>

        {error ? <p className="message error">{error}</p> : null}

        {result ? (
          <div className="result-card">
            <h2>{result.title}</h2>
            <p><strong>ASIN:</strong> {result.asin}</p>
            <p><strong>Price:</strong> {result.price}</p>
            <a href={result.url} target="_blank" rel="noreferrer">
              Open product page
            </a>
          </div>
        ) : null}
      </section>
    </div>
  )
}

export default ImportProducts
