# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 3.3 (TH 3.3) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 3.3: Khớp nối ngã ba chữ T (T-Junction) - Nhánh rẽ lệch phải trong nhịp tiêu chuẩn 1800 mm (Cột phải dùng chung, cấy cột phụ mép trái tại $X_L = X_{\text{end}} - W = X_{\text{start}} + 600\text{ mm}$)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để kiểm toán và nghiệm thu dễ dàng.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 3.3

```
                    Y (+)
                     ▲
                     │ (Đường xanh dọc L1)
                     │
                     │          (Cột R2 - Cấy)      (Cột R1 - Chung)
                     │          (xL, yTop)          (xR, yTop)
                     │             ┌───────────────────┐ (Đầu hồi đỉnh nhánh B)
                     │             │     HỘP ĐỎ        │
                     │             │    Nhánh rẽ B     │ (Nhánh rẽ chữ T nằm
                     │             │   (L_branch x W)  │  về bên phải đường L1)
                     │             │ (1800 x 1200 mm)  │
                     │ (xL, y0)    │                   │ (xR, y0)
     ────────────────┼──────────[CỘT CẤY]────────────[CỘT CHUNG]────────────► X (+)
     (x0, y0)        │             │◄─────── W ───────►│ (Đường xanh ngang L0)
        ┌────────────┴─────────────┼───────────────────┤────────────┐
        │        HỘP ĐEN 1         │     HỘP ĐEN 2     │ HỘP ĐEN 3  │
        │       (Nhịp trước)       │  (Nhịp ngã ba)    │ (Nhịp sau) │
        │         1800 x W         │     1800 x W      │  1800 x W  │
        │                          │◄600►│◄── 1200 ───►│            │
        └──────────────────────────┴─────┴─────────────┴────────────┘
     (x0, y0 - W)             (xStart, y0-W)        (xR, y0-W)   (xEnd, y0-W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba (mặc định: `1800, 1800, 1800`).
- `L2Bays`: Danh sách nhịp của nhánh rẽ chữ T vươn sang $+Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ Then Chốt:
1. **Vị trí nhịp ngã ba của dãy chính**:
   Giả sử nhịp ngã ba là Nhịp 2 ($X \in [x_{\text{start}}, x_{\text{end}}] = [x_0 + 1800, x_0 + 3600]$), chiều dài $1800\text{ mm}$.
   - Điểm đầu nhịp: $x_{\text{start}} = x_0 + 1800\text{ mm}$.
   - Điểm cuối nhịp: $x_{\text{end}} = x_{\text{start}} + 1800 = x_0 + 3600\text{ mm}$.
2. **Nhánh rẽ lệch phải**:
   - Vách phải nhánh rẽ: $x_R = x_{\text{end}}$ (Trùng khớp 100% với cột hiện hữu của dãy chính $\implies$ **CỘT DÙNG CHUNG**).
   - Vách trái nhánh rẽ: $x_L = x_R - W = x_{\text{end}} - 1200 = x_{\text{start}} + 600\text{ mm}$ ($\implies$ **CỘT CẤY PHỤ**).
3. **Khoảng hở $600\text{ mm}$ phía trước dãy chính ($X \in [x_{\text{start}}, x_L]$)**:
   - Giằng chân $h_1$ Family 4 ($L = 600\text{ mm}$).
   - Lan can kép Family 4 tại $h_6$ ($Z = 2430\text{ mm}$) và $h_7$ ($Z = 2880\text{ mm}$).
   - Giằng giữa Family 4 tại $h_3$ ($Z = 1080\text{ mm}$) và ván chắn biên Family 7 ($L = 600\text{ mm}$).
4. **Cửa ngõ thông suốt $1200\text{ mm}$ ($X \in [x_L, x_R]$ tại $Y = y_0$)**:
   - Mở hoàn toàn $100\%$, không có lan can $h_3, h_6, h_7$ và không có ván chắn biên cản trở.
   - Giằng chân $h_1$ Family 4 ($L = 1200\text{ mm}$) khóa cứng chân cột cấy và cột dùng chung.
   - Mâm sàn thao tác Family 6 phẳng liên tục tại cao độ $h_5$ ($Z = 1980\text{ mm}$).
5. **Đỉnh vươn của nhánh rẽ B**:
   $$y_{\text{Top}} = y_0 + L_{\text{branch}}$$
   - Hai cột đỉnh nhánh rẽ nằm tại $(x_L, y_{\text{Top}})$ và $(x_R, y_{\text{Top}})$.

### Quy Tắc Bất Biến Toàn Hệ Giàn (Universal Structural Rule):
- **Mặt ngoài (Outer Face)**:
  - Dãy chính: Mặt sau ($Y = y_0 - W$).
  - Nhánh rẽ B: Vách phải ($X = x_R$).
  - $\implies$ **Bắt buộc bố trí Giằng chéo Family 5** (qua `PlaceBayDiagonalBraces`), ván chắn biên Family 7. Tuyệt đối không dùng lan can giằng dọc trên mặt ngoài.
- **Mặt trong (Inner Face)**:
  - Dãy chính: Mặt trước ($Y = y_0$) hướng vào công trình.
  - Nhánh rẽ B: Vách trái ($X = x_L$) hướng vào công trình.
  - $\implies$ **Bố trí Giằng dọc Family 4** (lan can bảo vệ kép $h_6, h_7$, giằng giữa $h_3$).
- **Giằng Đỉnh (`HasTopTransom`)**:
  - Khi kích hoạt, đặt giằng đỉnh Family 4 tại cao độ đỉnh cột `topZ` cho cả mặt trước (đoạn cửa ngõ và đoạn gap), mặt sau dãy chính, hai vách của nhánh rẽ B và các thanh giằng ngang đỉnh tại từng bước cột.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (ScaffoldingBuilder.Cases3.cs)

Hàm xử lý `ExecuteCase3_3` được triển khai chuẩn hóa theo 8 bước:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 3.3: KHỚP NỐI NGÃ BA CHỮ T (T-JUNCTION) - LỆCH PHẢI TRONG NHỊP 1800 MM
        /// Dãy chính chạy thẳng liên tục qua ngã ba; Nhánh rẽ B mọc từ vị trí lệch phải của nhịp ngã ba:
        /// - Cột mép phải (xR, y0) là CỘT DÙNG CHUNG trùng với cột cuối nhịp dãy chính.
        /// - Cột mép trái (xL, y0) là CỘT CẤY PHỤ tại xL = xR - W = xStart + 600 mm.
        /// - Khoảng [xStart, xL] (600 mm) có đầy đủ lan can an toàn và ván chắn biên.
        /// - Khoảng [xL, xR] (1200 mm) mở cửa ngõ thông suốt 100% làm lối đi.
        /// </summary>
        public static string ExecuteCase3_3(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // BƯỚC 1: Khởi tạo thông số hình học & cao độ
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;

            List<double> l1Bays = GetL1Bays(opts);
            if (l1Bays == null || l1Bays.Count < 3)
            {
                l1Bays = new List<double> { 1800.0, 1800.0, 1800.0 };
            }
            List<double> l2Bays = GetL2Bays(opts);
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);
            double zH1 = z0 + jackH + 135.0;

            // BƯỚC 2: Tải và kích hoạt Families
            var sym1 = GetOrLoadFamilySymbol(doc, "1", opts.FamilyFolder);
            var sym2 = GetOrLoadFamilySymbol(doc, "2", opts.FamilyFolder);
            var sym3 = GetOrLoadFamilySymbol(doc, "3", opts.FamilyFolder);
            var sym4 = GetOrLoadFamilySymbol(doc, "4", opts.FamilyFolder);
            var sym5 = GetOrLoadFamilySymbol(doc, "5", opts.FamilyFolder);
            var sym6 = GetOrLoadFamilySymbol(doc, "6", opts.FamilyFolder);
            var sym7 = GetOrLoadFamilySymbol(doc, "7", opts.FamilyFolder);

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

            // BƯỚC 3: Dựng dãy chính A
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            // BƯỚC 4: Xác định nhịp ngã ba và cấy cột phụ mép trái
            int juncBayIdx = (nMainBays >= 2) ? 1 : 0;
            double xStart = colX[juncBayIdx];
            double xEnd = colX[juncBayIdx + 1];
            double xR = xEnd; // Cột phải dùng chung
            double xL = xR - W; // Cột cấy mép trái (xStart + 600 mm)
            double cutFrontLen = xL - xStart; // 600 mm

            // Cấy cột phụ tại (xL, y0)
            bool hasColAtXL = false;
            foreach (var cx in colX)
            {
                if (Math.Abs(cx - xL) < 10.0) { hasColAtXL = true; break; }
            }
            if (!hasColAtXL)
            {
                var jkL = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkL != null) placedIds.Add(jkL.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xL, y0, z0 + jackH, opts);
            }

            // BƯỚC 5: Dựng các nhịp tiêu chuẩn dãy chính & xử lý nhịp ngã ba
            for (int i = 0; i < nMainBays; i++)
            {
                if (i != juncBayIdx)
                {
                    BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                            colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
                }
                else
                {
                    double bayLen = l1Bays[i];
                    double px = colX[i];

                    // Mặt sau: Ván lót chân + Giằng chân + Giằng chéo Family 5 + Mâm sàn + Ván chắn biên
                    var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", bayLen);
                    var lgH1Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", bayLen);
                    if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                    if (lgH1Back != null) placedIds.Add(lgH1Back.Id.GetIdValue());

                    // Mặt trước:
                    // 1) Đoạn [xStart, xL] (600 mm):
                    if (cutFrontLen > 10.0)
                    {
                        var bbFrontCut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "l", cutFrontLen);
                        var lgH1FrontCut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", cutFrontLen);
                        if (bbFrontCut != null) placedIds.Add(bbFrontCut.Id.GetIdValue());
                        if (lgH1FrontCut != null) placedIds.Add(lgH1FrontCut.Id.GetIdValue());
                    }

                    // 2) Đoạn cửa ngõ [xL, xR] (1200 mm): Giằng chân h1
                    var lgH1Junc = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(zH1)), 0, "l", W);
                    if (lgH1Junc != null) placedIds.Add(lgH1Junc.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        // Giằng chéo mặt sau Family 5
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, bayLen, z0, jackH, s, storyCount, opts);

                        // Mâm sàn thao tác chạy suốt 1800 mm
                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                        }

                        // Ván chắn biên mặt sau
                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 66.0), MmToFt(zDeck)), 0, "l", bayLen);
                            if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                        }

                        // Lan can an toàn kép & ván chắn biên cho đoạn 600 mm mặt trước
                        if (cutFrontLen > 10.0)
                        {
                            var lgH3Cut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH3)), 0, "l", cutFrontLen);
                            var lgH6Cut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), 0, "l", cutFrontLen);
                            var lgH7Cut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), 0, "l", cutFrontLen);
                            if (lgH3Cut != null) placedIds.Add(lgH3Cut.Id.GetIdValue());
                            if (lgH6Cut != null) placedIds.Add(lgH6Cut.Id.GetIdValue());
                            if (lgH7Cut != null) placedIds.Add(lgH7Cut.Id.GetIdValue());

                            if (sym7 != null)
                            {
                                var tbCut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - 66.0), MmToFt(zDeck)), 0, "l", cutFrontLen);
                                if (tbCut != null) placedIds.Add(tbCut.Id.GetIdValue());
                            }
                        }
                    }
                }
            }

            // BƯỚC 6: Dựng nhánh rẽ B vươn về phía +Y
            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                double nextY = curY + bLen;

                // Cặp cột tại nextY: (xL, nextY) và (xR, nextY)
                var jkL = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xL), MmToFt(nextY), MmToFt(z0)), 0, "h", jackH);
                var jkR = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(nextY), MmToFt(z0)), 0, "h", jackH);
                if (jkL != null) placedIds.Add(jkL.Id.GetIdValue());
                if (jkR != null) placedIds.Add(jkR.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xL, nextY, z0 + jackH, opts);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, nextY, z0 + jackH, opts);

                // Giằng ngang đỡ mâm tại nextY
                var tr = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(nextY), MmToFt(zH1)), 0, "l", W);
                if (tr != null) placedIds.Add(tr.Id.GetIdValue());

                // Ván lót chân dọc 2 vách trái và phải
                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(z0)), 90, "l", bLen);
                var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(z0)), 90, "l", bLen);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());

                // Giằng chân h1 dọc 2 vách
                var lgH1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH1)), 90, "l", bLen);
                var lgH1R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(zH1)), 90, "l", bLen);
                if (lgH1L != null) placedIds.Add(lgH1L.Id.GetIdValue());
                if (lgH1R != null) placedIds.Add(lgH1R.Id.GetIdValue());

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zH5 = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    // Giằng ngang đỡ mâm h5 tại nextY
                    var trH5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(nextY), MmToFt(zH5)), 0, "l", W);
                    if (trH5 != null) placedIds.Add(trH5.Id.GetIdValue());

                    // Lan can an toàn kép dọc vách trái (xL) và vách phải (xR)
                    var lgH3L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH3)), 90, "l", bLen);
                    var lgH6L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH6)), 90, "l", bLen);
                    var lgH7L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgH3L != null) placedIds.Add(lgH3L.Id.GetIdValue());
                    if (lgH6L != null) placedIds.Add(lgH6L.Id.GetIdValue());
                    if (lgH7L != null) placedIds.Add(lgH7L.Id.GetIdValue());

                    var lgH3R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(zH3)), 90, "l", bLen);
                    var lgH6R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(zH6)), 90, "l", bLen);
                    var lgH7R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgH3R != null) placedIds.Add(lgH3R.Id.GetIdValue());
                    if (lgH6R != null) placedIds.Add(lgH6R.Id.GetIdValue());
                    if (lgH7R != null) placedIds.Add(lgH7R.Id.GetIdValue());

                    // Mâm sàn thao tác nhánh rẽ B (chạy dọc từ curY -> nextY)
                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xL + pOff), MmToFt(curY), MmToFt(zH5)), 90, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - pOff), MmToFt(curY), MmToFt(zH5)), 90, "l", bLen);
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                    }

                    // Ván chắn biên 2 vách
                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xL + 66.0), MmToFt(curY), MmToFt(zH5)), 90, "l", bLen);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0), MmToFt(curY), MmToFt(zH5)), 90, "l", bLen);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                    }
                }

                curY = nextY;
            }

            // BƯỚC 7: Đóng kín đầu hồi đỉnh nhánh rẽ B (tại Y = curY)
            var trEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH1)), 0, "l", W);
            if (trEnd1 != null) placedIds.Add(trEnd1.Id.GetIdValue());

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830.0 + sOff;
                double zH5 = z0 + jackH + 1730.0 + sOff;
                double zH6 = z0 + jackH + 2180.0 + sOff;
                double zH7 = z0 + jackH + 2630.0 + sOff;

                var trH3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH3)), 0, "l", W);
                var trH5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH5)), 0, "l", W);
                var trH6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH6)), 0, "l", W);
                var trH7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(zH7)), 0, "l", W);
                if (trH3 != null) placedIds.Add(trH3.Id.GetIdValue());
                if (trH5 != null) placedIds.Add(trH5.Id.GetIdValue());
                if (trH6 != null) placedIds.Add(trH6.Id.GetIdValue());
                if (trH7 != null) placedIds.Add(trH7.Id.GetIdValue());

                if (sym7 != null)
                {
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xL), MmToFt(curY - 66.0), MmToFt(zH5)), 0, "l", W);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                }
            }

            return null; // Thành công
        }
    }
}
```

