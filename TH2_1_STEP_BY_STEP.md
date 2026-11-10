# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 2.1 (TH 2.1) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 2.1: Khớp nối góc 90° hướng +Y mép sau phẳng (Nhóm 2 - Vươn ngoài đầu hồi)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 2.1

```
                    Y (+)
                     ▲
                     │                               (Cột R2)         (Cột R1)
                     │                               (xA, yTop)       (xB, yTop)
                     │                                  ┌───────────────┐ (Đầu hồi đỉnh nhánh B)
                     │                                  │   Nhánh rẽ    │
                     │                                  │    Dãy B      │
                     │                                  │  (L2Bays x W) │
                     │                                  │               │
                     │                   (xA, yFront)   │               │
     ────────────────┼─────────────────────────┼────────┴───────────────┤ (xB, yFront)
     (x0, y0)        │                         │  (Vách trong giật cấp  │
        ┌────────────┴─────────────────────────┤   L_A = L2 - W:        │
        │             Dãy chính A              │   Lan can kép h6, h7)  │
        │        (Giữ nguyên 100% nhịp)        │                        │
        └──────────────────────────────────────┴────────────────────────┘ (xB, yBack)
     (x0, yBack)                               (xA, yBack)
                                                (Mép sau phẳng liên tục: Y = yBack)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp dãy chính (giữ nguyên vẹn 100% chiều dài, nhịp cuối không cắt ngắn).
- `L2Bays`: Danh sách nhịp nhánh rẽ vươn ra ngoài đầu hồi sang $+Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo.

### Công Thức Tọa Độ Then Chốt:
1. **Dãy chính A giữ nguyên 100%**:
   $$x_A = x_0 + \sum L_{1\text{Bays}}, \quad x_B = x_A + W, \quad y_{\text{Back}} = y_0 - W, \quad y_{\text{Front}} = y_0$$
2. **Mép sau phẳng liên tục ($Y = y_{\text{Back}}$)**:
   Mép sau của nhánh rẽ B thẳng hàng tuyệt đối với mép sau của dãy chính A tại $Y = y_{\text{Back}} = y_0 - W$.
3. **Đỉnh vươn của nhánh rẽ B**:
   $$y_{\text{Top}} = y_{\text{Back}} + L_{\text{ret}} = y_0 - W + L_2$$
   *(Khi $L_2 = 1800$ mm, $y_{\text{Top}} = y_0 + 600$ mm, tức nhô cao hơn mép trước dãy chính một đoạn $L_A = L_2 - W = 600$ mm)*.
4. **Cửa ngõ thông suốt qua đầu hồi dãy A**:
   Đoạn $Y \in [y_{\text{Back}}, y_{\text{Front}}]$ tại $X = x_A$ mở $100\%$ làm lối đi thông suốt giữa dãy A và dãy B.
5. **Giằng chéo Family 5**:
   - Bố trí tại mép ngoài bên phải ($X = x_B$, suốt chiều dài $L_{\text{ret}}$).
   - Bố trí tại đầu hồi đáy ($Y = y_{\text{Back}}$, rộng $W$).
   - Tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$ nơi đã có giằng chéo.
6. **Vách trong giật cấp ($X = x_A$, đoạn $Y \in [y_{\text{Front}}, y_{\text{Top}}]$ dài $L_A$)**:
   Bố trí lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases2.cs)

Hàm xử lý `ExecuteCase2_1` được bóc tách theo 8 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 2.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP SAU PHẲNG - VƯƠN NGOÀI ĐẦU HỒI)
        /// Dãy chính A giữ nguyên vẹn chiều dài; Nhánh rẽ B nằm ngoài đầu hồi xA -> xB.
        /// Mép sau phẳng tại Y = y0 - W; Cửa ngõ thông suốt qua đầu hồi xA.
        /// </summary>
        public static string ExecuteCase2_1_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO BIẾN KÍCH THƯỚC VÀ CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> mainBays = GetL1Bays(opts);
            List<double> l2Bays   = GetL2Bays(opts);

            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            // =========================================================================
            // BƯỚC 2: TẢI & KÍCH HOẠT CÁC FAMILY SYMBOL
            // =========================================================================
            var sym1 = GetOrLoadFamilySymbol(doc, "1", opts.FamilyFolder);
            var sym2 = GetOrLoadFamilySymbol(doc, "2", opts.FamilyFolder);
            var sym3 = GetOrLoadFamilySymbol(doc, "3", opts.FamilyFolder);
            var sym4 = GetOrLoadFamilySymbol(doc, "4", opts.FamilyFolder);
            var sym5 = GetOrLoadFamilySymbol(doc, "5", opts.FamilyFolder);
            var sym6 = GetOrLoadFamilySymbol(doc, "6", opts.FamilyFolder);
            var sym7 = GetOrLoadFamilySymbol(doc, "7", opts.FamilyFolder);

            // =========================================================================
            // BƯỚC 3: DỰNG TOÀN BỘ DÃY CHÍNH A (GIỮ NGUYÊN 100% CÁC NHỊP)
            // =========================================================================
            int nMainBays = mainBays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in mainBays) { curX += b; colX.Add(curX); }
            double xA = colX[nMainBays];
            double xB = xA + W;
            double yBack = y0 - W;
            double yFront = y0;

            // Đầu hồi trái (k=0) có lan can; Đầu hồi phải xA (k=n) là cửa ngõ sang B nên KHÔNG CHẶN
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0), opts);

            for (int i = 0; i < nMainBays; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, mainBays[i], true, opts);
            }

            // =========================================================================
            // BƯỚC 4: TÍNH TOÁN KÍCH THƯỚC NHÁNH RẼ B VƯƠN NGOÀI
            // =========================================================================
            double L_ret = l2Bays[0];
            double yTop = yBack + L_ret;
            double W_ret = W;
            double L_A = (yTop - yFront > 10.0) ? (yTop - yFront) : 0.0; // 600 mm khi L_ret=1800, W=1200

            // =========================================================================
            // BƯỚC 5: CẤY CỘT CHỐNG CHO NHÁNH RẼ B
            // Các cột mới gồm: (xB, yBack), (xB, yTop), và (xA, yTop) khi L_A > 10
            // =========================================================================
            PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(z0)), 0, "h", jackH);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yBack, z0 + jackH, opts);

            PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xB), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yTop, z0 + jackH, opts);

            if (L_A > 10.0)
            {
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xA), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xA, yTop, z0 + jackH, opts);
            }

            // =========================================================================
            // BƯỚC 6: DỰNG GIẰNG CHÂN h1 & GIẰNG ĐỠ MÂM CHO NHÁNH RẼ B
            // =========================================================================
            double zH1 = z0 + jackH + 135;
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBack), MmToFt(zH1)), 0, "l", W_ret);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(zH1)), 0, "l", W_ret);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(zH1)), 90, "l", L_ret);
            if (L_A > 10.0)
            {
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH1)), 90, "l", L_A);
            }

            // =========================================================================
            // BƯỚC 7: DỰNG GIẰNG CHÉO, MÂM SÀN VÀ LAN CAN KÉP THEO TẦNG
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;

                // 7.1. Giằng chéo Family 5: Đáy mép sau và Mép ngoài bên phải X = xB
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yBack, 0, W_ret, z0, jackH, s, storyCount, opts);
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yBack, 90, L_ret, z0, jackH, s, storyCount, opts);

                // 7.2. Lan can an toàn vách trong giật cấp X = xA (đoạn L_A = 600 mm)
                if (L_A > 10.0)
                {
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(z0 + jackH + 830 + sOff)), 90, "l", L_A);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zDeck + 450)), 90, "l", L_A);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zDeck + 900)), 90, "l", L_A);
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66), MmToFt(yFront), MmToFt(zDeck)), 90, "l", L_A);
                }

                // 7.3. Mâm sàn thao tác nhánh rẽ B (Family 6)
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(yBack), MmToFt(zDeck)), 90, "l", L_ret);
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(yBack), MmToFt(zDeck)), 90, "l", L_ret);

                // 7.4. Ván chắn biên Family 7
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yBack + 66), MmToFt(zDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yTop - 66), MmToFt(zDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(yBack), MmToFt(zDeck)), 90, "l", L_ret);
            }

            // =========================================================================
            // BƯỚC 8: ĐÓNG ĐẦU HỒI ĐỈNH (Y = yTop)
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop), MmToFt(sDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop), MmToFt(sDeck + 450)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop), MmToFt(sDeck + 900)), 0, "l", W_ret);
            }

            return "Dựng thành công TH 2.1: Khớp nối góc 90° hướng +Y mép sau phẳng (Vươn ngoài đầu hồi).";
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Dãy chính nguyên vẹn 100%**: Tất cả các nhịp tiêu chuẩn giữ nguyên chiều dài, nhịp cuối không cắt ngắn.
- [x] **Mép sau phẳng tuyệt đối ($Y = y_{\text{Back}}$)**: Thẳng hàng liên tục từ mép sau dãy A sang mép sau nhánh B.
- [x] **Cửa ngõ thông suốt qua đầu hồi $X = x_A$**: Mở $100\%$ từ $Y = y_{\text{Back}} \to y_{\text{Front}}$, không có lan can chặn lối đi.
- [x] **Giằng chéo Family 5**: Đặt tại mép ngoài bên phải ($X = x_B$) và đầu hồi đáy ($Y = y_{\text{Back}}$); tuyệt đối không lắp Family 4 ở $h_3, h_6$.
- [x] **Vách trong giật cấp ($X = x_A$, đoạn $L_A = 600$ mm)**: Bố trí lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn bằng lan can kép, giằng chân và ván chắn biên.
