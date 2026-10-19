import { useState } from "react";
import InRound from "./InRound";

const questionTypes = {
  capital: "Sostinės",
  flag: "Vėliavos",
  map: "Žemėlapiai",
  outline: "Kontūrai",
  neighbour: "Kaimyninės šalys",
};

function GameRoom({
  category,
  roomCode,
  players,
}: {
  category: string;
  roomCode: string;
  players: string[];
}) {
  const [playing, setPlaying] = useState(false);
  const [questionCount, setQuestionCount] = useState(5);
  const [selectedTypes, setSelectedTypes] = useState(
    Object.keys(questionTypes),
  );

  if (playing) {
    return (
      <InRound
        questionCount={questionCount}
        questionTypes={selectedTypes}
        onBack={() => setPlaying(false)}
      />
    );
  }

  return (
    <main className="game-room">
      <h2>{category}</h2>

      <div className="room-code">
        <p>Kambario kodas</p>
        <h1>{roomCode}</h1>
      </div>

      <div className="room-players">
        <h2>Žaidėjai</h2>

        {players.map((player) => (
          <div className="player" key={player}>
            {player}
          </div>
        ))}
      </div>

      {category === "Geografija" && (
        <>
          <p>Klausimų skaičius:</p>
          <input
            type="number"
            min={1}
            value={questionCount}
            onChange={(e) => setQuestionCount(Number(e.target.value))}
          />

          <p>Klausimų tipai:</p>
          {Object.entries(questionTypes).map(([type, label]) => (
            <label key={type}>
              <input
                type="checkbox"
                checked={selectedTypes.includes(type)}
                onChange={() =>
                  setSelectedTypes((old) =>
                    old.includes(type)
                      ? old.filter((t) => t !== type)
                      : [...old, type],
                  )
                }
              />
              {label}
            </label>
          ))}
        </>
      )}

      <button
        className="start-game-button"
        disabled={
          category !== "Geografija" ||
          !Number.isInteger(questionCount) ||
          questionCount < 1 ||
          selectedTypes.length === 0
        }
        onClick={() => setPlaying(true)}
      >
        Pradėti žaidimą
      </button>
    </main>
  );
}

export default GameRoom;
