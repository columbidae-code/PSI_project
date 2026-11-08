function GameMode({
  category,
  onClose,
  onCreateRoom,
  onPlaySolo,
  soloAvailable,
}: {
  category: string;
  onClose: () => void;
  onCreateRoom: () => void;
  onPlaySolo: () => void;
  soloAvailable: boolean;
}) {
  return (
    <div className="popup-background">
      <div className="game-mode">
        <button className="close-button" onClick={onClose}>
          &#10006;
        </button>

        <h2>{category}</h2>
        <p>Kaip nori žaisti?</p>

        <div className="game-mode-buttons">
          <button
            className="game-mode-button"
            disabled={!soloAvailable}
            onClick={onPlaySolo}
          >
            Žaisti vienam
          </button>

          <button className="game-mode-button" onClick={onCreateRoom}>
            Kurti kambarį
          </button>
        </div>
      </div>
    </div>
  );
}

export default GameMode;
