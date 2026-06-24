# Phase 5 — Animations, Sound & Ticket Labels: Walkthrough

**Goal:** Make the demo feel alive. Customers pop in/out with scale tweens, order tickets show the item name, and SFX play on serve/craft events via a configurable ScriptableObject.

---

## What changed in Phase 5

| File | Change |
|---|---|
| `CustomerView.cs` | `PlaySpawnAnim()` — scale 0→1 over 0.2s; `PlayLeaveAnim(callback)` — scale 1→0 over 0.15s then despawns |
| `CustomerController.cs` | Calls `View.PlaySpawnAnim()` in `Initialize()` |
| `LeavingState.cs` | Despawn is now deferred: `View.PlayLeaveAnim(() → Pool.Despawn(...))` |
| `SoundConfig.cs` | New SO: `SoundEntry[]` (key + AudioClip + volume) at `Resources/SoundConfig.asset` |
| `SoundService.cs` | Upgraded: creates dedicated `AudioSource` GameObject; `Play(key)` calls `PlayOneShot` |
| `OrderTicketModel.cs` | Added `string ItemName` |
| `OrderTicketView.cs` | Added `TextMeshPro itemLabel` (world-space); renders `ItemName` or falls back to `ItemId` |
| `OrderTicketController.cs` | Resolves `IRecipeRepository.GetRecipe(itemId)?.outputItem?.displayName` in `Initialize()` |

---

## Step 1 — Add Ticket Labels to OrderTicketPrefab

```
Tools > DreamCafé > Setup Phase 5 — Ticket Labels
```

This edits `OrderTicketPrefab` in-place:
- Adds a `TextMeshPro` (world-space) child named `ItemLabel` at local position `(0, 0.18, -0.05)`.
- Font size 2, dark color, overflow = Ellipsis, sorting order 6 (above the yellow/green square).
- Wires the component to `OrderTicketView.itemLabel`.

**Idempotent** — safe to run again if it finds the label already present.

---

## Step 2 — Create SoundConfig Asset

```
Tools > DreamCafé > Create Sound Config Asset
```

Creates `Assets/_Game/Resources/SoundConfig.asset` and selects it in the Project panel.

**In the Inspector, add entries:**

| Key | Suggested sound | Notes |
|---|---|---|
| `serve_ding` | short "ding" or coin sound | Fires when player taps table to serve |
| `craft_complete` | click / pop | Fires when player taps crafting station |
| `customer_arrive` | door bell or step | Fires on `CustomerSpawned` (optional — not wired by default) |
| `customer_leave` | chair scrape | Fires on `CustomerLeft` (optional) |

**Leave `clip = null` for any unused entry** — the SoundService falls back to console log silently.

---

## Step 3 — Wire Optional Sound Events in GameBootstrap

`serve_ding` and `craft_complete` are already wired in `GameBootstrap.Awake`:

```csharp
ctx.Events.Subscribe<OrderServed>(evt => sound.Play("serve_ding"));
ctx.Events.Subscribe<ItemCrafted>(_ => sound.Play("craft_complete"));
```

To add `customer_arrive` / `customer_leave`:
1. Open `GameBootstrap.cs`.
2. Inside the "Wire sound hooks" block, add:

```csharp
ctx.Events.Subscribe<CustomerSpawned>(_ => sound.Play("customer_arrive"));
ctx.Events.Subscribe<CustomerLeft>(_ => sound.Play("customer_leave"));
```

---

## Step 4 — Verify ItemData Display Names

Ticket labels come from `ItemData.displayName`. Check your recipe assets:

1. In the Project panel, find `Assets/_Game/Data/` → your `RecipeData` assets.
2. Select each recipe → check `Output Item` → click through to its `ItemData`.
3. Make sure `Display Name` is filled in (e.g., "Cà phê đen", "Trà sữa", "Bánh mì").
4. If `displayName` is empty, the ticket shows `itemId` as a fallback.

---

## Step 5 — Play Test

1. Save scene (**Ctrl+S**).
2. Press **Play**.
3. **Spawn animation**: when a customer appears, it scales from 0 → 1 over 0.2s (pop-in effect).
4. **Order ticket**: when an order is placed the ticket square now shows the item name in dark text.
5. **Craft**: click the crafting station → ticket turns green + `craft_complete` SFX plays (if clip assigned).
6. **Serve**: click the table → `serve_ding` SFX plays, customer starts eating.
7. **Leave animation**: customer scales from 1 → 0 over 0.15s before disappearing (shrink-out effect).

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Ticket label invisible | `itemLabel` not wired | Re-run Setup Phase 5 — Ticket Labels; or manually drag TMP child to `OrderTicketView.itemLabel` |
| Ticket shows `itemId` not name | `ItemData.displayName` is empty | Fill in `displayName` on each ItemData asset |
| No SFX | `SoundConfig` not at `Resources/SoundConfig.asset` | Run Create Sound Config Asset; check Resources/ folder |
| SFX logs instead of playing | Clip is `null` in SoundConfig | Drag AudioClips into the SoundConfig entries |
| Customer pops instantly (no animation) | `CustomerView.PlaySpawnAnim` not called | Check `CustomerController.Initialize` line order — `PlaySpawnAnim()` must be after `View.Render(Model)` |
| Customer disappears before despawn anim | Old `Pool.Despawn` call still in LeavingState | Confirm LeavingState calls `View.PlayLeaveAnim(callback)` not direct `Pool.Despawn` |
| NullReferenceException in PlayLeaveAnim | Customer despawned mid-coroutine | Harmless — pool reuse stops the coroutine via `StopAllCoroutines` on next spawn |

---

## Demo Polish Checklist

- [ ] All `ItemData.displayName` fields filled (Vietnamese or English)
- [ ] `SoundConfig.asset` has at least `serve_ding` + `craft_complete` clips
- [ ] Spawn pop-in visible (customers don't teleport in)
- [ ] Despawn shrink visible (customers don't snap away)
- [ ] Ticket label shows item name (not `itemId` code)
- [ ] HUD balance ticks up on each serve
- [ ] End-of-day summary shows correct revenue/served/lost
- [ ] "Next Day ▶" button starts Day 2

If all of the above are working, the prototype is **demo-ready** for sharing.
