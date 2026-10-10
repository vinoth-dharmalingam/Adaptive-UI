const moodInput = document.querySelector("#mood-input");
const moodChips = [...document.querySelectorAll(".mood-chip")];
const energySlider = document.querySelector("#energy-slider");
const energyValue = document.querySelector("#energy-value");
const recommendButton = document.querySelector("#recommend-button");
const songList = document.querySelector("#song-list");
const playlistIntro = document.querySelector("#playlist-intro");
const sourceLabel = document.querySelector("#source-label");

moodInput.addEventListener("input", () => {
  moodChips.forEach((chip) => chip.classList.remove("selected"));
});

moodChips.forEach((chip) => {
  chip.addEventListener("click", () => {
    moodChips.forEach((item) => item.classList.remove("selected"));
    chip.classList.add("selected");
    moodInput.value = chip.dataset.mood;
  });
});

energySlider.addEventListener("input", () => {
  energyValue.innerHTML = `${energySlider.value} <span>/ 10</span>`;
});

recommendButton.addEventListener("click", async () => {
  const mood = moodInput.value.trim();
  if (!mood) {
    moodInput.focus();
    showError("Add a few words about how you're feeling, and we'll find your soundtrack.");
    return;
  }

  setLoading(true);
  try {
    const response = await fetch("/api/recommendations", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        mood,
        genre: document.querySelector("#genre-select").value,
        energy: Number(energySlider.value),
        count: 5
      })
    });

    const result = await response.json();
    if (!response.ok) {
      throw new Error(result.detail || result.title || "Something went wrong. Please try again.");
    }

    renderRecommendations(result);
  } catch (error) {
    showError(error.message || "We couldn't reach your music curator. Please try again.");
  } finally {
    setLoading(false);
  }
});

function renderRecommendations(result) {
  playlistIntro.textContent = result.intro;
  sourceLabel.textContent = result.source === "ai" ? "AI-CURATED FOR YOU" : "CURATED STARTING POINT";
  songList.replaceChildren();

  result.recommendations.forEach(({ song, reason }, index) => {
    const card = document.createElement("article");
    card.className = "song-card";
    card.style.animationDelay = `${index * 55}ms`;

    const art = document.createElement("div");
    art.className = "album-art";
    art.style.setProperty("--cover", song.color);
    const monogram = document.createElement("span");
    monogram.textContent = song.title.slice(0, 1);
    art.append(monogram);

    const info = document.createElement("div");
    info.className = "song-info";
    const title = document.createElement("h3");
    title.textContent = song.title;
    const artist = document.createElement("p");
    artist.textContent = `${song.artist} · ${song.genre}`;
    const songReason = document.createElement("p");
    songReason.className = "song-reason";
    songReason.textContent = reason;
    info.append(title, artist, songReason);

    const link = document.createElement("a");
    link.className = "spotify-link";
    link.href = `https://open.spotify.com/search/${encodeURIComponent(`${song.title} ${song.artist}`)}`;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.setAttribute("aria-label", `Find ${song.title} by ${song.artist} on Spotify`);
    link.textContent = "↗";

    card.append(art, info, link);
    songList.append(card);
  });

  document.querySelector("#results-subtitle").textContent =
    `${result.recommendations.length} songs, picked for this moment.`;
}

function showError(message) {
  playlistIntro.textContent = "Let's try that again.";
  songList.replaceChildren();
  const error = document.createElement("p");
  error.className = "error-state";
  error.textContent = message;
  songList.append(error);
}

function setLoading(isLoading) {
  recommendButton.disabled = isLoading;
  recommendButton.querySelector("span:first-child").textContent =
    isLoading ? "Finding your soundtrack..." : "Find my soundtrack";
  if (isLoading) {
    playlistIntro.textContent = "Listening for the feeling behind your words...";
    songList.innerHTML = '<div class="empty-state"><span class="empty-orbit" aria-hidden="true">♫</span><p>Finding your sound...</p><span>A moment, while we tune in.</span></div>';
  }
}
