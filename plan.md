# SayTheSpire2 Architecture Plan

## UI Element System

### Overview

Two categories of UI elements:

1. **Dynamic Elements** - Mod-owned UI (settings screens, help overlays, etc.)
2. **Proxy Elements** - Wrappers around game-owned Godot controls

Both share a common base so the speech system treats them uniformly.

### Class Hierarchy

```
UIElement (abstract base)
  - Owns a LocalizationContext
  - Builds focus string: Label + Extras + Type + Status + Position
  - GetFocusString(), GetLabel(), GetExtrasString(), GetTypeKey(),
    GetStatusString(), GetPosition()

  Dynamic Elements (mod UI):
    Button
    Checkbox
    Slider
    (others as needed)

  ProxyElement (abstract, extends UIElement)
    - Constructor takes a Godot Control reference
    - Shared extraction logic (finding child labels, stripping BBCode, etc.)
    - Ephemeral: created per focus event, not cached

    ProxyButton        - NButton and generic subclasses; extracts label text
    ProxyCheckbox      - NTickbox (extends NButton); extracts label + checked state
    ProxyDropdown      - NDropdown; extracts selected value
    ProxyCard          - NCardHolderHitbox (extends NButton); navigates to
                         parent NCardHolder to find NCard.Model (CardModel)
    ProxyRelic         - NRelicInventoryHolder (extends NButton); finds child
                         NRelic.Model (RelicModel) for name/description
    ProxyPotion        - NPotionHolder (extends NClickableControl); reads
                         .Potion child's NPotion.Model (PotionModel)
    ProxyOrb           - NOrb (extends NClickableControl); reads OrbModel
                         directly for passive/evoke amounts
    (others as needed: NTopBarHp, NEndTurnButton, etc.)
```

### Game's Control Hierarchy (reference)

Important: cards, relics, and potions are display-only Controls, NOT
NClickableControl subclasses. Interaction happens via container/holder classes.

```
Control (Godot)
├── NClickableControl
│   ├── NButton
│   │   ├── NTickbox (checkbox/toggle)
│   │   ├── NTinyCard (small card button)
│   │   ├── NCardHolderHitbox (card interaction target)
│   │   ├── NRelicInventoryHolder (relic interaction target)
│   │   ├── NEndTurnButton, NBackButton, NConfirmButton, NProceedButton...
│   │   ├── NEventOptionButton, NDivinationButton...
│   │   ├── NPotionPopupButton
│   │   └── ~20 other button subclasses
│   ├── NDropdown
│   ├── NOrb (combat orbs - directly interactive)
│   ├── NPotionHolder (combat potion slots - directly interactive)
│   ├── NTopBarHp, NTopBarGold, NTopBarBossIcon...
│   └── other specialized controls
│
├── NCard (display only, has .Model : CardModel)
├── NRelic (display only, has .Model : RelicModel)
├── NPotion (display only, has .Model : PotionModel)
│
├── NCardHolder (abstract container for NCard instances)
│   ├── NHandCardHolder
│   ├── NGridCardHolder
│   └── NPreviewCardHolder, NSelectedHandCardHolder
│
├── NRelicInventory : FlowContainer
└── NPotionContainer
```

### Focus Flow (Game UI)

1. Godot focus lands on a control (`RefreshFocus` hook fires)
2. Check the control's class type via `is` checks (most specific first)
3. Create the appropriate ProxyElement, passing it the Control
4. Proxy extracts data:
   - Simple controls (NButton, NTickbox): read label/state directly
   - Game object controls: navigate to parent/child to find the Model
     (e.g., NCardHolderHitbox → parent NCardHolder → NCard.Model)
5. Build focus string via the base UIElement logic
6. Speak it

### Type Detection

- Use C# `is` pattern matching, ordered most-specific-first:
  - `is NTickbox` before `is NButton` (NTickbox extends NButton)
  - `is NCardHolderHitbox` before `is NButton`
  - `is NRelicInventoryHolder` before `is NButton`
  - etc.
- Game object data comes from Model properties (CardModel, RelicModel,
  PotionModel, OrbModel) — these have name, description, stats
