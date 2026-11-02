---
name: nmk-scaffolding
description: >-
  Kỹ năng chuyên gia phát triển, bảo trì và tự động hóa hệ thống giàn giáo Revit Scaffolding (NMKScaffolding).
  Kế thừa 100% kiến trúc phần mềm Add-in Revit 2022 (WPF/MVVM, Partial Classes, ToolWindowHost), tiêu chuẩn giàn giáo
  nêm Nhật Bản (ALINCO Rosette Table), ma trận 6 trường hợp góc nối liên tục (Corner Cases 1.1 - 2.2 & Master),
  nguyên tắc cô lập trường hợp độc lập, quy trình kiểm soát chất lượng không lỗi và cơ chế đồng bộ SSOT Markdown (1800x1200.md).
---

# Skill: NMK Scaffolding Automation & Engineering Lead

Kỹ năng này đóng vai trò là **Bản đồ Tri thức Toàn diện & Tiêu chuẩn Kỹ thuật (SSOT)** cho dự án **NMKScaffolding**. Bất kỳ phiên chat mới hoặc máy tính mới nào khi kích hoạt kỹ năng này đều kế thừa trọn vẹn toàn bộ ngữ cảnh, nguyên tắc thiết kế, cấu trúc mã nguồn, quy chuẩn ALINCO và lịch sử sửa lỗi đã tích lũy.

---

## 1. Thông Tin Dự Án & Kho Lưu Trữ (Repository)

- **Git Remote (GitHub)**: `https://github.com/nmkhanh/NMKScaffolding.git` (Nhánh chính: `master`).
- **File SSOT Kỹ thuật Dự án**:
  - Cục bộ: `1800x1200.md` tại thư mục gốc của repository.
  - Google Drive: `H:\My Drive\Family\New folder\Child\1800x1200.md`.
- **Target Framework**: .NET Framework 4.8 / Revit API 2022.
- **Thư mục Output**: `bin/RevitScaffoldingTool.dll`.
- **Script Build Chuẩn**: `powershell -ExecutionPolicy Bypass -File build_tool.ps1` (Luôn yêu cầu đạt **0 Warning, 0 Error**).

---

## 2. Kiến Trúc Mã Nguồn (Clean Architecture / Partial Class)

Dự án áp dụng mô hình WPF / MVVM hiện đại với kỹ thuật chia tách `partial class`:

| Đường dẫn tệp | Trách nhiệm chính |
| :--- | :--- |
| `RevitScaffoldingTool/Views/ScaffoldingWindow.xaml` | Giao diện 2 cột: Cột trái (Thông số kỹ thuật, cấu hình cột 1-5, số tầng, giằng đỉnh, Pick Face, From Data checkbox); Cột phải (Danh sách các nút bấm thi công). |
| `RevitScaffoldingTool/ViewModels/ScaffoldingViewModel.cs` | ViewModel kế thừa `ObservableObject`, xử lý `RelayCommand`, quản lý trạng thái, lưu và khôi phục cài đặt qua `Properties.Settings`. |
| `RevitScaffoldingTool/Commands/ScaffoldingCommand.cs` | Điểm vào `IExternalCommand`, tích hợp `ToolWindowHost` ghim MainWindow của Revit và cơ chế `AppDomain.AssemblyResolve` nạp DLL phụ thuộc. |
| `RevitScaffoldingTool/ScaffoldingBuilder.cs` | Module lõi: Điều phối `ExecuteBuild`, cấu trúc `BuildOptions`, `BuildResult`, và các helper dựng lưới: `BuildMainColumnGrid`, `BuildStandardBayElements`, `PlaceConfiguredPosts`, `ConfigureAlincoColumn`. |
| `RevitScaffoldingTool/ScaffoldingBuilder.Cases1.cs` | Triển khai độc lập các trường hợp Khớp nối Góc: **TH 1.1**, **TH 1.2**, **TH 1.3**, **TH 1.4**. |
| `RevitScaffoldingTool/ScaffoldingBuilder.Cases2.cs` | Triển khai độc lập các trường hợp Vươn Ngoài & Đa nhịp: **TH 2.1**, **TH 2.2**, **MultiBay Straight**. |
| `RevitScaffoldingTool/ScaffoldingBuilder.Master.cs` | Triển khai **MASTER - 6 Continuous Corners**: Dựng liền mạch 6 góc liên tiếp trên cùng một mặt bằng. |

---

## 3. Tiêu Chuẩn Kỹ Thuật Giàn Giáo Nêm ALINCO (Rosette Specs)

Quy chuẩn kích thước và vị trí đĩa liên kết (Rosette) theo tiêu chuẩn ALINCO Nhật Bản:

