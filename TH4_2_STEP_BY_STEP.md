# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 4.2 (TH 4.2) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 4.2: Khớp nối ngã ba chữ T Ô Vuông $W \times W = 1200 \times 1200$ mm Mirrored (Hệ giàn 2 có mặt trong phía ngược TH 3.2)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 4.2

```
                    Y (+)
                     ▲
                     │ (Vách trái - MẶT NGOÀI)        (Vách phải - MẶT TRONG)
                     │ (xJunc, yTop)                  (xR, yTop)
                     │     ┌──────────────────────────────┐ (Đầu hồi đỉnh nhánh B)
                     │     │          HỘP ĐỎ              │
                     │     │         Nhánh rẽ B           │
                     │     │       (L_branch x W)         │
                     │     │  GIẰNG CHÉO (Family 5)       │ LAN CAN KÉP (Family 4: h6, h7)
                     │     │ (Mặt ngoài - Quay ra ngoài)  │ GIẰNG GIỮA (Family 4: h3)
                     │     │                              │ (Mặt trong - Hướng công trình)
                     │ (xJunc, y0)                        │ (xR, y0)
     ────────────────┼─[Cột Chung]────────────────[Cột Chung]──────► X (+)
     (x0, y0)        │     │◄──────────── W ─────────────►│ (Đường xanh ngang)
        ┌────────────┴─────┼──────────────────────────────┤────────────┐
        │    HỘP ĐEN 1     │      Ô VUÔNG NGÃ BA          │ HỘP ĐEN 3  │
        │   (Nhịp trước)   │         HỘP ĐEN 2            │ (Nhịp sau) │
        │     L1 x W       │           W x W              │   L3 x W   │
        └──────────────────┴──────────────────────────────┴────────────┘
     (x0, y0 - W)      (xJunc, y0 - W)                (xR, y0 - W) (xEnd, y0 - W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba (trong đó nhịp ngã ba $L_2 = W = 1200$ mm).
- `L2Bays`: Danh sách nhịp của nhánh rẽ chữ T (mặc định `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ & Nguyên Lý Kết Cấu Mirrored:
1. **Ô vuông ngã ba $1200 \times 1200$ mm**:
   $$x_{\text{Junc}} = x_0 + L_1, \quad x_R = x_{\text{Junc}} + W = x_{\text{Junc}} + 1200\text{ mm}$$
2. **100% Cột Dùng Chung**:
   Cả 2 cột $(x_{\text{Junc}}, y_0)$ và $(x_R, y_0)$ đều là cột mép trước của ô vuông ngã ba, tận dụng đĩa rosette ALINCO 8 lỗ tiêu chuẩn, không cần cấy bất kỳ cột phụ nào.
3. **Cửa ngõ thông suốt $1200$ mm**:
   Đoạn $X \in [x_{\text{Junc}}, x_R]$ tại $Y = y_0$ mở trọn vẹn $1200$ mm.
   - **TUYỆT ĐỐI KHÔNG LẮP**: Ván lót đáy Family 1 qua cửa ngõ, lan can kép Family 4 và ván chắn biên Family 7.
   - Mâm sàn thao tác Family 6 tại $h_5$ ($Z = 1980$ mm) phẳng liên tục giữa ô vuông và nhánh rẽ qua thanh giằng ngang `trDoorDeck`.
4. **Quy Tắc Bất Biến Mirrored**:
   - **Mặt ngoài (Outer Face)**:
     - Dãy chính: Mặt sau ($Y = y_0 - W$).
     - Nhánh rẽ B: **VÁCH TRÁI ($X = x_{\text{Junc}}$)** $\implies$ **Bố trí Giằng chéo Family 5** (qua `PlaceBayDiagonalBraces`), ván chắn biên Family 7.
   - **Mặt trong (Inner Face)**:
     - Dãy chính: Mặt trước ($Y = y_0$).
     - Nhánh rẽ B: **VÁCH PHẢI ($X = x_R$)** $\implies$ **Bố trí Giằng dọc Family 4** (lan can kép $h_6, h_7$, giằng giữa $h_3$).
   - **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn với lan can kép $h_6, h_7$, toeboard và giằng đỉnh tại `topZ` (chỉ 1 lần, triệt tiêu đè trùng).
5. **Điều Kiện Quét Nhận Diện Tự Động Trong Revit**:
   - Dãy chính (Row A): Ô vuông $1200 \times 1200$ mm ($|L_k - W| \le 100$ mm).
   - Nhánh rẽ B (Row B): Đường DetailLine xanh nằm ở **VÁCH PHẢI ($X = x_R$)**.
   - Tọa độ chiếu $s$ thỏa mãn: $|(s - W) - C_k| \le 150$ mm và nhịp là ô vuông $\implies$ Tự động phát hiện **TH 4.2**.
   - Khi chạy Table Run, gốc Row B tự động tịnh tiến sang vách trái $ox = x_R - W \sin\theta$ để khớp nối 100% với ô vuông Row 0.

---

## 2. Mã Nguồn C# Thực Thi Chuẩn Hóa

```csharp
// Trích xuất từ ScaffoldingBuilder.Cases4.cs: ExecuteCase4_2
// Xem chi tiết trong file mã nguồn c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool\ScaffoldingBuilder.Cases4.cs
```
