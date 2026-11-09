import { useEffect, useState } from "react";
import connection from "./signalR";
import Login from "./pages/Login";
import Register from "./pages/Register";
import GameMode from "./pages/GameMode";
import GameRoom from "./pages/GameRoom";
import "./App.css";
import Leaderboard from "./pages/Leaderboard";
import UserPage from "./pages/UserPage";
import { gameRegistry } from "./games/gameRegistry";

type RoomInfo = {
  code: string;
  category: string;
  players: string[];
};

function App() {
  const [showLogin, setShowLogin] = useState(false);
  const [showRegister, setShowRegister] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [roomCode, setRoomCode] = useState<string | null>(null);
  const [joinCode, setJoinCode] = useState("");
  const [players, setPlayers] = useState<string[]>([]);
  const [showNamePopup, setShowNamePopup] = useState(false);
  const [joinUsername, setJoinUsername] = useState("");
  const [username, setUsername] = useState<string | null>(null);
  const [authChecked, setAuthChecked] = useState(false);
  const [showProfile, setShowProfile] = useState(false);
  const [nameAction, setNameAction] = useState<"create" | "join" | null>(null);
  const [soloCategory, setSoloCategory] = useState<string | null>(null);
  // The server keeps the login in a cookie
  useEffect(() => {
    fetch("/api/me")
      .then((response) => (response.ok ? response.json() : null))
      .then((data) => setUsername(data?.username ?? null))
      .catch(() => setUsername(null))
      .finally(() => setAuthChecked(true));
  }, []);

  function handleLogin(name: string) {
    setUsername(name);
  }

  async function handleLogout() {
    await fetch("/api/logout", { method: "POST" });
    setUsername(null);
    setShowProfile(false);
  }

  useEffect(() => {
    if (connection.state === "Disconnected") {
      connection
        .start()
        .then(() => console.log("SignalR connected"))
        .catch((error) => console.log(error));
    }
  }, []);

  useEffect(() => {
    const handlePlayerJoined = (username: string) => {
      setPlayers((players) => {
        if (players.includes(username)) {
          return players;
        }

        return [...players, username];
      });
    };

    connection.on("PlayerJoined", handlePlayerJoined);

    return () => {
      connection.off("PlayerJoined", handlePlayerJoined);
    };
  }, []);

  async function createRoom() {
    if (!selectedCategory) return;

    if (!username) {
      setNameAction("create");
      setShowNamePopup(true);
      return;
    }

    const code = await connection.invoke<string>(
      "CreateRoom",
      username,
      selectedCategory,
    );

    setPlayers([username]);
    setRoomCode(code);
  }
  async function leaveRoom() {
    if (roomCode) {
      const name = username ?? joinUsername;
      try {
        await connection.invoke("LeaveRoom", roomCode, name);
      } catch (error) {
        console.log(error); // still let the user leave if call fails
      }
    }

    setRoomCode(null);
    setSelectedCategory(null);
    setPlayers([]);
    setJoinCode("");
    setNameAction(null);
    setShowNamePopup(false);
  }

  async function handleJoinRoom(playerName: string) {
    const room = await connection.invoke<RoomInfo | null>(
      "JoinRoom",
      joinCode,
      playerName,
    );

    if (room === null) {
      alert("Kambarys nerastas");
      return;
    }

    setSelectedCategory(room.category);
    setRoomCode(room.code);
    setPlayers(room.players);
    setShowNamePopup(false);
  }

  const handlePlayerLeft = (name: string) => {
    setPlayers((players) => players.filter((p) => p !== name));
  };

  connection.on("PlayerLeft", handlePlayerLeft);

  connection.off("PlayerLeft", handlePlayerLeft);

  async function handleNameSubmit() {
    if (joinUsername.trim() === "") return;

    if (nameAction === "join") {
      await handleJoinRoom(joinUsername);
    }

    if (nameAction === "create") {
      if (!selectedCategory) return;

      const code = await connection.invoke<string>(
        "CreateRoom",
        joinUsername,
        selectedCategory,
      );

      setPlayers([joinUsername]);
      setRoomCode(code);
      setShowNamePopup(false);
    }
  }

  const SoloComponent = soloCategory
    ? gameRegistry[soloCategory]?.SoloComponent
    : undefined;

  if (SoloComponent) {
    return <SoloComponent onBack={() => setSoloCategory(null)} />;
  }

  if (roomCode && selectedCategory) {
    return (
      <GameRoom
        category={selectedCategory}
        roomCode={roomCode}
        players={players}
        onLeave={leaveRoom}
      />
    );
  }
  if (username && showProfile) {
    return (
      <UserPage
        username={username}
        onBack={() => setShowProfile(false)}
        onLogout={handleLogout}
      />
    );
  }

  return (
    <div>
      <header className="navbar">
        <div className="logo">Beveik žinojau!</div>

        <nav>
          <a href="/">Pagrindinis</a>
          <a href="#categories">Kategorijos</a>
          <a href="#leaderboard">Lyderiai</a>
        </nav>

        <div className="account-buttons">
          {!authChecked ? null : username ? (
            <>
              <button onClick={() => setShowProfile(true)}>{username}</button>
              <button onClick={handleLogout}>Atsijungti</button>
            </>
          ) : (
            <>
              <button onClick={() => setShowLogin(true)}>Prisijungti</button>
              <button onClick={() => setShowRegister(true)}>
                Registruotis
              </button>
            </>
          )}
        </div>
      </header>

      <main className="main-page">
        <div className="welcome">
          <h1>Beveik žinojau!</h1>
          <p>Pasirink kategoriją ir pasitikrink savo žinias</p>
        </div>

        <div className="join-room">
          <input
            type="text"
            placeholder="Kambario kodas"
            value={joinCode}
            onChange={(e) => setJoinCode(e.target.value)}
          />

          <button
            onClick={() => {
              if (username) {
                handleJoinRoom(username);
              } else {
                setNameAction("join");
                setShowNamePopup(true);
              }
            }}
          >
            Prisijungti
          </button>
        </div>

        <section className="categories" id="categories">
          {Object.entries(gameRegistry).map(([category, config]) => (
            <div
              className="category-card"
              key={category}
              onClick={() => setSelectedCategory(category)}
            >
              <h2>{category}</h2>
              <p>{config.description}</p>
            </div>
          ))}
        </section>

        <section className="how-to-play">
          <h2>Kaip žaisti?</h2>

          <div className="steps">
            <div className="step">
              <div className="step-number">1</div>
              <h3>Pasirink kategoriją</h3>
              <p>Išsirink temą, kurios žinias nori pasitikrinti.</p>
            </div>

            <div className="step">
              <div className="step-number">2</div>
              <h3>Atsakyk į klausimus</h3>
              <p>Stenkis atsakyti teisingai ir kuo greičiau.</p>
            </div>

            <div className="step">
              <div className="step-number">3</div>
              <h3>Rink taškus</h3>
              <p>Surink kuo daugiau taškų ir kilk lyderių lentelėje.</p>
            </div>
          </div>
        </section>

        <Leaderboard />
      </main>

      {showLogin && (
        <Login onClose={() => setShowLogin(false)} onLogin={handleLogin} />
      )}

      {showRegister && <Register onClose={() => setShowRegister(false)} />}

      {selectedCategory && (
        <GameMode
          category={selectedCategory}
          onClose={() => setSelectedCategory(null)}
          onCreateRoom={createRoom}
          soloAvailable={!!gameRegistry[selectedCategory]?.SoloComponent}
          onPlaySolo={() => {
            setSoloCategory(selectedCategory);
            setSelectedCategory(null);
          }}
        />
      )}

      {showNamePopup && (
        <div className="popup-background">
          <div className="popup">
            <button
              className="close-button"
              onClick={() => setShowNamePopup(false)}
            >
              &#10006;
            </button>

            <h2>Įvesk savo vardą</h2>

            <input
              type="text"
              placeholder="Vardas"
              value={joinUsername}
              onChange={(e) => setJoinUsername(e.target.value)}
            />

            <button className="submit-button" onClick={handleNameSubmit}>
              Tęsti
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
