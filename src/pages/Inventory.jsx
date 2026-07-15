import { useState } from 'react'

// Placeholder data until the database/API is wired up.
const placeholderProducts = []

function Inventory() {
  const [products, setProducts] = useState(placeholderProducts)
  const [selectedIds, setSelectedIds] = useState([])

  const allSelected = products.length > 0 && selectedIds.length === products.length

  function toggleSelectAll() {
    setSelectedIds(allSelected ? [] : products.map((product) => product.id))
  }

  function toggleSelectOne(id) {
    setSelectedIds((current) =>
      current.includes(id) ? current.filter((selectedId) => selectedId !== id) : [...current, id]
    )
  }

  function handleDelete(id) {
    setProducts((current) => current.filter((product) => product.id !== id))
    setSelectedIds((current) => current.filter((selectedId) => selectedId !== id))
  }

  return (
    <section className="min-h-screen p-8 dark:bg-slate-950">
      <div className="mx-auto max-w-6xl rounded-3xl border border-gray-200 bg-white p-8 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">
        <h1 className="mt-2 text-3xl font-bold dark:text-slate-100">Inventory</h1>
        <p className="mb-6 mt-2 text-gray-600 dark:text-slate-400">Products imported from the database will appear here.</p>

        <div className="overflow-x-auto rounded-2xl border border-slate-200 dark:border-slate-700">
          <table className="w-full border-collapse text-left">
            <thead>
              <tr className="bg-slate-50 text-sm uppercase tracking-wide text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                <th className="px-4 py-3">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={toggleSelectAll}
                    disabled={products.length === 0}
                    aria-label="Select all products"
                  />
                </th>
                <th className="px-4 py-3">ASIN</th>
                <th className="px-4 py-3">Title</th>
                <th className="px-4 py-3">Amazon Price</th>
                <th className="px-4 py-3">Selling Price</th>
                <th className="px-4 py-3">Delete</th>
              </tr>
            </thead>
            <tbody>
              {products.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-slate-500 dark:text-slate-400">
                    No products to display yet.
                  </td>
                </tr>
              ) : (
                products.map((product) => (
                  <tr key={product.id} className="border-t border-slate-200 dark:border-slate-700 dark:text-slate-200">
                    <td className="px-4 py-3">
                      <input
                        type="checkbox"
                        checked={selectedIds.includes(product.id)}
                        onChange={() => toggleSelectOne(product.id)}
                        aria-label={`Select ${product.asin}`}
                      />
                    </td>
                    <td className="px-4 py-3">{product.asin}</td>
                    <td className="px-4 py-3">{product.title}</td>
                    <td className="px-4 py-3">{product.amazonPrice}</td>
                    <td className="px-4 py-3">{product.sellingPrice}</td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        onClick={() => handleDelete(product.id)}
                        className="cursor-pointer rounded-lg bg-red-600 px-3 py-1.5 text-white transition hover:bg-red-700"
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
      </div>
    </section>
  )
}

export default Inventory