- If class type alone isn't sufficient for some controls, add context-based
  overrides as needed

### GameScreen Registry

Screens that need control-specific labels (e.g., settings) use a GameScreen
registry instead of relying solely on ephemeral proxies.

```
GameScreen (abstract base)
  - Dictionary<Control, UIElement> registry
  - OnOpen() -> calls BuildRegistry()
  - OnClose() -> clears registry
  - OnUpdate() -> virtual, for dynamic content
  - GetElement(Control) -> lookup in registry
  - Register(Control, ProxyElement) -> add to registry

GameScreenManager (static)
  - Maps game IScreenContext types to GameScreen factories
  - Tracks ActiveScreen via ScreenHooks (patches ActiveScreenContext.Update)
  - Announces screen name on change
  - FocusHooks.ResolveElement checks ActiveScreen registry first,
    falls back to ProxyFactory.Create for unregistered controls

SettingsGameScreen : GameScreen
  - Walks %GeneralSettings, %GraphicsSettings, %SoundSettings, %InputSettings
  - Registers all focusable controls with labels from sibling Label nodes
  - Special handling for NDropdownPositioner (two-pass: collect then register)
```

#### NDropdownPositioner Pattern

NDropdownPositioner extends Control directly (not NClickableControl). It wraps
a child NDropdown but receives focus itself. The pattern:

1. Register the positioner (not its child) as the registry key
2. Create ProxyDropdown with the child `_dropdownNode` (via reflection) so
   it reads the dropdown's selected value
3. Set OverrideLabel from the sibling Label node
4. Connect FocusEntered signal (since it's not an NClickableControl,
   RefreshFocus hook won't fire)

#### Two-Pass Registration

Settings panels are walked recursively. NDropdownPositioners are collected
during the first pass and registered last, so their label registrations
overwrite any that were registered for the child dropdown during the walk.

### Design Decisions

- **Registry for labeled screens**: Settings controls need sibling labels that
  can't be discovered from the control alone. GameScreen registry pre-maps
  controls to labeled proxies. Other screens may not need a registry.
- **Ephemeral fallback**: Controls not in any GameScreen registry get ephemeral
  proxies via ProxyFactory (same as before). Registry is additive, not required.
- **Shared base**: ProxyElement is a child of UIElement, not a sibling.
  Both produce focus strings the same way.
- **Godot focus handles hierarchy**: We don't track parent-child navigation
  ourselves. Godot's built-in focus system handles arrowing through menus.
- **Dynamic UI kept simple for now**: Mod screens can hook into Godot's
  scene tree. No custom container/input routing system yet.
- **Display vs Interactive distinction**: Cards/relics/potions are display-only
  Controls. Focus lands on their interactive wrappers (NCardHolderHitbox,
  NRelicInventoryHolder, NPotionHolder). Proxies for these navigate to the
  child/parent display node to read the Model data.
- **Control vs NClickableControl**: NSettingsSlider, NPaginator, and
  NDropdownPositioner extend Control directly. They need separate OnFocus
  patches or FocusEntered signal connections since RefreshFocus only fires
  for NClickableControl subclasses.

## Localization System

### Overview

Custom localization system modeled after STS2's own `LocManager` pattern but
independently maintained. Avoids dependency on the game's localization internals.

### Architecture

- **`LocalizationManager`** - Singleton/static manager
  - Loads JSON tables from mod resources
  - Tracks current language
  - Resolves keys: `LocalizationManager.Get("ui", "BUTTON.LABEL")`
  - Falls back to English when a key is missing in the current language

- **`LocalizationString`** - Holds a table + key reference with optional variables
  - Lazy resolution (rendered at display time, not creation time)
  - Variable substitution: `{varName}` syntax
  - Example: `new LocalizationString("elements", "MONSTER.SHORT_STRING")`
    with `.Add("name", "Jaw Worm").Add("health", "42/44")`

### File Structure

```
localization/
  eng/
    ui.json
    elements.json
    events.json
    positions.json
    (other tables as needed)
  rus/
    ui.json
    ...
```

- JSON files are flat key-value: `{ "BUTTON.LABEL": "Button", ... }`
- Dot-separated keys for logical grouping within a table (flat, not nested JSON)
- One table per domain to avoid STS1's monolithic file problem
- Files embedded in the PCK; source JSON lives in the GitHub repo for translators

### Language Fallback

Current language -> English. No temp/override layer unless needed later.

### Design Decisions

- **Own system, not game's**: Keeps us independent of game localization changes.
  Modeled on the same pattern (JSON tables, two-part keys) for familiarity.
- **Flat key-value JSON**: Simpler than STS1's nested JSON. Dot separators in
  keys provide logical grouping without nesting complexity.
- **No SmartFormat**: Simple `{varName}` interpolation. Add advanced formatting
  (plurals, conditionals) only if we hit a real need.
- **Tables split by domain**: Keeps files small and focused. Easier for
  translators to work on specific areas.
- **PCK distribution**: Translators contribute via GitHub, players get the PCK.

## Position System

No dedicated position classes. STS1's Position hierarchy (ListPosition,
GridPosition, CategoryListPosition) was overengineered for what amounts to
localized format strings with variables.

