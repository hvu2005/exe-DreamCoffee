# DreamCafé — Phase 2 Walkthrough (Order + Craft)

> Thực hiện theo thứ tự. Mỗi bước đánh dấu ✅ khi xong.

---

## Điều kiện tiên quyết

- [ ] Phase 1 đã verify xong (khách spawn, ngồi bàn, patience drain, bỏ đi).
- [ ] `Resources/CustomerRoster.asset` đã có 4 khách.
- [ ] `Resources/Prefabs/CustomerPrefab.prefab` đã tồn tại.
- [ ] Console không có lỗi đỏ.

---

## Bước 1 — Tạo Phase 2 Prefabs

**Tools > DreamCafé > Create Phase 2 Prefabs**

Tạo:
```
Assets/_Game/Resources/Prefabs/
  OrderTicketPrefab.prefab   ← yellow square → green khi crafted
Assets/_Game/Prefabs/
  CraftingStationPrefab.prefab  ← beige square + status dot + BoxCollider2D
```

> Yêu cầu Phase 1 sprites (sq_center.png, circle.png) đã tồn tại trong `Art/Sprites/`.
> Nếu chưa có: chạy **Create Phase 1 Prefabs** trước.

---

## Bước 2 — Setup Scene

**Tools > DreamCafé > Setup Phase 2 Scene**

Tự động thêm vào scene:
- `CraftingStation_0` tại `(0, 2, 0)` — giữa màn hình, phía trên tables
- `OrderTicketSpawner` tại `(0, 3.5, 0)` — tickets hiện ra từ đây sang phải
- `PlayerInputRouter` — không có position (logic only)

### Hierarchy sau Phase 2 Setup:
```
Main Scene
├── Main Camera
├── Directional Light
├── GameBootstrap          [GameBootstrap.cs]
│   └── [PoolRoot]
├── SpawnPoint
├── Table_0..3             [TableController]
├── CraftingStation_0      [CraftingStationController] [CraftingStationView] [BoxCollider2D]
├── OrderTicketSpawner     [OrderTicketSpawner]
│   └── TicketAnchor
└── PlayerInputRouter      [PlayerInputRouter]
```

---

## Bước 3 — Gán RecipeRepository vào Services

`OrderingState` cần `IRecipeRepository` để pick món cho khách. Kiểm tra ServiceInstaller đã đăng ký chưa:

1. Mở **`Assets/_Game/Scripts/Bootstrap/ServiceInstaller.cs`**.
2. Tìm dòng đăng ký `CraftingService` — xác nhận `IRecipeRepository` / `ScriptableRecipeRepository` cũng được đăng ký.

> Nếu chưa, thêm dòng sau vào `ServiceInstaller.Install()`:
> ```csharp
> services.Register<IRecipeRepository>(new ScriptableRecipeRepository(recipeRepo));
> ```
> Xem chi tiết bên dưới.

### Kiểm tra ServiceInstaller

Mở `Assets/_Game/Scripts/Bootstrap/ServiceInstaller.cs`. Cần có:
- `IRecipeRepository` được đăng ký với `ScriptableRecipeRepository` đã load asset.

---

## Bước 4 — Verify Phase 2

1. Nhấn **Play**.
2. Mở **Console** (`Ctrl+Shift+C`).

### 4.1 Log thứ tự xảy ra

```
# t = 0s: Startup
[GameBootstrap] ✓ Startup complete.

# t = 8s: Khách 1 spawn
[Analytics] CustomerSpawned { customerId=cus_1, type=Worker, isTakeaway=False }

# t = 11s: Sau 3s ngồi, auto-order
[OrderService] Placed: order_1 — item_caphe_den (25,000đ)
[Analytics] OrderPlaced { orderId=order_1, customerId=cus_1, itemId=item_caphe_den }

# → OrderTicketPrefab spawn ở vị trí TicketAnchor, màu vàng
```

### 4.2 Test tap crafting station

- **Click chuột trái** vào hình chữ nhật beige (CraftingStation_0).
- Console hiện:
```
[CraftingService] Crafted 'item_caphe_den' at 'station_0' → order order_1
[OrderService] Crafted: order_1
[Analytics] ItemCrafted { itemId=item_caphe_den, stationId=station_0, orderId=order_1 }
```
- OrderTicket đổi màu từ **vàng → xanh lá**.
- Status dot trên CraftingStation đổi màu **xám → xanh lá**.
- Customer (còn ở bàn) nhận event → chuyển sang **EatingState**.
- Sau 5 giây: customer bỏ đi, `CustomerLeft { wasSatisfied=True }`.

### 4.3 Nếu click không có reaction

Kiểm tra:
- `CraftingStation_0` có component **BoxCollider2D** không?
- **PlayerInputRouter** có tồn tại trong scene không?
- Camera layer mask của `PlayerInputRouter` có include layer của station không?
  - Mở PlayerInputRouter → Inspector → **Tappable Layer**: set All nếu cần.

---

## Tuning nhanh

| Muốn thay đổi | Nơi thay đổi |
|---|---|
| Thời gian khách chờ trước khi order | `CustomerModel.AutoOrderDelay = 3f` |
| Thời gian ăn | `CustomerModel.EatDuration = 5f` |
| Khoảng cách giữa tickets | `OrderTicketSpawner` → Inspector → **Ticket Spacing** |
| Số crafting stations | Thêm station mới từ `CraftingStationPrefab`, đặt stationId khác nhau |

---

## Sơ đồ luồng Phase 2

```
[Khách ngồi bàn — SeatedState]
  └─ AutoOrderTimer -= dt
     └─ khi timer ≤ 0 → OrderingState.Enter()
          ├─ RecipeRepository.GetUnlockedRecipes() → random pick
          ├─ OrderService.PlaceOrder(customerId, itemId, price) → orderId
          ├─ Publish(OrderPlaced)
          │    └─ OrderTicketSpawner: Spawn OrderTicketPrefab (màu vàng)
          └─ FSM → WaitingForOrderState
               ├─ patience drains
               └─ Subscribe(ItemCrafted): if orderId match → EatingState

[Player click chuột]
  └─ PlayerInputRouter.OnTapPerformed()
       └─ Physics2D.OverlapPoint → CraftingStationController
            └─ ITappable.OnTap()
                 └─ CraftingService.TryCraft("station_0")
                      ├─ OrderService.TryGetOldestPending() → order_1
                      ├─ OrderService.MarkCrafted("order_1")
                      ├─ Publish(ItemCrafted)
                      │    ├─ OrderTicketController: đổi màu vàng → xanh
                      │    ├─ CraftingStationController: status dot → xanh
                      │    └─ WaitingForOrderState: orderId match → EatingState
                      └─ return true

[EatingState — 5s]
  └─ EatTimer depletes → LeavingState
       └─ Publish(CustomerLeft { wasSatisfied=True })
```

---

## Tiếp theo — Phase 3 (Serve + Economy)

Khi Phase 2 verify xong → Phase 3:
- Player tap table (ITappable trên bàn) khi station ở trạng thái Ready → `OrderService.ServeOrder(orderId)`.
- `PaymentReceived` → `EconomyService.Balance` tăng.
- `ITipStrategy` dùng patience còn lại để tính tip.
- `TimeService` day cycle: 3 phút → DayEnded → summary.
