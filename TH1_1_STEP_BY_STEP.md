# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 1.1 (TH 1.1) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 1.1: Khớp nối góc 90° hướng +Y (Mép ngoài phẳng)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 1.1

```
                    Y (+)
                     ▲
                     │          (Cột R2)
                     │          (xMax, yTop)
                     │             ┌───────┐ (Cột R1)
                     │             │       │ (xC, yTop)
                     │             │       │
                     │             │Nhánh  │
                     │             │rẽ (B) │
                     │             │L2 x W │
                     │             │       │
                     │  (xC, y0)   │       │ (xMax, y0)
     ────────────────┼──[Cột Cấy]──┴───────┴──────► X (+)
     (x0, y0)        │     │◄─── W ────►│
        ┌────────────┴─────┤ (Cửa ngõ sang B)
        │  Nhịp dãy chính  │
        │    Dãy A (L1)    │
        └──────────────────┘ (xMax, y0 - W)
     (x0, y0 - W)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của hệ giàn.
- `L1`: Chiều dài nhịp hệ giàn đầu (dãy chính A), ví dụ `1800` mm.
- `L2`: Chiều dài nhịp hệ giàn sau (nhánh rẽ B), ví dụ `1800` mm.
- `W`: Bề rộng hệ giàn, ví dụ `1200` mm.
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1`).

### Công Thức Tọa Độ Then Chốt:
1. **Đoạn mép trước giàn A thu ngắn**:
   $$L_{cut} = \max(0, L_1 - W)$$
   *(Ví dụ: $1800 - 1200 = 600$ mm)*.
2. **Vị trí cột cấy**:
   $$x_C = x_0 + L_{cut}$$
   *(Ví dụ: $x_0 + 600$ mm)*.
3. **Mép ngoài cùng bên phải**:
   $$x_{Max} = x_0 + L_1$$
   *(Ví dụ: $x_0 + 1800$ mm)*.
4. **Cửa ngõ mở thông sang nhánh rẽ B**:
   Khoảng cách từ $x_C \to x_{Max}$ đúng bằng $(x_0 + L_1) - (x_0 + L_1 - W) = W$ ($1200$ mm).
5. **Đỉnh vươn của nhánh rẽ B**:
   $$y_{Top} = y_0 + L_2$$
   *(Ví dụ: $y_0 + 1800$ mm)*.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Có Đầy Đủ Chú Thích)