Positions are handled entirely through the localization system:

```json
// positions.json
"LIST": "{position} of {total}"
"GRID": "row {row}, column {column}"
"CATEGORY_LIST": "{position} of {total} {category}"
```

UIElement has a method that returns a LocalizationString for position (or null).
Each element populates the variables as needed. If richer position logic is
needed later, we can extract a class then.

## Speech System

### Overview

Replace the current direct System.Speech.Synthesis approach with a handler-based
system using Tolk for screen reader integration. Tolk auto-detects the active
screen reader (NVDA, JAWS, etc.) and provides a unified API for speech + braille.

### Architecture

```
SpeechManager
  - Maintains priority-ordered list of ISpeechHandler
  - On init, tries each handler in order, uses first that loads
  - Delegates Speak/Silence/Output calls to active handler
  - Configurable handler priority order (future)

ISpeechHandler (interface)
  - Detect() : bool       — can this handler work on this system?
  - Load() / Unload()     — init/teardown
  - Speak(text, interrupt) — speech only
  - Output(text, interrupt) — speech + braille
  - Silence()              — stop current speech

TolkHandler : ISpeechHandler
  - Loads Tolk.dll via TolkDotNet.dll (P/Invoke .NET wrapper)
  - Auto-detects active screen reader (NVDA, JAWS, SuperNova, etc.)
  - SAPI as fallback within Tolk (configurable via TrySAPI/PreferSAPI)
  - Provides braille output for free

SapiHandler : ISpeechHandler
  - Current System.Speech.Synthesis approach
  - Fallback if Tolk can't load (no Tolk DLLs present, etc.)

ClipboardHandler : ISpeechHandler
  - Last resort fallback
  - Copies text to system clipboard
```

### Native Dependencies

Shipped loose in the mods/ directory alongside the mod DLL:
- Tolk.dll (native, the core library)
- TolkDotNet.dll (managed .NET P/Invoke wrapper)
- nvdaControllerClient64.dll (NVDA support)
- SAAPI64.dll (System Access support)

Assembly resolver (already exists for System.Speech.dll) extended to also
load TolkDotNet.dll from the mods/ directory. Windows finds the native
Tolk.dll automatically if it's in the same directory.

### Future: Bundled Resources

Could embed native DLLs in the managed DLL as embedded resources or in the
PCK, extract to a temp directory on startup. Not needed now — loose files
in mods/ are fine.

### Design Decisions

- **Tolk over direct SAPI**: Blind users already have a screen reader running.
  Tolk delegates to it, giving proper integration + braille. SAPI is a fallback.
- **Handler abstraction**: Same pattern as STS1, proven flexible. Allows adding
  new handlers (e.g., Linux Speech Dispatcher) without changing SpeechManager.
- **Windows-only for now**: No SpeechdHandler (Linux). Can add later if needed.
- **Loose DLL deployment**: Simplest approach. Bundle into PCK/embedded resources
  later if single-file distribution becomes important.
