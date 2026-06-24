# Phase 4 — Polish: Walkthrough

**Goal:** Sendable demo. Minimal Canvas HUD (balance, day, customers, progress bar), end-of-day summary, 2.5D orthographic camera, and balance-tuning via ScriptableObject — all without recompiling.

---

## What's new in Phase 4

| File | Role |
|---|---|
| `HudModel.cs` | POCO: balance, day, active customers, day progress, summary data |
| `HudView.cs` | Canvas presenter with TMP labels + filled Image progress bar + summary overlay |
| `HudController.cs` | ControllerBase; subscribes to PaymentReceived/DayStarted/DayEnded/CustomerSpawned/CustomerLeft; polls DayProgress each frame |
| `CafeConfig.cs` | ScriptableObject: spawnIntervalSeconds, dayLengthSeconds, defaultPatienceSeconds |
| `CafeCamera.cs` | Orthographic 2.5D tilt — attaches to the Main Camera |
| `GameBootstrap.cs` | Now loads + applies CafeConfig, finds and binds HudController |

---

## Prerequisites

Before Phase 4 setup you must have:
- Phase 1 scene (customers + tables)
- Phase 2 scene (crafting station + OrderTicketSpawner + PlayerInputRouter)
- Phase 3 scene (tables with BoxCollider2D on the Tappable layer)
- TMP Essential Resources imported: **Window → TextMeshPro → Import TMP Essential Resources**

---

## Step 1 — Import TMP Essential Resources

If you haven't done this yet:
1. **Window → TextMeshPro → Import TMP Essential Resources**
2. Click **Import** in the dialog.
3. Unity creates `Assets/TextMesh Pro/` with the default font.

Required before running the Phase 4 HUD creator.

---

## Step 2 — Create CafeConfig Asset

```
Tools > DreamCafé > Create CafeConfig Asset
```

This creates `Assets/_Game/Data/CafeConfig.asset`.

**Key fields (select the asset to tune):**

| Field | Default | Description |
|---|---|---|
| `spawnIntervalSeconds` | 8 | Seconds between customer spawn attempts |
| `dayLengthSeconds` | 180 | Real-time seconds per in-game day (3 min) |
| `defaultPatienceSeconds` | 0 | 0 = use per-CustomerType defaults |
| `eatDurationSeconds` | 5 | How long a customer eats after being served |

---

## Step 3 — Create the HUD Canvas

```
Tools > DreamCafé > Create Phase 4 HUD
```

Creates the following hierarchy:

```
HUD  [Canvas, CanvasScaler, GraphicRaycaster, HudController, HudView]
 ├─ TopBar  [Image bg]
 │   ├─ TxtBalance   — "0đ"       (left)
 │   ├─ TxtDay       — "Day 1"    (center)
 │   └─ TxtCustomers — "0/10"     (right)
 ├─ DayProgressBG  [Image dark bg]
 │   └─ DayProgressFill  [Image, type=Filled, Horizontal]
 └─ SummaryOverlay  [Image dark bg, disabled]
     └─ Content  [panel]
         ├─ TxtTitle
         ├─ TxtRevenue
         ├─ TxtServed
         ├─ TxtLost
         └─ BtnNextDay  [Button]
```

All TMP labels and the progress Image are **automatically wired** to HudView's serialized fields.

---

## Step 4 — Finish Scene Setup

```
Tools > DreamCafé > Setup Phase 4 Scene
```

This:
- Adds `CafeCamera` component to the Main Camera (orthographic, 15° tilt, size 6).
- Wires `CafeConfig.asset` to the `GameBootstrap.cafeConfig` field.

---

## Step 5 — Verify Inspector

**Select `HUD` in the Hierarchy:**
- HudController: no extra refs needed (resolves via ServiceContext).
- HudView: all 9 labels/images should be wired (check in Inspector).

**Select `Main Camera`:**
- `CafeCamera` component present
- Orthographic Size: 6, Tilt: 15°, Vertical Offset: 1.5

**Select `GameBootstrap`:**
- `Cafe Config` field → `CafeConfig (CafeConfig)`

---

## Step 6 — Full Demo Run

1. Press **Play**.
2. Console init log order:
   ```
   [GameBootstrap] CafeConfig applied — spawn: 8s, day: 180s.
   [GameBootstrap] ✓ Startup complete.
   ```
3. **HUD visible**: `0đ | Day 1 | 0/10` in the top bar. Blue progress bar begins empty.
4. Wait ~8s → customer spawns. `0/10 → 1/10`.
5. Wait ~3s → customer orders → ticket appears.
6. **Click crafting station** → ticket turns green.
7. **Click the table** → customer starts eating → `PaymentReceived`.
8. Balance label updates: e.g. `2,500đ`.
9. Customer eats ~5s → leaves → `0/10`.
10. At 180s → **SummaryOverlay appears**:
    ```
    Day 1 Done!
    Revenue  2,500đ
    Served   1
    Lost     0
    [Next Day ▶]
    ```
11. Click **Next Day ▶** → Day 2 starts, summary hides, new customers spawn.

---

## Tuning Without Recompile

Select `Assets/_Game/Data/CafeConfig.asset` in the Project panel:

- **Faster testing**: set `dayLengthSeconds = 30` for a 30-second day.
- **More customers**: lower `spawnIntervalSeconds` to `4`.
- **Patient VIPs**: raise `defaultPatienceSeconds` to `60`.

Changes take effect next Play session (or next day if changed at runtime via the GameBootstrap apply step).

---

## Camera 2.5D Tuning

Select the **Main Camera** in the Hierarchy → CafeCamera component:

| Field | Suggested | Effect |
|---|---|---|
| `orthographicSize` | 5–8 | Zoom in/out |
| `tiltDegrees` | 10–25 | Isometric feel strength |
| `verticalOffset` | 1–3 | Camera height — raises scene |

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| "TMP default font not found" | TMP Essential Resources not imported | Window → TextMeshPro → Import TMP Essential Resources |
| HUD labels all show "0đ" + "Day 0" | HudController.Bind not called | Check GameBootstrap has the HUD binding; ensure HUD is in the scene before Play |
| Progress bar never fills | CafeConfig not wired / dayLengthSeconds too high | Re-run Setup Phase 4 Scene; confirm CafeConfig field on GameBootstrap |
| Summary never appears | DayEnded event not fired (TimeService not started) | Confirm GameBootstrap calls `_timeService.StartDay()` on Awake |
| Camera looks flat (no 2.5D) | CafeCamera not on Main Camera | Re-run Setup Phase 4 Scene |
| NullReferenceException on HudView | TMP labels not wired | Re-run "Create Phase 4 HUD" on a fresh scene or wire manually in HudView Inspector |

---

## Prototype Complete ✓

All five phases are done. The core loop runs end-to-end:

```
Spawn → Take order → Craft (tap station) → Serve (tap table)
  → Eat → Leave → PaymentReceived → HUD balance updates
  → Day ends → Summary → Next Day
```

**Next steps for a shippable version (beyond this prototype):**
- Staff AI (waiter automatically serves when order is ready)
- Inventory / ingredient system
- Recipe discovery + upgrade tree
- Decor + theme system
- DOTween animations on customers + tickets
- Audio with real SFX/music
- Save system
