# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 1.3 (TH 1.3) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 1.3: Khớp nối góc 90° hướng -Y (Mép ngoài phẳng - Nhịp cuối cắt 600 mm với Giằng Chéo Thu Ngắn Family 5)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 1.3

```
     Y (+)
      ▲
   y0 ┼────────────────────────────────────┬──────────────► X (+)
      │          Dãy chính A               │ (Mặt trước Y = y0: Đầy đủ L1)
      │  (xBay5, y0)                       │ (xMax, y0)
      │      ┌─────────────────────────────┤
      │      │ Nhịp cuối (1800 x 1200 mm)  │
y0 - W┼──────┴─────────[Cột Cấy]───────────┴──────────────
      │ (xBay5, y0-W)   (xC, y0-W)        (xMax, y0-W)
      │◄── L_cut=600 ──►│◄────── W = 1200 ──────►│
      │[Giằng chéo 600] │  (Cửa ngõ sang B)      │
      │                 │                        │
      │                 │     Nhánh rẽ B         │
      │                 │   (Hướng -Y, L2 x W)   │ (Mép ngoài X = xMax:
      │                 │                        │  Lan can kép h6, h7)
      │                 │ (Vách trong X = xC:    │
      │                 │  Giằng chéo Family 5)  │
      │                 └────────────────────────┘ (xMax, yBot)
      │                 (xC, yBot)               (Đầu hồi đáy nhánh B)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp dãy chính (nhịp cuối thường là `1800` mm).
- `L2Bays`: Danh sách nhịp nhánh rẽ vươn sang $-Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo.

### Công Thức Tọa Độ Then Chốt:
1. **Đoạn cắt mép sau nhịp cuối**:
   $$L_{\text{cut}} = L_{\text{bayLast}} - W = 1800 - 1200 = 600\text{ mm}, \quad x_C = x_{\text{Bay5}} + L_{\text{cut}}$$
2. **Giằng chéo thu ngắn Family 5 ($L = 600$ mm)**:
   Tại đoạn $X \in [x_{\text{Bay5}}, x_C]$ ở mép sau $Y = y_0 - W$, bố trí **GIẰNG CHÉO THU NGẮN Family 5** ($L = 600\text{ mm}, H = 900\text{ mm}$) ở cả 2 tầng. Tuyệt đối không lắp giằng dọc Family 4 tại $h_3, h_6$.
3. **Cột cấy phụ tại $(x_C, y_0 - W)$**:
   Tạo thành khung cửa ngõ rộng trọn vẹn $W = 1200$ mm từ $x_C \to x_{\text{Max}}$ tại $Y = y_0 - W$.
4. **Vách trong nhánh rẽ ($X = x_C$)**:
   Nối tiếp từ mặt giằng chéo của dãy chính $\rightarrow$ Bố trí **GIẰNG CHÉO Family 5** ($L = 1800\text{ mm}, H = 900\text{ mm}$), tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
5. **Mép ngoài nhánh rẽ ($X = x_{\text{Max}}$)**:
   Nối tiếp từ mặt lan can phía trước của dãy chính $\rightarrow$ Bố trí **GIẰNG DỌC Family 4 tại $h_3$** và **LAN CAN KÉP Family 4 tại $h_6, h_7$**.
6. **Đầu hồi đáy nhánh rẽ ($Y = y_{\text{Bot}} = y_0 - W - L_2$)**:
   Đóng kín bằng lan can kép $h_6, h_7$, giằng chân $h_1$ và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases1.cs)

Hàm xử lý `ExecuteCase1_3` được bóc tách theo 8 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.3: KHỚP NỐI GÓC 90° HƯỚNG -Y (MÉP NGOÀI PHẲNG - CẮT 600 MM CÓ GIẰNG CHÉO THU NGẮN)
        /// Dãy chính A có mép sau thu ngắn L_cut = 600 mm, lắp Giằng chéo thu ngắn Family 5 (L=600 mm).
        /// Cấy cột tại xC; Nhánh rẽ B vươn sang -Y có vách trong là giằng chéo và mép ngoài là lan can.
        /// </summary>
        public static string ExecuteCase1_3_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO BIẾN KÍCH THƯỚC VÀ CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> l1Bays = GetL1Bays(opts);
            List<double> l2Bays = GetL2Bays(opts);

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
            // BƯỚC 3: DỰNG LƯỚI CỘT CHO DÃY CHÍNH A
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            // =========================================================================
            // BƯỚC 4: XỬ LÝ NHỊP CUỐI DÃY CHÍNH A (MẶT TRƯỚC VÀ MẶT SAU CẮT 600 MM)
            // =========================================================================
            double xBay5 = colX[nMainBays - 1];
            double L_bay5 = l1Bays[nMainBays - 1];
            double L_cut = L_bay5 > W ? (L_bay5 - W) : 0; // 1800 - 1200 = 600 mm
            double xC = xBay5 + L_cut;

            // 4.1. Mặt trước Y = y0: Chạy đủ chiều dài L_bay5 (1800 mm) có lan can kép
            PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_bay5);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0 + jackH + 135)), 0, "l", L_bay5);
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(sDeck + 450)), 0, "l", L_bay5);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(sDeck + 900)), 0, "l", L_bay5);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(sDeck)), 0, "l", L_bay5);
            }

            // 4.2. Mặt sau Y = y0 - W tại đoạn cắt 600 mm: Lắp GIẰNG CHÉO THU NGẮN Family 5 (L=600 mm)
            PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0 + jackH + 135)), 0, "l", L_cut);
            for (int s = 0; s < storyCount; s++)
            {
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay5, y0 - W, 0, L_cut, z0, jackH, s, storyCount, opts);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(z0 + jackH + 1730 + s * 1800)), 0, "l", L_cut);
            }

            // =========================================================================
            // BƯỚC 5: CẤY CỘT TẠI X = xC TRÊN MÉP SAU (Y = y0 - W)
            // =========================================================================
            PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0 - W, z0 + jackH, opts);

            // =========================================================================
            // BƯỚC 6: DỰNG CÁC CẶP CỘT TIẾP THEO CỦA NHÁNH RẼ B VỀ PHÍA -Y
            // =========================================================================
            double curY = y0 - W;
            foreach (var bLen in l2Bays)
            {
                curY -= bLen;
                // Cột vách trong
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, curY, z0 + jackH, opts);

                // Cột mép ngoài
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, curY, z0 + jackH, opts);
            }
            double yBot = curY;

            // =========================================================================
            // BƯỚC 7: DỰNG CẤU KIỆN NHÁNH RẼ B
            // Vách trong X = xC: Giằng chéo Family 5.
            // Mép ngoài X = xMax: Giằng dọc h3 + Lan can kép h6, h7 Family 4.
            // =========================================================================
            double runY = y0 - W;
            foreach (var bLen in l2Bays)
            {
                double nextY = runY - bLen;

                // Giằng chân h1
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(nextY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);

                // Giằng chéo vách trong X = xC (Family 5)
                for (int s = 0; s < storyCount; s++)
                {
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC, nextY, 90, bLen, z0, jackH, s, storyCount, opts);

                    // Lan can kép mép ngoài X = xMax (Family 4)
                    double sDeck = z0 + jackH + 1730 + s * 1800;
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(sDeck + 450)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(sDeck + 900)), 90, "l", bLen);
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(nextY), MmToFt(sDeck)), 90, "l", bLen);
                }
                runY = nextY;
            }

            // =========================================================================
            // BƯỚC 8: ĐÓNG ĐẦU HỒI ĐÁY NHÁNH RẼ (Y = yBot)
            // =========================================================================
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0 + jackH + 135)), 0, "l", W);
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(sDeck)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(sDeck + 450)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(sDeck + 900)), 0, "l", W);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yBot + 66), MmToFt(sDeck)), 0, "l", W);
            }

            return "Dựng thành công TH 1.3: Khớp nối góc 90° hướng -Y (Mép ngoài phẳng, giằng thu ngắn 600 mm).";
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Giằng chéo thu ngắn Family 5 ($L = 600$ mm)**: Tại đoạn cắt mép sau $Y = y_0 - W$, bố trí đầy đủ giằng chéo thu ngắn ở cả 2 tầng; tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
- [x] **Cột cấy phụ tại $(x_C, y_0 - W)$**: Kích chân $h = 250$ mm và cấu hình cột chống đứng đầy đủ.
- [x] **Cửa ngõ sang B ($Y = y_0 - W$, đoạn $[x_C, x_{\text{Max}}]$)**: Mở trọn vẹn $1200$ mm, không có lan can hay ván chắn biên cản lối đi.
- [x] **Vách trong nhánh rẽ ($X = x_C$)**: Bố trí giằng chéo Family 5 suốt chiều dài; tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
- [x] **Mép ngoài nhánh rẽ ($X = x_{\text{Max}}$)**: Bố trí lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$ và ván chắn biên Family 7.
- [x] **Đầu hồi đáy ($Y = y_{\text{Bot}}$)**: Khóa kín an toàn bằng lan can kép, giằng chân và ván chắn biên.
