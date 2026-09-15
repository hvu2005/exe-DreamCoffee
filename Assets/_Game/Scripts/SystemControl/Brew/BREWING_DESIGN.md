# Thiết kế hệ pha chế — đề xuất để thẩm định

> Trạng thái: **bản đề xuất, chưa code gì.** Đọc xong chốt giúp mình phần "Câu hỏi cần bạn quyết"
> ở cuối, mình mới bắt tay làm.
>
> Nguyên tắc xuyên suốt: **không thêm asset mới, không thêm trường dữ liệu mới nếu tránh được,
> không thêm scene/prefab mới.** Mọi đề xuất dưới đây đều chỉ sửa trong `SystemControl/Brew/`
> và tái dùng dữ liệu đã có sẵn trong `RecipeItem` / `InventoryItem`.

---

## 1. Hiện tại đang là gì

Vòng lặp hiện có:

```
chọn tối đa 3 nguyên liệu  →  rót nước (≥100ml)  →  BREW
   → khớp PairValue  → ra món (mở khoá nếu lần đầu)
   → không khớp     → mất nguyên liệu, báo "not a drink"
```

Vấn đề về mặt "vui":

| Vấn đề | Vì sao |
|---|---|
| **Mò là mò mù** | Sai chỉ nhận đúng một chữ "không phải đồ uống". Không học được gì từ lần thất bại. |
| **Không gian mò quá lớn so với phần thưởng** | 6 nguyên liệu × tối đa 3 ô = **83 tổ hợp** (41 nếu cấm trùng). Chỉ có **2 công thức đang khoá** để tìm. Trung bình vét cạn ~40 lần thử cho 1 khám phá → chán trước khi trúng. |
| **Nước chỉ là cái công tắc** | Thanh trượt 0–400ml nhìn như một cơ chế, nhưng thực chất chỉ là điều kiện "≥100ml". Người chơi kéo 1 lần rồi quên. |
| **BREW không có nhịp** | Bấm cái ra kết quả ngay. `CraftTimeSeconds` trong database **đang bị bỏ không**. |
| **Thắng và thua giống nhau về cảm giác** | Pha đúng món đã biết cũng chỉ hiện một dòng chữ. |

Điểm mấu chốt: **hệ hiện tại thiếu phản hồi, không thiếu cơ chế.** Nên hướng sửa là làm giàu
phản hồi trên đúng vòng lặp đang có, chứ không phải chồng thêm cơ chế mới.

---

## 2. Ba đề xuất lõi (xếp theo "vui / công sức")

### A. Phản hồi "gần đúng" kiểu Mastermind ⭐ đề xuất mạnh nhất

Khi pha sai, thay vì báo trống không thì cho biết **mức độ gần**:

- `Sắp được rồi! Có công thức chỉ khác cốc này 1 nguyên liệu.`
- `Có công thức dùng 2 trong số này, nhưng còn thiếu thứ khác.`
- `Không có công thức nào dùng tổ hợp kiểu này.` (khi giao nhau = 0)

Cách tính: duyệt các công thức **chưa mở khoá**, lấy số nguyên liệu giao nhau lớn nhất và
chênh lệch số ô. Chỉ báo *mức độ*, **không lộ tên món, không lộ nguyên liệu nào**.

- Sửa: thêm một hàm ~25 dòng trong `BrewingController`, trả về trong `BrewResult`.
- Dữ liệu mới: **không có.**
- Vì sao đáng làm: biến 83 tổ hợp mò mù thành một bài suy luận có tiến triển. Mỗi lần sai
  vẫn *tiến lên*, nên mất nguyên liệu không còn cay cú.

### B. Gợi ý lộ dần theo số lần thử ⭐ nên làm cùng A

Bảng Hint hiện dump thẳng cả `Description`. Đổi thành lộ dần theo số lần người chơi đã thử hụt:

| Số lần thử hụt | Bảng Hint hiện |
|---|---|
| 0 | Mô tả mơ hồ (`Description` sẵn có) — *"Something thick and creamy."* |
| 2 | Thêm số ô: *"Cần 3 nguyên liệu."* |
| 4 | Thêm nhóm: *"Có một thứ thuộc nhóm Fresh."* (lấy từ `ItemCategory`) |
| 6 | Lộ thẳng 1 nguyên liệu: *"Chắc chắn có Sữa."* |

- Sửa: một `Dictionary<string,int>` đếm lần hụt + hàm chọn câu chữ (~40 dòng).
- Dữ liệu mới: **không có** (dùng `RequiredIngredientIds` + `ItemCategory` đã có).
- Vì sao đáng làm: đảm bảo **không ai bị kẹt vĩnh viễn**, mà người giỏi vẫn được thưởng vì
  đoán ra sớm. Đây là cái van an toàn cho độ khó.

### C. Mực nước quyết định *chất lượng*, không quyết định *món*

Giữ nguyên: nước không ảnh hưởng việc ra món nào. Nhưng mỗi công thức có một **vùng nước lý tưởng**
suy ra từ số nguyên liệu — `lý tưởng = 100ml × số nguyên liệu`, sai số ±40ml:

