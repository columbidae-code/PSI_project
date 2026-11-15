import { useEffect, useState } from "react";
import connection from "../signalR";
import AccentuationRound from "./AccentuationRound";
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

const accentuationLevels = [
  { level: 1, label: "I lygis" },
  { level: 2, label: "II lygis" },
  { level: 3, label: "III lygis" },
  { level: 4, label: "IV lygis" },
  { level: 5, label: "V lygis" },
];

function GameRoom({
  category,
  roomCode,
  players,
  isHost,
  onLeave,
}: {
  category: string;
  roomCode: string | null;
  players: string[];
  isHost: boolean;
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
  const [selectedLevels, setSelectedLevels] = useState<number[]>([1]);
  const [seed, setSeed] = useState<number | null>(null);

  useEffect(() => {
    const handleGameStarted = (
      count: number,
      types: string[],
      levels: number[],
      gameSeed: number,
    ) => {
      setQuestionCount(count);
      setSelectedTypes(types);
      setSelectedOperations(types);
      setSelectedLevels(levels);
      setSeed(gameSeed);
      setPlaying(true);
    };

    connection.on("GameStarted", handleGameStarted);

    return () => {
      connection.off("GameStarted", handleGameStarted);
    };
  }, []);

  async function handleStartClick() {
    if (roomCode) {
      const types =
        category === "Matematika"
          ? selectedOperations
          : category === "Geografija"
            ? selectedTypes
            : [];
      await connection.invoke(
        "StartGame",
        roomCode,
        questionCount,
        types,
        selectedLevels,
      );
    } else {
      setPlaying(true);
    }
  }

  if (playing) {
    if (category === "Lietuvių kalba") {
      return (
        <AccentuationRound
          questionCount={questionCount}
          levels={selectedLevels}
          seed={seed}
          onBack={() => setPlaying(false)}
        />
      );
    }

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

  const isValidQuestionCount =
    Number.isInteger(questionCount) && questionCount >= 1;
  const canStart =
    isValidQuestionCount &&
    ((category === "Geografija" && selectedTypes.length > 0) ||
      (category === "Matematika" && selectedOperations.length > 0) ||
      (category === "Lietuvių kalba" && selectedLevels.length > 0));

  return (
    <main className="game-room">
      <h2>{category}</h2>

      {roomCode && (
        <>
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
        </>
      )}

      {isHost ? (
        <>
          {(category === "Geografija" ||
            category === "Matematika" ||
            category === "Lietuvių kalba") && (
            <div className="acc-room-settings">
              <p>
                {category === "Lietuvių kalba"
                  ? "Žodžių skaičius:"
                  : "Klausimų skaičius:"}
              </p>
              <input
                type="number"
                min={1}
                max={category === "Lietuvių kalba" ? 50 : undefined}
                value={questionCount}
                onChange={(event) =>
                  setQuestionCount(Number(event.target.value))
                }
              />
            </div>
          )}

          {category === "Geografija" && (
            <div className="acc-room-settings">
              <p>Klausimų tipai:</p>
              <div className="acc-level-checkboxes">
                {Object.entries(questionTypes).map(([type, label]) => (
                  <label key={type} className="acc-level-label">
                    <input
                      type="checkbox"
                      checked={selectedTypes.includes(type)}
                      onChange={() =>
                        setSelectedTypes((current) =>
                          current.includes(type)
                            ? current.filter((item) => item !== type)
                            : [...current, type],
                        )
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
            </div>
          )}

          {category === "Matematika" && (
            <div className="acc-room-settings">
              <p>Veiksmai:</p>
              <div className="acc-level-checkboxes">
                {Object.entries(mathOperations).map(([type, label]) => (
                  <label key={type} className="acc-level-label">
                    <input
                      type="checkbox"
                      checked={selectedOperations.includes(type)}
                      onChange={() =>
                        setSelectedOperations((current) =>
                          current.includes(type)
                            ? current.filter((item) => item !== type)
                            : [...current, type],
                        )
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
            </div>
          )}

          {category === "Lietuvių kalba" && (
            <div className="acc-room-settings">
              <p>Pasirink lygius:</p>
              <div className="acc-level-checkboxes">
                {accentuationLevels.map(({ level, label }) => (
                  <label key={level} className="acc-level-label">
                    <input
                      type="checkbox"
                      checked={selectedLevels.includes(level)}
                      onChange={() =>
                        setSelectedLevels((current) =>
                          current.includes(level)
                            ? current.filter((item) => item !== level)
                            : [...current, level],
                        )
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
            </div>
          )}
        </>
      ) : (
        <p className="acc-instruction">
          Laukiama, kol kambario kūrėjas parinks nustatymus ir pradės žaidimą...
        </p>
      )}

      <div className="acc-room-buttons">
        <button className="acc-back-btn" onClick={onLeave}>
          {roomCode ? "Išeiti iš kambario" : "Grįžti į pradžią"}
        </button>
        {isHost && (
          <button
            className="start-game-button"
            disabled={!canStart}
            onClick={handleStartClick}
          >
            Pradėti žaidimą
          </button>
        )}
      </div>
    </main>
  );
}

export default GameRoom;
