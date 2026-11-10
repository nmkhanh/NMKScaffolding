# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 1.4 (TH 1.4) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 1.4: Khớp nối góc 90° hướng -Y (Nhịp cuối bằng bề rộng rẽ $W = 1200$ mm, ô vuông $1200 \times 1200$ mm)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 1.4

```
     Y (+)
      ▲
   y0 ┼────────────────────────────────────┬──────────────► X (+)
      │          Dãy chính A               │ (Mặt trước Y = y0: Đầy đủ L1)
      │  (xBayLast, y0)                    │ (xMax, y0)
      │      ┌─────────────────────────────┤
      │      │ Nhịp cuối (Ô vuông W x W)   │ (L_bayLast = W = 1200 mm)
y0 - W┼──────┴─────────────────────────────┴──────────────
      │ (xBayLast, y0-W)                   │ (xMax, y0-W)
      │      │◄──────── W = 1200 ─────────►│ (Không cấy cột phụ)
      │      │      (Cửa ngõ sang B)       │
      │      │                             │
      │      │         Nhánh rẽ B          │ (Mép ngoài X = xMax:
      │      │     (Hướng -Y, L2 x W)      │  Lan can kép h6, h7)
      │      │                             │
      │      │ (Vách trong X = xBayLast:   │
      │      │  Giằng chéo Family 5)       │
      │      └─────────────────────────────┘ (xMax, yBot)
      │      (xBayLast, yBot)              (Đầu hồi đáy nhánh B)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp dãy chính (nhịp cuối có $L_{\text{last}} = W = 1200$ mm).
- `L2Bays`: Danh sách nhịp nhánh rẽ vươn sang $-Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo.

### Công Thức Tọa Độ Then Chốt:
1. **Ô vuông góc hoàn hảo**:
   $$L_{\text{bayLast}} = W = 1200\text{ mm}, \quad x_{\text{BayLast}} = x_{\text{Max}} - W$$
2. **Không cấy cột phụ**:
   Hai cột mép sau $(x_{\text{BayLast}}, y_0 - W)$ và $(x_{\text{Max}}, y_0 - W)$ được dùng trực tiếp làm 2 chân cột bắt đầu cho nhánh rẽ B.
3. **Cửa ngõ mở thông suốt 100%**:
   Mép sau $Y = y_0 - W$ (đoạn $[x_{\text{BayLast}}, x_{\text{Max}}]$) mở trọn vẹn $1200$ mm sang nhánh rẽ B, không có lan can hay ván chắn biên cản trở.
4. **Vách trong nhánh rẽ ($X = x_{\text{BayLast}}$)**:
   Nối tiếp từ mặt giằng chéo dãy chính $\rightarrow$ Bố trí **GIẰNG CHÉO Family 5** ($L = 1800\text{ mm}, H = 900\text{ mm}$), tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
5. **Mép ngoài nhánh rẽ ($X = x_{\text{Max}}$)**:
   Nối tiếp từ mặt lan can dãy chính $\rightarrow$ Bố trí **GIẰNG DỌC $h_3$** và **LAN CAN KÉP $h_6, h_7$ Family 4**.
6. **Đầu hồi đáy nhánh rẽ ($Y = y_{\text{Bot}} = y_0 - W - L_2$)**:
   Đóng kín bằng lan can kép $h_6, h_7$, giằng chân $h_1$ và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases1.cs)

Hàm xử lý `ExecuteCase1_4` được bóc tách theo 8 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.4: KHỚP NỐI GÓC 90° HƯỚNG -Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM)
        /// Dãy chính A kết thúc bằng ô vuông W x W (1200x1200 mm).
        /// Nhánh rẽ B vươn sang -Y tựa trực tiếp vào 2 cột mép sau của ô vuông mà không cần cấy cột phụ.
        /// </summary>
        public static string ExecuteCase1_4_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            // BƯỚC 4: DỰNG CẤU KIỆN Ô VUÔNG GÓC 1200x1200 NHỊP CUỐI DÃY CHÍNH
            // Mặt trước Y = y0 có lan can kép. Mặt sau Y = y0 - W MỞ CỬA NGÕ SANG B.
            // =========================================================================
            double xBayLast = colX[nMainBays - 1];
            double L_bayLast = l1Bays[nMainBays - 1];

            // Mặt trước Y = y0: Lan can kép đầy đủ L_bayLast
            PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(z0)), 0, "l", L_bayLast);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(z0 + jackH + 135)), 0, "l", L_bayLast);
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(sDeck + 450)), 0, "l", L_bayLast);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(sDeck + 900)), 0, "l", L_bayLast);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - 66), MmToFt(sDeck)), 0, "l", L_bayLast);

                // 2 mâm sàn ô vuông góc
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - 345), MmToFt(sDeck)), 0, "l", L_bayLast);
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + 345), MmToFt(sDeck)), 0, "l", L_bayLast);
            }

            // =========================================================================
            // BƯỚC 5: DỰNG CỘT CHO NHÁNH RẼ B VỀ PHÍA -Y
            // Hai cột gốc đã có sẵn tại (xBayLast, y0-W) và (xMax, y0-W).
            // Ta dựng các cặp cột tiếp theo dọc theo -Y.
            // =========================================================================
            double curY = y0 - W;
            foreach (var bLen in l2Bays)
            {
                curY -= bLen;
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xBayLast), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xBayLast, curY, z0 + jackH, opts);

                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, curY, z0 + jackH, opts);
            }
            double yBot = curY;

            // =========================================================================
            // BƯỚC 6: DỰNG CẤU KIỆN NHÁNH RẼ B
            // Vách trong X = xBayLast: Giằng chéo Family 5.
            // Mép ngoài X = xMax: Lan can an toàn Family 4.
            // =========================================================================
            double runY = y0 - W;
            foreach (var bLen in l2Bays)
            {
                double nextY = runY - bLen;

                // Giằng chân h1
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(nextY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);

                // Giằng chéo vách trong X = xBayLast (Family 5)
                for (int s = 0; s < storyCount; s++)
                {
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, nextY, 90, bLen, z0, jackH, s, storyCount, opts);

                    // Lan can kép mép ngoài X = xMax (Family 4)
                    double sDeck = z0 + jackH + 1730 + s * 1800;
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(sDeck + 450)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(nextY), MmToFt(sDeck + 900)), 90, "l", bLen);
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(nextY), MmToFt(sDeck)), 90, "l", bLen);

                    // Mâm sàn nhánh rẽ
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast + 345), MmToFt(nextY), MmToFt(sDeck)), 90, "l", bLen);
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(nextY), MmToFt(sDeck)), 90, "l", bLen);
                }
                runY = nextY;
            }

            // =========================================================================
            // BƯỚC 7: ĐÓNG ĐẦU HỒI ĐÁY NHÁNH RẼ (Y = yBot)
            // =========================================================================
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yBot), MmToFt(z0 + jackH + 135)), 0, "l", W);
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yBot), MmToFt(sDeck)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yBot), MmToFt(sDeck + 450)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yBot), MmToFt(sDeck + 900)), 0, "l", W);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(yBot + 66), MmToFt(sDeck)), 0, "l", W);
            }

            return "Dựng thành công TH 1.4: Khớp nối góc 90° hướng -Y (Ô vuông góc 1200x1200).";
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Ô vuông góc $1200 \times 1200$ mm**: Nhịp cuối dãy chính có chiều dài $L_{\text{last}} = W = 1200$ mm.
- [x] **Không cấy cột phụ**: Tận dụng 100% 2 cột mép sau của ô vuông góc làm cột chân cho nhánh rẽ.
- [x] **Cửa ngõ sang B ($Y = y_0 - W$)**: Mở trọn vẹn $1200$ mm, không có lan can hoặc ván chắn cản lối đi.
- [x] **Vách trong nhánh rẽ ($X = x_{\text{BayLast}}$)**: Bố trí giằng chéo Family 5 suốt chiều dài; tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
- [x] **Mép ngoài nhánh rẽ ($X = x_{\text{Max}}$)**: Đầy đủ lan can kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Đầu hồi đáy ($Y = y_{\text{Bot}}$)**: Khóa kín an toàn bằng lan can kép, giằng chân và ván chắn biên.
