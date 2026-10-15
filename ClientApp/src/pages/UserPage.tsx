import { useEffect, useState } from "react"

type CategoryStats = {
  category: string
  totalScore: number
  gamesPlayed: number
  gamesWon: number
  gamesLost: number
  totalQuestions: number
  correctAnswers: number
  totalAnswerTimeSeconds: number
}

type UserPageProps = {
  username: string
  onBack: () => void
  onLogout: () => void
}

function UserPage({ username, onBack, onLogout }: UserPageProps) {
  const [stats, setStats] = useState<CategoryStats[]>([])

  useEffect(() => {
    fetch(`/api/stats/${encodeURIComponent(username)}`)
      .then((response) => response.json())
      .then((data) => setStats(data))
  }, [username])

  // Sum of one field across all categories
  const sum = (field: keyof CategoryStats) =>
    stats.reduce((total, s) => total + (s[field] as number), 0)

  return (
    <div className="user-page">
      <h1>Sveiki, {username}!</h1>

      <p className="user-page-subtitle">Tavo žaidimų statistika</p>

      {stats.length === 0 ? (
        <p className="user-page-empty">Dar nesi žaidęs nė vieno žaidimo</p>
      ) : (
        <table className="stats-table">
          <thead>
          <tr>
            <th>Kategorija</th>
            <th>Taškai</th>
            <th>Žaidimai</th>
            <th>Laimėta</th>
            <th>Pralaimėta</th>
            <th>Teisingi atsakymai</th>
          </tr>
          </thead>
          <tbody>
          {stats.map((s) => (
            <tr key={s.category}>
              <td>{s.category}</td>
              <td>{s.totalScore}</td>
              <td>{s.gamesPlayed}</td>
              <td>{s.gamesWon}</td>
              <td>{s.gamesLost}</td>
              <td>{s.correctAnswers} / {s.totalQuestions}</td>
            </tr>
          ))}
          </tbody>
          <tfoot>
          <tr>
            <th>Iš viso</th>
            <th>{sum("totalScore")}</th>
            <th>{sum("gamesPlayed")}</th>
            <th>{sum("gamesWon")}</th>
            <th>{sum("gamesLost")}</th>
            <th>{sum("correctAnswers")} / {sum("totalQuestions")}</th>
          </tr>
          </tfoot>
        </table>
      )}
      <div className="user-page-buttons">
      <button className="back-button" onClick={onBack}>Atgal</button>
      <button onClick={onLogout}>Atsijungti</button>
      </div>
    </div>
  )
}

export default UserPage