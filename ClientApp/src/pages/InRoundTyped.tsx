import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'

// A round where the player types the answer instead of picking an option.
// It works with any game whose API has the same shape as /api/mathquestions.

type Session = { sessionId: string; playerId: string }
type Question = {
  questionId: string; questionNumber: number; totalQuestions: number
  text: string; timeLimitSeconds: number
}
type Answer = {
  isCorrect: boolean; correctAnswer: number
  pointsEarned: number; totalPoints: number
}
type Result = {
  correctAnswers: number; totalQuestions: number
  totalPoints: number; saved: boolean
}

async function api<T>(url: string, data?: object): Promise<T> {
  const response = await fetch(url, data ? {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  } : undefined)

  if (!response.ok) throw new Error(await response.text())
  return response.json()
}

export default function InRoundTyped({ apiPath, questionCount, questionTypes, onBack }: {
  apiPath: string
  questionCount: number
  questionTypes: string[]
  onBack: () => void
}) {
  const [session, setSession] = useState<Session | null>(null)
  const [question, setQuestion] = useState<Question | null>(null)
  const [answer, setAnswer] = useState<Answer | null>(null)
  const [result, setResult] = useState<Result | null>(null)
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [deadline, setDeadline] = useState(0)
  const [now, setNow] = useState(() => Date.now())
  const inputRef = useRef<HTMLInputElement>(null)

  // Re-render four times a second while a question is waiting for an answer
  useEffect(() => {
    if (!question || answer) return
    const id = setInterval(() => setNow(Date.now()), 250)
    return () => clearInterval(id)
  }, [question, answer])

  function startTimer(q: Question) {
    setNow(Date.now())
    setDeadline(Date.now() + q.timeLimitSeconds * 1000)
  }

  const secondsLeft = Math.max(0, Math.ceil((deadline - now) / 1000))

  // Put the cursor in the input whenever a new question is ready
  useEffect(() => {
    if (question && !answer && !busy) inputRef.current?.focus()
  }, [question, answer, busy])

  useEffect(() => {
    let active = true

    async function start() {
      try {
        const s = await api<Session>(`${apiPath}/session`, {
          questionCount, questionTypes
        })
        const q = await api<Question>(
          `${apiPath}/session/${s.sessionId}/questions`, {}
        )
        if (active) {
          setSession(s)
          setQuestion(q)
          startTimer(q)
        }
      } catch (e) {
        if (active) setError(String(e))
      }
    }

    void start()
    return () => { active = false }
  }, [apiPath, questionCount, questionTypes])

  async function submit(event: FormEvent) {
    event.preventDefault() // stop the browser from reloading the page
    if (!session || !question || answer || busy || !text.trim()) return
    setBusy(true)
    setError('')
    try {
      setAnswer(await api<Answer>(
        `${apiPath}/session/${session.sessionId}/answer`,
        {
          questionId: question.questionId,
          playerId: session.playerId,
          answer: text
        }
      ))
    } catch (e) { setError(String(e)) }
    finally { setBusy(false) }
  }

  async function next() {
    if (!session || !question || busy) return
    setBusy(true)
    try {
      const url = `${apiPath}/session/${session.sessionId}`

      if (question.questionNumber === question.totalQuestions) {
        setResult(await api<Result>(
          `${url}/result?playerId=${session.playerId}`
        ))
      } else {
        const q = await api<Question>(`${url}/questions`, {})
        setQuestion(q)
        startTimer(q)
        setText('')
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
        <>
          <h2>Rezultatas: {result.correctAnswers}/{result.totalQuestions}</h2>
          <h3>Taškai: {result.totalPoints}</h3>
          <p>
            {result.saved
              ? 'Rezultatas išsaugotas tavo profilyje.'
              : 'Prisijunk, kad rezultatas būtų išsaugotas.'}
          </p>
        </>
      ) : question ? (
        <>
          <h2>{question.questionNumber}/{question.totalQuestions}</h2>
          <p>
            Laiko liko: {secondsLeft} s
            {answer && ` | Taškai: ${answer.totalPoints}`}
          </p>
          <h3>{question.text}</h3>

          <form onSubmit={submit}>
            <input
              ref={inputRef}
              type="text"
              value={text}
              onChange={(e) => setText(e.target.value)}
              disabled={!!answer || busy}
              pattern="-?[0-9]+"
              title="Įrašyk sveikąjį skaičių"
              placeholder="Tavo atsakymas"
              autoComplete="off"
            />
            <button type="submit" disabled={!!answer || busy || !text.trim()}>
              Atsakyti
            </button>
          </form>

          {answer && (
            <>
              <p>{answer.isCorrect ? 'Teisingai!' : 'Neteisingai!'} +{answer.pointsEarned} points.</p>
              <p>Teisingas atsakymas: {answer.correctAnswer}</p>
              <button disabled={busy} onClick={next}>Toliau</button>
            </>
          )}
        </>
      ) : <p>Kraunama...</p>}
    </main>
  )
}