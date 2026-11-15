import { useCallback, useEffect, useRef, useState } from "react";

type Session = { sessionId: string; playerId: string };
type Card = { id: string; title: string };
type RevealedCard = Card & { displayDate: string; correctPosition: number };
type Question = {
  questionId: string;
  questionNumber: number;
  totalQuestions: number;
  timeLimitSeconds: number;
  cards: Card[];
};
type Answer = {
  isCorrect: boolean;
  correctTimeline: RevealedCard[];
  pointsEarned: number;
  totalPoints: number;
};
type Result = {
  totalQuestions: number;
  correctAnswers: number;
  wrongAnswers: number;
  accuracyPercent: number;
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

export default function HistoryInRound({
  questionCount,
  scope,
  period,
  difficulty,
  onBack,
}: {
  questionCount: number;
  scope: string;
  period: string;
  difficulty: number;
  onBack: () => void;
}) {
  const [session, setSession] = useState<Session | null>(null);
  const [question, setQuestion] = useState<Question | null>(null);
  const [orderedCards, setOrderedCards] = useState<Card[]>([]);
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [result, setResult] = useState<Result | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [deadline, setDeadline] = useState(0);
  const [now, setNow] = useState(() => Date.now());
  const timeoutSent = useRef<string | null>(null);

  useEffect(() => {
    if (!question || answer) return;
    const timerId = setInterval(() => setNow(Date.now()), 250);
    return () => clearInterval(timerId);
  }, [question, answer]);

  function startTimer(nextQuestion: Question) {
    setNow(Date.now());
    setDeadline(Date.now() + nextQuestion.timeLimitSeconds * 1000);
  }

  const secondsLeft = Math.max(0, Math.ceil((deadline - now) / 1000));

  useEffect(() => {
    let active = true;

    async function start() {
      try {
        const createdSession = await api<Session>("/api/historyquestions/session", {
          questionCount,
          scope,
          period,
          difficulty,
        });
        const firstQuestion = await api<Question>(
          `/api/historyquestions/session/${createdSession.sessionId}/questions`,
          {},
        );
        if (active) {
          setSession(createdSession);
          setQuestion(firstQuestion);
          setOrderedCards(firstQuestion.cards);
          startTimer(firstQuestion);
        }
      } catch (caught) {
        if (active) setError(String(caught));
      }
    }

    void start();
    return () => {
      active = false;
    };
  }, [questionCount, scope, period, difficulty]);

  const submit = useCallback(async () => {
    if (!session || !question || answer || busy) return;

    setBusy(true);
    setError("");
    try {
      const checked = await api<Answer>(
        `/api/historyquestions/session/${session.sessionId}/answer`,
        {
          questionId: question.questionId,
          playerId: session.playerId,
          orderedEventIds: orderedCards.map((card) => card.id),
        },
      );
      setAnswer(checked);
    } catch (caught) {
      setError(String(caught));
    } finally {
      setBusy(false);
    }
  }, [session, question, answer, busy, orderedCards]);

  useEffect(() => {
    if (!question || answer || busy || deadline === 0 || secondsLeft > 0) return;
    if (timeoutSent.current === question.questionId) return;

    timeoutSent.current = question.questionId;
    void submit();
  }, [question, answer, busy, deadline, secondsLeft, submit]);

  function moveCard(index: number, direction: -1 | 1) {
    setOrderedCards((current) => {
      const destination = index + direction;
      if (destination < 0 || destination >= current.length) return current;

      const reordered = [...current];
      [reordered[index], reordered[destination]] = [
        reordered[destination],
        reordered[index],
      ];
      return reordered;
    });
  }

  async function next() {
    if (!session || !question || busy) return;

    setBusy(true);
    setError("");
    try {
      const sessionUrl = `/api/historyquestions/session/${session.sessionId}`;
      if (question.questionNumber === question.totalQuestions) {
        setResult(await api<Result>(`${sessionUrl}/result?playerId=${session.playerId}`));
      } else {
        const nextQuestion = await api<Question>(`${sessionUrl}/questions`, {});
        setQuestion(nextQuestion);
        setOrderedCards(nextQuestion.cards);
        setAnswer(null);
        startTimer(nextQuestion);
      }
    } catch (caught) {
      setError(String(caught));
    } finally {
      setBusy(false);
    }
  }

  const correctIds = answer?.correctTimeline.map((card) => card.id);

  return (
    <main className="history-game">
      <button onClick={onBack}>Grįžti į nustatymus</button>

      {error && <p className="history-error">Klaida: {error}</p>}

      {result ? (
        <section className="history-result">
          <h2>
            Rezultatas: {result.correctAnswers}/{result.totalQuestions}
          </h2>
          <p>Taiklumas: {result.accuracyPercent}%</p>
          <h3>Taškai: {result.totalPoints}</h3>
          <p>
            {result.saved
              ? "Rezultatas išsaugotas tavo profilyje."
              : "Prisijunk, kad rezultatas būtų išsaugotas."}
          </p>
          <button onClick={onBack}>Baigti</button>
        </section>
      ) : question ? (
        <section className="history-question">
          <h2>
            Istorijos laiko juosta: {question.questionNumber}/{question.totalQuestions}
          </h2>
          <p>
            Išdėstyk įvykius nuo seniausio iki naujausio. Liko {secondsLeft} s.
            {answer && ` Taškai: ${answer.totalPoints}`}
          </p>

          <ol className="history-timeline">
            {orderedCards.map((card, index) => {
              const correctIndex = correctIds?.indexOf(card.id) ?? -1;
              const revealed = answer?.correctTimeline[correctIndex];

              return (
                <li
                  className={
                    answer
                      ? correctIndex === index
                        ? "history-card history-card-correct"
                        : "history-card history-card-wrong"
                      : "history-card"
                  }
                  key={card.id}
                >
                  <span className="history-card-number">{index + 1}</span>
                  <span className="history-card-title">{card.title}</span>
                  {revealed && (
                    <span className="history-card-date">{revealed.displayDate}</span>
                  )}
                  {!answer && (
                    <span className="history-card-controls">
                      <button
                        aria-label={`Perkelti „${card.title}“ aukščiau`}
                        disabled={busy || index === 0 || secondsLeft === 0}
                        onClick={() => moveCard(index, -1)}
                      >
                        ↑
                      </button>
                      <button
                        aria-label={`Perkelti „${card.title}“ žemiau`}
                        disabled={
                          busy || index === orderedCards.length - 1 || secondsLeft === 0
                        }
                        onClick={() => moveCard(index, 1)}
                      >
                        ↓
                      </button>
                    </span>
                  )}
                </li>
              );
            })}
          </ol>

          {answer ? (
            <>
              <p>{answer.isCorrect ? "Teisingai!" : "Neteisingai!"}</p>
              <button disabled={busy} onClick={() => void next()}>
                {question.questionNumber === question.totalQuestions
                  ? "Rezultatai"
                  : "Toliau"}
              </button>
            </>
          ) : (
            <>
              <button
                disabled={busy || secondsLeft === 0}
                onClick={() => void submit()}
              >
                Atsakyti
              </button>
              {secondsLeft === 0 && error && (
                <button disabled={busy} onClick={() => void submit()}>
                  Atsakymas neišsiųstas - bandyti dar kartą
                </button>
              )}
            </>
          )}
        </section>
      ) : error ? (
        <button disabled={busy} onClick={onBack}>Grįžti į nustatymus</button>
      ) : (
        <p>Kraunama...</p>
      )}
    </main>
  );
}
