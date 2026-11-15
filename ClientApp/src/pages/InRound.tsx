import { useCallback, useEffect, useRef, useState } from "react";

type Session = { sessionId: string; playerId: string };
type Question = {
  questionId: string;
  questionNumber: number;
  totalQuestions: number;
  text: string;
  imageUrl: string | null;
  options: string[];
  timeLimitSeconds: number;
};
type Answer = {
  isCorrect: boolean;
  correctOptionIndex: number;
  pointsEarned: number;
  totalPoints: number;
};
type Result = {
  correctAnswers: number;
  totalQuestions: number;
  totalPoints: number;
  saved: boolean;
};

async function api<T>(url: string, data?: object): Promise<T> {
  const response = await fetch(
    url,
    data
      ? {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        }
      : undefined,
  );

  if (!response.ok) throw new Error(await response.text());
  return response.json();
}

export default function InRound({
  questionCount,
  questionTypes,
  onBack,
}: {
  questionCount: number;
  questionTypes: string[];
  onBack: () => void;
}) {
  const [session, setSession] = useState<Session | null>(null);
  const [question, setQuestion] = useState<Question | null>(null);
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [selectedOption, setSelectedOption] = useState<number | null>(null);
  const [result, setResult] = useState<Result | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [deadline, setDeadline] = useState(0);
  const [now, setNow] = useState(() => Date.now());
  const timeoutSent = useRef<string | null>(null);
  const [streak, setStreak] = useState(0);
  const [bestStreak, setBestStreak] = useState(0);
  const [answerStatus, setAnswerStatus] = useState<
    "normal" | "skipped" | "timeout"
  >("normal");

  useEffect(() => {
    if (!question || answer) return;
    const id = setInterval(() => setNow(Date.now()), 250);
    return () => clearInterval(id);
  }, [question, answer]);

  function startTimer(currentQuestion: Question) {
    setNow(Date.now());
    setDeadline(Date.now() + currentQuestion.timeLimitSeconds * 1000);
  }

  const secondsLeft = Math.max(0, Math.ceil((deadline - now) / 1000));

  useEffect(() => {
    let active = true;

    async function start() {
      try {
        const newSession = await api<Session>("/api/geoquestions/session", {
          questionCount,
          questionTypes,
        });
        const firstQuestion = await api<Question>(
          `/api/geoquestions/session/${newSession.sessionId}/questions`,
          {},
        );
        if (active) {
          setSession(newSession);
          setQuestion(firstQuestion);
          startTimer(firstQuestion);
        }
      } catch (e) {
        if (active) setError(String(e));
      }
    }

    void start();
    return () => {
      active = false;
    };
  }, [questionCount, questionTypes]);

  const choose = useCallback(
    async (index: number | null) => {
      if (!session || !question || answer || busy) return;

      setBusy(true);
      setError("");

      try {
        const checked = await api<Answer>(
          `/api/geoquestions/session/${session.sessionId}/answer`,
          {
            questionId: question.questionId,
            playerId: session.playerId,
            selectedOptionIndex: index,
          },
        );

        setSelectedOption(index);
        setAnswer(checked);
        setAnswerStatus(
          index === null
            ? secondsLeft === 0
              ? "timeout"
              : "skipped"
            : "normal",
        );

        const newStreak = checked.isCorrect ? streak + 1 : 0;
        setStreak(newStreak);
        setBestStreak((previous) => Math.max(previous, newStreak));
      } catch (e) {
        setError(String(e));
      } finally {
        setBusy(false);
      }
    },
    [session, question, answer, busy, secondsLeft, streak],
  );

  useEffect(() => {
    if (!session || !question || answer || busy) return;
    if (deadline === 0 || secondsLeft > 0) return;
    if (timeoutSent.current === question.questionId) return;

    timeoutSent.current = question.questionId;
    void choose(null);
  }, [session, question, answer, busy, deadline, secondsLeft, choose]);

  async function next() {
    if (!session || !question || busy) return;
    setBusy(true);
    try {
      const url = `/api/geoquestions/session/${session.sessionId}`;

      if (question.questionNumber === question.totalQuestions) {
        setResult(
          await api<Result>(`${url}/result?playerId=${session.playerId}`),
        );
      } else {
        const nextQuestion = await api<Question>(`${url}/questions`, {});
        setQuestion(nextQuestion);
        setAnswer(null);
        setSelectedOption(null);
        setAnswerStatus("normal");
        startTimer(nextQuestion);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setBusy(false);
    }
  }

  function getOptionClass(index: number): string {
    const classes = ["geo-option-btn"];
    if (!answer) return classes.join(" ");

    if (index === answer.correctOptionIndex) {
      classes.push("geo-option-correct");
    } else if (index === selectedOption && !answer.isCorrect) {
      classes.push("geo-option-incorrect");
    }
    return classes.join(" ");
  }

  return (
    <main className="acc-game-container">
      <div className="acc-top-bar">
        <button className="acc-back-btn" onClick={onBack}>
          ← Grįžti į kambarį
        </button>
        {question && !result && (
          <div className="acc-stats-bar">
            <span>
              Klausimas: {question.questionNumber}/{question.totalQuestions}
            </span>
            <span>Laikas: {secondsLeft} s</span>
            {answer && <span>Taškai: {answer.totalPoints}</span>}
          </div>
        )}
      </div>

      {error && <p className="acc-error">Klaida: {error}</p>}

      {result ? (
        <div className="acc-card">
          <h2>Raundas baigtas!</h2>
          <p className="acc-result-score">
            Teisingai:{" "}
            <strong>
              {result.correctAnswers} / {result.totalQuestions}
            </strong>
          </p>
          <h3 className="acc-result-points">Iš viso taškų: {result.totalPoints}</h3>
          <p>🔥 Geriausia teisingų atsakymų serija: {bestStreak}</p>
          <p>
            {result.saved
              ? "Rezultatas išsaugotas tavo profilyje."
              : "Prisijunk, kad rezultatas būtų išsaugotas."}
          </p>
          <button className="start-game-button" onClick={onBack}>
            Žaisti dar kartą
          </button>
        </div>
      ) : question ? (
        <div className="acc-card">
          <h2 className="geo-question-text">{question.text}</h2>

          {question.imageUrl && (
            <div className="geo-image-wrapper">
              <img
                src={question.imageUrl}
                alt="Klausimas"
                className="geo-question-image"
              />
            </div>
          )}

          {streak >= 3 && <p>🔥 {streak} teisingi atsakymai iš eilės!</p>}
          <div className="geo-options-grid">
            {question.options.map((option, index) => (
              <button
                key={index}
                className={getOptionClass(index)}
                disabled={!!answer || busy || secondsLeft === 0}
                onClick={() => void choose(index)}
              >
                {option}
              </button>
            ))}
          </div>

          {!answer && secondsLeft > 0 && (
            <div style={{ marginTop: "24px" }}>
              <button disabled={busy} onClick={() => void choose(null)}>
                Praleisti klausimą
              </button>
            </div>
          )}

          {!answer && secondsLeft === 0 && error && (
            <button disabled={busy} onClick={() => void choose(null)}>
              Bandyti užbaigti klausimą dar kartą
            </button>
          )}

          {answer && (
            <div className="geo-feedback">
              <p
                className={
                  answer.isCorrect ? "geo-status-correct" : "geo-status-wrong"
                }
              >
                {answerStatus === "timeout"
                  ? "Laikas baigėsi!"
                  : answerStatus === "skipped"
                    ? "Klausimas praleistas!"
                    : answer.isCorrect
                      ? `Teisingai! (+${answer.pointsEarned} tšk.)`
                      : `Neteisingai! Teisingas atsakymas: ${question.options[answer.correctOptionIndex]}`}
              </p>
              <button
                className="start-game-button"
                disabled={busy}
                onClick={next}
              >
                {question.questionNumber === question.totalQuestions
                  ? "Rezultatai"
                  : "Kitas klausimas"}
              </button>
            </div>
          )}
        </div>
      ) : (
        <p>Kraunama...</p>
      )}
    </main>
  );
}
