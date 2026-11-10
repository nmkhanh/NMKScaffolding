# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 3.2 (TH 3.2) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 3.2: Khớp nối ngã ba chữ T (T-Junction) - Nhịp tại ngã ba là Ô Vuông $W \times W = 1200 \times 1200$ mm (Không cấy cột phụ)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 3.2

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
     ────────────────┼─[Cột Chung]─┴───[Cột Chung]─┴──────────────► X (+)
     (x0, y0)        │      │◄─────── W ──────────►│ (Đường xanh ngang)
        ┌────────────┴──────┼──────────────────────┤────────────┐
        │    HỘP ĐEN 1      │   Ô VUÔNG GÓC NGÃ BA │ HỘP ĐEN 3  │
        │   (Nhịp trước)    │      HỘP ĐEN 2       │ (Nhịp sau) │
        │     L1 x W        │        W x W         │   L3 x W   │
        │   (1800 x 1200)   │    (1200 x 1200 mm)  │(1800 x 1200│
        └───────────────────┴──────────────────────┴────────────┘
     (x0, y0 - W)       (xJunc, y0 - W)        (xR, y0 - W) (xEnd, y0 - W)
                         DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC QUA NGÃ BA)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy chính A.
- `L1Bays`: Danh sách nhịp của dãy chính chạy thẳng qua ngã ba, trong đó **nhịp tại ngã ba có chiều dài $L_{\text{junc}} = W = 1200$ mm** (ví dụ: `1800, 1200, 1800`).
- `L2Bays`: Danh sách nhịp của nhánh rẽ chữ T vươn sang $+Y$ (ví dụ: `1800` mm).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc từ Data).

### Công Thức Tọa Độ Then Chốt:
1. **Ô vuông góc ngã ba hoàn hảo**:
   $$L_{\text{junc}} = W = 1200\text{ mm}, \quad x_{\text{Junc}} = x_0 + L_1, \quad x_R = x_{\text{Junc}} + W$$
2. **Không cần cấy cột phụ (100% Cột dùng chung)**:
   - Cột chân trái $(x_{\text{Junc}}, y_0)$: Trùng khớp hoàn toàn với Cột mép trước trái của ô vuông $1200 \times 1200$.
   - Cột chân phải $(x_R, y_0)$: Trùng khớp hoàn toàn với Cột mép trước phải của ô vuông $1200 \times 1200$.
   - Cả 2 cột đều tận dụng đĩa rosette ALINCO 8 lỗ tiêu chuẩn, không cần cấy thêm bất kỳ cột phụ nào trên dãy chính!
3. **Cửa ngõ thông suốt $1200$ mm**:
   Đoạn $X \in [x_{\text{Junc}}, x_R]$ tại $Y = y_0$ mở trọn vẹn $1200$ mm.
   - **TUYỆT ĐỐI KHÔNG LẮP**: Lan can kép Family 4 ($h_6, h_7$) và Ván chắn biên Family 7 tại cửa ngõ này.
   - Mâm sàn thao tác Family 6 tại $h_5$ ($Z = 1980$ mm) nối tiếp phẳng $100\%$ giữa ô vuông ngã ba và nhánh rẽ.
4. **Đỉnh vươn của nhánh rẽ B**:
   $$y_{\text{Top}} = y_0 + L_{\text{branch}}$$
   - Hai cột đỉnh nhánh rẽ nằm tại $(x_{\text{Junc}}, y_{\text{Top}})$ và $(x_R, y_{\text{Top}})$.
5. **Bảo vệ an toàn các vách nhánh rẽ**:
   - Vách trái ($X = x_{\text{Junc}}$): Lan can an toàn kép Family 4 ($h_6, h_7$), giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
   - Vách phải ($X = x_R$): Lan can an toàn kép Family 4 ($h_6, h_7$), giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
   - Đầu hồi đỉnh ($Y = y_{\text{Top}}$): Khóa kín an toàn nếu là nhịp kết thúc.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (ScaffoldingBuilder.Cases3.cs)

