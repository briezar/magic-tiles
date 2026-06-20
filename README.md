# Magic Tiles

A rhythm game where you race to tap and hold tiles as they fall in sync with the beat. Land Perfect hits to build your combo multiplier, play through the run without dropping a tile, and see how high you can push your score.

Keyboard support is available: keys 1,2,3,4 correspond to lanes 1,2,3,4.

---

## 🎮 Play in Browser

Play the latest build at: https://briezar.github.io/magic-tiles/

## Screenshots

| | | |
|---|---|---|
| ![Screenshot 1](docs/images/screenshot-1.png) | ![Screenshot 2](docs/images/screenshot-2.png) | ![Screenshot 3](docs/images/screenshot-3.png) |

---

## Overview

Tiles fall in sync with the music across four lanes. Tap them on time to score, build your combo, and chase perfection. Misses won't end the run. Simple to pick up, hard to master.

---

## Features

### Gameplay

| | |
|---|---|
| Rhythm Sync | Tiles spawn and fall in sync with the song's BPM |
| Tap Judgement | Four accuracy tiers: **Perfect, Great, Cool,** and **Miss** |
| Combo & Multiplier | Consecutive Perfects build a combo that scales your score |
| Game Feel | Hit effects, miss flashes, screen shake, and background pulse |
| Song Selection | Pick from multiple tracks on the main menu |
| Autoplay | Demo mode plays the game automatically |

### Technical

| | |
|---|---|
| `.sm` Beatmap Format | Charts authored in Arrow Vortex and parsed into ScriptableObjects |
| Data-Driven Architecture | ScriptableObjects handle song metadata, beatmap data, and runtime state |
| Event-Driven Systems | Tile spawning, input, scoring, and UI communicate through events |
| Object Pooling | Tiles are pooled and recycled to avoid runtime allocation |
| URP Shader | Custom HLSL gradient shader for hold note visuals |
| Reactive Gameplay | Pulses, tweens, particle effects will react to your actions |

---

## How to Run

1. **Clone the repository**

2. **Open in Unity 6000.0.73f1**

3. **Open the bootstrap scene**
   - In the Project window, navigate to `Assets/_Project/Scenes/`
   - Open `Bootstrap`

4. **Press Play**
   - Select a song from the main menu and tap tiles as they fall

---

## Design Choices

### Architecture
The project uses a flat, component-based structure with no complex state machines or service locators. Runtime ScriptableObjects handle state across scenes, keeping systems loosely coupled without singletons. Tile spawning, input, scoring, and UI each live in their own MonoBehaviour and communicate through events and direct references.

### Beatmap Pipeline
Songs are charted in **Arrow Vortex**, a StepMania-compatible editor, and exported as `.sm` files. A custom parser reads them and converts note data into tile spawn events. Adding a new song is as simple as dropping in a `.sm` file and audio clip.

### Scoring
Taps are judged by how close they land to the beat: **Perfect, Great, Cool,** or **None**. Missing a tile doesn't end the run — it just breaks the combo. The base loop stays accessible since any hit scores something, but chasing accuracy still matters.

Consecutive Perfects build a combo that drives a score multiplier. Any rank below Perfect resets it.

### Game Feel
Feedbacks are mostly driven by MMF_Player from the Feel package. Each event — tile tap, combo update, miss, win — has its own dedicated feedback GameObject, making them easy to tune independently.

---

## Asset Attributions

| Asset | Source | License |
|-------|--------|---------|
| Feel | [More Mountains — Unity Asset Store](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) | Paid / Commercial |
| Arrow Vortex | [arrowvortex.ddrnl.com](https://arrowvortex.ddrnl.com) | Freeware |
| "IRIS OUT" | Kenshi Yonezu | All rights reserved |
| "Otonoke" | Creepy Nuts | All rights reserved |
| "TEST ME" | Chanmina | All rights reserved |
| SFX | [Freesound.org](https://freesound.org) | CC0 |
| Font | [Google Fonts — Inter](https://fonts.google.com/specimen/Inter) | OFL |

> **Music Notice:** All music tracks are the property of their respective artists and rights holders. They are included solely for non-commercial portfolio demonstration and I do not claim any ownership over them. If you are a rights holder and would like the files removed, please open an issue or contact me directly and I will comply promptly.

---


*Made by Briezar — 2026*