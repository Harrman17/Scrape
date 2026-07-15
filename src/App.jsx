import { useEffect, useState } from 'react'
import './App.css'
import ImportProducts from './pages/ImportProducts'
import Home from './pages/Home';
import Inventory from './pages/Inventory';
import { Routes, Route, Link } from "react-router-dom";

function App() {
    const [isDark, setIsDark] = useState(() => {
        const stored = window.localStorage.getItem('theme')
        if (stored) return stored === 'dark'
        return window.matchMedia('(prefers-color-scheme: dark)').matches
    })

    useEffect(() => {
        document.documentElement.classList.toggle('dark', isDark)
        window.localStorage.setItem('theme', isDark ? 'dark' : 'light')
    }, [isDark])

    return (
        <>
            <nav className="flex items-center justify-between gap-4 border-b border-slate-200 bg-white px-6 py-4 dark:border-slate-800 dark:bg-slate-900">
                <div className="flex gap-4 text-slate-700 dark:text-slate-200">
                    <Link to="/">Home</Link>
                    <Link to="/import-products">
                        Import Products
                    </Link>
                    <Link to="/inventory">
                        Inventory
                    </Link>
                </div>

                <button
                    type="button"
                    onClick={() => setIsDark((prev) => !prev)}
                    className="cursor-pointer rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                    {isDark ? '☀️ Light mode' : '🌙 Dark mode'}
                </button>
            </nav>

            <Routes>
                <Route path="/"  element={<Home />}/>
                <Route path="/import-products" element={<ImportProducts />} />
                <Route path="/inventory" element={<Inventory />} />
            </Routes>
        </>
    );
}

export default App
