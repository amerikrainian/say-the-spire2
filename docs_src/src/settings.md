# Settings

Press **Ctrl+M** (keyboard), **R2+Start** (Xbox), or **RT+Start** (PS5) to open the mod settings screen from anywhere in the game.

## Top-Level Categories

- **Advanced** — Verbose Logging and Performance Profiling toggles for debugging the mod itself, plus the **UI Enhancements** sub-category (toggles for individual UI improvements the mod adds — auto-focus, bundle preview, keep summons focusable, and so on, one entry per affected screen).
- **Map** — map navigation behavior (auto-advance to choice nodes, read intermediate nodes when going backward, announce current location when the map opens), with a **Points of Interest** sub-category for which POI types are exposed when navigating by POI (Elite, Shop, Treasure, Rest Site, Boss, Ancient, Quest-marked, etc.).
- **Speech** — which speech backend the mod uses, plus per-backend tuning (rate, volume, voice).
- **Announcements** — global toggles for each announcement type (Tooltip, Position, etc.) and any settings the announcement type itself exposes (e.g. Verbose mode, Include Prefix). These are the defaults that per-element overrides inherit from.
- **UI** — a tree with one entry per element type the mod knows about (Creature, Card, Relic, Potion, Map Node, etc.). Each entry has its own Announcements list where you can override the globals just for that element type and reorder how its announcements are spoken.
- **Events** — one entry per event the mod can announce (cards played, HP changes, gold gained, turn changes, etc.), visually grouped into Combat, Cards, Resources, Multiplayer, and Other. See [Event Settings](#event-settings).
- **Keybindings** — every action the mod exposes, grouped into categories (Combat, Map, Markers, Buffers, Navigation, Run Information, etc.). See [Keybindings](#keybindings).

## Per-Element Announcement Overrides

Every UI element type (creature, card, relic, potion, map node, etc.) has its own **Announcements** list under **UI**. The list contains one entry per announcement that element supports — for example a creature's announcements include Label, Type, HP, Block, Energy, Stars, Cards in Hand, Monster Intents, and so on, in the order they're spoken when you focus the element.

### What's on an element's Announcements screen

The first item on the screen is a **Reset to defaults** button. Activating it clears every override under that element back to "inherit", and restores the original announcement order.

After that comes one row per announcement. Each row is a horizontal group of three buttons:

1. **Configure** — opens a screen with that announcement's settings. The settings are the same ones you see at the global level (Announce, Verbose, Include Prefix, etc.), but rendered as per-element overrides — they inherit from the global setting until you change one, after which they remember your explicit choice for this element type only. There is also a Reset to defaults button at the top of the configure screen, that resets only that announcement back to inheriting.
2. **Move Up** — moves this row above the row before it. Focus stays on the Move Up button so you can press it repeatedly to keep moving the row up.
3. **Move Down** — same, but downward.

When you arrow up or down onto a row, the mod announces the row name (e.g. "Label horizontal bar"), the focused button within the row, and the row's position in the list (e.g. "1 of 9"). Use Left and Right to move between the three buttons within the row.

When you move a row, the mod tells you where it landed — "moved between Block and Energy", "moved before Type", "moved after Cards in Hand", etc. The new order is saved immediately and persists across game launches. Adding new announcement types in a future mod update will slot them into your saved order at their canonical position rather than dumping them at the bottom.

## Event Settings

Each event type has its own category with at least:

- **Announce** — speak the event when it happens.
- **Add to buffer** — log it into the events buffer so you can review it later with the buffer controls.

Some events expose extra settings on top of those, like **Include Totals** (running totals on Block/Gold) or **Show Round Number** (on the Player Turn Start event).

### Source Filtering

Events that involve a creature have a **Sources** sub-category with toggles for who's allowed to trigger an announcement:

- **Current Player** — your own actions
- **Other Players** — other players' actions in multiplayer
- **Enemies** — enemy actions

The mod only shows source toggles the game itself provides visual feedback for, so different events will have different combinations of these.

## Speech Settings

The speech category controls how the mod talks to you.

- **Speech Handler** — picks the speech backend. Auto picks the first one that's working on your system. Prism is a unified abstraction that talks to NVDA, JAWS, SAPI, OneCore, etc. behind the scenes. SAPI uses Windows' built-in speech directly. Clipboard copies output to the clipboard instead of speaking it.
- **Per-handler settings** — appear underneath. Prism has a Backend dropdown (which screen reader / TTS engine to pin it to). SAPI has Rate, Volume, and Voice.

## Keybindings

The Keybindings category holds every action the mod exposes, grouped by purpose (Combat, Map, Markers, Buffers, Navigation, Run Information, Mod, etc.). Each action is a button that opens a binding list screen for that action.

On a binding list screen you can:

- **Add Keyboard Binding** — listens for a keystroke to add as a new binding for this action.
- **Add Controller Binding** — listens for a controller button.
- **Replace** — replaces an existing binding for this action with a new one.
- **Delete** — removes a binding.

Every action can have any number of keyboard or controller bindings. There's also a top-level **Reset to defaults** button on the Keybindings screen if you want to wipe your changes.
