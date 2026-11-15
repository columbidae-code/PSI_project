import { useEffect, useState } from 'react'

type Session = { sessionId: string; playerId: string }

type Question = {
  questionId: string
  questionNumber: number
  totalQuestions: number
  level: number
  timeLimitSeconds: number
  word: string
  explanation: string
}

type AccentType = 'LEFT' | 'RIGHT' | 'ACUTE'

type Answer = {
  status: 'correct' | 'halfcorrect' | 'incorrect'
  isCorrect: boolean
  correctPosition: number
  correctAccentType: AccentType
  accentedWord: string
  rule: string
  pointsEarned: number
  totalPoints: number
}

type Result = {
  correctAnswers: number
  totalQuestions: number
  totalPoints: number
  saved: boolean
}

const ACCENT_ORDER: AccentType[] = ['LEFT', 'RIGHT', 'ACUTE']

function getAccentChar(type: AccentType): string {
  if (type === 'LEFT') return '\u0300'  // Grave (kairinis)
  if (type === 'RIGHT') return '\u0301' // Acute (dešininis)
  return '\u0303'                       // Tilde (riestinis)
}

async function api<T>(url: string, data?: object): Promise<T> {
  const response = await fetch(
    url,
    data
      ? {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(data),
        }
      : undefined
  )

  if (!response.ok) throw new Error(await response.text())
  return response.json()
}

