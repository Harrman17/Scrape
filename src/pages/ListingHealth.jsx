import { useState, useEffect } from 'react'
import Loading from '../components/Loading'

function ListingHealth() {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [uploading, setUploading] = useState(false)
  const token = localStorage.getItem('authToken')

  useEffect(() => {
    loadJobs()
  }, [])

  async function loadJobs() {
    try {
      setLoading(true)
      const res = await fetch('http://localhost:5211/api/listing-health-jobs', {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (!res.ok) throw new Error('Failed to load listing health jobs')
      setJobs(await res.json())
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleUploadCsv(e) {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      setUploading(true)
      setError('')

      const formData = new FormData()
      formData.append('file', file)

      const res = await fetch('http://localhost:5211/api/listing-health-jobs/upload', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      })

      if (!res.ok) {
        const error = await res.json()
        throw new Error(error.error || 'Failed to upload CSV')
      }
      
      await loadJobs()
      e.target.value = '' // Reset file input
    } catch (err) {
      setError(err.message)
    } finally {
      setUploading(false)
    }
  }

  function formatDate(iso) {
    if (!iso) return '—'
    return new Date(iso).toLocaleString()
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-240 rounded-3xl border border-gray-200 bg-white p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        <div className="mb-5 flex items-center justify-between">
          <h1 className="text-2xl font-bold dark:text-slate-100">
            Listing Health
            {jobs.length > 0 && (
              <span className="ml-2 text-sm font-normal text-slate-400">{jobs.length} jobs</span>
            )}
          </h1>
          <label className="cursor-pointer rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-blue-700 disabled:opacity-50">
            {uploading ? 'Uploading...' : 'Upload eBay CSV'}
            <input
              type="file"
              accept=".csv"
              onChange={handleUploadCsv}
              disabled={uploading}
              className="hidden"
            />
          </label>
        </div>

        {error && (
          <div className="mb-4 rounded bg-red-100 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
            {error}
          </div>
        )}

        <div className="mb-4 rounded-lg border border-blue-100 bg-blue-50 p-4 dark:border-blue-900/30 dark:bg-blue-900/10">
          <p className="text-sm text-slate-700 dark:text-slate-300">
            <strong>How it works:</strong> Download the "Price and Stock Update" CSV from your eBay seller hub,
            then upload it here to automatically sync listing statuses with your inventory.
          </p>
        </div>

        {loading ? (
          <div className="flex justify-center py-12">
            <Loading />
          </div>
        ) : jobs.length === 0 ? (
          <p className="text-sm text-slate-500 dark:text-slate-400">No listing health checks yet. Upload an eBay CSV to get started.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr>
                  <th className="pb-3 pr-4 text-left">Processed</th>
                  <th className="pb-3 pr-4 text-left">Healthy</th>
                  <th className="pb-3 pr-4 text-left">Errors</th>
                  <th className="pb-3 pr-4 text-left">Status</th>
                  <th className="pb-3 pr-4 text-left">Started</th>
                  <th className="pb-3 text-left">Completed</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {jobs.map((job) => (
                  <tr key={job.id}>
                    <td className="py-3 pr-4 text-slate-700 dark:text-slate-300">{job.processedItems ?? 0}</td>
                    <td className="py-3 pr-4 text-green-600 dark:text-green-400">{job.healthyItems ?? 0}</td>
                    <td className="py-3 pr-4 text-red-500 dark:text-red-400">{job.errorItems ?? 0}</td>
                    <td className="py-3 pr-4">
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          job.status === 'completed'
                            ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                            : job.status === 'processing'
                              ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
                              : job.status === 'failed'
                                ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
                                : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'
                        }`}
                      >
                        {job.status ? job.status.charAt(0).toUpperCase() + job.status.slice(1) : 'Unknown'}
                      </span>
                    </td>
                    <td className="py-3 pr-4 text-slate-500 dark:text-slate-400">{formatDate(job.startedAt)}</td>
                    <td className="py-3 text-slate-500 dark:text-slate-400">{formatDate(job.completedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}

export default ListingHealth
