# **SLOK: Starfield Load Order Keeper**

A lightweight Windows tool that keeps your Starfield `Plugins.txt` stable and prevents 
accidental load‑order changes that can break saves.

---

## **Why It Exists**

Starfield depends on the exact order of mods listed in `Plugins.txt`.  
If that order changes after you start a save, items, quests, or entire characters can break.  
Unfortunately, Starfield and many mod managers reorder this file automatically.

SLOK helps you to stop that from happening.

---

## **What It Does**

- Saves a reference copy of a known‑good `Plugins.txt`
- Detects when the file changes
- Restores the correct order with one click
- Appends new mods safely at the end
- Normalizes file name casing based on the actual files in the game’s `Data` folder
- Supports multiple profiles (different load orders for different characters)
- Shows a simple diff of what changed
- Can launch the game (uses SFSE if present)

---

## **Profiles**

Profiles let you keep separate load orders for different playthroughs.

Each profile stores:

- A current load order
- A reference load order
- Optional metadata (label, description)

Switching profiles automatically swaps the correct `Plugins.txt` content into place 
and saves your current state.

### **Profile Storage Structure**

```
AppData/Starfield/Profiles/
  ├── default/
  │   ├── main.txt
  │   └── reference.txt
  ├── my-profile/
  │   ├── profile.json
  │   ├── main.txt
  │   └── reference.txt
```

---

---

## **How It Works (Brief)**

1. You set up your load order in Starfield or your mod manager.  
2. When you're happy with it, you tell the app to create a reference.  
3. The app monitors `Plugins.txt` for changes.  
4. If something modifies it, you can:
   - Accept the new order  
   - Restore your reference order  
   - Or revert everything to the reference file  

Disabled mods (lines without `*`) are ignored, just like Starfield does.

---

## **Setup**

On first launch, the app asks for:
- Starfield AppData folder (where `Plugins.txt` lives)
- Starfield installation folder (where the `Data` folder is)

It auto-detects common locations but you can browse manually.

---

## **License**

[MIT License](/LICENSE)

## Credits

I created this for my own use, and decided to share it with the community.

It was entirely developed using **AI-assisted coding techniques** with GPT-5.1-Codex and Claude Sonnet 4.
If you are interested in the development process, the folder [Docs/Agents/Development History](./Docs/Agents/Development%20History)
contains the individual agent plans used to incrementally build the application.
