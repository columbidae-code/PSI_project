function GameMode({
  category,
  onClose
}: {
  category: string
  onClose: () => void
}) {
  return (
    <div className="popup-background">
      <div className="game-mode">

        <button className="close-button" onClick={onClose}>
          &#10006;
        </button>

        <h2>{category}</h2>

        <div className="game-mode-buttons">

          <button className="game-mode-button">
            <h3>Žaisti vienam</h3>
          </button>

          <button className="game-mode-button">
            <h3>Kurti kambarį</h3>
          </button>

        </div>

      </div>
    </div>
  )
}

export default GameMode