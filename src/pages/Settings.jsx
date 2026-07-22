import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'

function Settings({ isDark, setIsDark, onLogout }) {
  const navigate = useNavigate()
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

  function handleLogout() {
    onLogout()
    navigate('/login')
  }

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
    <section className="min-h-screen p-4 dark:bg-slate-950 sm:p-6">
      <div className="mx-auto max-w-2xl">
        <div className="mb-4 flex items-center justify-between">
          <h1 className="text-xl font-semibold dark:text-slate-100">Settings</h1>
        </div>

        {message && (
          <div
            className={`mb-4 rounded p-3 text-sm ${
              message.startsWith('Error')
                ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
                : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
            }`}
          >
            {message}
          </div>
        )}

        {/* App Settings */}
        <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
          <div className="space-y-3">
            {/* Dark Mode */}
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-slate-700 dark:text-slate-200">Dark Mode</label>
              <button
                type="button"
                onClick={() => setIsDark((prev) => !prev)}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition ${
                  isDark ? 'bg-blue-600' : 'bg-slate-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${
                    isDark ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>
          </div>
        </div>

        {/* Import Settings */}
        {!loading && (
          <form onSubmit={handleSave} className="space-y-4">
            <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
              <h2 className="mb-4 text-sm font-semibold text-slate-600 dark:text-slate-300">Import Settings</h2>
              
              <div className="space-y-3">
                {/* Default Quantity */}
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Quantity</label>
                  <input
                    type="number"
                    min="1"
                    value={settings.qty}
                    onChange={(e) => setSettings({ ...settings, qty: e.target.value })}
                    className="mt-1 w-full rounded border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                  />
                </div>

                {/* Profit Markup */}
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Profit Markup (%)</label>
                  <input
                    type="number"
                    step="0.01"
                    value={settings.profitMarkup}
                    onChange={(e) => setSettings({ ...settings, profitMarkup: e.target.value })}
                    className="mt-1 w-full rounded border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                  />
                </div>

                {/* Block Products Under */}
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Block Products Under (£)</label>
                  <input
                    type="number"
                    step="0.01"
                    value={settings.blockProductsUnder}
                    onChange={(e) => setSettings({ ...settings, blockProductsUnder: e.target.value })}
                    className="mt-1 w-full rounded border border-slate-300 px-3 py-2 mb-2 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                    placeholder="Leave empty for no limit"
                  />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="autoRemoveBrand"
                    checked={settings.autoRemoveBrand}
                    onChange={(e) => setSettings({ ...settings, autoRemoveBrand: e.target.checked })}
                    className="h-4 w-4 cursor-pointer rounded border-slate-300"
                  />
                  <label htmlFor="autoRemoveBrand" className="cursor-pointer text-xs font-medium text-slate-700 dark:text-slate-300">
                    Auto-remove brand names from titles
                  </label>
                </div>
              </div>
            </div>

            {/* Item Location */}
            <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
              <h2 className="mb-4 text-sm font-semibold text-slate-600 dark:text-slate-300">Item Location</h2>
              
              <div className="grid gap-3 sm:grid-cols-2">
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Postcode</label>
                  <input
                    type="text"
                    value={settings.itemLocationPostcode}
                    onChange={(e) => setSettings({ ...settings, itemLocationPostcode: e.target.value })}
                    className="mt-1 w-full rounded border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                    placeholder="e.g., SW1A 1AA"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">City</label>
                  <input
                    type="text"
                    value={settings.itemLocationCity}
                    onChange={(e) => setSettings({ ...settings, itemLocationCity: e.target.value })}
                    className="mt-1 w-full rounded border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-200 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-blue-900"
                    placeholder="e.g., London"
                  />
                </div>
              </div>
            </div>
              {/* Save Button */}
              <button
                type="submit"
                disabled={saving}
                className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-blue-700 disabled:opacity-50"
              >
                {saving ? 'Saving...' : 'Save'}
              </button>
              <button
                type="button"
                onClick={handleLogout}
                className="w-full rounded-lg bg-slate-700 px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-800"
              >
                Sign Out
              </button>
            </form>
          )}

          {loading && (
            <div className="rounded-lg border border-slate-200 bg-white p-8 text-center dark:border-slate-800 dark:bg-slate-900">
              <p className="text-sm text-slate-600 dark:text-slate-400">Loading settings...</p>
            </div>
          )}
      </div>
    </section>
  )
}

export default Settings

