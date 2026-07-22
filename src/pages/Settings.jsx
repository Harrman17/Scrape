import { useState, useEffect } from 'react'

function Settings() {
  const [settings, setSettings] = useState({
    qty: 1,
    profitMarkup: 0,
    blockProductsUnder: '',
    itemLocationPostcode: '',
    itemLocationCity: '',
    autoRemoveBrand: false,
  })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const token = localStorage.getItem('authToken')

  useEffect(() => {
    loadSettings()
  }, [])

  async function loadSettings() {
    try {
      setLoading(true)
      const response = await fetch('http://localhost:5211/api/inventory/settings', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!response.ok) throw new Error('Failed to load settings')
      const data = await response.json()
      setSettings({
        qty: data.qty || 1,
        profitMarkup: data.profitMarkup || 0,
        blockProductsUnder: data.blockProductsUnder ? data.blockProductsUnder.toString() : '',
        itemLocationPostcode: data.itemLocationPostcode || '',
        itemLocationCity: data.itemLocationCity || '',
        autoRemoveBrand: data.autoRemoveBrand || false,
      })
    } catch (err) {
      setMessage(`Error: ${err.message}`)
    } finally {
      setLoading(false)
    }
  }

  async function handleSave(e) {
    e.preventDefault()
    try {
      setSaving(true)
      const response = await fetch('http://localhost:5211/api/inventory/settings', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          qty: parseInt(settings.qty) || 1,
          profitMarkup: parseFloat(settings.profitMarkup) || 0,
          blockProductsUnder: settings.blockProductsUnder ? parseFloat(settings.blockProductsUnder) : null,
          itemLocationPostcode: settings.itemLocationPostcode || null,
          itemLocationCity: settings.itemLocationCity || null,
          autoRemoveBrand: settings.autoRemoveBrand,
        })
      })
      if (!response.ok) throw new Error('Failed to save settings')
      setMessage('Settings saved successfully!')
      setTimeout(() => setMessage(''), 3000)
    } catch (err) {
      setMessage(`Error: ${err.message}`)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <section className="grid min-h-screen place-items-center p-8 dark:bg-slate-950">
        <p className="text-slate-600 dark:text-slate-400">Loading settings...</p>
      </section>
    )
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-2xl rounded-3xl border border-gray-200 bg-white p-8 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">
        <h1 className="mb-2 text-2xl font-bold dark:text-slate-100">Settings</h1>
        <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">Configure default values used when importing products.</p>

        {message && (
          <div
            className={`mb-4 rounded-lg p-4 text-sm ${
              message.startsWith('Error')
                ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
                : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
            }`}
          >
            {message}
          </div>
        )}

        <form onSubmit={handleSave} className="space-y-6">
          {/* Default Quantity */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
              Default Quantity per Product
            </label>
            <input
              type="number"
              min="1"
              value={settings.qty}
              onChange={(e) => setSettings({ ...settings, qty: e.target.value })}
              className="mt-2 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
            />
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              Applied to all imported products automatically
            </p>
          </div>

          {/* Profit Markup */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
              Profit Markup (%)
            </label>
            <input
              type="number"
              step="0.01"
              value={settings.profitMarkup}
              onChange={(e) => setSettings({ ...settings, profitMarkup: e.target.value })}
              className="mt-2 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
            />
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              Example: 20% markup means selling price = Amazon price × 1.2
            </p>
          </div>
          <hr />
          {/* Block Products Under */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
              Block Products Under (£)
            </label>
            <input
              type="number"
              step="0.01"
              value={settings.blockProductsUnder}
              onChange={(e) => setSettings({ ...settings, blockProductsUnder: e.target.value })}
              className="mt-2 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
              placeholder="Leave empty for no limit"
            />
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              Products below this Amazon price will not be imported
            </p>
          </div>
           <hr />
          {/* Item Location */}
          <h3>Item location</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
               Postcode
              </label>
              <input
                type="text"
                value={settings.itemLocationPostcode}
                onChange={(e) => setSettings({ ...settings, itemLocationPostcode: e.target.value })}
                className="mt-2 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                placeholder="e.g., SW1A 1AA"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
                City
              </label>
              <input
                type="text"
                value={settings.itemLocationCity}
                onChange={(e) => setSettings({ ...settings, itemLocationCity: e.target.value })}
                className="mt-2 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                placeholder="e.g., London"
              />
            </div>
          </div>

          {/* Auto Remove Brand */}
          <div className="flex items-center gap-3">
            <input
              type="checkbox"
              id="autoRemoveBrand"
              checked={settings.autoRemoveBrand}
              onChange={(e) => setSettings({ ...settings, autoRemoveBrand: e.target.checked })}
              className="h-4 w-4 cursor-pointer rounded border-slate-300"
            />
            <label htmlFor="autoRemoveBrand" className="cursor-pointer text-sm font-medium text-slate-700 dark:text-slate-300">
              Auto-remove brand names from titles
            </label>
          </div>

          {/* Save Button */}
          <div className="flex gap-3 pt-4">
            <button
              type="submit"
              disabled={saving}
              className="cursor-pointer rounded-lg bg-blue-600 px-4 py-2 font-medium text-white transition hover:bg-blue-700 disabled:opacity-50"
            >
              {saving ? 'Saving...' : 'Save Settings'}
            </button>
          </div>
        </form>
      </div>
    </section>
  )
}

export default Settings

