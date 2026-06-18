# DreamCafé — Phase 0 Setup Walkthrough

> Thực hiện theo thứ tự. Mỗi bước đánh dấu ✅ khi xong.

---

## Bước 1 — Cài DOTween (thực hiện trước Phase 1, bỏ qua cho Phase 0)

DOTween chưa dùng trong Phase 0. Cài khi bắt đầu Phase 1:

1. Vào **Unity Asset Store** → tìm **"DOTween (HOTween v2)"** → Add to My Assets → Import.
2. Sau khi import: **Tools > Demigiant > DOTween Utility Panel** → **"Setup DOTween..."** → **Apply**.

> Phase 0 không cần DOTween — có thể bỏ qua bước này hoàn toàn.

---

## Bước 2 — Tạo Demo Assets (1 click)

1. Sau khi dự án compile xong (không còn lỗi đỏ trong Console): **Tools > DreamCafé > Create Demo Assets**.
2. Script sẽ tạo trong `Assets/_Game/Data/`:
   ```
   Items/
     item_caphe_den.asset
     item_tra_sua.asset
     item_banh_mi.asset
   Recipes/
     recipe_caphe_den.asset
     recipe_tra_sua.asset
     recipe_banh_mi.asset
     RecipeRepository.asset  ← cần gán thủ công (xem bước 3)
   Customers/
     cus_sinh_vien.asset
     cus_dan_van_phong.asset
     cus_du_khach.asset
     cus_vip.asset
   ```

---

## Bước 3 — Gán Recipe vào RecipeRepository

1. Click **`Assets/_Game/Data/Recipes/RecipeRepository.asset`** trong Project window.
2. Trong Inspector: **Recipes** array → Size = 3.
3. Kéo 3 recipe assets vào 3 slot:
   - Element 0: `recipe_caphe_den`
   - Element 1: `recipe_tra_sua`
   - Element 2: `recipe_banh_mi`
4. Nhấn **Ctrl+S** để save.

---

## Bước 4 — Tạo Main Scene

### Option A — Dùng menu (nhanh nhất)
**Tools > DreamCafé > Create Main Scene** → scene được tạo tại `Assets/_Game/Scenes/Main.unity`.

### Option B — Tạo thủ công
1. **File > New Scene** → chọn template **Basic (Built-in)** hoặc **Empty**.
2. Lưu tại `Assets/_Game/Scenes/Main.unity` (`Ctrl+Shift+S`).

---

## Bước 5 — Thiết lập GameBootstrap GameObject

> Bỏ qua nếu đã dùng **Create Main Scene** từ menu — GameBootstrap đã được tạo tự động.

1. Trong Hierarchy: **Right-click > Create Empty** → đặt tên **`GameBootstrap`**.
2. Với GameObject `GameBootstrap` được chọn, trong Inspector: **Add Component > GameBootstrap** (tìm bằng search).
3. **Pool Root**: để trống — tự tạo `[PoolRoot]` child lúc runtime. Hoặc tạo thêm Empty child tên `[PoolRoot]` và kéo vào slot này.

### Hierarchy tối thiểu cho Phase 0:
```
Main Scene
├── Main Camera
├── Directional Light
└── GameBootstrap          [GameBootstrap.cs]
    └── [PoolRoot]         (optional — tự tạo nếu để trống)
```

---

## Bước 6 — Verify Phase 0

1. Nhấn **Play**.
2. Mở **Console** (`Ctrl+Shift+C`).
3. Bạn sẽ thấy log theo thứ tự này:

```
[CompositionRoot] Infrastructure created.
[CompositionRoot] ServiceContext built.
[TimeService] Initialized. Day length: 180s.
[EconomyService] Initialized. Starting balance: 500,000đ
[CustomerService] Initialized. Spawn interval: 8s.
[OrderService] Initialized.
[CraftingService] Initialized.
[StaffService] Initialized (stub).
[InventoryService] Initialized (stub — infinite stock).
[DiscoveryService] Initialized (stub).
[ReputationService] Initialized (stub).
[SoundService] Initialized (stub).
[AnalyticsService] Initialized (stub).
[SaveService] Initialized (stub).
[ServiceManager] InitAll complete — 12 services.
[PoolManager] Prewarm skipped — prefab not found: 'Prefabs/CustomerPrefab'
[PoolManager] Prewarm skipped — prefab not found: 'Prefabs/OrderTicketPrefab'
[Analytics] DayStarted
[TimeService] Day 1 started.
[GameBootstrap] ✓ Startup complete.
```

> ⚠️ Hai dòng "Prewarm skipped" là **bình thường** — prefab sẽ tạo ở Phase 1. Đây không phải lỗi.

4. Nhấn **Stop** → Console hiện:
```
[SaveService] Shutdown.
...
[TimeService] Shutdown.
[ServiceManager] ShutdownAll complete.
[CompositionRoot] Disposed.
[GameBootstrap] Shutdown complete.
```

**Phase 0 hoàn thành khi bạn thấy đủ 2 khối log trên.**

---

## Tuning nhanh (không cần recompile)

| Muốn thay đổi | Mở file |
|---|---|
| Thời gian 1 ngày | `TimeService.cs` → `DayLengthSeconds = 180f` (hoặc expose qua Inspector SO sau) |
| Balance ban đầu | `EconomyService.cs` → `Balance = 500_000f` |
| Khoảng cách spawn khách | `CustomerService.cs` → `SpawnIntervalSeconds = 8f` |
| Giá món | `Assets/_Game/Data/Items/*.asset` → `basePrice` |
| Patience của khách | `Assets/_Game/Data/Customers/*.asset` → `patienceSeconds` |

---

## Sơ đồ kiến trúc (Phase 0)

```
GameBootstrap (MonoBehaviour, scene)
│
└── CompositionRoot (plain C#, owned by Bootstrap)
    ├── EventBus          ← tất cả sự kiện đi qua đây
    ├── PoolManager       ← object pool cho Customer, OrderTicket
    └── ServiceManager
        ├── TimeService        → DayStarted / DayEnded events
        ├── EconomyService     ← PaymentReceived
        ├── CustomerService    → CustomerSpawned / CustomerLeft
        ├── OrderService       → OrderPlaced / OrderServed / PaymentReceived
        ├── CraftingService    → ItemCrafted
        └── Stubs × 7         (Staff, Inventory, Discovery, Reputation, Sound, Analytics, Save)
```

---

## Tiếp theo — Phase 1

Khi Phase 0 verify xong → implement Phase 1 (Customer Flow):
- Tạo `CustomerPrefab` (primitive quad) tại `Assets/_Game/Resources/Prefabs/`.
- Add `CustomerController`, `CustomerView` components.
- Bật spawn logic trong `CustomerService.Tick()`.
- Test: khách spawn → ngồi bàn → patience drain → bỏ đi.