Hàm xử lý `ExecuteCase3_2` được bóc tách theo 8 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 3.2: KHỚP NỐI NGÃ BA CHỮ T (T-JUNCTION) - Ô VUÔNG GÓC W x W (1200x1200 MM)
        /// Dãy chính chạy thẳng liên tục qua ngã ba với nhịp ngã ba bằng đúng W = 1200 mm.
        /// Nhánh rẽ B mọc từ ô vuông vươn sang +Y, tựa trực tiếp lên 2 cột trước của ô vuông mà KHÔNG CẦN CẤY CỘT PHỤ.
        /// </summary>
        public static string ExecuteCase3_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO CÁC THÔNG SỐ HÌNH HỌC & CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            
            // Dãy chính gồm 3 nhịp: [1800, 1200, 1800] (nhịp giữa là ô vuông góc ngã ba)
            List<double> l1Bays = GetL1Bays(opts);
            if (l1Bays == null || l1Bays.Count < 3)
            {
                l1Bays = new List<double> { 1800.0, W, 1800.0 };
            }
            List<double> l2Bays = GetL2Bays(opts); // Nhánh rẽ (ví dụ: [1800])

            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);
            double zH1 = z0 + jackH + 135.0;

            // =========================================================================
            // BƯỚC 2: TẢI & KÍCH HOẠT CÁC FAMILY SYMBOL (FAMILY 1 ĐẾN 7)
            // =========================================================================
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

            // =========================================================================
            // BƯỚC 3: DỰNG DÃY GIÀN CHÍNH (CHẠY THẲNG LIÊN TỤC VỚI Ô VUÔNG 1200x1200 Ở GIỮA)
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

            // Dựng các nhịp tiêu chuẩn của dãy chính (Mặt trước nhịp ngã ba để mở cửa ngõ)
            for (int i = 0; i < nMainBays; i++)
            {
                double bayLen = l1Bays[i];
                double px = colX[i];
                bool isJuncBay = (Math.Abs(bayLen - W) < 10.0) && (i > 0 && i < nMainBays - 1);

                if (!isJuncBay)
                {
                    BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                            px, y0, z0, W, jackH, bayLen, true, opts);
                }
                else
                {
                    // Ô vuông ngã ba: Mặt sau (Y = y0 - W) có giằng chéo Family 5 và mâm sàn;
                    // Mặt trước (Y = y0) MỞ CỬA NGÕ SANG NHÁNH RẼ B (không có lan can và ván chắn)
                    PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", bayLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", bayLen);
                    PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", bayLen);

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;

                        // Giằng chéo mặt sau Family 5
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, bayLen, z0, jackH, s, storyCount, opts);

                        // 2 mâm sàn ô vuông ngã ba
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
                    }
                }
            }

            // =========================================================================
            // BƯỚC 4: XÁC ĐỊNH VỊ TRÍ Ô VUÔNG NGÃ BA
            // xJunc và xR là 2 cột mép trước của ô vuông ngã ba (khoảng cách đúng bằng W = 1200 mm)
            // =========================================================================
            double xJunc = colX[1];
            double xR    = colX[2]; // Đúng bằng xJunc + W

            // =========================================================================
            // BƯỚC 5: DỰNG CÁC CẶP CỘT TIẾP THEO CHO NHÁNH RẼ B VỀ PHÍA +Y
            // Hai cột chân đã có sẵn tại (xJunc, y0) và (xR, y0) -> KHÔNG CẤY CỘT PHỤ!
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
            // BƯỚC 6: DỰNG CẤU KIỆN TỪNG NHỊP CỦA NHÁNH RẼ B
            // =========================================================================
            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                // Giằng chân h1
                var lgLeft_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                var lgRight_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                if (lgLeft_H1 != null) placedIds.Add(lgLeft_H1.Id.GetIdValue());
                if (lgRight_H1 != null) placedIds.Add(lgRight_H1.Id.GetIdValue());

                // Ván lót đáy chân giàn
                var bbLeft = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(z0)), 90, "l", bLen);
                var bbRight = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(z0)), 90, "l", bLen);
                if (bbLeft != null) placedIds.Add(bbLeft.Id.GetIdValue());
                if (bbRight != null) placedIds.Add(bbRight.Id.GetIdValue());

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    // Giằng giữa h3 vách trái & vách phải
                    var lgLeft_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    var lgRight_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    if (lgLeft_H3 != null) placedIds.Add(lgLeft_H3.Id.GetIdValue());
                    if (lgRight_H3 != null) placedIds.Add(lgRight_H3.Id.GetIdValue());

                    // Lan can an toàn kép h6, h7 vách trái & vách phải
                    var lgLeft_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgLeft_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    var lgRight_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgRight_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR),    MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgLeft_H6 != null) placedIds.Add(lgLeft_H6.Id.GetIdValue());
                    if (lgLeft_H7 != null) placedIds.Add(lgLeft_H7.Id.GetIdValue());
                    if (lgRight_H6 != null) placedIds.Add(lgRight_H6.Id.GetIdValue());
                    if (lgRight_H7 != null) placedIds.Add(lgRight_H7.Id.GetIdValue());

                    // Ván chắn biên Family 7
                    if (sym7 != null)
                    {
                        var tbLeft = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc + 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        var tbRight = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0),    MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (tbLeft != null) placedIds.Add(tbLeft.Id.GetIdValue());
                        if (tbRight != null) placedIds.Add(tbRight.Id.GetIdValue());
                    }

                    // 2 Mâm thao tác Family 6 (rộng 500 mm)
                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xJunc + pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                    }

                    // Giằng ngang đỡ mâm tại cuối nhịp
                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY + bLen), MmToFt(zDeck)), 0, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.GetIdValue());
                }

                runY += bLen;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 0, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.GetIdValue());
            }

            // =========================================================================
            // BƯỚC 7: ĐÓNG ĐẦU HỒI ĐỈNH NHÁNH RẼ (Y = yTop)
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                double zH6 = z0 + jackH + 2180.0 + s * 1800.0;
                double zH7 = z0 + jackH + 2630.0 + s * 1800.0;

                var trTopDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(sDeck)), 0, "l", W);
                var trTopH6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH6)), 0, "l", W);
                var trTopH7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH7)), 0, "l", W);
                if (trTopDeck != null) placedIds.Add(trTopDeck.Id.GetIdValue());
                if (trTopH6 != null) placedIds.Add(trTopH6.Id.GetIdValue());
                if (trTopH7 != null) placedIds.Add(trTopH7.Id.GetIdValue());

                if (sym7 != null)
                {
                    var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc), MmToFt(yTop - 66.0), MmToFt(sDeck)), 0, "l", W);
                    if (tbTop != null) placedIds.Add(tbTop.Id.GetIdValue());
                }
            }

            return string.Format("Dựng thành công TH 3.2: Khớp nối ngã ba chữ T (Ô vuông 1200x1200 - Không cấy cột phụ) tại X = {0:F0} mm.", xJunc);
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Ô vuông ngã ba $1200 \times 1200$ mm**: Nhịp dãy chính tại ngã ba có chiều dài $L_{\text{junc}} = W = 1200$ mm.
- [x] **KHÔNG CẦN CẤY CỘT PHỤ (0 Cột Cấy)**: Hai cột chân của nhánh rẽ B trùng khớp 100% với 2 cột mép trước của ô vuông ngã ba.
- [x] **Cửa ngõ thông suốt $1200$ mm**: Tuyệt đối không lắp lan can kép $h_6, h_7$ hay ván chắn biên tại cửa ngõ tiếp giáp giữa dãy chính và nhánh B.
- [x] **Mặt sàn phẳng liên tục**: Mâm sàn thao tác của ô vuông và nhánh rẽ gặp nhau cùng cao độ $h_5$ ($Z = 1980$ mm).
- [x] **Vách trái & vách phải nhánh rẽ**: Bố trí lan can an toàn kép $h_6, h_7$, giằng giữa $h_3$, giằng chân $h_1$ và ván chắn biên Family 7.
- [x] **Đầu hồi đỉnh ($Y = y_{\text{Top}}$)**: Khóa kín an toàn bằng lan can kép, giằng ngang và ván chắn biên.
