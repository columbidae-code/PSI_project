function GameRoom({
    category,
    roomCode,
    players
}: {
    category: string
    roomCode: string
    players: string[]
}) {
    return (
        <main className="game-room">
            <h2>{category}</h2>

            <div className="room-code">
                <p>Kambario kodas</p>
                <h1>{roomCode}</h1>
            </div>

            <div className="room-players">
                <h2>Žaidėjai</h2>

                {players.map((player) => (
                    <div className="player" key={player}>
                        {player}
                    </div>
                ))}
            </div>

            <button className="start-game-button">
                Pradėti žaidimą
            </button>
        </main>
    )
}

export default GameRoom