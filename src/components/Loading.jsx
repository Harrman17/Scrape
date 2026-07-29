import { useEffect, useRef } from 'react'
import lottie from 'lottie-web'

export default function Loading({ message = '' }) {
  const containerRef = useRef(null)

  useEffect(() => {
    fetch('/opener-loading.json')
      .then((res) => res.json())
      .then((data) => {
        if (containerRef.current) {
          lottie.loadAnimation({
            container: containerRef.current,
            renderer: 'svg',
            loop: true,
            autoplay: true,
            animationData: data,
          })
        }
      })
      .catch((err) => console.error('Failed to load animation:', err))
  }, [])

  return (
    <div className="flex flex-col items-center justify-center gap-4">
      <div ref={containerRef} className="h-32 w-32"></div>
      <p className="text-sm text-slate-600 dark:text-slate-400">{message}</p>
    </div>
  )
}
