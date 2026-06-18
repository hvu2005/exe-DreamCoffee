# DreamCafé — Phase 1 Walkthrough (Customer Flow)

> Thực hiện theo thứ tự. Mỗi bước đánh dấu ✅ khi xong.

---

## Bước 0 — Điều kiện tiên quyết

- [ ] Phase 0 đã verify xong (xem `Phase0-Walkthrough.md`).
- [ ] Console không có lỗi đỏ.
- [ ] (Khuyến nghị) Cài DOTween từ Asset Store trước khi làm Phase 1:
  **Window > Asset Store** → tìm **"DOTween (HOTween v2)"** → Add to My Assets → Import.
  Sau khi import: **Tools > Demigiant > DOTween Utility Panel** → **"Setup DOTween..."** → Apply.
  > Phase 1 không dùng DOTween trực tiếp nhưng cài sớm tránh phải restart Unity sau.

---

## Bước 1 — Tạo Demo Assets (cập nhật để thêm CustomerRoster)

**Tools > DreamCafé > Create Demo Assets**

Script sẽ tạo thêm:
```
Assets/_Game/Resources/
  CustomerRoster.asset    ← CustomerService dùng để pick ngẫu nhiên khách
```

4 CustomerData đã được gán tự động vào CustomerRoster. Kiểm tra:
1. Click **`Assets/_Game/Resources/CustomerRoster.asset`** trong Project window.
2. Inspector → **Customers** array → Size = 4, 4 slot có dữ liệu.

---

## Bước 2 — Tạo CustomerPrefab

### 2.1 Cấu trúc prefab

Trong **Hierarchy**, tạo cấu trúc sau (tạm thời trong scene, sẽ kéo thành Prefab sau):

```
CustomerPrefab                  [CustomerController] [CustomerView]
├── Body                        [SpriteRenderer]  (Sprite = "Knob" hoặc "Square")
└── PatienceBar                 [GameObject, dùng làm root của thanh máu]
    ├── BG                      [SpriteRenderer, màu xám, scale X=1]
    └── Fill                    [SpriteRenderer, màu xanh ban đầu]
```

### 2.2 Các bước tạo từng phần

#### Root object `CustomerPrefab`:
1. Hierarchy → **Right-click > Create Empty** → đặt tên `CustomerPrefab`.
2. **Add Component > CustomerController** (tìm bằng search).
3. **Add Component > CustomerView** (tìm bằng search).

#### Child `Body`:
1. Right-click `CustomerPrefab` → **2D Object > Sprite** → đặt tên `Body`.
2. Inspector → **Sprite Renderer**:
   - **Sprite**: chọn bất kỳ sprite mặc định (Assets/Sprites hoặc Unity built-in "UISprite").
   - **Color**: để mặc định (tintColor từ CustomerData sẽ ghi đè lúc runtime).
3. **Scale**: `(0.5, 1, 1)` — hình chữ nhật đứng trông như người.

#### Child `PatienceBar`:
1. Right-click `CustomerPrefab` → **Create Empty** → đặt tên `PatienceBar`.
2. Position: `(0, 0.8, 0)` — hiện trên đầu nhân vật.

#### Child `PatienceBar/BG`:
1. Right-click `PatienceBar` → **2D Object > Sprite** → đặt tên `BG`.
2. Sprite Renderer → **Sprite**: chọn "UISprite" hoặc bất kỳ sprite hình chữ nhật.
3. **Color**: `(0.3, 0.3, 0.3, 1)` — xám tối.
4. **Scale**: `(1, 0.1, 1)`.

#### Child `PatienceBar/Fill`:
1. Right-click `PatienceBar` → **2D Object > Sprite** → đặt tên `Fill`.
2. Sprite Renderer → **Sprite**: cùng sprite với BG.
3. **Color**: `(0.2, 0.85, 0.2, 1)` — xanh lá (sẽ đổi màu theo patience).
4. **Scale**: `(1, 0.1, 1)` — giống BG, Scale X sẽ thu nhỏ dần lúc runtime.

### 2.3 Gán references vào CustomerView

Chọn `CustomerPrefab` root → Inspector → **Customer View** component:
- **Body**: kéo child `Body` vào slot.
- **Patience Bar Root**: kéo child `PatienceBar` vào slot.
- **Patience Bar Fill**: kéo child `PatienceBar/Fill` vào slot.
- **Patience Bar Fill Renderer**: kéo child `PatienceBar/Fill` vào slot.

### 2.4 Lưu thành Prefab

1. Trong Project window, tạo thư mục (nếu chưa có): `Assets/_Game/Resources/Prefabs/`.
2. Kéo `CustomerPrefab` từ Hierarchy vào `Assets/_Game/Resources/Prefabs/`.
3. Đặt tên chính xác: **`CustomerPrefab`** (khớp với `PoolKey.Customer = "Prefabs/CustomerPrefab"`).
4. Xoá instance trong scene (click > Delete).

---

## Bước 3 — Thiết lập Tables trong scene

Phase 1 cần ít nhất 1 table. Tạo 4 tables cho một quán nhỏ:

### 3.1 Tạo 1 Table (lặp lại cho mỗi bàn)

1. Hierarchy → **Right-click > Create Empty** → đặt tên `Table_0`.
2. **Add Component > TableController** (tìm bằng search).
3. Inspector → **Table Controller**:
   - **Table Index**: `0` (Table_1 = 1, Table_2 = 2, Table_3 = 3).
   - **Seat Transform**: để trống (tự dùng Transform của chính nó).
