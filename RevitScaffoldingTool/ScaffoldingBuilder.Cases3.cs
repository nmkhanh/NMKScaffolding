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
        private static string ExecuteCase3_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> l1Bays = GetL1Bays(opts);
            List<double> l2Bays = GetL2Bays(opts);
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);
            double zH1 = z0 + jackH + 135.0;

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
            // PHẦN A: DÃY GIÀN CHÍNH (CHẠY LIÊN TỤC QUA NGÃ BA)
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

            // Dựng các nhịp tiêu chuẩn của dãy chính
            for (int i = 0; i < nMainBays; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            // =========================================================================
            // PHẦN B: ĐIỂM NGÃ BA & CỘT CẤY PHỤ TẠI MÉP TRƯỚC (Y = y0)
            // =========================================================================
            // Vị trí ngã ba: mặc định tại điểm nối giữa nhịp 1 và nhịp 2 (nếu có >= 2 nhịp)
            double xJunc = (nMainBays >= 2) ? colX[1] : colX[0];
            double xR = xJunc + W; // Vách phải của nhánh rẽ chữ T

            // Cấy thêm 1 cột phụ tại (xR, y0) nếu chưa trùng với cột nào của dãy chính
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

            // Giằng ngang chân tại cửa ngõ tiếp giáp [xJunc, xR]
            var trRF_Junc = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0), MmToFt(zH1)), 0, "l", W);
            if (trRF_Junc != null) placedIds.Add(trRF_Junc.Id.GetIdValue());

            // =========================================================================
            // PHẦN C: DỰNG CÁC CẶP CỘT TIẾP THEO CHO NHÁNH RẼ B VỀ PHÍA +Y
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
            // PHẦN D: DỰNG CẤU KIỆN TỪNG NHỊP CỦA NHÁNH RẼ B
            // =========================================================================
            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                // Giằng chân h1
                var lgLeft_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                var lgRight_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                if (lgLeft_H1 != null) placedIds.Add(lgLeft_H1.Id.GetIdValue());
                if (lgRight_H1 != null) placedIds.Add(lgRight_H1.Id.GetIdValue());

                // Ván lót đáy chân giàn
                var bbLeft = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(z0)), 90, "l", bLen);
                var bbRight = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(z0)), 90, "l", bLen);
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
                    var lgRight_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    if (lgLeft_H3 != null) placedIds.Add(lgLeft_H3.Id.GetIdValue());
                    if (lgRight_H3 != null) placedIds.Add(lgRight_H3.Id.GetIdValue());

                    // Lan can an toàn kép h6, h7 vách trái & vách phải
                    var lgLeft_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgLeft_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    var lgRight_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgRight_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgLeft_H6 != null) placedIds.Add(lgLeft_H6.Id.GetIdValue());
                    if (lgLeft_H7 != null) placedIds.Add(lgLeft_H7.Id.GetIdValue());
                    if (lgRight_H6 != null) placedIds.Add(lgRight_H6.Id.GetIdValue());
                    if (lgRight_H7 != null) placedIds.Add(lgRight_H7.Id.GetIdValue());

                    // Ván chắn biên Family 7
                    if (sym7 != null)
                    {
                        var tbLeft = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc + 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        var tbRight = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
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
            // PHẦN E: ĐÓNG ĐẦU HỒI ĐỈNH NHÁNH RẼ (Y = yTop)
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

            return string.Format("Dựng thành công TH 3.1: Khớp nối ngã ba chữ T (T-Junction) tại X = {0:F0} mm (Nhánh rẽ vươn đến Y = {1:F0} mm).", xJunc, yTop);
        }
    }
}
