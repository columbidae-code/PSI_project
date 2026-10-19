
import { useEffect, useState } from 'react'

type Session = { sessionId: string; playerId: string }
type Question = {
  questionId: string; questionNumber: number; totalQuestions: number
  text: string; imageUrl: string | null; options: string[]
}
type Answer = { isCorrect: boolean; correctOptionIndex: number }
type Result = { correctAnswers: number; totalQuestions: number }

async function api<T>(url: string, data?: object): Promise<T> {
  const response = await fetch(url, data ? {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  } : undefined)

  if (!response.ok) throw new Error(await response.text())
  return response.json()
}

export default function InRound({ questionCount, questionTypes, onBack }: {
  questionCount: number
  questionTypes: string[]
  onBack: () => void
}) {
  const [session, setSession] = useState<Session | null>(null)
  const [question, setQuestion] = useState<Question | null>(null)
  const [answer, setAnswer] = useState<Answer | null>(null)
  const [result, setResult] = useState<Result | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true

    async function start() {
      try {
        const s = await api<Session>('/api/geoquestions/session', {
          questionCount, questionTypes
        })
        const q = await api<Question>(
          `/api/geoquestions/session/${s.sessionId}/questions`, {}
        )
        if (active) {
          setSession(s)
          setQuestion(q)
        }
      } catch (e) {
        if (active) setError(String(e))
      }
    }

    void start()
    return () => { active = false }
  }, [questionCount, questionTypes])

  async function choose(index: number) {
    if (!session || !question || answer || busy) return
    setBusy(true)
    try {
      setAnswer(await api<Answer>(
        `/api/geoquestions/session/${session.sessionId}/answer`,
        {
          questionId: question.questionId,
          playerId: session.playerId,
          selectedOptionIndex: index
        }
      ))
    } catch (e) { setError(String(e)) }
    finally { setBusy(false) }
  }

  async function next() {
    if (!session || !question || busy) return
    setBusy(true)
    try {
      const url = `/api/geoquestions/session/${session.sessionId}`

      if (question.questionNumber === question.totalQuestions) {
        setResult(await api<Result>(
          `${url}/result?playerId=${session.playerId}`
        ))
      } else {
        const q = await api<Question>(`${url}/questions`, {})
        setQuestion(q)
        setAnswer(null)
      }
    } catch (e) { setError(String(e)) }
    finally { setBusy(false) }
  }

  return (
    <main>
      <button onClick={onBack}>Grįžti į lobby</button>

      {error && <p>Klaida: {error}</p>}

      {result ? (
        <h2>Rezultatas: {result.correctAnswers}/{result.totalQuestions}</h2>
      ) : question ? (
        <>
          <h2>{question.questionNumber}/{question.totalQuestions}</h2>
          <h3>{question.text}</h3>

          {question.imageUrl &&
            <img src={question.imageUrl} width={300} alt="Klausimas" />}

          {question.options.map((option, i) => (
            <div key={i}>
              <button disabled={!!answer || busy} onClick={() => choose(i)}>
                {option}
              </button>
            </div>
          ))}

          {answer && (
            <>
              <p>{answer.isCorrect ? 'Teisingai!' : 'Neteisingai!'}</p>
              <p>Teisingas: {question.options[answer.correctOptionIndex]}</p>
              <button disabled={busy} onClick={next}>Toliau</button>
            </>
          )}
        </>
      ) : <p>Kraunama...</p>}
    </main>
  )
}