4. Position: đặt ở vị trí bàn trên màn hình. Gợi ý:
   ```
   Table_0: (-3, -1, 0)
   Table_1: (-1, -1, 0)
   Table_2: ( 1, -1, 0)
   Table_3: ( 3, -1, 0)
   ```

### 3.2 Hierarchy sau khi tạo Tables

```
Main Scene
├── Main Camera
├── Directional Light
├── GameBootstrap          [GameBootstrap.cs]
│   └── [PoolRoot]
├── SpawnPoint             (Empty GameObject — xem bước 4)
├── Table_0                [TableController]
├── Table_1                [TableController]
├── Table_2                [TableController]
└── Table_3                [TableController]
```

---

## Bước 4 — Thiết lập SpawnPoint và GameBootstrap

### 4.1 Tạo SpawnPoint

1. Hierarchy → **Right-click > Create Empty** → đặt tên `SpawnPoint`.
2. Position: `(0, 3, 0)` — phía trên màn hình, khách sẽ "hiện ra" ở đây rồi đi xuống bàn.

### 4.2 Gán vào GameBootstrap

1. Chọn **GameBootstrap** trong Hierarchy.
2. Inspector → **Game Bootstrap**:
   - **Pool Root**: để trống (tự tạo) hoặc kéo `[PoolRoot]` child vào.
   - **Customer Spawn Point**: kéo `SpawnPoint` vào slot.

---

## Bước 5 — Verify Phase 1

1. Nhấn **Play**.
2. Mở **Console** (`Ctrl+Shift+C`).

### 5.1 Log khởi động (bổ sung sau Phase 0)

Bạn sẽ thấy thêm:
```
[PoolManager] Prewarmed 10x 'Prefabs/CustomerPrefab'
[GameBootstrap] ✓ Startup complete.
```

> Nếu vẫn thấy `"Prewarm skipped"` → kiểm tra tên Prefab và đường dẫn (phải là `Assets/_Game/Resources/Prefabs/CustomerPrefab.prefab`).

### 5.2 Log sau ~8 giây

Sau 8 giây (SpawnIntervalSeconds = 8), bạn sẽ thấy:
```
[Analytics] CustomerSpawned { customerId=cus_1, type=Student, isTakeaway=False }
```

### 5.3 Kiểm tra visual

- Một hình chữ nhật màu (tintColor của CustomerData) xuất hiện ở vị trí SpawnPoint.
- Sau đó di chuyển đến vị trí bàn đầu tiên.
- Thanh patience màu xanh hiện phía trên đầu, thu nhỏ dần theo thời gian.

### 5.4 Sau ~25–60 giây (patience cạn kiệt)

```
[Analytics] CustomerLeft { customerId=cus_1, wasSatisfied=False, emotion=Angry }
```

- Nhân vật biến mất (despawn về pool).
- Thanh patience tắt.

### 5.5 Kiểm tra pool reuse

Sau khi khách đầu tiên rời đi, khách thứ hai spawn sau ~8s — Unity không log "Prewarmed" hay "Instantiate", nghĩa là pool đang được tái sử dụng.

---

## Tuning nhanh (không cần recompile)

| Muốn thay đổi | Nơi thay đổi |
|---|---|
| Tốc độ spawn khách | `CustomerService.cs` → `SpawnIntervalSeconds = 8f` |
| Patience của từng loại khách | `Assets/_Game/Data/Customers/*.asset` → `patienceSeconds` |
| Màu sắc nhân vật | `Assets/_Game/Data/Customers/*.asset` → `tintColor` |
| Số bàn | Thêm/bớt Table_X GameObject trong scene |
| Vị trí hàng chờ takeaway | `CustomerService.cs` → `TakeawayQueueSpacing` |

---

## Sơ đồ luồng Phase 1

```
[GameBootstrap.Update]
    │
    ├─ TimeService.Tick(dt)   ← đếm ngày
    └─ CustomerService.Tick(dt)
           │
           ├─ mỗi 8s: TrySpawnCustomer()
           │     ├─ Load CustomerRoster (Resources, cached)
           │     ├─ Random pick CustomerData
           │     ├─ Pool.Spawn<ControllerBase>(PoolKey.Customer)
           │     │     └─ GetComponent<ICustomerSpawnable>()
           │     ├─ spawnable.Initialize(ctx, data, isTakeaway, id)
           │     │     └─ FSM.Enter(WaitingForSeatState)
           │     ├─ FindFreeTable() → table.Occupy(id)
           │     └─ spawnable.AssignSeat(pos, tableIndex)
           │           └─ FSM.Enter(SeatedState)
           │
           └─ [Per-frame trong CustomerController.Update()]
                 └─ FSM.Tick(dt)
                       ├─ SeatedState: TickPatience, View.Render, View.UpdatePatienceBar
                       └─ khi Patience01 ≤ 0: FSM.Enter(LeavingState)
                             ├─ Publish(CustomerLeft)
                             ├─ CustomerService.OnCustomerDespawning → table.Vacate()
                             └─ Pool.Despawn → OnDespawned → Unbind → reset
```

---

## Tiếp theo — Phase 2

Khi Phase 1 verify xong → implement Phase 2 (Order + Craft):
- Tạo `OrderTicketPrefab` — hiện thị món đã chọn trên bàn.
- `CraftingStationPrefab` — điểm pha chế.
- Tap mechanic: player tap vào station → instant craft → `ItemCrafted` event.
- `OrderService` tạo và track orders, `OrderPlaced` / `OrderServed` events.
