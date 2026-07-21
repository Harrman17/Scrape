import { useState, useEffect } from 'react'

const DEFAULTS = {
  profitMarkup: '',
  defaultQty: '',
}

function loadSettings() {
  try {
    const stored = localStorage.getItem('appSettings')
    return stored ? { ...DEFAULTS, ...JSON.parse(stored) } : { ...DEFAULTS }
  } catch {
    return { ...DEFAULTS }
  }
}

function Settings() {
  const [settings, setSettings] = useState(loadSettings)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (saved) {
      const t = setTimeout(() => setSaved(false), 2000)
      return () => clearTimeout(t)
    }
  }, [saved])

  function handleChange(e) {
    const { name, value } = e.target
    setSettings((prev) => ({ ...prev, [name]: value }))
  }

  function handleSave(e) {
    e.preventDefault()
    localStorage.setItem('appSettings', JSON.stringify(settings))
    setSaved(true)
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-2xl rounded-3xl border border-gray-200 bg-white p-8 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">
        <h1 className="text-2xl font-bold dark:text-slate-100">Settings</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Configure default values used across the app.</p>

        <form onSubmit={handleSave} className="mt-8 space-y-8">

          {/* Profit Settings */}
          <div>
            <h2 className="mb-4 border-b border-slate-100 pb-2 text-sm font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-700 dark:text-slate-400">
              Profit Settings
            </h2>

            <div className="space-y-5">
              {/* Profit Markup */}
              <div>
                <label
                  htmlFor="profitMarkup"
                  className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-200"
                >
                  Profit Markup
                </label>
                <p className="mb-2 text-xs text-slate-500 dark:text-slate-400">
                  The percentage added on top of the Amazon price to calculate the eBay selling price.
                </p>
                <div className="relative w-48">
                  <input
                    id="profitMarkup"
                    name="profitMarkup"
                    type="number"
                    min="0"
                    step="0.1"
                    value={settings.profitMarkup}
                    onChange={handleChange}
                    placeholder="e.g. 20"
                    className="w-full rounded-lg border border-slate-300 bg-white py-2 pl-3 pr-10 text-sm text-slate-800 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:border-blue-400"
                  />
                  <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-400">
                    %
                  </span>
                </div>
              </div>

              {/* Default Quantity */}
              <div>
                <label
                  htmlFor="defaultQty"
                  className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-200"
                >
                  Quantity in Stock
                </label>
                <p className="mb-2 text-xs text-slate-500 dark:text-slate-400">
                  The default quantity applied to all inventory items when listed.
                </p>
                <input
                  id="defaultQty"
                  name="defaultQty"
                  type="number"
                  min="0"
                  step="1"
                  value={settings.defaultQty}
                  onChange={handleChange}
                  placeholder="e.g. 3"
                  className="w-48 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:border-blue-400"
                />
              </div>
            </div>
          </div>

          {/* Save */}
          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              className="cursor-pointer rounded-lg bg-blue-600 px-5 py-2 text-sm font-medium text-white transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500/40"
            >
              Save settings
            </button>
            {saved && (
              <span className="text-sm font-medium text-green-600 dark:text-green-400">
                ✓ Saved
              </span>
            )}
          </div>
        </form>
      </div>
    </section>
  )
}

export default Settings
