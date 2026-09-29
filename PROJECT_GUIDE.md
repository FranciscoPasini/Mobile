# Project Guide

This is a **mobile town-defense / tower-defense** game built in **Unity 6** (`6000.3.11f1`).

You move a player around a town, shoot enemies that walk toward the town hall, collect coins, buy turrets and other buildings, and pick upgrades when you level up. The game is designed for phones: touch joystick, portrait UI (`1080×1920`), Cinemachine camera.

If you are new to the project, read **How a round works** first, then the system that you need. If you are an AI agent, jump to [Current state for the next agent](#current-state-for-the-next-agent) then [AI agent notes](#ai-agent-notes).

---

## Tech stack

| Piece | Version / note |
|---|---|
| Unity | 6 (`6000.3.11f1`) |
| Render pipeline | URP 17.3 |
| Camera | Cinemachine 3.1.7 (`Unity.Cinemachine`) |
| Input | Input System + Enhanced Touch |
| UI | uGUI + TextMeshPro (HUD is UGUI; buy-zone cost labels are world-space `TextMeshPro`) |
| Inspector extras | NaughtyAttributes (`[Button]`, `[ShowIf]`, `[ReadOnly]`) |
| Navigation | NavMesh + `NavMeshAgent` on enemies |

Scenes in the build: `Assets/Scenes/MainMenu.unity` then `Assets/Scenes/Game.unity`.

---

## Folder map (scripts)

Everything gameplay lives under `Assets/Scripts/`. Unity compiles it into `Assembly-CSharp` (no assembly definitions).

```
Assets/Scripts/
  Camera/          Camera shake, pinch zoom
  Coins/           Coin pickups and the coin pool
  Enemy/           Enemies, enemy pool, difficulty curves
  Interfaces/      IDamageable, IPoolable
  Managers/        TownManager, lose panel
  Menus/           Main menu, pause, HUD texts, safe area
  Player/
    PlayerController.cs
    ExperienceSystem/   XP, levels, upgrades, level-up UI
    WeaponSystem/       Weapons, bullets, targeting, pickups
  Town/            Town hall, turrets, purchase areas, buy-zone visual + cost label
  Editor/          One-shot scene setup menus (editor only)
```

Data lives next to the thing it describes:

- Weapons: `Assets/Resources/Weapons/`
- Bullets: `Assets/Resources/Bullets/`
- Purchasable building costs: `Assets/Resources/Buildings/`
- Prefabs: `Assets/Prefabs/` (turret, purchase area, bullets, pickups)
- Scenes: `Assets/Scenes/`

---

## How a round works

This is the loop everything else hangs off.

```
MainMenu  --Play-->  Game scene
                         |
                         |  TownManager starts waves on a timer
                         v
              EnemyPool spawns weighted enemy types
                         |
                         |  enemies path to Building_Town
                         v
         Player shoots (PlayerWeaponSystem + range sphere)
                         |
                         |  enemy dies
                         v
     coin drop + XP + maybe a weapon pickup
                         |
          XP fills the bar --> level up popup (3 cards)
                         |
     coins buy / upgrade by standing in a zone
       - Building_Turret has its own zone (do not migrate)
       - other buildings use a separate PurchaseArea
                         |
              town HP hits 0 --> game over (timeScale = 0)
```

**Player** and **turrets** both shoot `Base_Bullet` prefabs. **Enemies** and the **town** both implement `IDamageable`. **Coins**, **enemies**, and **weapon pickups** are pooled (`IPoolable`): they deactivate instead of being destroyed.

---

## Systems

### 1. Town and waves — `TownManager`

`Assets/Scripts/Managers/TownManager.cs`

The hub for a run:

- Resets the static `Coins` value in `Awake` (statics survive if domain reload is off).
- Every `nextWaveTime` seconds (default 40), spawns a wave **even if the last wave is still alive**.
- Wave size: `enemiesPerWave + (wave - 1) * extraEnemiesPerWave`, capped by `maxEnemiesPerWave`.
- Forwards town damage / heal to the HUD and camera.
- On town death: `Time.timeScale = 0` and shows the lose panel.

**Coins** are a static int on `TownManager` (`TrySpendCoins`, `AddCoins`, `OnCoinsChanged`). This is a placeholder. The comment in the script says they will move to a currency manager later. Do not add a second coin counter.

Related:

- HUD: `UIsTexts` listens to coins, wave, and `OnTownHealthChanged`.
- Camera: `CameraShake` listens to `OnTownDamageTaken` only (upgrading town health must not shake the camera).

---

### 2. Town hall — `Building_Town`

`Assets/Scripts/Town/Building_Town.cs`

The thing enemies walk to and shoot. Implements `IDamageable`.

Events: `onDamageTaken`, `onHealed`, `onDied`, `onRespawned`.

`SetMaxHealth` only changes the cap. `Heal` is what raises current HP **and** notifies the UI. When a town-health upgrade is picked, `Player_ExperienceAndStats` sets the new max and heals the **amount that was added**, so a damaged town is not fully restored.

---

### 3. Turrets — `Building_Turret`

`Assets/Scripts/Town/Building_Turret.cs`  
Data: `Assets/Scripts/Town/Data/Building_Turret_Data.cs`  
Prefab: `Assets/Prefabs/Turret.prefab`

Kingshot-style purchase: stand in a **collider zone on the turret** and coins drain until the level is paid.

- Level 0 = unbought. Entry 0 in `levels` is the purchase.
- `infiniteLevels` + `TurretScaling` keep going past the hand-made list.
- Combat: overlap-sphere for enemies, then spawn bullets from `bulletData` (same bullet pipeline as the player).
- The buy-zone sphere uses `BuyZoneVisual` + `Custom/BuyZoneFill` shader (object-space XZ on a **sphere**, not a quad).
- Remaining coins for the next buy float above the zone via `PurchaseCostLabel` (world-space TMP, faces the camera). Hides at max level, same as the fill.

**Do not migrate turrets onto `PurchaseArea`.** They keep their own zone, payment loop, and combat. `IPurchasable` is for everything else.

The player has **no Rigidbody** for movement (transform is set directly), so zone checks use `collider.ClosestPoint` against a point at the zone’s height. Do not rely on `OnTriggerEnter` for “player is in the zone.”

`RemainingCost` is `NextCost - paidTowardNext`. `BuyZoneVisual` and `PurchaseCostLabel` both prefer a `PurchaseArea` on the same object, then fall back to `Building_Turret` in parents.

---

### 4. Purchase areas and other buildings

Pay zone and building are **separate objects**.

| Piece | Role |
|---|---|
| `PurchaseArea` | Collider the player stands in. Drains coins. Does not own combat. |
| `IPurchasable` | Contract the zone talks to (`NextCost`, `RemainingCost`, `ApplyPayment`, events). |
| `Base_PurchaseableBuilding` | Default implementer. Visuals, cost list, enable/disable lists. |
| `Building_Purchaseable_Data` | ScriptableObject: `costs[0]` is the first buy, later entries are upgrades, optional `infiniteLevels`. |
| `Building_Obstacle` | Wall. Enables `NavMeshObstacle`s on first purchase. |
| `Building_PathClear` | Door / rubble. Disables extra blockers on first purchase. |

Prefab: `Assets/Prefabs/PurchaseArea.prefab` already has `PurchaseArea` + `BuyZoneVisual` + `PurchaseCostLabel`. Link `purchasable` to a `Base_PurchaseableBuilding` in the scene. If that field is empty, the area **disables itself**.

**Unlock chain.** `areasToUnlockOnPurchase` stays off until this building is bought, then `Unlock()` turns those areas on (collider, fill, cost label). Use this for wall → door, first pad → second pad. `hideUnlockAreasUntilPurchased` disables listed areas at start.

Payment rules match the turret: start delay, drain over `paymentDuration`, `requireReentryAfterPurchase`, closest-point (not trigger enter). At max level the area disables.

**`BuyZoneVisual`** works on both: it looks for `PurchaseArea` first, then `Building_Turret`. Do not put both on the same object unless you mean the area to win.

**`PurchaseCostLabel`** (`Assets/Scripts/Town/PurchaseCostLabel.cs`):

- Same lookup as the fill (area, then turret).
- Creates a child world-space `TextMeshPro` at runtime if none is assigned.
- Shows `RemainingCost` (counts down while paying).
- `LateUpdate` copies `Camera.main` rotation so the number faces the screen.
- Compensates parent scale (turret `PurchaseZone` is scaled ~1.94).
- Height: `worldOffset` (default `(0, 1.35, 0)` world units above the zone).
- `PurchaseArea` / turret `Start` will `AddComponent` this if it is missing.

Cost data assets: `Assets/Resources/Buildings/` (example `Obstacle.asset`). New ScriptableObject class must stay in `Building_Purchaseable_Data.cs`.

---

### 5. Enemies — `Base_Enemy` + `EnemyPool`

`Assets/Scripts/Enemy/Base_Enemy.cs`  
`Assets/Scripts/Enemy/EnemyPool.cs`  
`Assets/Scripts/Enemy/EnemyScaling.cs`

Enemies:

- Use `NavMeshAgent`. Pathing is throttled (`pathUpdateInterval`), not every frame.
- Target `Building_Town`. Attack is a short raycast (triggers ignored so coins do not eat the shot).
- On a **real death** (not pool cleanup): drop a coin, grant XP, maybe drop a weapon, then `Despawn`.
- Pooled enemies are **deactivated**, not destroyed. Use `IsAlive` (`!isDead && activeInHierarchy`), never a null check alone.
- `Stun` / `Knockback` stop the agent and optionally warp them on the NavMesh (shotgun / grenades).

**Difficulty** lives on the prefab (`EnemyScaling`), because a fast weak enemy and a slow tank should grow differently.

The **pool** decides *when* difficulty goes up:

- `wavesPerDifficultyIncrease` (default 2): 1 = every wave, 2 = every other wave, …
- Difficulty level = `(wave - 1) / wavesPerDifficultyIncrease`
- Stats are recalculated from **base** values on spawn, so they never compound when the enemy is reused.
- Already-alive enemies keep the stats they spawned with unless you press **Apply Difficulty To Active**.

**Types:** `Enemy Types` is a weighted list (`prefab`, `spawnWeight`, `unlockWave`, `initialSize`). If the list is empty, the old single `enemyPrefab` is used.

Instances are created under an **inactive** parent so `Awake` and the NavMeshAgent wait until the enemy is placed on the mesh.

---

### 6. Player movement — `PlayerController`

`Assets/Scripts/Player/PlayerController.cs`

Touch joystick via Enhanced Touch. Moves by setting `transform.position`. Rotates toward the move direction.

`playerSpeed` is the **base**. `CalculatedPlayerSpeed()` multiplies it by `GetMoveSpeedMultiplier()` whenever stats upgrade.

The player Rigidbody (if present on the prefab) is **not** what moves the character. Trigger events against a sleeping dynamic Rigidbody are unreliable; targeting and magnets do **not** depend on them.

---

### 7. Weapons and shooting

Three layers:

1. **`Base_Weapon_Data`** — ScriptableObject. Name, base stats, bullet data, fire sound, `WeaponScaling`, and a `[SerializeReference] weaponClass` (which C# class fires).
2. **`Base_Weapon`** — runtime object. Copies stats, applies player upgrade levels, auto-fires.
3. **`PlayerWeaponSystem`** — on the player. Owns the **default** weapon (infinite ammo) and one **picked-up** weapon (finite ammo). Always updates **both** when you level a stat.

`TargetCollisionUpdater` sits on a range sphere. It does **not** use trigger callbacks. Every frame it `OverlapSphereNonAlloc`s the enemy layer and gives the closest living enemy to the weapon. Dead pooled enemies are skipped.

`Base_Weapon_Data` **must** live in a file named `Base_Weapon_Data.cs`. Unity will not link a ScriptableObject if the class is nested in another file. A missing script on `Pistol.asset` made the player unable to shoot when loading Game from MainMenu (editor already had the asset in memory if you pressed Play on Game directly).

**Firing:** `AutoFire` needs a valid target. `FireEffect` is overridden per weapon:

| Class | What it does |
|---|---|
| `Weapon_Pistol` / `Weapon_SMG` | One bullet at the target |
| `Weapon_Shotgun` | Several pellets in a cone (`pelletCount` / `spreadAngle` on the class). Pellets pass `null` target so they can hit anyone. |
| `Weapon_Railgun` | One hitscan beam |
| `Weapon_GrenadeLauncher` | One arcing grenade |

`CreateRuntimeWeapon` clones `weaponClass` with `JsonUtility`, then `Initialize` copies stats and **`bulletData` from the asset**. Nested `bulletData` on the serialized class is often `{fileID: 0}` — that is normal. The asset-level `bulletData` is what must be assigned.

Assets in `Assets/Resources/Weapons/`: `Pistol`, `SMG`, `Shotgun`, `Railgun`, `GrenadeLauncher`.

---

### 8. Bullets

`Assets/Scripts/Player/WeaponSystem/Bullet/`

- `Base_Bullet_Data.Spawn` instantiates `bulletPrefab`, aims at a point, finds `Base_Bullet` on the root **or children**, then `Initialize(speed, damage, target, …)`.
- Speed and damage come from the **weapon**, not the bullet data.
- If `bulletPrefab` is empty, Spawn logs a warning and returns null (nothing flies).
- `Base_Bullet` moves forward and dies on lifetime. If `targetObject` is set, the default trigger only hits that object. Shotgun pellets pass `null` so they can hit anyone in the cone.

**Wiring that must stay in sync** (this already broke once — only SMG worked because it used the pistol bullet):

| Weapon | Bullet data | Prefab | Component on the prefab |
|---|---|---|---|
| Pistol / SMG | `StandardBullet` | `Assets/Prefabs/Bullets/Bullet.prefab` | `Bullet_Standard` |
| Shotgun | `ShotgunPellet` | `Assets/Prefabs/Bullets/Bullet_Shotgun.prefab` | `Bullet_Shotgun` |
| Railgun | `RailBeam` | `Assets/Prefabs/Bullets/Bullet_Railgun.prefab` | `Bullet_Rail` |
| Grenade launcher | `Grenade` | `Assets/Prefabs/Bullets/Bullet_Grenade.prefab` | `Bullet_Grenade` |

Special bullets:

- `Bullet_Standard` — damage tagged `Enemy`, then destroy.
- `Bullet_Shotgun` — damage + `Knockback` (`knockbackStrength`, `stunDuration`).
- `Bullet_Rail` — raycast pierce, `LineRenderer` trail (adds one if missing), no travel.
- `Bullet_Grenade` — gravity, SphereCast into solids, explode, radius damage + knockback.

**Do not duplicate `Bullet.prefab` and leave `Bullet_Standard` on the copy.** Duplicating keeps the pistol script. Swap the MonoBehaviour to the special class, then assign that prefab on the matching `Base_Bullet_Data`. Confirm in the Inspector that `bulletPrefab` is not `None`.

---

### 9. Experience, stats, level-up

`Player_ExperienceAndStats` is the single source of upgrade **levels**. It does not store weapon numbers. Each weapon asset turns those levels into stats with its own `WeaponScaling`.

| Upgrade (`UpgradeType`) | What it changes |
|---|---|
| Weapon damage / fire rate / bullet speed / range / ammo | `WeaponStatLevels` → both equipped weapons |
| Move speed | PlayerController |
| Pickup range | Coin magnets **and** weapon-pickup magnets |
| Town health | Town max HP + heal the added amount |
| Experience gain | Multiplies XP from kills (asymptotic cap 3×) |

XP curve: `ExperienceCurve` (polynomial default). `AddExperience` applies `currentExpMultiplier`, can grant several levels at once, leftover XP carries over.

**Level-up UI**

- `Upgrade_Class` — name, description, icon, type (not a MonoBehaviour).
- `UpgradeCard` — one button; fills text from an `Upgrade_Class`.
- `LevelUpPopup` — listens to `onPlayerLevelUp`, pauses (`timeScale = 0`), shows 3 random **different** types, applies the pick, deals extra hands if several levels arrived at once.

Editor menu **Tools → Game → Setup Level Up UI** builds the bar + popup in the open scene.

`ExperienceBar` fills on `onExperienceChanged` using **unscaled** time so it still animates while the popup has the game paused.

---

### 10. Coins

`CoinPool` + `Base_Coin`

On death, a coin type is rolled from wave-weighted odds (copper common early, diamond later).

Lifecycle:

1. Launch up with a Rigidbody, land on the ground.
2. Rest. Player magnet (closest-point test, same reason as the turret zone).
3. Fly to the player and add `TownManager.Coins`.
4. If **lifetime** runs out while idle: **despawn, no coins**. They blink in the last 5 seconds (faster as time runs out).
5. `Collect()` still pulls from any distance — used by a future “collect all” building. `CollectAll()` on the pool is that hook.
6. If the pool is almost empty, oldest **idle** coins are recycled (no payout). Coins already flying to the player are left alone.

Pickup range upgrades scale the magnet collider from the prefab’s original size (never compound).

---

### 11. Weapon pickups

`WeaponPickup` + `WeaponPickupPool`

Same magnet / blink / fly pattern as coins. Collecting calls `PlayerWeaponSystem.PickUpWeapon`. Ammo empty → back to the default pistol. Picking up another weapon **replaces** the current one.

Drop chance is on the **pool** (`dropChance`, default 8%), not on each enemy. Weighted list of `Base_Weapon_Data`. If the list is empty at runtime, it loads `Resources/Weapons/` except the Pistol.

Pickup range upgrades apply here too.

**Game scene weights** (on the `WeaponPickupPool` object, `dropChance` 0.08):

| Weapon | Unlock wave | Base weight | Per wave | Min–max |
|---|---|---|---|---|
| SMG | 1 | 50 | −2 | 20–50 |
| Shotgun | 1 | 30 | +0.5 | 15–40 |
| Grenade launcher | 3 | 12 | +1.2 | 0–30 |
| Railgun | 1 | 6 | +1 | 0–20 |

Pistol is the default gun, not a drop.

---

### 12. Camera

- `CinemachineCamera` follows the player.
- `CameraShake` on the Main Camera: Cinemachine impulse + a red vignette flash on town damage.
- `MobileCameraZoom` pinch-zooms the Cinemachine camera (Enhanced Touch).

---

### 13. Menus and HUD

- `MainMenuManager.PlayButton` loads `"Game"`.
- `PauseManager` sets `timeScale` 0/1. Returning to the menu sets it back to 1.
- `LosePanelManager` currently does **not** reset `timeScale`. If you wire its buttons, reset scale or the next scene starts frozen.
- `UIsTexts` must unsubscribe in `OnDisable` (especially from static `OnCoinsChanged`).
- Ammo TMP: `Ammo: Infinite` on the default pistol, otherwise remaining shots on the pickup. Pickup ammo is polled in `Update` because shots do not fire `OnActiveWeaponChanged`.

---

## Shared ideas (read these once)

**Pooling.** `Despawn` = deactivate + event. `Spawn` = activate + reset. Do not `Destroy` pooled objects. Validity = `activeInHierarchy` and, for enemies, `IsAlive`.

**Triggers vs closest-point.** The player is moved by transform, not physics. Use overlap queries or `ClosestPoint` for “is the player inside this volume.”

**Scaling.** `StatScaling` + `ScalingMethod` (Linear, Exponential, Logarithmic, SquareRoot, Asymptotic, None). Higher-is-better stats multiply the factor; fire rate (seconds between shots) **divides**. Always scale from a cached **base**, never from the already-scaled value.

**Static events.** Subscribe and unsubscribe. `TownManager.OnCoinsChanged` is static.

**`FindFirstObjectByType`.** Used as a fallback when a reference is empty. Prefer wiring in the Inspector. With `using System;`, `Object` is ambiguous — use `UnityEngine.Object` or drop the prefix.

**Two buy-zone systems.** Turret zone is built into `Building_Turret`. Everything else uses `PurchaseArea` + `IPurchasable`. Shared visuals: `BuyZoneVisual`, `PurchaseCostLabel`.

---

## What you still hook up in the Editor

Scripts cannot finish these for you:

1. **Player_ExperienceAndStats** must exist in the Game scene (the setup menu can add it to `Manager`).
2. Enemy prefabs: assign each type on `EnemyPool`, tune `EnemyScaling` per prefab.
3. Bullet prefabs: `Bullet_Shotgun` / `Bullet_Rail` / `Bullet_Grenade` components on the meshes, assigned on the matching bullet data assets (see the table in [Bullets](#8-bullets)).
4. **WeaponPickup** prefab + `WeaponPickupPool` in the scene, weights pointing at shotgun / SMG / railgun / grenade launcher.
5. Turret buy-zone collider + `BuyZone.mat` + `PurchaseCostLabel` (already on `Turret.prefab`).
6. Non-turret buildings: place `PurchaseArea.prefab`, link `purchasable`, optionally fill `areasToUnlockOnPurchase`.
7. Fire sounds on weapon assets if you want them.

---

## Current state for the next agent

Read this before changing weapons, bullets, or buy zones. This is what is true in the repo **now**, including bugs already fixed.

### Weapons / bullets (fixed)

Only the SMG appeared to fire because shotgun / rail / grenade **data had `bulletPrefab: None`**, and the three special prefabs still ran `Bullet_Standard` (they were copies of `Bullet.prefab`).

That is wired now. If a pickup “does nothing,” check in this order:

1. Weapon asset `bulletData` points at the right `Base_Bullet_Data`.
2. That data’s `bulletPrefab` is not empty.
3. The prefab’s script is `Bullet_Shotgun` / `Bullet_Rail` / `Bullet_Grenade`, not `Bullet_Standard`.
4. Console: `has no bullet prefab assigned` vs `missing a Base_Bullet component`.

Pistol / SMG still share `StandardBullet` → `Bullet.prefab` → `Bullet_Standard`. That is intended.

### Purchase areas (new)

- Turrets **unchanged**: own collider, own drain loop, own combat.
- Other buildings: `Base_PurchaseableBuilding` + separate `PurchaseArea`.
- Zones can unlock other zones after buy (`areasToUnlockOnPurchase`).
- `BuyZoneVisual` follows `PurchaseArea` or turret.
- `PurchaseCostLabel` shows remaining cost, billboards to the camera, hides at max. On `PurchaseArea.prefab` and turret `PurchaseZone`. Creates TMP at runtime.
- Game scene currently has **turret prefab instances**. `PurchaseArea.prefab` exists for walls / path-clears; place and link it when adding those buildings.

### HUD

`UIsTexts.ammoText`: Infinite vs remaining pickup ammo.

### Do not

- Do not convert `Building_Turret` to `IPurchasable` / `PurchaseArea` unless the user asks.
- Do not auto-collect idle coins for value. Lifetime timeout = despawn, no payout. `Collect()` still pulls from any distance.
- Do not use player trigger overlap. Transform movement.
- Do not nest a new ScriptableObject class in another file.

---

## AI agent notes

Give this section to an agent working in this repo. Follow it even if a later chat asks you to “just hack it in.”

### What this project is

Unity 6 URP mobile game. C#. No tests. No asmdefs. Gameplay is almost entirely `Assets/Scripts/**/*.cs` plus ScriptableObjects under `Assets/Resources/`. Do not “fix” `Library/`, `Temp/`, or `Logs/`.

### Before you edit

1. Read the system you are touching in this guide, then open the files it names.
2. Match existing patterns: pools, `StatScaling`, Inspector `[Tooltip]`, NaughtyAttributes buttons, `FindFirstObjectByType` only as fallback.
3. Do not introduce a new singleton/manager if `TownManager` or `Player_ExperienceAndStats` already owns that data.
4. Do not add `using System;` to a file that uses `Object.Find…` or `Random` without fixing ambiguity (`UnityEngine.Object`, `UnityEngine.Random`).
5. New ScriptableObject classes go in **their own file named after the class**. Nested SO classes break asset script links (this already shipped as a MainMenu → Game “player does not shoot” bug).

### How to add common things

**New enemy type**

- Duplicate an enemy prefab, change base stats, tune `EnemyScaling`.
- Add an `EnemySpawnEntry` on `EnemyPool` (weight, unlock wave, initial size).
- Do not new-up enemies in `TownManager`; the pool owns instances.

**New weapon**

- Subclass `Base_Weapon`, override `FireEffect`.
- Optional: subclass `Base_Bullet` on a **new** projectile prefab (do not leave `Bullet_Standard` on a duplicated mesh).
- Create `Base_Bullet_Data` (assign the prefab) and `Base_Weapon_Data` (set `weaponClass` and `bulletData` in the Inspector).
- `CreateRuntimeWeapon` clones `weaponClass` via `JsonUtility` so extra fields (pellet count, etc.) copy. Keep extra data as `[SerializeField]` fields on the subclass, not as unserialized locals. Runtime `bulletData` is filled in `Initialize` from the **asset**.
- Add the weapon to `WeaponPickupPool` weights if it should drop. Skip the default pistol.

**New level-up upgrade**

- Add a value to `UpgradeType`.
- Implement `Upgrade…` / `ApplyUpgrade` / `GetUpgradeLevel` on `Player_ExperienceAndStats`.
- Add an `Upgrade_Class` entry on `LevelUpPopup` (or it will not appear). Scene-serialized lists do not pick up new C# defaults; add the row in the Inspector or `EnsureUpgradeInPool` at runtime.
- Listeners that need the new stat should subscribe to `onPlayerStatsUpgraded` or `onTownStatsUpgraded`.

**New pickup that flies to the player**

- Copy `Base_Coin` / `WeaponPickup` (Rigidbody launch, closest-point magnet, blink, no auto-payout on timeout unless the design says so).
- Scale magnets from cached prefab size when `GetPickupRangeMultiplier()` changes.

**New turret**

- Duplicate `Turret.prefab` / follow `Building_Turret`. Keep the built-in zone.
- Fill + cost label live on the `PurchaseZone` child.

**New non-turret building (wall, door, extra pad)**

- Subclass `Base_PurchaseableBuilding` if you need extra OnLevelReached behaviour, or use the base + enable/disable lists.
- Create `Building_Purchaseable_Data` (own file). Entry 0 = first purchase.
- Place `PurchaseArea.prefab` **separate from the building**, assign `purchasable`.
- Player presence = closest-point, not trigger enter.
- To sequence pads: list later `PurchaseArea`s in `areasToUnlockOnPurchase`.
- If it blocks enemies, use a carving `NavMeshObstacle` (runtime NavMesh rebuild is not set up). `Building_Obstacle` already enables listed obstacles on first buy.

### Hard rules from bugs we already hit

- **Do not use `OnTriggerEnter` for player-overlap.** Player motion is kinematic/transform. Use `OverlapSphere` / `ClosestPoint`.
- **Pooled objects are inactive, not null.** Retargeting, weapons, and pools must check `activeInHierarchy` / `IsAlive`.
- **Never leave `Time.timeScale` at 0** when loading a scene. Pause and game-over both freeze time. Reset on resume, retry, and main menu.
- **Unsubscribe static events** in `OnDisable`/`OnDestroy`.
- **Enemy raycasts** must `QueryTriggerInteraction.Ignore` or coins / range spheres eat the shot.
- **Default weapon vs pickup:** leveling a stat must refresh **both**. Pickup create path must apply current `WeaponStatLevels`.
- **Special weapons need their own bullet prefab + script.** Empty `bulletPrefab` or leftover `Bullet_Standard` looks like “the gun does not shoot.”
- **Turrets stay on their own purchase zone.** Do not fold them into `PurchaseArea` unless asked.
- **Idle coins do not pay out.** Timeout despawns. `Collect()` is the explicit pull.
- **Editor-only code** goes under an `Editor` folder (`Assets/Scripts/Editor` or `…/WeaponSystem/Editor`).
- **Do not commit** unless the user asks. Do not touch git config.

### Where to look (quick)

| Task | Start here |
|---|---|
| Waves, coins, game over | `TownManager` |
| Enemy AI, death loot, stun | `Base_Enemy` |
| Spawn / difficulty / types | `EnemyPool`, `EnemyScaling` |
| Player shoot / swap gun | `PlayerWeaponSystem`, `Base_Weapon`, `TargetCollisionUpdater` |
| New projectile behavior | `Base_Bullet` subclass + matching data prefab |
| XP / level / upgrades | `Player_ExperienceAndStats`, `LevelUpPopup` |
| HUD numbers | `UIsTexts`, `ExperienceBar` |
| Turret buy/upgrade / shoot | `Building_Turret`, `Building_Turret_Data` |
| Non-turret buy/upgrade | `PurchaseArea`, `IPurchasable`, `Base_PurchaseableBuilding` |
| Buy-zone fill / remaining cost | `BuyZoneVisual`, `PurchaseCostLabel` |
| Coin drop / magnet | `CoinPool`, `Base_Coin` |
| Gun drop / magnet | `WeaponPickupPool`, `WeaponPickup` |
| Move speed | `PlayerController` |
| Town HP | `Building_Town` |

### Code style for this repo

- Serialize fields private with `[SerializeField]`.
- Tooltips on non-obvious numbers.
- `[Button]` on debug / preview methods (NaughtyAttributes).
- Comments only where the *why* is not obvious (stun vs destroy, closest-point vs trigger, pooled inactive, etc.).
- No empty catch blocks. Log with the component as context (`this`).
- Prefer `FindFirstObjectByType` over `FindObjectOfType`.
- Rigidbody velocity in Unity 6 is `linearVelocity`.

### Verification

There is no automated test suite. After gameplay changes, reason through: spawn → combat → death (pool return) → loot → HUD. For UI, remember the level-up popup sets `timeScale = 0`; bars that must move then should use `Time.unscaledDeltaTime`.

If you change weapon or bullet ScriptableObject scripts, open the asset in Unity and confirm the script reference is not `None` (`m_Script: {fileID: 0}`) and that `bulletPrefab` is assigned.

After a new weapon: pick it up in Play mode and confirm a projectile (or rail trail / grenade arc) actually appears, not only the ammo HUD changing.