### Bảng cao độ đĩa cùm giàn giáo:
- **Cột tiêu chuẩn 2700 mm (7 đĩa)**:
  - $h_1 = 135\text{ mm}$ (Giằng chân ngang & dọc).
  - $h_2 = 380\text{ mm}$ (Điểm chân giằng chéo tầng đáy).
  - $h_3 = 830\text{ mm}$ (Giằng dọc trung gian).
  - $h_4 = 1280\text{ mm}$ (Điểm đỉnh giằng chéo tầng đáy).
  - $h_5 = 1730\text{ mm}$ (Đỡ mâm sàn thao tác & điểm chân giằng chéo tầng trên).
  - $h_6 = 2180\text{ mm}$ (Lan can an toàn dưới).
  - $h_7 = 2630\text{ mm}$ (Lan can an toàn trên & điểm đỉnh giằng chéo tầng trên).
  - $h_8 \dots h_{15} = 0\text{ mm}$.
- **Cột 1800 mm (4 đĩa)**: $h_1 = 380, h_2 = 830, h_3 = 1280, h_4 = 1730\text{ mm}$ (Không có đĩa 135 ở đáy).
- **Cột 3600 mm (8 đĩa)**: $h_1 = 380, h_2 = 830, \dots, h_8 = 3530\text{ mm}$ (Không có đĩa 135 ở đáy).
- **Cột nối 450 mm**:
  - `450 nối giữa`: 1 đĩa tại $h_1 = 380\text{ mm}$.
  - `450 giằng chân`: 2 đĩa tại $h_1 = 135\text{ mm}, h_2 = 380\text{ mm}$.

### Hệ 7 Family Cốt Lõi:
1. `Family 1` (Ván lót đáy / Sole Plate): Đặt tại cao độ đáy $Z = z_0$, tham số `l`.
2. `Family 2` (Kích chân / Jack Base): Đặt tại $Z = z_0$, tham số `h` (chiều cao kích chân `jackH`).
3. `Family 3` (Cột chống đứng / Standard Post): Đặt tại $Z = z_0 + \text{jackH}$, tham số `h` và bảng đĩa `h1`...`h15`.
4. `Family 4` (Giằng ngang, giằng dọc, lan can an toàn): Đặt tại các cao độ đĩa $h_1, h_3, h_5, h_6, h_7, topZ$, tham số `l`.
5. `Family 5` (Giằng chéo / Diagonal Brace): Đặt tại $h_2$ (tầng đáy) hoặc $h_5$ (tầng sàn), tham số `l` (nhịp ngang) và `h = 900 mm` (chiều cao vươn giữa 2 đĩa).
6. `Family 6` (Mâm thao tác / Steel Plank): Đặt tại $h_5$, tham số `l` và `w = 500 mm`.
7. `Family 7` (Ván chắn biên / Edge Toeboard): Đặt tại $h_5$, offset mép biên 66 mm, tham số `l`.

---

## 4. Nguyên Tắc Cô Lập Bất Biến (Strict Case Isolation)

> [!CAUTION]
> **QUY TẮC SỐNG CÒN**: MỌI TRƯỜNG HỢP (TH 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, MASTER) PHẢI ĐƯỢC TÁCH BIỆT HOÀN TOÀN 100%.
> Tuyệt đối KHÔNG gộp chung logic, KHÔNG dùng biến dùng chung làm biến dạng trường hợp khác. Khi nhận yêu cầu sửa một trường hợp cụ thể, CHỈ ĐƯỢC PHÉP tác động bên trong hàm xử lý của trường hợp đó!

### Chi tiết các trường hợp quan trọng:

#### A. TH 1.2 (+X Thu ô vuông 1200x1200 hướng +Y) & TH 1.4 (+X Thu ô vuông 1200x1200 hướng -Y):
- Tại nhánh rẽ vuông $1200 \times 1200\text{ mm}$, **BẮT BUỘC ĐẦY ĐỦ 2 ĐẦU HỒI**:
  - **Đầu kết nối (Đầu nhánh)**:
    - Giằng chân đầu (`trRF1`): Family 4 tại $h_1$ ($Z = z_0 + \text{jackH} + 135$).
    - Đỡ mâm đầu (`trRD1`): Family 4 tại $h_5$ ($Z = z_0 + \text{jackH} + 1730 + s \times 1800$) để đỡ trọn 2 mâm sàn.
    - Giằng đỉnh đầu (`trTopRF1`): Family 4 tại `topZ` khi `HasTopTransom == true`.
  - **Đầu mút ngoài (Cuối nhánh)**:
    - Giằng chân cuối (`trRF2`), Đỡ mâm cuối (`trRD2`), Giằng đỉnh cuối (`trTopRF2`).

