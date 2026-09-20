import { useState } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import './App.css'

function App() {
  const [showLogin, setShowLogin] = useState(false)
  const [showRegister, setShowRegister] = useState(false)

  return (
    <div>
      <header className="navbar">
        <div className="logo">
          Beveik žinojau!
        </div>

        <nav>
          <a href="/">Pagrindinis</a>
          <a href="#categories">Kategorijos</a>
          <a href="#leaderboard">Lyderiai</a>
        </nav>

        <div className="account-buttons">
          <button onClick={() => setShowLogin(true)}>
            Prisijungti
          </button>

          <button onClick={() => setShowRegister(true)}>
            Registruotis
          </button>
        </div>
      </header>

      {showLogin && (
        <Login onClose={() => setShowLogin(false)} />
      )}

      {showRegister && (
        <Register onClose={() => setShowRegister(false)} />
      )}
    </div>
  )
}

export default App