# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 3.1 (TH 3.1) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 3.1: Khớp nối ngã ba chữ T (T-Junction) đâm vuông góc vào giữa thân dãy chính** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 3.1

```
                    Y (+)
                     ▲
                     │ (Đường xanh dọc)
                     │
                     │          (Cột R2)         (Cột R1)
                     │          (xJunc, yTop)    (xR, yTop)
                     │             ┌───────────────┐ (Đầu hồi đỉnh nhánh B)
                     │             │   HỘP ĐỎ      │
                     │             │  Nhánh rẽ B   │ (Nhánh rẽ chữ T nằm
                     │             │ (L_branch x W)│  về bên phải đường dọc)
                     │             │               │
                     │ (xJunc, y0) │               │ (xR, y0)
     ────────────────┼─[Cột Chung]─┴───[Cột Cấy]───┴──────────────► X (+)
     (x0, y0)        │      │◄─────── W ──────────►│ (Đường xanh ngang)
        ┌────────────┴──────┼──────────────────────┬────────────┐
        │    HỘP ĐEN 1      │     (Cửa ngõ sang B) │ HỘP ĐEN 2  │
        │   (Nhịp trước)    │      HỘP ĐEN 2       │ (Đoạn sau  │
        │     L1 x W        │ (Đoạn W tiếp giáp B) │  L2 - W)   │
        └───────────────────┴──────────────────────┴────────────┘
     (x0, y0 - W)       (xJunc, y0 - W)        (xR, y0 - W) (xEnd, y0 - W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba (gồm ít nhất 2 nhịp: nhịp trước ngã ba $L_1$ và nhịp sau ngã ba $L_2$).
- `L_branch`: Chiều dài nhịp của nhánh rẽ chữ T (mặc định `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ Then Chốt:
1. **Điểm giao cắt ngã ba (Junction Point)**:
   $$x_{\text{Junc}} = x_0 + L_1, \quad y_0 = \text{Mép trước dãy chính}$$
2. **Cột góc dưới - trái nhánh B (Cột dùng chung - Shared Post)**:
   Nằm tại $(x_{\text{Junc}}, y_0)$, trùng hoàn toàn với Cột mép trước của dãy chính tại ngã ba.
   - Cột có đĩa Rosette 8 lỗ: Cắm giằng sang trái (Nhịp 1), sang phải (Nhịp 2), cắm giằng ngang đi xuống $-Y$, và cắm giằng dọc vách trái của nhánh rẽ đi lên $+Y$.
3. **Cột góc dưới - phải nhánh B (Cột cấy phụ - Additional Post)**:
   $$x_R = x_{\text{Junc}} + W = x_{\text{Junc}} + 1200\text{ mm}, \quad Y = y_0$$
   - Vì nhịp sau của dãy chính là $L_2 = 1800$ mm, cột tiếp theo của dãy chính nằm ở $x_{\text{End}} = x_{\text{Junc}} + 1800$ mm.
   - Do đó, tại $x_R$ ta cấy thêm 1 cột phụ trên mép trước của dãy chính (cách cột tiếp theo của dãy chính $1800 - 1200 = 600$ mm).
4. **Cửa ngõ thông suốt $1200$ mm**:
   Đoạn $X \in [x_{\text{Junc}}, x_R]$ tại $Y = y_0$ mở trọn vẹn $1200$ mm.
   - **TUYỆT ĐỐI KHÔNG LẮP**: Lan can kép Family 4 ($h_6, h_7$) và Ván chắn biên Family 7 tại cửa ngõ này.
   - Mâm sàn thao tác Family 6 tại $h_5$ ($Z = 1980$ mm) nối tiếp phẳng $100\%$ giữa dãy chính và nhánh rẽ.
5. **Đỉnh vươn của nhánh rẽ B**:
   $$y_{\text{Top}} = y_0 + L_{\text{branch}}$$
   - Hai cột đỉnh nhánh rẽ nằm tại $(x_{\text{Junc}}, y_{\text{Top}})$ và $(x_R, y_{\text{Top}})$.
