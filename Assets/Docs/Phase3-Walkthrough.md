# Phase 3 — Serve + Economy: Walkthrough

**Goal:** Complete the core loop. Player taps a table to deliver a crafted item → OrderServed event → customer eats → payment collected → EconomyService balance increases.

---

## What changed in Phase 3

| File | Change |
|---|---|
| `OrderData` | Added `CustomerType` field for tip calculation |
| `IOrderService` | `PlaceOrder` now takes `CustomerType`; added `TryGetReadyOrderForCustomer` |
| `OrderService` | `ServeOrder` uses `BaseTipStrategy` + publishes `OrderServed` + `PaymentReceived` |
| `IEconomyService` | Added `DayRevenue` property |
| `EconomyService` | Tracks `DayRevenue`; resets on `DayStarted` |
| `ICustomerService` | Added `DayCustomersServed` / `DayCustomersLost` |
| `CustomerService` | Tracks served/lost on `CustomerLeft`; resets on `DayStarted` |
| `OrderingState` | Passes `owner.Model.Type` (CustomerType) to `PlaceOrder` |
| `WaitingForOrderState` | Subscribes `OrderServed` (not `ItemCrafted`) — player must tap table |
| `TimeService` | Stores `_ctx`; `PublishDayEnded` lazy-resolves Economy + Customer for summary |
| **`TableController`** | Implements `ITappable`; `OnTap()` → `TryGetReadyOrderForCustomer` → `ServeOrder` |

---

## Step 1 — Add the "Tappable" Physics Layer

Phase 3 serving uses the same tap system as Phase 2. Tables need a BoxCollider2D on the **Tappable** layer so `Physics2D.OverlapPoint` can detect them.

1. Open **Edit → Project Settings → Tags & Layers**.
2. Under **User Layers**, find an empty slot (e.g. Layer 7 or 8).
3. Type **`Tappable`** and press Enter.
4. Confirm the same layer is already set for your CraftingStation prefab (should be from Phase 2).

---

## Step 2 — Run the Phase 3 Scene Setup

```
Tools > DreamCafé > Setup Phase 3 Scene
```

This script:
- Finds all `TableController` GameObjects in the active scene.
- Adds a `BoxCollider2D` (1×1 size) to any table that doesn't have one.
- Sets those tables to the **Tappable** layer.

It is **idempotent** — safe to run again; existing colliders are skipped.

---

## Step 3 — Verify the Hierarchy

After setup your scene should look like:

```
GameBootstrap
CustomerSpawnPoint
  └─ [spawn marker]
Table_0  ← TableController + BoxCollider2D (layer: Tappable)
Table_1  ← TableController + BoxCollider2D (layer: Tappable)
CraftingStation_0  ← CraftingStationController + BoxCollider2D (layer: Tappable)
OrderTicketSpawner
PlayerInputRouter
```

**PlayerInputRouter Inspector:**
- `Game Camera` → drag your main Camera.
- `Tappable Layer` → select the **Tappable** layer mask.

---

## Step 4 — Verify TableController in Inspector

Select any Table object:

| Field | Value |
|---|---|
| `Table Index` | unique int (0, 1, 2…) |
| `Seat Transform` | leave empty to default to self-transform |
| BoxCollider2D size | `(1, 1)` |
| Layer | `Tappable` |

---

## Step 5 — Full Loop Test (Play Mode)

1. Press **Play**.
2. Console shows ordered service init:
   ```
   [CustomerService] Initialized. Spawn interval: 8s.
   [TimeService] Initialized. Day length: 180s.
   ```
3. Wait ~8 seconds → customer spawns, walks to an empty table.
4. Wait ~3 seconds → customer auto-orders → `OrderPlaced` → ticket appears near crafting station.
5. **Click the crafting station** → ticket turns green (`ItemCrafted`).
6. **Click the table where the customer is seated** → `OrderServed` fires.
   - Customer transitions to **Eating** (patience bar disappears).
   - Console: `[OrderService] ServeOrder: ord_1 served, tip = …`
7. After ~5 seconds the customer eats and leaves → `CustomerLeft(wasSatisfied: true)`.
8. Console: `[EconomyService] PaymentReceived: base=…, tip=…, balance=…`

---

## Step 6 — Event Flow Reference

```
PlayerInputRouter.OnTap (table tapped)
  └─ TableController.OnTap()
       └─ IOrderService.TryGetReadyOrderForCustomer(customerId)
            └─ OrderService.ServeOrder(orderId)
                 ├─ BaseTipStrategy.ComputeTip(basePrice, 0.5f patience, CustomerType)
                 ├─ Publish: OrderServed(orderId, customerId)
                 │    └─ WaitingForOrderState → EatingState
                 │    └─ OrderTicketController.OnOrderServed → Despawn
                 └─ Publish: PaymentReceived(customerId, baseAmount, tip)
                      └─ EconomyService.OnPaymentReceived → Balance += total
```

---

## Step 7 — Day Cycle Test

`TimeService.DayLengthSeconds` defaults to **180 seconds** (3 minutes). To test quickly:

1. Select **GameBootstrap** in hierarchy.
2. In the Inspector, find the serialized `TimeService` reference (or add a test method).
3. Alternatively: set `DayLengthSeconds = 30f` in `TimeService.Init` temporarily.
4. After 30 seconds:
   ```
   [TimeService] Day 1 ended — 1200đ, 3 served, 1 lost.
   [DayEnded] dayNumber=1, revenue=1200, served=3, lost=1
   ```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Tapping table does nothing | Collider missing or wrong layer | Re-run Setup Phase 3 Scene; check `PlayerInputRouter.tappableLayer` |
| Customer stuck in WaitingForOrder | Order not marked Ready | Tap crafting station first; check `IOrderService.TryGetOldestPending` |
| No PaymentReceived log | `OrderService.ServeOrder` — order status not Ready | Confirm `MarkCrafted` fired: look for `ItemCrafted` log after tapping station |
| `NullReferenceException` in OnTap | `TableController.Ctx` null | Make sure `GameBootstrap.BindSceneControllers` runs before first tap |
| DayEnded revenue is 0 | `TryResolve<IEconomyService>` failed | Verify `EconomyService` is registered in `ServiceInstaller` |

---

## Next — Phase 4 (Polish)

- Minimal HUD: money counter, day timer, customer count (`HudView` subscribing `PaymentReceived` / `DayStarted` / `DayEnded`).
- End-of-day summary overlay (revenue, served vs. lost).
- 2.5D camera tilt (orthographic + slight pitch).
- Balance tuning via ScriptableObjects (spawn rate, patience, prices).
