import { useState } from "react";
import {
  historyDifficulties,
  historyPeriods,
  historyScopes,
} from "../games/historySettings";
import HistoryInRound from "./HistoryInRound";

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
          {historyScopes.map(({ value, label }) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>

      <label className="history-setting">
        Laikotarpis:
        <select value={period} onChange={(event) => setPeriod(event.target.value)}>
          {historyPeriods.map(({ value, label }) => (
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
          {historyDifficulties.map(({ value, label }) => (
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