Dưới đây là hàm xử lý trọn vẹn của **TH 1.1** được bóc tách theo 9 bước:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP NGOÀI PHẲNG)
        /// Dãy chính (A) có chiều dài L1, mép trước thu ngắn L_cut = L1 - W để cấy cột tại xC.
        /// Nhánh rẽ (B) vươn sang +Y có chiều dài L2, nối trực tiếp vào cửa ngõ [xC, xMax].
        /// </summary>
        public static string ExecuteCase1_1_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO CÁC BIẾN KÍCH THƯỚC VÀ CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double L1 = opts.L1 > 0 ? opts.L1 : 1800.0;
            double L2 = opts.L2 > 0 ? opts.L2 : 1800.0;

            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            // Các mốc cao độ theo tầng (ALINCO rosette tiêu chuẩn)
            double zH1 = z0 + jackH + 135;   // Giằng chân h1
            double zH3 = z0 + jackH + 830;   // Giằng dọc giữa h3
            double zDeck = z0 + jackH + 1730; // Mâm sàn thao tác & giằng đỡ sàn
            double zH6 = z0 + jackH + 2180;  // Lan can dưới h6
            double zH7 = z0 + jackH + 2630;  // Lan can trên h7

            // Tính toán khoảng cắt mép trước và tọa độ cột cấy
            double L_cut = L1 > W ? (L1 - W) : 0; // 1800 - 1200 = 600 mm
            double xC = x0 + L_cut;               // x0 + 600 mm
            double xMax = x0 + L1;                // x0 + 1800 mm
            double yTop = y0 + L2;                // y0 + 1800 mm
            double L_bayMain = L1;
            double L_return = L2;
            double W_return = W;

            // =========================================================================
            // BƯỚC 2: TẢI & KÍCH HOẠT CÁC FAMILY SYMBOL (FAMILY 1 ĐẾN 7)
            // =========================================================================
            var sym1 = GetOrLoadFamilySymbol(doc, "1", opts.FamilyFolder); // Base board (Ván lót đáy)
            var sym2 = GetOrLoadFamilySymbol(doc, "2", opts.FamilyFolder); // Base Jack (Kích chân)
            var sym3 = GetOrLoadFamilySymbol(doc, "3", opts.FamilyFolder); // Post (Cột chống đứng)
            var sym4 = GetOrLoadFamilySymbol(doc, "4", opts.FamilyFolder); // Ledger / Transom (Giằng ngang/dọc)
            var sym5 = GetOrLoadFamilySymbol(doc, "5", opts.FamilyFolder); // Diagonal Brace (Giằng chéo)
            var sym6 = GetOrLoadFamilySymbol(doc, "6", opts.FamilyFolder); // Plank (Mâm thao tác)
            var sym7 = GetOrLoadFamilySymbol(doc, "7", opts.FamilyFolder); // Toe Board (Ván chắn biên)

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
            // BƯỚC 3: DỰNG 2 HÀNG CỘT CHO DÃY CHÍNH (4 CỘT CƠ BẢN)
            // Cột trước Y = y0, Cột sau Y = y0 - W.
            // Đầu hồi trái X = x0 (k=0), Đầu hồi phải X = xMax (k=1).
            // =========================================================================
            List<double> colX = new List<double> { x0, xMax };
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            // =========================================================================
            // BƯỚC 4: DỰNG CẤU KIỆN DÃY CHÍNH MẶT SAU (Y = y0 - W) ĐẦY ĐỦ NHỊP L1
            // Mặt sau không bị vướng cửa ngõ nên chạy đủ chiều dài L1 = 1800 mm.
            // =========================================================================
            double yBack = y0 - W;

            // 4.1. Ván lót đáy mặt sau (Family 1)
            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0), MmToFt(yBack), MmToFt(z0)), 0, "l", L_bayMain);
            if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);

            // 4.2. Giằng chân mặt sau tại h1 (Family 4)
            var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(yBack), MmToFt(zH1)), 0, "l", L_bayMain);
            if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

            // 4.3. Giằng đỉnh mặt sau tại topZ (Family 4, nếu bật HasTopTransom)
            if (opts != null && opts.HasTopTransom)
            {
                var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(yBack), MmToFt(topZ)), 0, "l", L_bayMain);
                if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
            }

            // 4.4. Giằng chéo, ván chắn và mâm sàn mặt sau theo từng tầng
            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;

                // Giằng chéo mặt sau (Family 5)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, x0, yBack, 0, L_bayMain, z0, jackH, s, storyCount, opts);

                // Ván chắn biên sau (Family 7)
                if (sym7 != null)
                {
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0), MmToFt(yBack + 66), MmToFt(zDeck + sOff)), 0, "l", L_bayMain);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }

                // 2 mâm thao tác nhịp chính (Family 6, rộng 500 mm)
                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0), MmToFt(y0 - 345), MmToFt(zDeck + sOff)), 0, "l", L_bayMain);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0), MmToFt(yBack + 345), MmToFt(zDeck + sOff)), 0, "l", L_bayMain);
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // =========================================================================
            // BƯỚC 5: CẤY CỘT PHỤ TẠI X = xC TRÊN MÉP TRƯỚC (Y = y0)
            // Nhằm tạo khung cửa ngõ mở thông từ xC đến xMax có độ rộng đúng bằng W.
            // =========================================================================
            if (L_cut > 10)
            {
                // 5.1. Kích chân cột cấy
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

                // 5.2. Các phân đoạn cột chống tại xC
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0, z0 + jackH, opts);

                // =====================================================================
                // BƯỚC 6: CẤU KIỆN MÉP TRƯỚC (Y = y0) THU NGẮN VỀ L_cut (x0 -> xC)
                // =====================================================================
                // 6.1. Ván lót đáy mép trước thu ngắn (L = L_cut)
                var bbFrontCut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
                if (bbFrontCut != null) placedIds.Add(bbFrontCut.Id.IntegerValue);

                // 6.2. Giằng chân mép trước tại h1 thu ngắn (L = L_cut)
                var lgFFrontCut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH1)), 0, "l", L_cut);
                if (lgFFrontCut != null) placedIds.Add(lgFFrontCut.Id.IntegerValue);

                // 6.3. Giằng đỉnh mép trước tại topZ (nếu có)
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFrontCut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTopFrontCut != null) placedIds.Add(lgTopFrontCut.Id.IntegerValue);
                }

                // 6.4. Giằng giữa, lan can an toàn & ván chắn mép trước theo từng tầng
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;

                    // Giằng giữa h3 (L = L_cut)
                    var lgMidFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH3 + sOff)), 0, "l", L_cut);
                    if (lgMidFront != null) placedIds.Add(lgMidFront.Id.IntegerValue);

                    // Lan can dưới h6 (L = L_cut)
                    var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH6 + sOff)), 0, "l", L_cut);
                    if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

                    // Lan can trên h7 (L = L_cut)
                    var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH7 + sOff)), 0, "l", L_cut);
                    if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

                    // Ván chắn biên mép trước (L = L_cut)
                    if (sym7 != null)
                    {
                        var tbFrontCut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0), MmToFt(y0 - 66), MmToFt(zDeck + sOff)), 0, "l", L_cut);
                        if (tbFrontCut != null) placedIds.Add(tbFrontCut.Id.IntegerValue);
                    }
                }
            }

            // =========================================================================
            // BƯỚC 7: DỰNG 2 CỘT ĐỈNH CHO NHÁNH RẼ VƯƠN SANG +Y TẠI yTop
            // Nhánh rẽ dùng lại 2 cột gốc tại (xC, y0) và (xMax, y0).
            // Dựng mới 2 cột đỉnh tại (xC, yTop) và (xMax, yTop).
            // =========================================================================
            // 7.1. Kích chân 2 cột đỉnh nhánh rẽ
            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.IntegerValue);
            if (jkR2 != null) placedIds.Add(jkR2.Id.IntegerValue);

            // 7.2. Cột chống 2 cột đỉnh nhánh rẽ
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yTop, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yTop, z0 + jackH, opts);

            // =========================================================================
            // BƯỚC 8: VÁN LÓT ĐÁY VÀ HỆ GIẰNG CHÂN/ĐỈNH CHO NHÁNH RẼ
            // Thanh dọc chạy theo phương Y (góc quay 90°, dài L2).
            // Thanh ngang chạy theo phương X (góc quay 0°, dài W).
            // =========================================================================
            // 8.1. Ván lót đáy 2 bên vách nhánh rẽ (Family 1, quay 90°, dài L2)
            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            // 8.2. Giằng chân h1 nhánh rẽ (Family 4)
            // - 2 thanh dọc hai bên (dài L2, quay 90°)
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1)), 90, "l", L_return);
            // - 2 thanh ngang đáy & đỉnh (dài W, quay 0°)
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH1)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // 8.3. Giằng đỉnh nhánh rẽ tại topZ (nếu bật HasTopTransom)
            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
            }

            // =========================================================================
            // BƯỚC 9: HỆ GIẰNG CAO ĐỘ, MÂM SÀN, VÁN CHẮN VÀ LAN CAN CHO NHÁNH RẼ
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;

                // 9.1. Giằng dọc giữa h3 vách trong X = xC (dài L2, quay 90°)
                var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH3 + sOff)), 90, "l", L_return);
                if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

                // 9.2. Giằng chéo vách ngoài X = xMax (Family 5, dài L2, quay 90°)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, y0, 90, L_return, z0, jackH, s, storyCount, opts);

                // 9.3. Hai giằng ngang đỡ mâm sàn tại zDeck (dài W, quay 0°)
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zDeck + sOff)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck + sOff)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
                if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                // 9.4. Mâm sàn thao tác nhánh rẽ (Family 6, 2 mâm song song quay 90°, dài L2)
                if (sym6 != null)
                {
                    var plR1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + 345), MmToFt(y0), MmToFt(zDeck + sOff)), 90, "l", L_return);
                    if (plR1 != null) { SetParam(plR1, "w", 500); placedIds.Add(plR1.Id.IntegerValue); }

                    var plR2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(y0), MmToFt(zDeck + sOff)), 90, "l", L_return);
                    if (plR2 != null) { SetParam(plR2, "w", 500); placedIds.Add(plR2.Id.IntegerValue); }
                }

                // 9.5. Ván chắn biên nhánh rẽ (Family 7)
                if (sym7 != null)
                {
                    // Ván chắn biên trái X = xC (dài L2, quay 90°)
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(y0), MmToFt(zDeck + sOff)), 90, "l", L_return);
                    // Ván chắn biên phải X = xMax (dài L2, quay 90°)
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck + sOff)), 90, "l", L_return);
                    // Ván chắn đầu hồi đỉnh Y = yTop (dài W, quay 0°)
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck + sOff)), 0, "l", W_return);
                    if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }

                // 9.6. Lan can an toàn kép h6 & h7
                // - Lan can dưới h6: vách trái (dài L2) và đầu hồi đỉnh (dài W)
                var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH6 + sOff)), 90, "l", L_return);
                var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH6 + sOff)), 0, "l", W_return);
                if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

                // - Lan can trên h7: vách trái (dài L2) và đầu hồi đỉnh (dài W)
                var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH7 + sOff)), 90, "l", L_return);
                var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH7 + sOff)), 0, "l", W_return);
                if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
            }

            return null; // Thành công không có lỗi
        }
    }
}
```

---

## 3. Bảng Kiểm Tra & Nghiệm Thu Số Lượng Cấu Kiện Mẫu

Khi kiểm tra với cấu hình mặc định ($L_1 = 1800$, $L_2 = 1800$, $W = 1200$, 1 tầng):

| Nhóm Cấu Kiện | Vị Trí Cụ Thể | Family | Số Lượng |
| :--- | :--- | :--- | :---: |
| **Kích chân (Jack)** | 4 cột chính + 1 cột cấy + 2 cột nhánh rẽ | Family 2 | **7** |
| **Cột chống (Post)** | 7 vị trí chân cột (mỗi vị trí dựng post) | Family 3 | **7** |
| **Ván lót đáy (Base board)** | Dãy chính (1 sau $1800$, 1 trước $600$) + Nhánh rẽ ($2 \times 1800$) | Family 1 | **4** |
| **Thanh giằng & Lan can** | Chân h1, giữa h3, đỉnh/mâm, lan can kép h6 & h7 | Family 4 | **26** |
| **Giằng chéo (Brace)** | 1 giằng chéo sau dãy chính + 1 giằng chéo ngoài nhánh rẽ | Family 5 | **2** |
| **Mâm thao tác (Plank)** | 2 mâm dãy chính ($1800$) + 2 mâm nhánh rẽ ($1800$) | Family 6 | **4** |
| **Ván chắn biên (Toe board)** | Dãy chính (1 sau, 1 trước thu) + Nhánh rẽ (2 bên, 1 đỉnh) + Đầu hồi | Family 7 | **5** |
| **TỔNG CỘNG** | **Khớp chính xác 100% với hệ thống** | **F1 $\to$ F7** | **55** |

---

## 4. Hướng Dẫn Tùy Chỉnh

Bạn có thể mở tệp này để:
1. Xem chi tiết vị trí đặt từng cấu kiện theo 3 trục $X, Y, Z$.
2. Điều chỉnh lại hướng quay (Rotation `0` hoặc `90` độ).
3. Đổi cao độ các tầng Rosette hoặc bù trừ khoảng hở (`offset`).
4. Khi chỉnh sửa xong, báo lại để Agent cập nhật trực tiếp vào file mã nguồn [RevitScaffoldingTool/ScaffoldingBuilder.Cases1.cs](file:///c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/RevitScaffoldingTool/ScaffoldingBuilder.Cases1.cs).
