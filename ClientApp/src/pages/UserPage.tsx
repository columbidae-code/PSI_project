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

type PlayerSummary = {
  winRate: number
  accuracy: number
  averageAnswerTimeSeconds: number
}


function UserPage({ username, onBack, onLogout }: UserPageProps) {
  const [stats, setStats] = useState<CategoryStats[]>([])
  const [summary, setSummary] = useState<PlayerSummary | null>(null)
  
  useEffect(() => {
    fetch(`/api/stats/${encodeURIComponent(username)}/summary`)
      .then((response) => response.json())
      .then((data) => setSummary(data))
  }, [username])
  

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

      <h2>Statistika</h2>
      {summary && stats.length > 0 && (
        <div className="user-page-summary">
          <p>Laimėjimų dažnis: {Math.round(summary.winRate * 100)}%</p>
          <p>Tikslumas: {Math.round(summary.accuracy * 100)}%</p>
          <p>Vidutinis atsakymo laikas: {summary.averageAnswerTimeSeconds.toFixed(1)} s</p>
        </div>
      )}

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