| Lệch so với lý tưởng | Kết quả |
|---|---|
| trong ±40ml | **Perfect** — tiền ×1.25, +1 danh tiếng |
| ±40–100ml | Bình thường — tiền ×1.0 |
| xa hơn | **Loãng / quá đặc** — tiền ×0.6 |

- Sửa: ~15 dòng trong `BrewingController.Brew()` + đổi màu/chữ ở panel.
- Dữ liệu mới: **không có** (công thức suy ra từ `RequiredIngredientIds.Count`).
  Nếu sau này muốn chỉnh tay từng món thì thêm 1 trường `_idealWaterMl` là xong, không phá gì.
- Vì sao đáng làm: cho thanh trượt đang có một lý do tồn tại, và tạo *trần kỹ năng* — pha đúng
  món là sàn, pha Perfect mới là giỏi. Rẻ nhất trong ba cái về mặt code.

---

## 3. Hai thứ nhỏ nên kèm theo (mỗi cái < 30 dòng)

1. **Dùng `CraftTimeSeconds`** — bấm BREW thì cốc đầy dần trong 2.5–4 giây rồi mới ra kết quả.
   Một coroutine + `cupFill.fillAmount`. Tạo nhịp hồi hộp, và tận dụng trường dữ liệu đang bỏ không.
   *Nhớ khoá nút BREW trong lúc chạy.*

2. **Nguyên liệu trùng nhau (sữa + sữa) hiện luôn sai** — vì `PairValue` không gộp trùng.
   Chốt một trong hai:
   - **(rẻ)** Cấm bỏ trùng, báo "đã có trong cốc rồi" → thu không gian mò còn 41 tổ hợp, dễ thở hơn.
   - **(để dành)** Cho trùng = "double shot", món mạnh hơn — *đừng làm bây giờ*, cần thêm dữ liệu.

---

## 4. Cố tình KHÔNG làm (chốt để khỏi phình)

| Thứ bị loại | Lý do |
|---|---|
| Minigame canh nhịp / thanh timing | Cần animation, feel, tuning riêng. Không hợp game quản lý quán. |
| Nhiệt độ, độ xay, áp suất, sữa đánh bọt | Mỗi tham số nhân đôi ma trận cân bằng. Một tham số (nước) là đủ. |
| Kéo–thả nguyên liệu vào cốc | Bấm đã dùng được. Kéo–thả tốn UI + xử lý chạm mobile. |
| Số lượng riêng từng nguyên liệu (2 sữa, 1 cà phê) | Cần sửa cả `RecipeItem` lẫn cách so khớp. Để khi có "double shot". |
| Cây công thức / công thức mở ra công thức | Mới có 5 món, chưa đủ để dựng cây. |
| Nối thẳng vào hàng chờ khách | Nên làm **sau** khi pha chế đứng vững một mình. |
| Hệ chất lượng theo nguyên liệu (hạt loại A/B) | Nhân đôi số asset nguyên liệu. |

---

## 5. Đề xuất chốt

**Gói tối thiểu mà mình khuyến nghị: A + B + C + mục 3.1**

Tổng ước lượng: **~120 dòng code, sửa 2 file** (`BrewingController.cs`, `BrewingPanelView.cs`),
**0 asset mới, 0 trường dữ liệu mới, 0 thay đổi kiến trúc.** Không đụng tới `InventoryController`,
`RecipeController`, database hay scene.

Vòng lặp sau khi làm:

```
chọn nguyên liệu  →  canh mực nước  →  BREW  →  cốc đầy dần (2.5–4s)
   → đúng món + nước chuẩn  → "PERFECT! Cà Phê Sữa"  tiền ×1.25
   → đúng món, nước lệch    → "Hơi loãng"            tiền ×0.6
   → sai, nhưng gần         → "Chỉ sai 1 nguyên liệu!"  + bảng Hint lộ thêm
   → sai hẳn                → "Không giống công thức nào"
```

Người chơi giờ có **ba thứ để giỏi lên**: nhớ công thức, suy ra công thức mới từ phản hồi gần đúng,
và canh nước. Cả ba đều dạy được trong 30 giây.

### Nếu muốn cắt bớt nữa
Làm **A trước, một mình nó** (~25 dòng). Đó là thay đổi có tỉ lệ vui/công sức cao nhất trong cả
tài liệu này. B và C thêm vào sau vẫn được, không phải viết lại gì.

---

## 6. Câu hỏi cần bạn quyết

1. **Chọn gói nào?** Chỉ A · A+B · A+B+C · cả gói tối thiểu (A+B+C+hẹn giờ BREW).
2. **Pha sai có mất nguyên liệu không?** Hiện đang mất (`ConsumeOnFailure = true`). Có A rồi thì
   mất đồ mới "đáng", nhưng nếu muốn dễ thở thì mình đổi thành *chỉ mất khi giao nhau = 0*.
3. **Nguyên liệu trùng**: cấm hẳn (đề xuất) hay để nguyên trạng thái luôn-sai?
4. **Perfect thưởng gì?** Tiền ×1.25, hay thưởng danh tiếng (`CurrencyType.Reputation`) cho hợp
   với hệ mở khoá khách hàng đang có?
5. **Còn 5 công thức là hơi ít để mò.** Có muốn mình sinh thêm 3–4 công thức khoá sẵn bằng
   `DemoDataGenerator` không (chỉ là asset dữ liệu, không tốn code)?
