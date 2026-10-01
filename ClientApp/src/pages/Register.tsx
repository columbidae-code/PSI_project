import { useState } from 'react'

type RegisterClose = {
    onClose: () => void
}

function Register({ onClose }: RegisterClose) {

    // Stores the values entered in the registration form
    const [username, setUsername] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [repeatPassword, setRepeatPassword] = useState('')

    const handleRegister = async () => {
        if (password !== repeatPassword) {
            alert('Slaptažodžiai nesutampa')
            return
        }

        // Data that will be sent to the backend to register the user
        const data = {
            username: username,
            email: email,
            password: password
        }

        // Backend registration endpoint
        const url = '/api/register'

        // Sends the registration data to the backend as JSON
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
        const result = await response.json()
        alert(result.message) // Displays the message received from the backend
        if(result.ok) {
            onClose()
        }
    }

    return (
        <div className="popup-background">
            <div className="popup">

                <button className="close-button" onClick={onClose}>
                    &#10006;
                </button>

                <h2>Registracija</h2>

                <div>
                    <label>Vartotojo vardas</label>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                    />
                </div>

                <div>
                    <label>El. paštas</label>
                    <input
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                    />
                </div>

                <div>
                    <label>Slaptažodis</label>
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                    />
                </div>

                <div>
                    <label>Pakartokite slaptažodį</label>
                    <input
                        type="password"
                        value={repeatPassword}
                        onChange={(e) => setRepeatPassword(e.target.value)}
                    />
                </div>

                <button
                    className="submit-button"
                    onClick={handleRegister}
                >
                    Registruotis
                </button>

            </div>
        </div>
    )
}

export default Register