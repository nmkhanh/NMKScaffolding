# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 2.2 (TH 2.2) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 2.2: Khớp nối góc 90° hướng -Y mép trước phẳng (Nhóm 2 - Vươn ngoài đầu hồi)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 2.2

```
     Y (+)
      ▲
   y0 ┼────────────────────────────────────────┬────────────────────────► X (+)
      │              Dãy chính A               │ (xA, yFront)           (xB, yFront)
      │         (Giữ nguyên 100% nhịp)         ├────────────────────────┤ (Mép trước phẳng: Y = yFront)
      │                                        │                        │
y0 - W┼────────────────────────────────────────┤ (xA, yBack)            │
      │ (x0, yBack)                            │                        │
      │                                        │ (Vách trong giật cấp   │
      │                                        │  L_A = L2 - W:         │
      │                                        │  Giằng chéo Family 5)  │
      │                                        ├────────────────────────┘ (xB, yBot)
      │                                        │ (xA, yBot)             (Đầu hồi đáy nhánh B)
      │                                        │ Nhánh rẽ B vươn -Y: L2 x W
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp dãy chính (giữ nguyên vẹn 100% chiều dài, không cắt ngắn).
- `L2Bays`: Danh sách nhịp nhánh rẽ vươn ra ngoài đầu hồi sang $-Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo.

### Công Thức Tọa Độ Then Chốt:
1. **Dãy chính A giữ nguyên 100%**:
   $$x_A = x_0 + \sum L_{1\text{Bays}}, \quad x_B = x_A + W, \quad y_{\text{Front}} = y_0, \quad y_{\text{Back}} = y_0 - W$$
2. **Mép trước phẳng liên tục ($Y = y_{\text{Front}} = y_0$)**:
   Mép trước của nhánh rẽ B thẳng hàng tuyệt đối với mép trước của dãy chính A tại $Y = y_0$.
3. **Đáy vươn của nhánh rẽ B**:
   $$y_{\text{Bot}} = y_{\text{Front}} - L_{\text{ret}} = y_0 - L_2$$
   *(Khi $L_2 = 1800$ mm, $y_{\text{Bot}} = y_0 - 1800$ mm, tức nhô sâu ra phía sau mép sau dãy A một đoạn $L_A = L_2 - W = 600$ mm)*.
4. **Cửa ngõ thông suốt qua đầu hồi dãy A**:
   Đoạn $Y \in [y_{\text{Back}}, y_{\text{Front}}]$ tại $X = x_A$ mở $100\%$ làm lối đi thông suốt giữa dãy A và nhánh rẽ B.
5. **Vách trong giật cấp ($X = x_A$, đoạn $Y \in [y_{\text{Bot}}, y_{\text{Back}}]$ dài $L_A = 600$ mm)**:
   Tiếp nối từ mặt giằng chéo phía sau của dãy chính $\rightarrow$ Bố trí **GIẰNG CHÉO Family 5 ($L = 600\text{ mm}, H = 900\text{ mm}$)** ở cả 2 tầng. Tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6, h_7$.
6. **Mép ngoài bên phải ($X = x_B$, đoạn $Y \in [y_{\text{Bot}}, y_{\text{Front}}]$ dài $L_{\text{ret}} = 1800$ mm)**:
   Tiếp nối từ mặt lan can mép trước của dãy chính $\rightarrow$ Bố trí **GIẰNG DỌC Family 4 tại $h_3$** và **LAN CAN KÉP Family 4 tại $h_6, h_7$**. Tuyệt đối không có giằng chéo Family 5 ở mép ngoài này.
7. **Hai đầu hồi khóa an toàn**:
   - Mép trước $Y = y_{\text{Front}}$ ($X \in [x_A, x_B]$): Lan can an toàn kép $h_6, h_7$, giằng chân $h_1$ và ván chắn biên Family 7.
   - Đầu hồi đáy $Y = y_{\text{Bot}}$ ($X \in [x_A, x_B]$): Lan can an toàn kép $h_6, h_7$, giằng chân $h_1$ và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases2.cs)

Hàm xử lý `ExecuteCase2_2` được bóc tách theo 8 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 2.2: KHỚP NỐI GÓC 90° HƯỚNG -Y (MÉP TRƯỚC PHẲNG - VƯƠN NGOÀI ĐẦU HỒI)
        /// Dãy chính A giữ nguyên vẹn chiều dài; Nhánh rẽ B nằm ngoài đầu hồi xA -> xB.
        /// Mép trước phẳng tại Y = y0; Nhánh rẽ vươn về phía -Y (nhô ra sau mép sau một đoạn L_A).
        /// </summary>
        public static string ExecuteCase2_2_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            double yFront = y0;
            double yBack  = y0 - W;

            // Đầu hồi trái (k=0) có lan can; Đầu hồi phải xA là cửa ngõ sang B nên KHÔNG CHẶN
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0), opts);

            for (int i = 0; i < nMainBays; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, mainBays[i], true, opts);
            }

            // =========================================================================
            // BƯỚC 4: TÍNH TOÁN KÍCH THƯỚC NHÁNH RẼ B VƯƠN NGOÀI SANG -Y
            // =========================================================================
            double L_ret = l2Bays[0];
            double yBot = yFront - L_ret;
            double W_ret = W;
            double L_A = (yBack - yBot > 10.0) ? (yBack - yBot) : 0.0; // 600 mm khi L_ret=1800, W=1200

            // =========================================================================
            // BƯỚC 5: CẤY CỘT CHỐNG CHO NHÁNH RẼ B
            // Các cột gồm: (xB, yFront), (xB, yBot), và (xA, yBot) khi L_A > 10
            // =========================================================================
            PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xB), MmToFt(yFront), MmToFt(z0)), 0, "h", jackH);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yFront, z0 + jackH, opts);

            PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yBot, z0 + jackH, opts);

            if (L_A > 10.0)
            {
                PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xA, yBot, z0 + jackH, opts);
            }

            // =========================================================================
            // BƯỚC 6: DỰNG GIẰNG CHÂN h1 CHO NHÁNH RẼ B
            // =========================================================================
            double zH1 = z0 + jackH + 135;
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH1)), 0, "l", W_ret);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(zH1)), 0, "l", W_ret);
            PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot),   MmToFt(zH1)), 90, "l", L_ret);
            if (L_A > 10.0)
            {
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH1)), 90, "l", L_A);
            }

            // =========================================================================
            // BƯỚC 7: DỰNG GIẰNG CHÉO, MÂM SÀN VÀ LAN CAN KÉP THEO TẦNG
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;

                // 7.1. Giằng chéo Family 5 tại vách trong giật cấp X = xA (đoạn L_A = 600 mm)
                if (L_A > 10.0)
                {
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yBot, 90, L_A, z0, jackH, s, storyCount, opts);
                }

                // 7.2. Lan can kép mép ngoài bên phải X = xB (đoạn L_ret = 1800 mm)
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(z0 + jackH + 830 + sOff)), 90, "l", L_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zDeck + 450)), 90, "l", L_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zDeck + 900)), 90, "l", L_ret);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_ret);

                // 7.3. Mâm sàn thao tác nhánh rẽ B (Family 6)
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_ret);
                PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_ret);

                // 7.4. Ván chắn biên Family 7
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yFront - 66), MmToFt(zDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yBot + 66),   MmToFt(zDeck)), 0, "l", W_ret);
                if (L_A > 10.0)
                {
                    PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_A);
                }
            }

            // =========================================================================
            // BƯỚC 8: ĐÓNG CÁC ĐẦU HỒI HỞ (ĐÁY Y = yBot VÀ MÉP TRƯỚC Y = yFront)
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800;
                // Khóa an toàn đáy Y = yBot
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(sDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(sDeck + 450)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(sDeck + 900)), 0, "l", W_ret);

                // Khóa an toàn mép trước Y = yFront
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(sDeck)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(sDeck + 450)), 0, "l", W_ret);
                PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(sDeck + 900)), 0, "l", W_ret);
            }

            return "Dựng thành công TH 2.2: Khớp nối góc 90° hướng -Y mép trước phẳng (Vươn ngoài đầu hồi).";
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Dãy chính nguyên vẹn 100%**: Tất cả các nhịp tiêu chuẩn giữ nguyên chiều dài, nhịp cuối không cắt ngắn.
- [x] **Mép trước phẳng tuyệt đối ($Y = y_{\text{Front}}$)**: Thẳng hàng liên tục từ mép trước dãy A sang mép trước nhánh B.
- [x] **Cửa ngõ thông suốt qua đầu hồi $X = x_A$**: Mở $100\%$ từ $Y = y_{\text{Back}} \to y_{\text{Front}}$, không có lan can chặn lối đi.
- [x] **Giằng chéo Family 5**: Đặt tại vách trong giật cấp $X = x_A$ ($L_A = 600$ mm) ở cả 2 tầng; tuyệt đối không lắp giằng dọc Family 4 ở $h_3, h_6, h_7$.
- [x] **Mép ngoài bên phải ($X = x_B$)**: Đầy đủ lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Hai đầu hồi hở (đáy $y_{\text{Bot}}$ và mép trước $y_{\text{Front}}$)**: Khóa kín an toàn bằng lan can kép, giằng chân và ván chắn biên.
