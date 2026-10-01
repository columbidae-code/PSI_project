import { useState } from 'react'

type LoginClose = {
    onClose: () => void
}

function Login({ onClose }: LoginClose) {

    // Stores the values entered in the login form
    const [username, setUsername] = useState('')
    const [password, setPassword] = useState('')

    const handleLogin = async () => {

        // Data that will be sent to the backend for authentication
        const data = {
            username: username,
            password: password
        }

        // Backend login endpoint
        const url = '/api/login'

        // Sends the login data to the backend as JSON
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

                <h2>Prisijungimas</h2>

                <div>
                    <label>Vartotojo vardas</label>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
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

                <button className="submit-button" onClick={handleLogin}>
                    Prisijungti
                </button>

            </div>
        </div>
    )
}

export default Login