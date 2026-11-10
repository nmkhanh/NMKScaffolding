# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 4.1 (TH 4.1) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 4.1: Khớp nối ngã ba chữ T Mirrored (Hệ giàn 2 có mặt trong phía ngược TH 3.1)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 4.1

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
     ────────────────┼─[Cột Chung]────────────────[Cột Cấy]────────► X (+)
     (x0, y0)        │     │◄──────────── W ─────────────►│ (Đường xanh ngang)
        ┌────────────┴─────┼──────────────────────────────┬────────────┐
        │    HỘP ĐEN 1     │       (Cửa ngõ sang B)       │ HỘP ĐEN 2  │
        │   (Nhịp trước)   │          HỘP ĐEN 2           │ (Đoạn sau  │
        │     L1 x W       │     (Đoạn W tiếp giáp B)     │  L2 - W)   │
        └──────────────────┴──────────────────────────────┴────────────┘
     (x0, y0 - W)      (xJunc, y0 - W)                (xR, y0 - W) (xEnd, y0 - W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba (gồm ít nhất 2 nhịp: nhịp trước ngã ba $L_1$ và nhịp sau ngã ba $L_2$).
- `L_branch`: Chiều dài nhịp của nhánh rẽ chữ T (mặc định `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ & Nguyên Lý Kết Cấu Mirrored:
1. **Điểm giao cắt ngã ba (Junction Point)**:
   $$x_{\text{Junc}} = x_0 + L_1, \quad y_0 = \text{Mép trước dãy chính}$$
2. **Cột góc dưới - trái nhánh B (Cột dùng chung - Shared Post)**:
   Nằm tại $(x_{\text{Junc}}, y_0)$, trùng khớp hoàn toàn với Cột mép trước của dãy chính.
3. **Cột góc dưới - phải nhánh B (Cột cấy phụ - Additional Post)**:
   $$x_R = x_{\text{Junc}} + W = x_{\text{Junc}} + 1200\text{ mm}, \quad Y = y_0$$
4. **Cửa ngõ thông suốt $1200$ mm**:
   Đoạn $X \in [x_{\text{Junc}}, x_R]$ tại $Y = y_0$ mở trọn vẹn $1200$ mm.
   - **TUYỆT ĐỐI KHÔNG LẮP**: Ván lót đáy Family 1 qua cửa ngõ, lan can kép Family 4 và ván chắn biên Family 7.
   - Mâm sàn thao tác Family 6 tại $h_5$ ($Z = 1980$ mm) nối tiếp phẳng $100\%$ giữa dãy chính và nhánh rẽ qua thanh giằng ngang đỡ mâm `trDoorDeck`.
5. **Quy Tắc Bất Biến Mirrored (Mặt Trong Phía Ngược TH3)**:
   - **Mặt ngoài (Outer Face)**:
     - Dãy chính: Mặt sau ($Y = y_0 - W$).
     - Nhánh rẽ B: **VÁCH TRÁI ($X = x_{\text{Junc}}$)**.
     - $\implies$ **Bố trí Giằng chéo Family 5** (qua `PlaceBayDiagonalBraces`), ván chắn biên Family 7. Tuyệt đối không lắp giằng dọc lan can ở vách này.
   - **Mặt trong (Inner Face)**:
     - Dãy chính: Mặt trước ($Y = y_0$) hướng vào công trình.
     - Nhánh rẽ B: **VÁCH PHẢI ($X = x_R$)**.
     - $\implies$ **Bố trí Giằng dọc Family 4** (lan can bảo vệ kép $h_6, h_7$, giằng giữa $h_3$).
   - **Đầu hồi đỉnh nhánh rẽ ($Y = y_{\text{Top}}$)**: Khóa kín an toàn với lan can kép $h_6, h_7$, toeboard và giằng đỉnh tại `topZ` (chỉ đặt 1 lần, triệt tiêu 100% đè trùng).
6. **Điều Kiện Quét Nhận Diện Tự Động Trong Revit (DetailLine Scanning Condition)**:
   - Dãy chính (Row A): Đường DetailLine xanh nằm ở mép trước ($Y = y_0$).
   - Nhánh rẽ B (Row B): Đường DetailLine xanh nằm ở **VÁCH PHẢI ($X = x_R$)** (mặt trong hướng công trình).
   - Tọa độ chiếu $s$ thỏa mãn: $|(s - W) - C_k| \le 150$ mm (với $C_k = x_{\text{Junc}}$ là cột mốc ngã ba).
   - Chiều dài nhịp $L_k > W + 100$ mm ($L_k = 1800$ mm) $\implies$ Hệ thống tự động phát hiện **TH 4.1**.
   - Khi chạy Table Run, tọa độ gốc của Row B được tự động căn chỉnh sang vách trái $ox = x_R - W \sin\theta$ để khớp nối phẳng 100% với cửa ngõ Row 0.

---

## 2. Mã Nguồn C# Thực Thi Chuẩn Hóa

```csharp
// Trích xuất từ ScaffoldingBuilder.Cases4.cs: ExecuteCase4_1
// Xem chi tiết trong file mã nguồn c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool\ScaffoldingBuilder.Cases4.cs
```
