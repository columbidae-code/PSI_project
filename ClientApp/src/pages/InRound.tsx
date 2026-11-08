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
  const [result, setResult] = useState<Result | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [deadline, setDeadline] = useState(0);
  const [now, setNow] = useState(() => Date.now());
  const timeoutSent = useRef<string | null>(null);

  // Re-render four times a second while a question is waiting for an answer
  useEffect(() => {
    if (!question || answer) return;
    const id = setInterval(() => setNow(Date.now()), 250);
    return () => clearInterval(id);
  }, [question, answer]);

  function startTimer(q: Question) {
    setNow(Date.now());
    setDeadline(Date.now() + q.timeLimitSeconds * 1000);
  }

  const secondsLeft = Math.max(0, Math.ceil((deadline - now) / 1000));

  useEffect(() => {
    let active = true;

    async function start() {
      try {
        const s = await api<Session>("/api/geoquestions/session", {
          questionCount,
          questionTypes,
        });
        const q = await api<Question>(
          `/api/geoquestions/session/${s.sessionId}/questions`,
          {},
        );
        if (active) {
          setSession(s);
          setQuestion(q);
          startTimer(q);
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
        setAnswer(
          await api<Answer>(
            `/api/geoquestions/session/${session.sessionId}/answer`,
            {
              questionId: question.questionId,
              playerId: session.playerId,
              selectedOptionIndex: index,
            },
          ),
        );
      } catch (e) {
        setError(String(e));
      } finally {
        setBusy(false);
      }
    },
    [session, question, answer, busy],
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
        const q = await api<Question>(`${url}/questions`, {});
        setQuestion(q);
        startTimer(q);
        setAnswer(null);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main>
      <button onClick={onBack}>Grįžti į lobby</button>

      {error && <p>Klaida: {error}</p>}

      {result ? (
        <>
          <h2>
            Rezultatas: {result.correctAnswers}/{result.totalQuestions}
          </h2>
          <h3>Taškai: {result.totalPoints}</h3>
          <p>
            {result.saved
              ? "Rezultatas išsaugotas tavo profilyje."
              : "Prisijunk, kad rezultatas būtų išsaugotas."}
          </p>
        </>
      ) : question ? (
        <>
          <h2>
            {question.questionNumber}/{question.totalQuestions}
          </h2>
          <p>
            Laiko liko: {secondsLeft} s
            {answer && ` | Taškai: ${answer.totalPoints}`}
          </p>
          <h3>{question.text}</h3>

          {question.imageUrl && (
            <img src={question.imageUrl} width={300} alt="Klausimas" />
          )}

          {question.options.map((option, i) => (
            <div key={i}>
              <button
                disabled={!!answer || busy || secondsLeft === 0}
                onClick={() => void choose(i)}
              >
                {option}
              </button>
            </div>
          ))}

          {!answer && secondsLeft === 0 && error && (
            <button disabled={busy} onClick={() => void choose(null)}>
              Bandyti užbaigti klausimą dar kartą
            </button>
          )}

          {answer && (
            <>
              <p>
                {secondsLeft === 0 && !answer.isCorrect
                  ? "Neteisingai arba baigėsi laikas!"
                  : answer.isCorrect
                    ? "Teisingai!"
                    : "Neteisingai!"}{" "}
                +{answer.pointsEarned} taškų
              </p>
              <p>Teisingas: {question.options[answer.correctOptionIndex]}</p>
              <button disabled={busy} onClick={next}>
                Toliau
              </button>
            </>
          )}
        </>
      ) : (
        <p>Kraunama...</p>
      )}
    </main>
  );
}
