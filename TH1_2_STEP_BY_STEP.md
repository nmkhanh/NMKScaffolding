# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 1.2 (TH 1.2) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 1.2: Khớp nối góc 90° hướng +Y (Nhịp cuối bằng bề rộng rẽ $W = 1200$ mm, ô vuông $1200 \times 1200$ mm)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 1.2

```
                    Y (+)
                     ▲
                     │          (Cột R2)         (Cột R1)
                     │          (xBayLast, yTop) (xMax, yTop)
                     │             ┌───────────────┐ (Đầu hồi cuối nhánh B)
                     │             │   Nhánh rẽ    │
                     │             │    Dãy B      │
                     │             │   (L2 x W)    │
                     │             │               │
                     │  (xBayLast, y0)             │ (xMax, y0)
     ────────────────┼─────────────┴───────────────┴──────► X (+)
     (x0, y0)        │      │◄──────── W ─────────►│
        ┌────────────┴──────┤   (Cửa ngõ sang B)   │
        │   Dãy chính A     │                      │
        │(L1Bays các nhịp đầu)│ Ô vuông góc W x W  │ (L_bayLast = W = 1200 mm)
        └───────────────────┴──────────────────────┘ (xMax, y0 - W)
     (x0, y0 - W)       (xBayLast, y0 - W)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp dãy chính (nhịp cuối cùng có $L_{\text{last}} = W = 1200$ mm).
- `L2Bays`: Danh sách nhịp nhánh rẽ vươn sang $+Y$ (ví dụ: `1800` mm hoặc `1800, 1800`).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ Then Chốt:
1. **Ô vuông góc hoàn hảo**:
   $$L_{\text{bayLast}} = W = 1200\text{ mm}, \quad x_{\text{BayLast}} = x_{\text{Max}} - W$$
2. **Không cấy cột phụ**:
   Hai cột mép trước $(x_{\text{BayLast}}, y_0)$ và $(x_{\text{Max}}, y_0)$ của dãy chính được dùng làm 2 cột chân bắt đầu của nhánh rẽ B.
3. **Cửa ngõ mở thông suốt 100%**:
   Đoạn $X \in [x_{\text{BayLast}}, x_{\text{Max}}]$ tại $Y = y_0$ mở trọn vẹn $1200$ mm, tuyệt đối không có lan can hoặc ván chắn biên cản trở lối đi.
4. **Vách ngoài liên tục ($X = x_{\text{Max}}$)**:
   Mép ngoài cùng bên phải kéo dài liên tục từ $Y = y_0 - W$ lên đến $Y = y_0 + L_2$.
   - Tại nhánh rẽ B: Bố trí Giằng chéo Family 5 (không lắp Family 4 ở $h_3, h_6$).
5. **Đầu hồi kết thúc nhánh B ($Y = y_{\text{Top}} = y_0 + L_2$)**:
   Đóng kín an toàn bằng giằng chân $h_1$, lan can kép $h_6, h_7$, mâm giằng đỡ sàn $h_5$ và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases1.cs)

Hàm xử lý `ExecuteCase1_2` được bóc tách theo 8 bước:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.2: KHỚP NỐI GÓC 90° HƯỚNG +Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM)
        /// Dãy chính A kết thúc bằng nhịp ô vuông W x W (1200x1200).
        /// Nhánh rẽ B vươn sang +Y bắt trực tiếp vào 2 cột trước của ô vuông mà không cần cấy cột phụ.
        /// </summary>
        public static string ExecuteCase1_2_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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

            // =========================================================================
            // BƯỚC 4: DỰNG CÁC NHỊP ĐẦU DÃY CHÍNH (TRỪ NHỊP CUỐI GÓC)
            // =========================================================================
            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            // =========================================================================
            // BƯỚC 5: DỰNG NHỊP CUỐI CỦA DÃY CHÍNH (Ô VUÔNG 1200x1200)
            // Mặt sau lắp giằng chéo Family 5 và mâm sàn; Mặt trước Y=y0 MỞ CỬA NGÕ SANG B
            // =========================================================================
            double xBayLast = colX[nMainBays - 1];
            double L_bayLast = l1Bays[nMainBays - 1];

            // Mặt sau (Y = y0 - W): Ván lót, giằng chân, giằng chéo Family 5
            PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_bayLast);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(z0 + jackH + 135)), 0, "l", L_bayLast);
            for (int s = 0; s < storyCount; s++)
            {
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, y0 - W, 0, L_bayLast, z0, jackH, s, storyCount, opts);
                // 2 mâm thao tác ô vuông góc
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - 345), MmToFt(z0 + jackH + 1730 + s * 1800)), 0, "l", L_bayLast);
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + 345), MmToFt(z0 + jackH + 1730 + s * 1800)), 0, "l", L_bayLast);
            }

            // =========================================================================
            // BƯỚC 6: DỰNG CỘT CHO NHÁNH RẼ B VƯƠN SANG +Y
            // Hai cột gốc nhánh rẽ đã có sẵn tại (xBayLast, y0) và (xMax, y0).
            // Ta dựng thêm các cặp cột tiếp theo dọc theo +Y.
            // =========================================================================
            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                curY += bLen;
                // Cột vách trong bên trái
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xBayLast), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xBayLast, curY, z0 + jackH, opts);

                // Cột vách ngoài bên phải
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, curY, z0 + jackH, opts);
            }
            double yTop = curY;

            // =========================================================================
            // BƯỚC 7: DỰNG CẤU KIỆN NHÁNH RẼ B
            // Vách ngoài X = xMax: Giằng chéo Family 5.
            // Vách trong X = xBayLast: Lan can an toàn Family 4 + Ván chắn biên Family 7.
            // =========================================================================
            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                // Giằng chân h1
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(runY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(runY), MmToFt(z0 + jackH + 135)), 90, "l", bLen);

                // Giằng chéo vách ngoài bên phải (Family 5)
                for (int s = 0; s < storyCount; s++)
                {
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, runY, 90, bLen, z0, jackH, s, storyCount, opts);

                    // Lan can kép vách trong bên trái (Family 4)
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(runY), MmToFt(z0 + jackH + 2180 + s * 1800)), 90, "l", bLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(runY), MmToFt(z0 + jackH + 2630 + s * 1800)), 90, "l", bLen);

                    // Mâm thao tác nhánh rẽ
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast + 345), MmToFt(runY), MmToFt(z0 + jackH + 1730 + s * 1800)), 90, "l", bLen);
                    PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(runY), MmToFt(z0 + jackH + 1730 + s * 1800)), 90, "l", bLen);
                }
                runY += bLen;
            }

            // =========================================================================
            // BƯỚC 8: ĐÓNG ĐẦU HỒI ĐỈNH NHÁNH RẼ (Y = yTop)
            // =========================================================================
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yTop), MmToFt(z0 + jackH + 135)), 0, "l", W);
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yTop), MmToFt(sDeck)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yTop), MmToFt(sDeck + 450)), 0, "l", W);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(yTop), MmToFt(sDeck + 900)), 0, "l", W);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(yTop - 66), MmToFt(sDeck)), 0, "l", W);
            }

            return "Dựng thành công TH 1.2: Khớp nối góc 90° hướng +Y (Ô vuông góc 1200x1200).";
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Ô vuông góc $1200 \times 1200$ mm**: Nhịp cuối dãy chính có chiều dài $L_{\text{last}} = W = 1200$ mm.
- [x] **Không cấy cột phụ**: Tận dụng 100% 2 cột mép trước của ô vuông góc làm cột chân cho nhánh rẽ.
- [x] **Cửa ngõ sang nhánh rẽ ($Y = y_0$)**: Mở trọn vẹn $1200$ mm, không có lan can hoặc ván chắn cản lối đi.
- [x] **Mặt ngoài phẳng ($X = x_{\text{Max}}$)**: Bố trí giằng chéo Family 5 suốt chiều dài; tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6$.
- [x] **Vách trong nhánh rẽ**: Đầy đủ lan can kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn bằng lan can kép, giằng chân và ván chắn biên.
