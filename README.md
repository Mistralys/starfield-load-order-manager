# Starfield Load Order Keeper

**Automated Load Order Protection & Management**

## The Problem

Starfield manages mods through a file called `Plugins.txt`. Every time you add a mod, it gets 
a specific "slot" in your load order. Once you start a save game, this order becomes a critical 
foundation for your character.

If a mod manager or the game itself shifts these lines around, your save game can break. 
For example, if a mod that provides your favorite spacesuit moves to a different position, 
the suit's reference will no longer exist, causing it to disappear from your inventory.

## The Solution

This app acts as a silent guardian for your mod list. It ensures that once you are happy with your 
setup, it helps you to safeguard your load order-no matter what external tools try to do to your files.

---

## Core Features

### 🛡️ Smart Load Order Protection

Once you are satisfied with your mods, you create a **Reference** file with the app.

* **Sorting**: The tool ensures your existing mods always stay in their assigned spots.
* **Safe Appending**: New mods are intelligently added to the end of the list.
* **Case Correction**: The correct mod file name casing is automatically restored.

### 👥 Multi-Character Profiles

Like profiles in [Vortex][] or similar features in other mod managers, you can create
separate profiles for each of your playthroughs with separate load orders.

* **Instant Switching**: Change your entire load order with a single click when switching characters.
* **Isolated States**: Each profile remembers its own "Known-Good" reference and its current setup.
* **Seamless Backups**: The app automatically saves your current state before switching between profiles.

### 🔍 Change Detection & Visual Diff

The application monitors your files in the background. If something changes—like a mod being moved 
or a new one being added—the app alerts you.

* **Visual Comparison**: See exactly what changed in a clear, color-coded window.
* **Easy Fixes**: Instantly revert unauthorized changes or update your reference to accept changes.

### 🚀 Game Integration

Launch Starfield directly from the app.

* **SFSE Support**: Automatically detects and uses the Starfield Script Extender if you have it installed.
* **Quick Access**: Built-in shortcuts to your game folder, mod data, and configuration files.

---

## Getting Started

To get up and running, the application simply needs to know two things:

1. **Where your game is installed** (to cross-reference mod names).
2. **Where your AppData is** (to protect your `Plugins.txt`).

The app includes an **Auto-Discovery** feature that can pre-fill these paths for most Steam and 
Microsoft Store installations.

---

### Technical Note

While this is a lightweight tool, it is built on modern **.NET 9** architecture to ensure it is fast, 
responsive, and stays compatible with the latest Windows updates. It utilizes a clean, high-contrast 
Dark Mode interface designed to match the Starfield aesthetic.

It was entirely developed using **AI-assisted coding techniques** with GPT-5.1-Codex and Claude Sonnet 4.
If you are interested in the development process, the folder [Docs/Agents/Development History](./Docs/Agents/Development%20History)
contains the individual agent plans used to incrementally build the application.


[Vortex]: https://www.nexusmods.com/about/vortex/
