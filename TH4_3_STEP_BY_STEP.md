# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 4.3 (TH 4.3) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 4.3: Khớp nối ngã ba chữ T Lệch Phải Mirrored (Hệ giàn 2 có mặt trong phía ngược TH 3.3)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 4.3

```
                    Y (+)
                     ▲
                     │ (Vách trái - MẶT NGOÀI)        (Vách phải - MẶT TRONG)
                     │ (xL, yTop)                     (xR, yTop)
                     │     ┌──────────────────────────────┐ (Đầu hồi đỉnh nhánh B)
                     │     │          HỘP ĐỎ              │
                     │     │         Nhánh rẽ B           │
                     │     │       (L_branch x W)         │
                     │     │  GIẰNG CHÉO (Family 5)       │ LAN CAN KÉP (Family 4: h6, h7)
                     │     │ (Mặt ngoài - Quay ra ngoài)  │ GIẰNG GIỮA (Family 4: h3)
                     │     │                              │ (Mặt trong - Hướng công trình)
                     │ (xL, y0)                           │ (xR, y0)
     ────────────────┼──────────[CỘT CẤY]─────────────[CỘT CHUNG]──► X (+)
     (x0, y0)        │             │◄──────── W ────────►│ (Đường xanh ngang)
        ┌────────────┴─────────────┼─────────────────────┤────────────┐
        │        HỘP ĐEN 1         │      HỘP ĐEN 2      │ HỘP ĐEN 3  │
        │       (Nhịp trước)       │    (Nhịp ngã ba)    │ (Nhịp sau) │
        │         1800 x W         │       1800 x W      │  1800 x W  │
        │                          │◄600►│◄─── 1200 ────►│            │
        └──────────────────────────┴─────┴───────────────┴────────────┘
     (x0, y0 - W)             (xStart, y0-W)          (xR, y0-W)   (xEnd, y0-W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba (mặc định: `1800, 1800, 1800`).
- `L2Bays`: Danh sách nhịp của nhánh rẽ chữ T (mặc định: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ & Nguyên Lý Kết Cấu Mirrored:
1. **Vị trí nhịp ngã ba của dãy chính**:
   Nhịp ngã ba là Nhịp 2 ($X \in [x_{\text{start}}, x_{\text{end}}]$) dài 1800 mm.
   - $x_R = x_{\text{end}}$ (Trùng khớp 100% với cột hiện hữu của dãy chính $\implies$ **CỘT DÙNG CHUNG**).
   - $x_L = x_R - W = x_{\text{end}} - 1200 = x_{\text{start}} + 600\text{ mm}$ ($\implies$ **CỘT CẤY PHỤ**).
2. **Khoảng hở $600\text{ mm}$ phía trước dãy chính ($X \in [x_{\text{start}}, x_L]$)**:
   - Giằng chân $h_1$ Family 4 ($L = 600\text{ mm}$).
   - Lan can an toàn kép Family 4 tại $h_6, h_7$, giằng giữa $h_3$ và ván chắn biên Family 7 ($L = 600\text{ mm}$).
3. **Cửa ngõ thông suốt $1200\text{ mm}$ ($X \in [x_L, x_R]$ tại $Y = y_0$)**:
   - Mở hoàn toàn $100\%$, không có ván lót hay lan can cản trở.
   - Giằng chân $h_1$ Family 4 ($L = 1200\text{ mm}$) và giằng ngang đỡ mâm `trDoorDeck` tại $zDeck$.
4. **Quy Tắc Bất Biến Mirrored**:
   - **Mặt ngoài (Outer Face)**:
     - Dãy chính: Mặt sau ($Y = y_0 - W$).
     - Nhánh rẽ B: **VÁCH TRÁI ($X = x_L$)** $\implies$ **Bố trí Giằng chéo Family 5** (qua `PlaceBayDiagonalBraces`), ván chắn biên Family 7.
   - **Mặt trong (Inner Face)**:
     - Dãy chính: Mặt trước ($Y = y_0$).
     - Nhánh rẽ B: **VÁCH PHẢI ($X = x_R$)** $\implies$ **Bố trí Giằng dọc Family 4** (lan can bảo vệ kép $h_6, h_7$, giằng giữa $h_3$).
   - **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn với lan can kép $h_6, h_7$, toeboard và giằng đỉnh tại `topZ` (chỉ 1 lần, triệt tiêu đè trùng).

---

## 2. Mã Nguồn C# Thực Thi Chuẩn Hóa

```csharp
// Trích xuất từ ScaffoldingBuilder.Cases4.cs: ExecuteCase4_3
// Xem chi tiết trong file mã nguồn c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool\ScaffoldingBuilder.Cases4.cs
```
