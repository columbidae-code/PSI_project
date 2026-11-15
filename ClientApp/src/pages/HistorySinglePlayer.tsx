import { useState } from "react";
import HistoryInRound from "./HistoryInRound";

const periods = [
  ["All", "Visi laikotarpiai"],
  ["Senovė", "Senovė"],
  ["Viduramžiai", "Viduramžiai"],
  ["Naujieji amžiai", "Naujieji amžiai"],
  ["Modernybė", "Modernybė"],
];

const difficulties = [
  ["0", "Lengvas (4 įvykiai)"],
  ["1", "Vidutinis (6 įvykiai)"],
  ["2", "Sunkus (8 įvykiai)"],
];

export default function HistorySinglePlayer({
  onBack,
}: {
  onBack: () => void;
}) {
  const [started, setStarted] = useState(false);
  const [questionCount, setQuestionCount] = useState(5);
  const [scope, setScope] = useState("All");
  const [period, setPeriod] = useState("All");
  const [difficulty, setDifficulty] = useState(1);

  if (started) {
    return (
      <HistoryInRound
        questionCount={questionCount}
        scope={scope}
        period={period}
        difficulty={difficulty}
        onBack={() => setStarted(false)}
      />
    );
  }

  return (
    <main className="game-room">
      <h2>Istorija - žaisti vienam</h2>

      <label className="history-setting">
        Klausimų skaičius:
        <input
          type="number"
          min={1}
          max={20}
          value={questionCount}
          onChange={(event) => setQuestionCount(Number(event.target.value))}
        />
      </label>

      <label className="history-setting">
        Sritis:
        <select value={scope} onChange={(event) => setScope(event.target.value)}>
          <option value="All">Visos sritys</option>
          <option value="Pasaulis">Pasaulis</option>
        </select>
      </label>

      <label className="history-setting">
        Laikotarpis:
        <select value={period} onChange={(event) => setPeriod(event.target.value)}>
          {periods.map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>

      <label className="history-setting">
        Sudėtingumas:
        <select
          value={difficulty}
          onChange={(event) => setDifficulty(Number(event.target.value))}
        >
          {difficulties.map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>

      <div className="history-setup-buttons">
        <button onClick={onBack}>Grįžti</button>
        <button
          disabled={
            !Number.isInteger(questionCount) ||
            questionCount < 1 ||
            questionCount > 20
          }
          onClick={() => setStarted(true)}
        >
          Pradėti žaidimą
        </button>
      </div>
    </main>
  );
}
