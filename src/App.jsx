import { useState } from 'react'
import './App.css'
import ImportProducts from './pages/ImportProducts'
import Home from './pages/Home';
import { Routes, Route, Link } from "react-router-dom";

function App() {

    return (
        <>
            <nav>
                <Link to="/">Home</Link>
                <Link to="/import-products">
                    Import Products
                </Link>
            </nav>

            <Routes>
                <Route path="/"  element={<Home />}/>
                <Route path="/import-products" element={<ImportProducts />} />
            </Routes>
        </>
    );
}

export default App
