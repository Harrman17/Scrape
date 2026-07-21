import { useState, useEffect } from 'react'

const PAGE_SIZE = 10

function Inventory() {
  const [products, setProducts] = useState([])
  const [selectedIds, setSelectedIds] = useState([])
  const [currentPage, setCurrentPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const token = localStorage.getItem('authToken')

  useEffect(() => {
    loadInventory()
  }, [])

  async function loadInventory() {
    try {
      setLoading(true)
      const response = await fetch('http://localhost:5211/api/inventory', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!response.ok) throw new Error('Failed to load inventory')
      const data = await response.json()
      setProducts(data)
      setError('')
    } catch (err) {
      setError(err.message)
      console.error('Failed to load inventory:', err)
    } finally {
      setLoading(false)
    }
  }

  const totalPages = Math.max(1, Math.ceil(products.length / PAGE_SIZE))
  const pageProducts = products.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE)
  const pageIds = pageProducts.map((p) => p.userInventoryId)

  const allPageSelected = pageProducts.length > 0 && pageIds.every((id) => selectedIds.includes(id))

  function selectAllInPage() {
    setSelectedIds((current) => [...new Set([...current, ...pageIds])])
  }

  function deselectAllInPage() {
    setSelectedIds((current) => current.filter((id) => !pageIds.includes(id)))
  }

  function selectAll() {
    setSelectedIds(products.map((p) => p.userInventoryId))
  }

  function toggleSelectOne(id) {
    setSelectedIds((current) =>
      current.includes(id) ? current.filter((selectedId) => selectedId !== id) : [...current, id]
    )
  }

  async function handleDelete(id) {
    try {
      const response = await fetch(`http://localhost:5211/api/inventory/${id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!response.ok) throw new Error('Failed to delete product')
      setProducts((current) => current.filter((product) => product.userInventoryId !== id))
      setSelectedIds((current) => current.filter((selectedId) => selectedId !== id))
    } catch (err) {
      console.error('Failed to delete product:', err)
      alert('Failed to delete product')
    }
  }

  async function handleDeleteSelected() {
    if (!confirm(`Delete ${selectedIds.length} products?`)) return

    let deleted = 0
    for (const id of selectedIds) {
      try {
        const response = await fetch(`http://localhost:5211/api/inventory/${id}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${token}` }
        })
        if (response.ok) deleted++
      } catch (err) {
        console.error(`Failed to delete product ${id}:`, err)
      }
    }

    if (deleted > 0) {
      setProducts((current) => current.filter((product) => !selectedIds.includes(product.userInventoryId)))
      setSelectedIds([])
    }
  }

  function handlePageHeaderCheckbox() {
    allPageSelected ? deselectAllInPage() : selectAllInPage()
  }

  if (loading) {
    return (
      <section className="grid min-h-screen place-items-center p-8 dark:bg-slate-950">
        <p className="text-slate-600 dark:text-slate-400">Loading inventory...</p>
      </section>
    )
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-[1400px] rounded-3xl border border-gray-200 bg-white p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        {error && <div className="mb-4 rounded bg-red-100 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">{error}</div>}

        {/* Header row */}
        <div className="mb-4 flex items-center justify-between">
          <h1 className="text-2xl font-bold dark:text-slate-100">
            My Inventory
            {products.length > 0 && (
              <span className="ml-2 text-sm font-normal text-slate-400">{products.length} products</span>
            )}
          </h1>

          {/* Action buttons */}
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={selectAllInPage}
              disabled={products.length === 0}
              className="cursor-pointer rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
            >
              Select page
            </button>
            <button
              type="button"
              onClick={deselectAllInPage}
              disabled={products.length === 0}
              className="cursor-pointer rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
            >
              Deselect page
            </button>
            <button
              type="button"
              onClick={selectAll}
              disabled={products.length === 0}
              className="cursor-pointer rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
            >
              Select all
            </button>
            <button
              type="button"
              onClick={handleDeleteSelected}
              disabled={selectedIds.length === 0}
              className="cursor-pointer rounded-md bg-red-600 px-2.5 py-1 text-xs font-medium text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Delete selected{selectedIds.length > 0 ? ` (${selectedIds.length})` : ''}
            </button>
          </div>
        </div>

        <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-700">
          <table className="w-full border-collapse text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400">
                <th className="w-8 px-3 py-2">
                  <input
                    type="checkbox"
                    checked={allPageSelected}
                    onChange={handlePageHeaderCheckbox}
                    disabled={products.length === 0}
                    aria-label="Select all products on this page"
                  />
                </th>
                <th className="w-12 px-3 py-2">Image</th>
                <th className="px-3 py-2">Title</th>
                <th className="px-3 py-2">Amazon Price</th>
                <th className="px-3 py-2">Selling Price</th>
                <th className="px-3 py-2">Qty</th>
                <th className="px-3 py-2">ASIN</th>
                <th className="px-3 py-2">eBay Item ID</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
              {products.length === 0 ? (
                <tr>
                  <td colSpan={10} className="px-3 py-8 text-center text-slate-500 dark:text-slate-400">
                    No products to display yet. <a href="/import" className="text-blue-600 hover:underline">Import some products</a>.
                  </td>
                </tr>
              ) : (
                pageProducts.map((product, i) => (
                  <tr
                    key={product.userInventoryId}
                    className={`text-slate-700 transition-colors hover:bg-blue-50/40 dark:text-slate-300 dark:hover:bg-slate-800/60 ${
                      i % 2 === 1 ? 'bg-slate-50/60 dark:bg-slate-800/20' : 'bg-white dark:bg-transparent'
                    }`}
                  >
                    <td className="px-3 py-1.5">
                      <input
                        type="checkbox"
                        checked={selectedIds.includes(product.userInventoryId)}
                        onChange={() => toggleSelectOne(product.userInventoryId)}
                        aria-label={`Select ${product.asin}`}
                      />
                    </td>
                    <td className="px-3 py-1.5">
                      {product.imageUrl ? (
                        <img
                          src={product.imageUrl}
                          alt={product.asin}
                          className="h-9 w-9 rounded object-contain"
                        />
                      ) : (
                        <div className="h-9 w-9 rounded bg-slate-100 dark:bg-slate-700" />
                      )}
                    </td>
                    <td className="max-w-[260px] px-3 py-1.5">
                      <span className="line-clamp-2 leading-snug">{product.title}</span>
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 tabular-nums">
                      {product.currency || '£'}{product.amazonPrice ?? '—'}
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 tabular-nums font-medium">
                      {product.sellingPrice != null ? `${product.currency || '£'}${product.sellingPrice.toFixed(2)}` : '—'}
                    </td>
                    <td className="px-3 py-1.5 tabular-nums">{product.qty}</td>
                    <td className="px-3 py-1.5 font-mono text-xs tracking-wide text-slate-500 dark:text-slate-400">
                      {product.asin}
                    </td>
                    <td className="px-3 py-1.5 font-mono text-xs text-slate-500 dark:text-slate-400">
                      {product.ebayItemId ?? <span className="font-sans text-slate-400">—</span>}
                    </td>
                    <td className="px-3 py-1.5">
                      <span
                        className={`inline-block rounded px-2 py-0.5 text-xs font-medium ${
                          product.status === 'ACTIVE'
                            ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400'
                            : product.status === 'PENDING'
                              ? 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-400'
                              : 'bg-slate-100 text-slate-500 dark:bg-slate-700 dark:text-slate-400'
                        }`}
                      >
                        {product.status}
                      </span>
                    </td>
                    <td className="px-3 py-1.5">
                      <button
                        type="button"
                        onClick={() => handleDelete(product.userInventoryId)}
                        className="cursor-pointer rounded bg-red-600 px-2.5 py-1 text-xs font-medium text-white transition hover:bg-red-700"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="mt-3 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
          <span>
            {products.length > 0
              ? `Showing ${(currentPage - 1) * PAGE_SIZE + 1}–${Math.min(currentPage * PAGE_SIZE, products.length)} of ${products.length}`
              : 'No products'}
          </span>
          {totalPages > 1 && (
            <div className="flex gap-1.5">
              <button
                type="button"
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className="cursor-pointer rounded border border-slate-300 px-2.5 py-1 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-600 dark:hover:bg-slate-800"
              >
                ← Prev
              </button>
              <span className="flex items-center px-2 font-medium">
                {currentPage} / {totalPages}
              </span>
              <button
                type="button"
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                className="cursor-pointer rounded border border-slate-300 px-2.5 py-1 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-600 dark:hover:bg-slate-800"
              >
                Next →
              </button>
            </div>
          )}
        </div>
      </div>
    </section>
  )
}

export default Inventory


 