# Campsite Defense — Agent Handoff

**Last updated:** 2026-11-16  
**Project path:** `D:\New folder (2)\Mobile`  
**Unity:** 6 (6000.3.11f1), URP, mobile (Android)  
**Package name:** `com.DefaultCompany.CampsiteDefense` (matches `google-services.json`)  
**Owner:** Erick Barron

Keep things simple. Don't commit or push unless asked. A ScriptableObject's filename must match its class name. The InventoryManager / PROCURAI rules do **not** apply here.

---

## What this game is

Top-down mobile tower-defense / survivor hybrid. Defend a campsite in the center. Enemies spawn from N / NW / NE / W / E (not south — camera can't see it). Player moves with a floating joystick or gyroscope, shoots, buys turrets / walls / shock zones / coin magnets, levels up, collects pickups. Lose when the town dies.

---

## Scenes

| Scene | Path |
|---|---|
| Main menu | `Assets/Scenes/MainMenu.unity` |
| Game | `Assets/Scenes/Game.unity` |

APK output path used recently: `D:\New folder (2)\CampsiteDefense\CampsiteDefense.apk`

---

## Editor tools (run in Unity, then save the scene)

| Menu | What it does |
|---|---|
| `Tools/Game/Build Level` | Rebuilds the Level hierarchy (camp, corridors, turrets, walls, shocks, magnets, spawns, NavMesh). Moves old content into disabled `OldLevel`. |
| `Tools/Game/Build Settings Panel` | Fills SettingsPanel with Touch/Tilt + Music/SFX mute. Run in **both** MainMenu and Game. |
| `Tools/Game/Add Ability Button` | Bottom-left ability charge button on Game HUD. |
| `Tools/Game/Add Leaderboard` | LEADERBOARD button + panel on MainMenu. |
| `Tools/Game/Add Onboarding Tutorial` | First-run tutorial (defend → buy → upgrade). |
| `Tools/Game/Style Main Menu UI` | Restyles menu (keeps OnClick wiring). |
| `Tools/Game/Style Game HUD UI` | Restyles game HUD. |

Many features are built via these tools rather than hand-editing scenes. If something is "missing" in-scene, check whether the tool was run and the scene was saved.

---

## Major systems (current state)

### Level (`LevelBuilder.cs`)
- Camp in center. Plaza walls with N/W/E gates.
- Unlock chains expand corridors: camp turrets unlock Rapid / Grenade / Rail + corridor starts.
- Buyable walls zigzag corridors (NavMeshObstacle carve).
- Shock zones + coin magnets on kill hotspots; **Camp Magnet** behind camp for AFK coin collect.
- Camp ring turrets (~6 m out) so they can defend the camp.
- Turret types: Basic (50), Rapid/SMG (200), Grenade (250), Rail (300, `Rail_Turret.asset`).
- Names are clean ("Turret", "Wall", …) — no underscores. Internal ids only for wiring.
- Buy zones flattened + use `BuyZone.mat` (prefab was fixed; gray dome was wrong material on Turret prefab).
- Ground bake: NavMeshSurface on `Level/Ground`, asset `Assets/Scenes/Game/LevelNavMesh.asset`.

### Onboarding tutorial (`OnboardingTutorial.cs`)
- Phases: Defend → Build → Idle (wait first buy) → Upgrade → Finished.
- Mesh outline via `Custom/Outline` material. Edge arrows to spawn areas. Card with timer / dots.
- `onlyFirstRun` + PlayerPrefs.

### Active ability (`Player_ExperienceAndStats` + `AbilityButton`)
- Kill charge (`Base_Enemy.Killed` static event). Default 25 kills.
- Fires assigned one-time pickup data at player position (`ApplyEffectNow()`). Default: Shockwave.
- Bottom-left radial fill UI; glow + throb when ready.
- Inspector: Ability Pickup, Ability Kills To Charge. Debug: Fill Ability Charge.

### Settings + audio
- `GameSettings` (PlayerPrefs): UseGyro, MusicMuted, SfxMuted.
- Mixer: `Assets/Resources/Audio/GameAudio.mixer` — groups **Music** + **SFX**, exposed `MusicVolume` / `SfxVolume`.
- `GameAudio.PlaySfx` replaces `AudioSource.PlayClipAtPoint`. `FlatAudio` routes to SFX group.
- Scene music sources assigned to Music group in MainMenu + Game.
- Gyroscope: `tiltForFullSpeed = 12`, deadzone 1.5. Hold finger 2 s to recalibrate (+ vibrate).

### Controls
- `PlayerController` (touch joystick) + `GyroscpeController` (typo in class name is intentional / existing).
- `PlayerInputSelector` reads `GameSettings.UseGyro`. Mode change no longer auto-closes settings / resumes.

### Highscores / Firebase
- Packages imported: Auth + Firestore. `Assets/google-services.json` present. Project: `campsitedefense-67ff3`.
- Anonymous sign-in → permanent per-phone UID = Firestore doc id under `scores/{uid}`.
- Fields: `name`, `wave`, `survivedSeconds`, `coins`, `kills`, `upgrades`, `level`.
- Rank: highest wave, then longer survival.
- Only best run kept. Local cache + pending upload queue (one best pending). Retries every 5 s when online.
- Leaderboard: shows **only this phone's** cached best offline; never caches other players (`Source.Server`). Retries while panel open.
- Submit on town death via `RunStats` → `HighscoreService.SubmitRun`.
- Stats tracked: kills (`Base_Enemy.Killed`), coins (`RunStats.AddCoins` from `TownManager.AddCoins`), turret upgrades only (`PurchaseArea.OnAnyUpgraded` + `Building_Turret`), player level, wave, survival time (scaled time).

**Required Firestore rules** (publish in console if not done):

```
rules_version = '2';
service cloud.firestore {
  match /databases/{db}/documents {
    match /scores/{uid} {
      allow read: if true;
      allow write: if request.auth != null && request.auth.uid == uid
        && request.resource.data.wave is int
        && (resource == null
            || request.resource.data.wave > resource.data.wave
            || (request.resource.data.wave == resource.data.wave
                && request.resource.data.survivedSeconds > resource.data.survivedSeconds));
    }
  }
}
```

Do **not** create the `scores` collection by hand — first upload creates it.

Display names are generated (`Camper####`) from UID — no name entry UI yet.

---

## Important file map

```
Assets/Scripts/
  Audio/GameAudio.cs
  Managers/GameSettings.cs, TownManager.cs
  Highscores/HighscoreService.cs, RunStats.cs, RunResult.cs
  Menus/SettingsMenu.cs, LeaderboardPanel.cs, MainMenuManager.cs, PauseManager.cs
  Player/PlayerController.cs, GyroscpeController.cs, PlayerInputSelector.cs
  Player/ExperienceSystem/Player_ExperienceAndStats.cs
  Enemy/Base_Enemy.cs          # static Killed event
  Town/PurchaseArea.cs         # OnAnyUpgraded; FlatAudio
  Pickups/Base_OneTimePickup.cs # ApplyEffectNow()
  UI/AbilityButton.cs, OnboardingTutorial.cs
  Editor/LevelBuilder.cs, UIStyleSetup.cs

Assets/Resources/Audio/GameAudio.mixer
Assets/Resources/Pickups/Shockwave.asset
Assets/Materials/Outline.mat + Outline.shader
Assets/Materials/BuyZone.mat
Assets/google-services.json
```

---

## Patterns / gotchas

- **PurchaseArea:** unlock chains via `areasToUnlockOnPurchase`; `HideAsLocked` / `Unlock`. Buy circle often child `PurchaseZone`.
- **Building_Turret:** `turretData`, `bulletData`, NavMeshObstacle enabled on purchase.
- **Enemies:** NavMeshAgent → town. `detectionRange` 100. Speeds: 4 / 7 Fast / 1.8 Brute.
- **Ground top ~ y = 0.566**; turret roots ~ 0.54.
- **Camera:** looks +Z, ~54° pitch, Cinemachine. Offset ~ (0, 17.64, −13).
- **Coins / waves:** `TownManager` static `Coins`. Wave timer independent of clear.
- **Temp helpers** used before: `%TEMP%\scene_dump.py`, `%TEMP\ui_paths.py` for scene hierarchy dumps.
- Leftover: `Assets/Resources/VuforiaConfiguration.asset` (missing script warning — safe to delete if unused).
- MainMenu has two audio sources on the menu loop (object + FireGlow); may double music — delete FireGlow AudioSource if so.
- Xiaomi install: enable **Install via USB** / USB debugging security; `INSTALL_FAILED_USER_RESTRICTED` = phone canceled install prompt.
- InventoryManager rules.md in user rules is **unrelated** — ignore for this project.

---

## User preferences

- Short, direct answers; user frustrates easily.
- Scaffold / explain when architecture is open; for this game they usually want implementation.
- Don't commit unless asked.
- Visual polish matters (tutorial, UI, ability button throb).
- Prefer Editor menu tools that build scene content over huge hand-edited YAML.

---

## Recently completed (this stretch)

1. Onboarding tutorial + outline material + upgrade step after first tower buy.
2. Full level rebuild (camp-centered, unlock chains, walls, shocks, magnets, multi-dir spawns).
3. Gray buy-zone fix; camp-in-range turrets; clean names; camp magnet; larger map; Rapid/Grenade/Rail types.
4. Active ability (charge on kill → shockwave / configurable pickup) + ability HUD button.
5. Settings: Touch/Tilt, mute Music/SFX, AudioMixer Music+SFX, gyro sensitivity + 2s hold recalibrate.
6. Firebase highscores: per-phone anon UID, multi-stat best run, offline upload queue + offline "your best" leaderboard, menu button tool.

---

## Likely next / unfinished for next agent

- [ ] Confirm user ran and saved: Build Settings Panel (both scenes), Add Ability Button, Add Leaderboard.
- [ ] Confirm Firestore rules published + Anonymous auth enabled.
- [ ] Verify first score appears in Firebase after a real game-over (editor or device).
- [ ] Optional: player name entry instead of `Camper####`.
- [ ] Optional: delete Vuforia leftover; fix double menu music on FireGlow.
- [ ] Device install friction on Xiaomi (USB install permission) — not a code bug.
- [ ] GameOverStats still only shows waves — could show the new run stats on the lose panel.
- [ ] Grenade/Rail turret bullet behavior on placed turrets was noted as lightly unverified.

---

## How to verify quickly

1. Play Game → kill enemies → ability ring fills → tap → shockwave at player.
2. Pause → Settings → toggle Tilt / mute Music / mute SFX; values persist across scenes.
3. Gyro mode: hold screen 2s → phone vibrates, tilt re-zeros.
4. Lose a run → Console: no `Leaderboard:` warnings if online → Firebase Data shows `scores/{uid}`.
5. MainMenu → LEADERBOARD → top by wave; airplane mode → "OFFLINE - SHOWING YOUR BEST".
