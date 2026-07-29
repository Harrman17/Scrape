import { useState, useEffect } from 'react'
import Loading from '../components/Loading'

function ImportHistory() {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const token = localStorage.getItem('authToken')

  useEffect(() => {
    async function load() {
      try {
        const res = await fetch('http://localhost:5211/api/scraping-jobs', {
          headers: { Authorization: `Bearer ${token}` },
        })
        if (!res.ok) throw new Error('Failed to load import history')
        setJobs(await res.json())
      } catch (err) {
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  function formatDate(iso) {
    if (!iso) return '—'
    return new Date(iso).toLocaleString()
  }

  return (
    <section className="min-h-screen p-6 dark:bg-slate-950">
      <div className="mx-auto max-w-[960px] rounded-3xl border border-gray-200 bg-white p-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-slate-800 dark:bg-slate-900">

        <div className="mb-5 flex items-center justify-between">
          <h1 className="text-2xl font-bold dark:text-slate-100">
            Import History
            {jobs.length > 0 && (
              <span className="ml-2 text-sm font-normal text-slate-400">{jobs.length} jobs</span>
            )}
          </h1>
        </div>

        {error && (
          <div className="mb-4 rounded bg-red-100 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex justify-center py-12">
            <Loading />
          </div>
        ) : jobs.length === 0 ? (
          <p className="text-sm text-slate-500 dark:text-slate-400">No import jobs yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  <th className="pb-3 pr-4">ID</th>
                  <th className="pb-3 pr-4">Total</th>
                  <th className="pb-3 pr-4">Processed</th>
                  <th className="pb-3 pr-4">Successful</th>
                  <th className="pb-3 pr-4">Blocked</th>
                  <th className="pb-3 pr-4">Status</th>
                  <th className="pb-3 pr-4">Started</th>
                  <th className="pb-3">Completed</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {jobs.map((job) => (
                  <tr key={job.id} className="text-slate-700 dark:text-slate-300">
                    <td className="py-3 pr-4 font-mono text-xs text-slate-400">#{job.id}</td>
                    <td className="py-3 pr-4">{job.totalAsins}</td>
                    <td className="py-3 pr-4">{job.processedAsins}</td>
                    <td className="py-3 pr-4 text-green-600 dark:text-green-400">{job.successfulAsins}</td>
                    <td className="py-3 pr-4 text-red-500 dark:text-red-400">{job.blockedAsins}</td>
                    <td className="py-3 pr-4">
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          job.jobComplete
                            ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                            : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'
                        }`}
                      >
                        {job.jobComplete ? 'Complete' : 'In Progress'}
                      </span>
                    </td>
                    <td className="py-3 pr-4 text-slate-500 dark:text-slate-400">{formatDate(job.createdAt)}</td>
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

export default ImportHistory
