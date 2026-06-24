# Phase 6 — Upgrade Shop: Walkthrough

**Goal:** Add a between-days upgrade system. After each day ends, an upgrade shop overlay appears where the player spends earned balance on 4 permanent upgrades, then starts the next day.

---

## What's new in Phase 6

| File | Role |
|---|---|
| `UpgradeData.cs` | SO for one upgrade: id, effect type, cost, maxLevel, effectValues per level |
| `UpgradeConfig.cs` | SO listing all available upgrades (loaded from Resources/) |
| `IUpgradeService.cs` | Interface: GetLevel, Purchase, IsMaxed, NextCost + 4 effect properties |
| `UpgradeService.cs` | Tracks levels, deducts from IEconomyService, publishes UpgradePurchased, re-applies overrides on DayStarted |
| `UpgradeShopView.cs` | Canvas overlay: summary label + 4 upgrade cards + "Start Day ▶" button |
| `UpgradeShopController.cs` | ControllerBase; subscribes DayEnded (show) + UpgradePurchased (refresh); OnStartDayClicked calls StartDay() |
| `GameEvents.cs` | Added `UpgradePurchased` event struct |
| `GameEnums.cs` | Added `UpgradePurchased` to EventType, added `UpgradeEffect` enum |
| `ServiceInstaller.cs` | Registers `IUpgradeService` |
| `OrderService.cs` | `ServeOrder` multiplies tip by `IUpgradeService.TipMultiplier` |
| `CustomerController.cs` | `Initialize` applies `IUpgradeService.PatienceMultiplier` as `Model.ReputationFactor` |
| `GameBootstrap.cs` | Finds and binds `UpgradeShopController` in `BindSceneControllers` |

---

## The 4 Upgrades

| Upgrade | Effect | Levels | Cost (L1 / L2 / L3) |
|---|---|---|---|
| **Rush Hour** | SpawnIntervalSeconds: 5s → 3s | 2 | 800đ / 1,200đ |
| **Extended Hours** | DayLengthSeconds: 240s → 300s | 2 | 1,000đ / 1,500đ |
| **Quality Roast** | Tip multiplier: 1.25x → 1.5x → 2.0x | 3 | 500đ / 800đ / 1,280đ |
| **Cozy Vibes** | Patience (ReputationFactor): 1.25x → 1.5x | 2 | 600đ / 900đ |

---

## Step 1 — Create Phase 6 Assets

```
Tools > DreamCafé > Create Phase 6 Assets
```

Creates:
- `Assets/_Game/Resources/UpgradeConfig.asset` — lists all 4 upgrades
- `Assets/_Game/Data/Upgrades/rush_hour.asset`
- `Assets/_Game/Data/Upgrades/long_day.asset`
- `Assets/_Game/Data/Upgrades/quality_roast.asset`
- `Assets/_Game/Data/Upgrades/cozy_vibes.asset`

All values are pre-configured. Select any `.asset` in the Project panel to tweak costs or effect values without recompiling.

---

## Step 2 — Setup Phase 6 Scene

```
Tools > DreamCafé > Setup Phase 6 Scene
```

Creates the **UpgradeShop Canvas** (sortingOrder = 20, above HUD sortingOrder = 10):

```
UpgradeShop  [Canvas, sort=20, UpgradeShopController, UpgradeShopView]
  ├─ Overlay              — dark semi-transparent full-screen bg
  └─ Panel                — centered content box
      ├─ SummaryLabel     — "Day 1 Complete! Revenue 2,500đ  |  Served 3  |  Lost 1"
      ├─ Card_rush_hour
      │   ├─ Name, Desc, Level, Cost, BuyBtn
      ├─ Card_long_day
      ├─ Card_quality_roast
      ├─ Card_cozy_vibes
      └─ BtnStartDay      — "Start Day ▶"
```

All TMP labels and buttons are **automatically wired** to UpgradeShopView's serialized fields.

The Canvas starts **disabled** — `UpgradeShopController.OnDayEnded` enables it.

---

## Step 3 — Play Test

1. Save scene (**Ctrl+S**) → Press **Play**.
2. Play normally: spawn customers, craft orders, serve them, earn balance.
3. After 3 minutes (or `dayLengthSeconds`): day ends → **UpgradeShop overlay appears**.
4. Shop shows day summary + 4 upgrade cards.
   - Cards with insufficient balance show Buy button as **greyed-out (non-interactable)**.
   - Maxed cards show **"MAX"** level and **"—"** cost.
5. Click **Buy** on an affordable upgrade → balance decreases, level updates immediately.
6. Click **Start Day ▶** → shop closes, Day 2 begins.
7. **Rush Hour** effect: customer spawn interval decreases (customers arrive faster).
8. **Quality Roast** effect: check console `[OrderService] Served: ... x1.25 mult` on next serve.

---

## Day Transition Flow

```
DayEnded
  → HudController.OnDayEnded    → shows HUD summary panel (behind shop)
  → UpgradeShopController.OnDayEnded → shows UpgradeShop (sortingOrder=20, on top)

Player buys upgrades → UpgradePurchased
  → UpgradeShopController.RefreshCards → re-renders cards (costs, interactability)
  → UpgradeService.ApplyOverrides      → applies SpawnInterval / DayLength to services

Player clicks "Start Day ▶"
  → UpgradeShopController.OnStartDayClicked
       → UpgradeShop.SetActive(false)
       → ITimeService.StartDay()
            → DayStarted published
                 → HudController.OnDayStarted → hides HUD summary
                 → UpgradeService.OnDayStarted → re-applies all overrides (belt+suspenders)
```

---

## Tuning Upgrades

Select any `Assets/_Game/Data/Upgrades/*.asset` in the Project panel:

| Field | Effect |
|---|---|
| `Base Cost` | Starting price for Level 1 |
| `Cost Multiplier` | Each subsequent level costs `baseCost × mult^currentLevel` |
| `Max Level` | How many times this upgrade can be purchased |
| `Effect Values` | One value per level — what the upgrade actually changes |

Example: `quality_roast` effectValues = [1.25, 1.5, 2.0] → at level 2 you get 1.5× tip multiplier.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Shop never appears | UpgradeShopController not bound | Check GameBootstrap finds it via FindObjectsByType; scene must have UpgradeShop Canvas before Play |
| All Buy buttons greyed out | Balance too low | Earn more on first day, or lower `baseCost` on UpgradeData assets |
| Level stays 0 after buying | UpgradeConfig not wired | Re-run Create Phase 6 Assets; check `config.upgrades` in UpgradeConfig Inspector |
| Spawn still slow after Rush Hour | Upgrade not applied | Check `[UpgradeService]` log: "Rush Hour → Level 1"; check `SpawnIntervalOverride` returns 5f |
| TipMultiplier not visible in log | OrderService not updated | Confirm `OrderService.ServeOrder` contains `tipMultiplier` variable and `TryResolve<IUpgradeService>` |
| Shop appears behind HUD | Canvas sortingOrder | UpgradeShop canvas sortingOrder must be > HUD canvas sortingOrder (20 vs 10) |
