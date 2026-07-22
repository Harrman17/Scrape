import { useEffect, useState } from 'react'
import './App.css'
import ImportProducts from './pages/ImportProducts'
import Home from './pages/Home';
import Inventory from './pages/Inventory';
import Settings from './pages/Settings';
import Login from './pages/Login';
import Register from './pages/Register';
import { Routes, Route, Link, Navigate } from "react-router-dom";

function App() {
    const [isDark, setIsDark] = useState(() => {
        const stored = window.localStorage.getItem('theme')
        if (stored) return stored === 'dark'
        return window.matchMedia('(prefers-color-scheme: dark)').matches
    })

    const [user, setUser] = useState(() => {
        try {
            const stored = localStorage.getItem('authUser')
            return stored ? JSON.parse(stored) : null
        } catch {
            return null
        }
    })

    useEffect(() => {
        document.documentElement.classList.toggle('dark', isDark)
        window.localStorage.setItem('theme', isDark ? 'dark' : 'light')
    }, [isDark])

    function handleLogin(userData) {
        setUser(userData)
    }

    function handleLogout() {
        localStorage.removeItem('authToken')
        localStorage.removeItem('authUser')
        setUser(null)
    }

    if (!user) {
        return (
            <Routes>
                <Route path="/login" element={<Login onLogin={handleLogin} />} />
                <Route path="/register" element={<Register onLogin={handleLogin} />} />
                <Route path="*" element={<Navigate to="/login" replace />} />
            </Routes>
        )
    }

    return (
        <>
            <nav className="flex items-center justify-between gap-4 border-b border-slate-200 bg-white px-6 py-4 dark:border-slate-800 dark:bg-slate-900">
                <div className="flex gap-10 text-slate-700 dark:text-slate-200">
                    <Link to="/">logo</Link>
                    <Link to="/import-products">
                        Import Products
                    </Link>
                    <Link to="/inventory">
                        Inventory
                    </Link>
                </div>
                <div className='flex items-center gap-3'>
                    <span className="text-sm text-slate-500 dark:text-slate-400">{user.name}</span>
                    <button className="cursor-pointer rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800">
                        <Link to="/settings">
                            ⚙️
                        </Link>
                    </button>
                </div>
            </nav>

            <Routes>
                <Route path="/"  element={<Home />}/>
                <Route path="/import-products" element={<ImportProducts />} />
                <Route path="/inventory" element={<Inventory />} />
                <Route path="/settings" element={<Settings isDark={isDark} setIsDark={setIsDark} onLogout={handleLogout} />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </>
    );
}

export default App