---

## 3. Bảng Đối Chiếu So Sánh 3 Trường Hợp Ngã Ba Chữ T (TH 3.1 vs TH 3.2 vs TH 3.3)

| Đặc Điểm So Sánh | TH 3.1 (Lệch Trái) | TH 3.2 (Ô Vuông) | TH 3.3 (Lệch Phải) |
| :--- | :--- | :--- | :--- |
| **Chiều dài nhịp ngã ba trên dãy chính** | Tiêu chuẩn $1800\text{ mm}$ | Ô vuông $W = 1200\text{ mm}$ | Tiêu chuẩn $1800\text{ mm}$ |
| **Vị trí nhánh rẽ B ($W = 1200\text{ mm}$)** | Đoạn đầu nhịp $[0, 1200\text{ mm}]$ | Toàn bộ nhịp $[0, 1200\text{ mm}]$ | Đoạn cuối nhịp $[600, 1800\text{ mm}]$ |
| **Cột mép trái nhánh rẽ ($X_L$)** | **Cột dùng chung** tại $x_{\text{start}}$ | **Cột dùng chung** tại $x_{\text{start}}$ | **Cột cấy phụ** tại $x_{\text{start}} + 600\text{ mm}$ |
| **Cột mép phải nhánh rẽ ($X_R$)** | **Cột cấy phụ** tại $x_{\text{start}} + 1200\text{ mm}$ | **Cột dùng chung** tại $x_{\text{end}}$ | **Cột dùng chung** tại $x_{\text{end}}$ |
| **Số lượng cột cấy phụ cần thêm** | **1 cột** | **0 cột** | **1 cột** |
| **Khoảng hở $600\text{ mm}$ có lan can an toàn** | Phía bên phải $[1200, 1800\text{ mm}]$ | Không có | Phía bên trái $[0, 600\text{ mm}]$ |
| **Cửa ngõ thông suốt $1200\text{ mm}$** | $[0, 1200\text{ mm}]$ | $[0, 1200\text{ mm}]$ | $[600, 1800\text{ mm}]$ |
| **Mâm sàn thao tác** | Nối tiếp phẳng lỳ tại cao độ $h_5$ ($1980\text{ mm}$) | Nối tiếp phẳng lỳ tại cao độ $h_5$ ($1980\text{ mm}$) | Nối tiếp phẳng lỳ tại cao độ $h_5$ ($1980\text{ mm}$) |
