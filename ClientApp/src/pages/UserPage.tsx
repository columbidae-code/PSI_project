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

      <h2>Statistika</h2>

      {stats.length === 0 ? (
        <p>Dar nesi žaidęs nė vieno žaidimo</p>
      ) : (
        <table className="stats-table">
          <tr>
            <th>Kategorija</th>
            <th>Taškai</th>
            <th>Žaidimai</th>
            <th>Laimėta</th>
            <th>Pralaimėta</th>
            <th>Teisingi atsakymai</th>
          </tr>
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
          <tr>
            <th>Iš viso</th>
            <th>{sum("totalScore")}</th>
            <th>{sum("gamesPlayed")}</th>
            <th>{sum("gamesWon")}</th>
            <th>{sum("gamesLost")}</th>
            <th>{sum("correctAnswers")} / {sum("totalQuestions")}</th>
          </tr>
        </table>
      )}

      <button onClick={onBack}>Atgal</button>
      <button onClick={onLogout}>Atsijungti</button>
    </div>
  )
}

export default UserPage