#### B. TH 2.1 (+Y Vươn ngoài):
- **Giằng chéo (Family 5)**: Đặt ở mép NGOÀI $X = 9000\text{ mm}$ ($L = 1800\text{ mm}$) và đầu hồi đáy $Y = -1200\text{ mm}$ ($L = 1200\text{ mm}$).
- **Giằng dọc (Family 4)**: Đặt ở vách TRONG giật cấp $X = 7800\text{ mm}$ ($L = 600\text{ mm}$) gồm $h_3, h_6, h_7$ và ván chắn biên.
- **Loại bỏ trùng lặp**: Tuyệt đối không đặt giằng dọc $h_3$ thừa tại đỉnh $Y = +600$ và giằng dọc $h_7$ thừa tại đáy $Y = -1200$.

#### C. TH 2.2 (+X Vươn ngoài -Y):
- **Giằng chéo (Family 5)**: Đặt ở vách TRONG giật cấp $X = 7800\text{ mm}$ ($L = 600\text{ mm}$).
- **Giằng dọc (Family 4)**: Đặt ở mép NGOÀI bên phải $X = 9000\text{ mm}$ ($L = 1800\text{ mm}$) và đầu hồi đáy $Y = -1800\text{ mm}$ ($L = 1200\text{ mm}$) gồm $h_3, h_6, h_7$, ván chắn biên.

---

## 5. Quy Trình Làm Việc Chuẩn (Ask - Planed - Run - Sync)

1. **Chế độ Tra cứu & Khảo sát (`ask`)**:
   - Luôn đọc file mã nguồn thực tế và kiểm tra tọa độ, đĩa cùm, kích thước nhịp trước khi phát biểu.
2. **Chế độ Lập kế hoạch (`planed`)**:
   - Nếu thay đổi hình học hoặc logic lớn, phác thảo tọa độ X, Y, Z, góc xoay rotation, chiều dài $L$ và checklist `[ ]` ra Markdown.
3. **Chế độ Thực thi (`run`)**:
   - Chỉnh sửa đúng vị trí cục bộ.
   - Chạy lệnh build xác minh: `powershell -ExecutionPolicy Bypass -File build_tool.ps1`. Đảm bảo 0 warning, 0 error.
   - Cập nhật checklist `[x]` và nhật ký kỹ thuật vào [1800x1200.md](file:///c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/1800x1200.md).
   - Đồng bộ sang Google Drive: `Copy-Item "1800x1200.md" "H:\My Drive\Family\New folder\Child\1800x1200.md" -Force`.
   - Git commit, tạo tag milestone kỹ thuật và đẩy lên remote: `git push origin master --tags`.

---

## 6. Kỷ Luật Thép: Chống Báo Cáo Khống (Anti-Stub) & Chuẩn Hóa Table Run Connect

1. **Tuyệt đối cấm hàm rỗng / Stub / Ủy thác giả**:
   - Nghiêm cấm tạo stub, hàm rỗng hoặc gọi ủy thác tạm bợ sang trường hợp khác rồi báo cáo "đã xong".
   - Mọi hàm thi công (đặc biệt là các hàm connect `ConnectCorner_TH1_1` $\to$ `ConnectCorner_TH2_2`) PHẢI có đầy đủ thân hàm và bố trí cấu kiện thực tế 100%.

2. **Báo cáo Siêu Ngắn Gọn (Tối đa 3 - 5 dòng)**:
   - Cắt bỏ hoàn toàn văn vẻ dông dài hoặc lý thuyết rườm rà.
   - Chỉ báo cáo 3 thông tin cốt lõi: (1) Tính năng đã làm xong cụ thể, (2) Kết quả build và số dòng code thật, (3) Kết quả kiểm tra Pass/Fail.

3. **Chuẩn hóa Connect Vector Động & Hỗ trợ Vòng Khép Kín (Closed Loop)**:
   - Lấy kích thước thật: Sử dụng nhịp cuối của Row trước (`L_last`), nhịp đầu của Row sau (`L_first`) và bề rộng thực tế (`W_prev`, `W_next`) thay vì fix cứng số liệu mặc định.
   - Giữ nguyên 100% logic cấu kiện chuẩn của 6 trường hợp (cột cấy, giằng cắt ngắn, ván chắn, lan can, mâm bù góc, giằng chéo).
   - Hỗ trợ vòng khép kín: Xử lý trường hợp Row cuối cùng có thể connect nối lại với Row đầu tiên (Closed Loop).