export default function AccentuationRound({
  questionCount,
  levels,
  seed,
  onBack,
}: {
  questionCount: number
  levels: number[]
  seed?: number | null
  onBack: () => void
}) {
  const [session, setSession] = useState<Session | null>(null)
  const [question, setQuestion] = useState<Question | null>(null)
  const [answer, setAnswer] = useState<Answer | null>(null)
  const [result, setResult] = useState<Result | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [deadline, setDeadline] = useState(0)
  const [now, setNow] = useState(() => Date.now())

  // Currently selected letter index and accent mark
  const [selectedLetter, setSelectedLetter] = useState<number>(-1)
  const [selectedAccent, setSelectedAccent] = useState<AccentType>('LEFT')

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

  useEffect(() => {
    let active = true

    async function start() {
      try {
        const s = await api<Session>('/api/accentuation/session', {
          questionCount,
          levels,
          seed,
        })
        const q = await api<Question>(
          `/api/accentuation/session/${s.sessionId}/questions`,
          {}
        )
        if (active) {
          setSession(s)
          setQuestion(q)
          setSelectedLetter(-1)
          setSelectedAccent('LEFT')
          startTimer(q)
        }
      } catch (e) {
        if (active) setError(String(e))
      }
    }

    void start()
    return () => {
      active = false
    }
  }, [questionCount, levels, seed])

  // selects the letter or cycles the accent mark
  function handleLetterClick(index: number) {
    if (answer || busy) return

    if (selectedLetter !== index) {
      setSelectedLetter(index)
      setSelectedAccent('LEFT')
    } else {
      const currentIdx = ACCENT_ORDER.indexOf(selectedAccent)
      const nextAccent = ACCENT_ORDER[(currentIdx + 1) % ACCENT_ORDER.length]
      setSelectedAccent(nextAccent)
    }
  }

  async function submitAnswer() {
    if (!session || !question || answer || busy || selectedLetter === -1) return
    setBusy(true)
    try {
      const checked = await api<Answer>(
        `/api/accentuation/session/${session.sessionId}/answer`,
        {
          questionId: question.questionId,
          playerId: session.playerId,
          selectedPosition: selectedLetter,
          selectedAccentType: selectedAccent,
        }
      )
      setAnswer(checked)
    } catch (e) {
      setError(String(e))
    } finally {
      setBusy(false)
    }
  }

  async function nextQuestion() {
    if (!session || !question || busy) return
    setBusy(true)
    try {
      const url = `/api/accentuation/session/${session.sessionId}`

      if (question.questionNumber === question.totalQuestions) {
        setResult(await api<Result>(`${url}/result?playerId=${session.playerId}`))
      } else {
        const q = await api<Question>(`${url}/questions`, {})
        setQuestion(q)
        setAnswer(null)
        setSelectedLetter(-1)
        setSelectedAccent('LEFT')
        startTimer(q)
      }
    } catch (e) {
      setError(String(e))
    } finally {
      setBusy(false)
    }
  }

  // Determines what character + accent to display for letter at index i
  function renderLetterText(char: string, index: number): string {
    if (!answer) {
      if (index === selectedLetter) {
        return char + getAccentChar(selectedAccent)
      }
      return char
    }

    // After answer is checked, always show the correct accent on the correct letter
    if (index === answer.correctPosition) {
      return char + getAccentChar(answer.correctAccentType)
    }
    // And keep the player's wrong accent on their wrongly chosen letter
    if (index === selectedLetter && answer.status === 'incorrect') {
      return char + getAccentChar(selectedAccent)
    }
    return char
  }

  // Determines CSS class for letter at index i
  function getLetterClass(index: number): string {
    const classes = ['acc-letter']

    if (!answer) {
      if (index === selectedLetter) classes.push('acc-letter-selected')
      return classes.join(' ')
    }

    if (answer.status === 'correct' && index === answer.correctPosition) {
      classes.push('acc-letter-correct')
    } else if (answer.status === 'halfcorrect' && index === answer.correctPosition) {
      classes.push('acc-letter-halfcorrect')
    } else if (answer.status === 'incorrect') {
      if (index === answer.correctPosition) classes.push('acc-letter-correct')
      if (index === selectedLetter) classes.push('acc-letter-incorrect')
    }

    return classes.join(' ')
  }

  return (
    <main className="acc-game-container">
      <div className="acc-top-bar">
        <button className="acc-back-btn" onClick={onBack}>
          ← Grįžti į kambarį
        </button>
        {question && !result && (
          <div className="acc-stats-bar">
            <span>Klausimas: {question.questionNumber}/{question.totalQuestions}</span>
            <span>Lygis: {question.level}</span>
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
            Teisingai: <strong>{result.correctAnswers} / {result.totalQuestions}</strong>
          </p>
          <h3 className="acc-result-points">Iš viso taškų: {result.totalPoints}</h3>
          <p>
            {result.saved
              ? 'Rezultatas išsaugotas tavo profilyje.'
              : 'Prisijunk, kad rezultatas būtų išsaugotas.'}
          </p>
          <button className="start-game-button" onClick={onBack}>
            Žaisti dar kartą
          </button>
        </div>
      ) : question ? (
        <div className="acc-card">
          <p className="acc-instruction">
            {!answer
              ? 'Paspausk ant raidės, kurią nori sukirčiuoti. Spausk dar kartą, kad pakeistum kirčio ženklą.'
              : answer.status === 'correct'
              ? `Teisingai! (+${answer.pointsEarned} tšk.)`
              : answer.status === 'halfcorrect'
              ? `Beveik! Pasirinkai teisingą raidę, bet ne tą kirčio ženklą (+${answer.pointsEarned} tšk.)`
              : 'Neteisingai! Žalia raidė rodo teisingą kirtį.'}
          </p>

          {/* Word rendered as clickable letters */}
          <div className="acc-word">
            {question.word.split('').map((char, index) => (
              <span
                key={index}
                className={getLetterClass(index)}
                onClick={() => handleLetterClick(index)}
              >
                {renderLetterText(char, index)}
              </span>
            ))}
          </div>

          {/* Grammatical explanation under the word */}
          {question.explanation && (
            <div
              className="acc-explanation"
              dangerouslySetInnerHTML={{ __html: question.explanation }}
            />
          )}

          {/* Rule explanation shown after checking answer */}
          {answer && (
            <div className="acc-rule-box">
              <h4>Taisyklė:</h4>
              <p>{answer.rule}</p>
            </div>
          )}

          <div className="acc-actions">
            {!answer ? (
              <button
                className="start-game-button"
                disabled={selectedLetter === -1 || busy}
                onClick={submitAnswer}
              >
                Tikrinti
              </button>
            ) : (
              <button
                className="start-game-button"
                disabled={busy}
                onClick={nextQuestion}
              >
                {question.questionNumber === question.totalQuestions
                  ? 'Rezultatai'
                  : 'Kitas žodis'}
              </button>
            )}
          </div>
        </div>
      ) : (
        <p>Kraunama...</p>
      )}
    </main>
  )
}