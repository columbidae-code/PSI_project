import { useState } from "react";
import InRound from "./InRound";

const questionTypes = {
  capital: "Sostinės",
  flag: "Vėliavos",
  map: "Žemėlapiai",
  outline: "Kontūrai",
  neighbour: "Kaimyninės šalys",
};

export default function SinglePlayer({ onBack }: { onBack: () => void }) {
  const [started, setStarted] = useState(false);
  const [questionCount, setQuestionCount] = useState(5);
  const [selectedTypes, setSelectedTypes] = useState<string[]>(
    Object.keys(questionTypes),
  );

  if (started) {
    return (
      <InRound
        questionCount={questionCount}
        questionTypes={selectedTypes}
        onBack={() => setStarted(false)}
      />
    );
  }

  return (
    <main className="game-room">
      <h2>Geografija - žaisti vienam</h2>

      <label>
        Klausimų skaičius:
        <input
          type="number"
          min={1}
          value={questionCount}
          onChange={(e) => setQuestionCount(Number(e.target.value))}
        />
      </label>

      <h3>Klausimų tipai</h3>

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

      <button onClick={onBack}>Grįžti</button>

      <button
        disabled={
          !Number.isInteger(questionCount) ||
          questionCount < 1 ||
          selectedTypes.length === 0
        }
        onClick={() => setStarted(true)}
      >
        Pradėti žaidimą
      </button>
    </main>
  );
}
