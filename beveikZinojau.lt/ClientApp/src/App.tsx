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

      <main className="main-page">

        <div className="welcome">
          <h1>Beveik žinojau!</h1>
          <p>Pasirink kategoriją ir pasitikrink savo žinias</p>
        </div>

        <section className="categories" id="categories">

          <div className="category-card">
            <h2>Geografija</h2>
            <p>Šalys, miestai, vėliavos ir pasaulis</p>
          </div>

          <div className="category-card">
            <h2>Istorija</h2>
            <p>Įvykiai, datos ir istorinės asmenybės</p>
          </div>

          <div className="category-card">
            <h2>Lietuvių kalba</h2>
            <p>Kirčiavimas ir gal dar kažkas???</p>
          </div>

          <div className="category-card">
            <h2>Matematika</h2>
            <p>Skaičiai, formulės ir loginiai uždaviniai</p>
          </div>

        </section>

        <section className="how-to-play">
          <h2>Kaip žaisti?</h2>

          <div className="steps">

            <div className="step">
              <div className="step-number">1</div>
              <h3>Pasirink kategoriją</h3>
              <p>Išsirink temą, kurios žinias nori pasitikrinti.</p>
            </div>

            <div className="step">
              <div className="step-number">2</div>
              <h3>Atsakyk į klausimus</h3>
              <p>Stenkis atsakyti teisingai ir kuo greičiau.</p>
            </div>

            <div className="step">
              <div className="step-number">3</div>
              <h3>Rink taškus</h3>
              <p>Surink kuo daugiau taškų ir kilk lyderių lentelėje.</p>
            </div>

          </div>
        </section>

      </main>

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