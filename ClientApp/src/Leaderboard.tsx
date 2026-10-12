import { useEffect, useState } from "react";

type Player = {
  username: string;
  score: number;
};

function Leaderboard() {
  const [players, setPlayers] = useState<Player[]>([]);

  // Loads the leaderboard from the backend when the page opens
  useEffect(() => {
    fetch("/api/leaderboard")
      .then((response) => response.json())
      .then((data) => setPlayers(data));
  }, []);

  return (
    <section id="leaderboard">
      <h2>Lyderiai</h2>

      <table>
        <tr>
          <th>Vardas</th>
          <th>Taškai</th>
        </tr>
        {players.map((player) => (
          <tr key={player.username}>
            <td>{player.username}</td>
            <td>{player.score}</td>
          </tr>
        ))}
      </table>
    </section>
  );
}

export default Leaderboard;