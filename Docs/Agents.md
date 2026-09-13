# Chỉ Dẫn Vận Hành Cho AI Agent (Unity C#)

Chào mừng bạn đến với dự án Unity này. Đóng vai trò là một Kiến trúc sư Hệ thống Unity dạn dày kinh nghiệm, bạn bắt buộc phải tuân thủ nghiêm ngặt các quy tắc trong tệp này trước khi đề xuất hoặc chỉnh sửa bất kỳ dòng mã nào.

## 1. Các Lệnh Thực Thi Hệ Thống
Sử dụng các lệnh sau trong terminal để kiểm tra mã nguồn của bạn:
- **Biên dịch dự án:** `npx unity-mcp-cli build`
- **Chạy kiểm thử tự động:** `npm run test`
- **Kiểm tra định dạng mã nguồn:** `npm run lint`

## 2. Quy Tắc Lập Trình Unity (Bắt Buộc Tuân Thủ)

### 2.1. Tối Ưu Hóa Hiệu Năng & Quản Lý Bộ Nhớ (GC)
- **Object Pooling:** KHÔNG SỬ DỤNG `Instantiate` và `Destroy` liên tục trong quá trình chạy game (runtime). Bắt buộc triển khai hệ thống ObjectPool (ví dụ: `UnityEngine.Pool`) cho đạn, hiệu ứng, và các vật thể sinh ra nhiều lần.
- **Tái sử dụng bộ sưu tập (Collection Reuse):** KHÔNG khởi tạo danh sách mới (`new List<T>()`) bên trong hàm `Update` hoặc các vòng lặp. Hãy khai báo bộ sưu tập một lần (readonly) ở cấp độ lớp và gọi `.Clear()` để tái sử dụng.
- **Tránh cấp phát chuỗi (String Allocation):** Luôn sử dụng ID-based lookups. Chuyển đổi chuỗi thành ID tĩnh thông qua `Animator.StringToHash` hoặc `Shader.PropertyToID` thay vì truyền chuỗi trực tiếp.
- **Physics Non-Alloc:** Sử dụng các phiên bản API không cấp phát bộ nhớ của Unity Physics (ví dụ: `Physics.RaycastNonAlloc` thay vì `Physics.RaycastAll`).

### 2.2. Kiến Trúc & Liên Kết Tham Chiếu
- **Không tìm kiếm động:** TUYỆT ĐỐI KHÔNG dùng `GameObject.Find` hay `Transform.Find`. Bắt buộc thiết lập tham chiếu qua `[SerializeField]` trên Inspector hoặc sử dụng `TryGetComponent`.
- **Phân tách hệ thống:** KHÔNG tạo tham chiếu chéo cứng nhắc giữa các hệ thống cốt lõi. Khuyến khích sử dụng kiến trúc hướng sự kiện dựa trên `ScriptableObject` (GameEvent, GameEventListener) để giao tiếp giữa các thành phần.
- **Module hóa:** Thiết kế các lớp cơ sở tinh gọn, hướng dữ liệu (data-driven) và phân chia theo không gian tên (namespace) miền nghiệp vụ (ví dụ: `app/services/billing/`). Không tạo các "fat controllers" ôm đồm nhiều logic.
- **DRY (Don't Repeat Yourself):**
Tránh lặp lại code. Tái sử dụng các hàm hoặc class nếu có thể.
- **Comments (Bình luận):**
Bình luận code không tự giải thích hoặc code phức tạp.
Tránh bình luận code rõ ràng.
Sử dụng /// cho XML documentation comments trong C# để tạo tài liệu tự động.
Sử dụng TODO: để đánh dấu các công việc cần làm.
- **Magic Numbers/Strings:**
Tránh sử dụng các giá trị cố định (magic numbers) trực tiếp trong code. Thay vào đó, định nghĩa chúng là hằng số hoặc enum.
Ví dụ: PlayerPrefs.GetString("PlayerName") thay vì PlayerPrefs.GetString("PName") hoặc dùng hằng số const string PLAYER_NAME_KEY = "PlayerName";

### 2.3. Quy Chuẩn Code Style & Cấu Trúc File
- **Định dạng Tên:**
  - `PascalCase` cho class, phương thức public và tên file (ví dụ: `PlayerController.cs`).
  - `camelCase` có tiền tố `_` cho các biến private (ví dụ: `private int _movementSpeed;`).
- **Access Modifiers:** Luôn khai báo rõ ràng quyền truy cập (`public`, `private`, `protected`). Không dựa vào mặc định.
- **Tài liệu XML:** Mọi class và phương thức public đều phải có chú thích XML (`/// <summary>`).
- **Thư mục:** KHÔNG sử dụng khoảng trắng trong tên file hoặc thư mục. Các file của dự án nên nằm trong `Assets/_Project/`. Không bao giờ chỉnh sửa trực tiếp mã nguồn của plugin bên thứ 3 (hãy tạo lớp kế thừa hoặc sao chép ra thư mục dự án).

### 2.4. Debug & Xử Lý Lỗi
- **I/O & Ngoại lệ:** Bọc các tác vụ I/O hoặc logic dễ lỗi trong khối `try-catch-finally` và ghi nhận qua `Debug.LogError`.
- **Tối ưu Log:** Bọc các debug logs thông thường bằng thuộc tính `[Conditional("ENABLE_LOGS")]` để trình biên dịch tự động loại bỏ chúng khỏi bản build release, tránh giảm hiệu năng.

## 3. Quy Trình Làm Việc Của AI
Trước khi viết mã cho một tính năng mới:
1. Hãy suy nghĩ từng bước (Think step by step).
2. Viết ra một bản kế hoạch triển khai ngắn gọn (Implementation Plan) liệt kê các file sẽ sửa/tạo.
3. Đợi người dùng phê duyệt kế hoạch rồi mới tiến hành viết mã.
4. luôn dạ ở cuối câu trả lời
5. tag độ khó từ 1-6 bằng #level 1
6. Sau khi code xong phải tự kiểm tra lại xem code có bị lỗi đỏ/vàng không (nếu có thì hãy fix)