6. **Bảo vệ an toàn các vách nhánh rẽ**:
   - Vách trái ($X = x_{\text{Junc}}$): Lan can an toàn kép Family 4 ($h_6, h_7$), giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
   - Vách phải ($X = x_R$): Lan can an toàn kép Family 4 ($h_6, h_7$), giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
   - Đầu hồi đỉnh ($Y = y_{\text{Top}}$): Khóa kín an toàn nếu là nhịp kết thúc.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (ScaffoldingBuilder.Cases3.cs)

Dưới đây là hàm xử lý trọn vẹn của **TH 3.1** được bóc tách theo 9 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 3.1: KHỚP NỐI NGÃ BA CHỮ T (T-JUNCTION) ĐÂM VUÔNG GÓC VÀO THÂN DÃY CHÍNH
        /// Dãy chính chạy thẳng liên tục qua ngã ba; Nhánh rẽ B mọc từ tim ngã ba vươn sang +Y
        /// và nằm lệch về bên phải đường xanh dọc (+X) với bề rộng W.
        /// </summary>
        public static string ExecuteCase3_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO CÁC THÔNG SỐ HÌNH HỌC & CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> l1Bays = GetL1Bays(opts); // Dãy chính (ví dụ: [1800, 1800])
            List<double> l2Bays = GetL2Bays(opts); // Nhánh rẽ (ví dụ: [1800])

            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            // =========================================================================
            // BƯỚC 2: TẢI & KÍCH HOẠT CÁC FAMILY SYMBOL (FAMILY 1 ĐẾN 7)
            // =========================================================================
            var sym1 = GetOrLoadFamilySymbol(doc, "1", opts.FamilyFolder); // Base board
            var sym2 = GetOrLoadFamilySymbol(doc, "2", opts.FamilyFolder); // Base Jack
            var sym3 = GetOrLoadFamilySymbol(doc, "3", opts.FamilyFolder); // Post
            var sym4 = GetOrLoadFamilySymbol(doc, "4", opts.FamilyFolder); // Ledger / Transom
            var sym5 = GetOrLoadFamilySymbol(doc, "5", opts.FamilyFolder); // Diagonal Brace
            var sym6 = GetOrLoadFamilySymbol(doc, "6", opts.FamilyFolder); // Plank
            var sym7 = GetOrLoadFamilySymbol(doc, "7", opts.FamilyFolder); // Toe Board

            if (sym1 == null || sym2 == null || sym3 == null || sym4 == null)
            {
                return "Thiếu Family cốt lõi trong: " + opts.FamilyFolder;
            }

            ActivateSymbol(doc, sym1);
            ActivateSymbol(doc, sym2);
            ActivateSymbol(doc, sym3);
            ActivateSymbol(doc, sym4);
            if (sym5 != null) ActivateSymbol(doc, sym5);
            if (sym6 != null) ActivateSymbol(doc, sym6);
            if (sym7 != null) ActivateSymbol(doc, sym7);

            // =========================================================================
            // BƯỚC 3: DỰNG DÃY GIÀN CHÍNH (CHẠY LIÊN TỤC QUA NGÃ BA)
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            // Dựng lưới cột 2 hàng cho dãy chính (Y = y0 và Y = y0 - W)
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            // Dựng cấu kiện cho từng nhịp của dãy chính
            for (int i = 0; i < nMainBays; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            // =========================================================================
            // BƯỚC 4: XÁC ĐỊNH VỊ TRÍ NGÃ BA & CẤY CỘT PHỤ TẠI MÉP TRƯỚC
            // Giả định ngã ba xuất phát tại cột thứ 2 (k=1) tức sau nhịp đầu tiên L1Bays[0]
            // =========================================================================
            double xJunc = colX[1]; // Điểm ngã ba (ví dụ x0 + 1800)
            double xR    = xJunc + W; // Vách phải nhánh rẽ (xJunc + 1200)

            // Cấy thêm 1 cột phụ tại (xR, y0) trên mép trước dãy chính nếu chưa có cột
            bool hasColAtXR = false;
            foreach (var cx in colX)
            {
                if (Math.Abs(cx - xR) < 10.0) { hasColAtXR = true; break; }
            }

            if (!hasColAtXR)
            {
                var jkR = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkR != null) placedIds.Add(jkR.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, y0, z0 + jackH, opts);
            }

            // =========================================================================
            // BƯỚC 5: DỰNG CỘT CHO NHÁNH RẼ B VỀ PHÍA +Y
            // Hai cột chân đã có tại (xJunc, y0) và (xR, y0).
            // Ta dựng thêm các cặp cột tiếp theo theo danh sách nhịp l2Bays.
            // =========================================================================
            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                curY += bLen;
                // Cột vách trái (xJunc, curY)
                var jkLeft = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xJunc), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkLeft != null) placedIds.Add(jkLeft.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xJunc, curY, z0 + jackH, opts);

                // Cột vách phải (xR, curY)
                var jkRight = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkRight != null) placedIds.Add(jkRight.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, curY, z0 + jackH, opts);
            }
            double yTop = curY;

            // =========================================================================
            // BƯỚC 6: DỰNG GIẰNG CHÂN h1 CHO NHÁNH RẼ B
            // =========================================================================
            double zH1 = z0 + jackH + 135;
            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                // Giằng dọc chân vách trái và vách phải
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                runY += bLen;
                // Giằng ngang chân tại các mốc nhịp
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 0, "l", W);
            }

            // =========================================================================
            // BƯỚC 7: DỰNG LAN CAN KÉP & MÂM SÀN THAO TÁC CHO NHÁNH RẼ B
            // =========================================================================
            runY = y0;
            foreach (var bLen in l2Bays)
            {
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;

                    // Giằng giữa h3 vách trái & vách phải
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);

                    // Lan can an toàn kép h6, h7 vách trái & vách phải
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zDeck + 450)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zDeck + 900)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zDeck + 450)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zDeck + 900)), 90, "l", bLen);

                    // Ván chắn biên Family 7
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc + 66), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66),    MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);

                    // 2 Mâm sàn thao tác Family 6 (rộng 500 mm)
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xJunc + 345), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - 345),    MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);

                    // Giằng ngang đỡ mâm tại cuối nhịp
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY + bLen), MmToFt(zDeck)), 0, "l", W);
                }
                runY += bLen;
            }

            // =========================================================================
            // BƯỚC 8: ĐÓNG ĐẦU HỒI ĐỈNH NHÁNH RẼ (Y = yTop)
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(sDeck + 450)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(sDeck + 900)), 0, "l", W);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc), MmToFt(yTop - 66), MmToFt(sDeck)), 0, "l", W);
            }

            return string.Format("Dựng thành công TH 3.1: Khớp nối ngã ba chữ T (T-Junction) tại X = {0:F0} mm.", xJunc);
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Dãy chính chạy thẳng liên tục**: Giữ nguyên vẹn lưới cột và sàn thao tác của cả 2 nhịp trước và sau ngã ba.
- [x] **Cột dùng chung tại tim ngã ba $(x_{\text{Junc}}, y_0)$**: Cột có đĩa Rosette 8 lỗ liên kết đồng thời 3 hướng giàn giáo.
- [x] **Cột cấy phụ tại $(x_{\text{Junc}} + W, y_0)$**: Đầy đủ kích chân ($h = 250$ mm) và cấu hình cột đứng.
- [x] **Cửa ngõ thông suốt $1200$ mm**: Tuyệt đối không lắp lan can $h_6, h_7$ hoặc ván chắn biên Family 7 chắn ngang lối rẽ từ dãy chính vào nhánh B.
- [x] **Mặt sàn phẳng liên tục**: Mâm sàn thao tác của dãy chính và nhánh rẽ gặp nhau cùng cao độ $h_5$ ($Z = 1980$ mm).
- [x] **Vách trái & vách phải nhánh rẽ**: Bố trí lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn bằng lan can kép, giằng ngang và ván chắn biên.
