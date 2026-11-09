import { useState } from "react";
import InRound from "./InRound";
import InRoundTyped from "./InRoundTyped";


const questionTypes = {
  capital: "Sostinės",
  flag: "Vėliavos",
  map: "Žemėlapiai",
  outline: "Kontūrai",
  neighbour: "Kaimyninės šalys",
};
const mathOperations = {
  add: "Sudėtis",
  sub: "Atimtis",
  mul: "Daugyba",
  div: "Dalyba",
};

function GameRoom({
  category,
  roomCode,
  players,
  onLeave
}: {
  category: string;
  roomCode: string;
  players: string[];
  onLeave: () => void;
}) {
  const [playing, setPlaying] = useState(false);
  const [questionCount, setQuestionCount] = useState(5);
  const [selectedTypes, setSelectedTypes] = useState(
    Object.keys(questionTypes),
  );
  const [selectedOperations, setSelectedOperations] = useState(
    Object.keys(mathOperations),
  );

  if (playing) {
    if (category === "Matematika") {
       return (
         <InRoundTyped
           apiPath="/api/mathquestions"
           questionCount={questionCount}
           questionTypes={selectedOperations}
           onBack={() => setPlaying(false)}
         />
       );
     }
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
      {category === "Matematika" && (
        <>
          <p>Klausimų skaičius:</p>
          <input
            type="number"
            min={1}
            value={questionCount}
            onChange={(e) => setQuestionCount(Number(e.target.value))}
          />

          <p>Veiksmai:</p>
          {Object.entries(mathOperations).map(([type, label]) => (
            <label key={type}>
              <input
                type="checkbox"
                checked={selectedOperations.includes(type)}
                onChange={() =>
                  setSelectedOperations((old) =>
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
    
      <button onClick={onLeave}>Išeiti iš kambario</button>
      <button
        className="start-game-button"
        disabled={
          (category !== "Geografija" && category !== "Matematika") ||
          !Number.isInteger(questionCount) ||
          questionCount < 1 ||
          (category === "Matematika" ?  selectedOperations.length === 0 : selectedTypes.length === 0)
        }
        onClick={() => setPlaying(true)}
      >
        Pradėti žaidimą
      </button>
    </main>
  );
}

export default GameRoom;
