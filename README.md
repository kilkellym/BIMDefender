# BIM Defender 🎮

A retro arcade game disguised as a Revit add-in. Protect the standards from rogue users, haunted families, and exploded imports!

![BIM Defender Screenshot](screenshots/gameplay.png)

## Overview

**BIM Defender** is a Space Invaders-style game built entirely in WPF that runs inside Autodesk Revit. It's a fun Easter egg add-in that speaks to the daily struggles of BIM managers everywhere.

Every BIM manager knows the pain of:
- Users who ignore standards
- In-place families that haunt models for years
- Exploded CAD imports scattered across views
- Corrupt files that appear at the worst possible time

Now you can fight back. 🚀

## Features

- **Classic arcade gameplay** — Move, shoot, survive
- **BIM-themed enemies** — Each enemy type represents a real modeling nightmare
- **Boss battles** — Face the dreaded Corrupt File every 5 waves
- **Power-ups** — Purge All, Admin Mode, and Backup Save
- **High score leaderboard** — Top 5 scores with classic 3-letter initials
- **Retro sound effects** — Toggle on/off for office-friendly gaming
- **Screen shake & visual effects** — Satisfying feedback on hits and power-ups

## Enemies

| Enemy | Points | Behavior | Description |
|-------|--------|----------|-------------|
| 👾 **Rogue User** | 10 | Standard | Ignores your carefully crafted standards |
| 👻 **In-Place Family** | 25 | Faster | Haunts your model forever |
| 💀 **Exploded Import** | 50 | Shoots back | The aftermath of "Explode" on a CAD import |
| 🦠 **Corrupt File** | 500 | Boss | Appears every 5 waves with multi-shot attacks |

## Power-Ups

| Power-Up | Effect |
|----------|--------|
| 🔵 **Purge All (P)** | Clears all enemies on screen |
| 🟢 **Admin Mode (A)** | Rapid fire for 10 seconds |
| 🟣 **Backup Save (B)** | Absorbs one hit |

## Controls

| Key | Action |
|-----|--------|
| ← → or A/D | Move left/right |
| Space | Shoot |
| P or Esc | Pause |

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract the ZIP file
3. Copy the `BIMDefender` folder to your Revit add-ins directory:
   - `%AppData%\Autodesk\Revit\Addins\20XX\`
4. Launch Revit and find BIM Defender in the Add-Ins tab

## Building from Source

### Requirements

- Visual Studio 2019 or later
- .NET Framework 4.8
- Revit API references (RevitAPI.dll, RevitAPIUI.dll)

### Steps

1. Clone the repository
2. Open `BIMDefender.sln` in Visual Studio
3. Add Revit API references for your target version
4. Set the XAML files' Build Action to `Page`
5. Build the solution
6. Copy output to your Revit add-ins folder

## Compatibility

- Revit 2022, 2023, 2024, 2025, 2026

## Project Structure

```
BIMDefender/
├── Commands/
│   └── BIMDefenderCommand.cs    # Revit external command
├── Game/
│   ├── GameEngine.cs            # Core game logic
│   ├── InputHandler.cs          # Keyboard input
│   └── ScoreManager.cs          # High score persistence
├── Models/
│   ├── Player.cs                # Player state
│   ├── Enemy.cs                 # Enemy types
│   ├── Projectile.cs            # Bullets
│   ├── PowerUp.cs               # Power-up types
│   └── Boss.cs                  # Boss enemy
├── UI/
│   ├── GameWindow.xaml/.cs      # Main game window
│   ├── HighScoreDialog.xaml/.cs # Initials entry
│   └── AboutWindow.xaml/.cs     # Game info
├── Assets/
│   └── SoundManager.cs          # Audio effects
└── BIMDefender.addin            # Revit manifest
```

## Tips

- 💡 Watch out for Exploded Imports — they shoot back!
- 💡 Save your Purge All power-ups for crowded waves
- 💡 The Corrupt File boss appears every 5 waves
- 💡 Grab Backup Save before boss fights
- 💡 Enemies speed up as waves progress

## Contributing
Found a bug? Have an idea for a new enemy type? Pull requests are welcome!

Some ideas for contributions:
- New enemy types (Unplaced Rooms? Orphaned Tags?)
- Additional power-ups
- Difficulty settings
- Two-player mode

## License
MIT License — See [LICENSE](LICENSE) for details.

## Credits
Created by [ArchSmarter](https://www.archsmarter.com)

*"Because sometimes you need a break from coordination meetings."*

---

### Why does this exist?
Because BIM managers deserve to have fun too. And because the best way to deal with rogue users is to shoot them... in a video game.

If you enjoy BIM Defender, check out [ArchSmarter](https://archsmarter.com) for Revit automation courses, tools, and resources that help AEC professionals work smarter.

**Now go defend those standards!** 🚀
