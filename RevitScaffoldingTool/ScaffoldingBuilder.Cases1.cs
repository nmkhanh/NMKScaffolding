using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP1-TYPE1.1 - MÉP NGOÀI PHẲNG)
        /// </summary>
        private static string ExecuteCase1_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double L1 = opts.L1 > 0 ? opts.L1 : 1800.0;
            double L2 = opts.L2 > 0 ? opts.L2 : 1800.0;
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

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
            // PHẦN A: DÃY CHÍNH (1 NHỊP L1 X W)
            // =========================================================================
            double xMax = x0 + L1;
            List<double> colX = new List<double> { x0, xMax };

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            double xBay5 = x0;
            double L_bay5 = L1;

            // Ván lót đáy sau Family 1 (Y = -W)
            var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Back != null) placedIds.Add(bb5Back.Id.IntegerValue);

            // Giằng chân sau (Y = -W) tại h1
            double zH1_5 = z0 + jackH + 135;
            var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Back != null) placedIds.Add(lgF5Back.Id.IntegerValue);

            // Giằng đỉnh sau (Y = -W) tại topZ
            if (opts != null && opts.HasTopTransom)
            {
                var lgTop5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_bay5);
                if (lgTop5Back != null) placedIds.Add(lgTop5Back.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;

                // Giằng chéo sau Family 5 (Y = -W)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay5, y0 - W, 0, L_bay5, z0, jackH, s, storyCount, opts);

                // Ván chắn biên sau (Y = -W)
                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (tbBack5 != null) placedIds.Add(tbBack5.Id.IntegerValue);
                }

                // Mâm thao tác dãy chính
                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // CẤY CỘT TẠI X = xMax - W (MÉP TRƯỚC Y = 0) KHI L1 > W
            double L_cut = L1 > W ? (L1 - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0, z0 + jackH, opts);

                // MÉP TRƯỚC (Y = 0) THU NGẮN VỀ L_cut
                var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
                if (bb5Front != null) placedIds.Add(bb5Front.Id.IntegerValue);

                var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_cut);
                if (lgF5Front != null) placedIds.Add(lgF5Front.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTop5Front != null) placedIds.Add(lgTop5Front.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3_5 = z0 + jackH + 830 + sOff;
                    var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_cut);
                    if (lgMid5 != null) placedIds.Add(lgMid5.Id.IntegerValue);

                    double zH6_5 = z0 + jackH + 2180 + sOff;
                    var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_cut);
                    if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

                    double zH7_5 = z0 + jackH + 2630 + sOff;
                    var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_cut);
                    if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbFront5 != null) placedIds.Add(tbFront5.Id.IntegerValue);
                    }
                }
            }

            // =========================================================================
            // PHẦN B: DỰNG NHỊP RẼ VUÔNG GÓC 90° VƯƠN SANG +Y (L2 x W mm)
            // =========================================================================
            double yTop = y0 + L2;
            double L_return = L2;
            double W_return = W;

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.IntegerValue);
            if (jkR2 != null) placedIds.Add(jkR2.Id.IntegerValue);

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yTop, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yTop, z0 + jackH, opts);

            // Giằng chân Family 4 tại h1
            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // Giằng đỉnh Family 4 tại topZ
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

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_return);
                if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, y0, 90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
                if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                    if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
                var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
                if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
                var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
                if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.2: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP1-TYPE1.2 - HỖ TRỢ MỌI L1, L2, W)
        /// </summary>
        private static string ExecuteCase1_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double L1 = opts.L1 > 0 ? opts.L1 : 1200.0;
            double L2 = opts.L2 > 0 ? opts.L2 : 1200.0;
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

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

            double xMax = x0 + L1;
            List<double> colX = new List<double> { x0, xMax };

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            double xBay = x0;
            double L_last = L1;

            var bbLastBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_last);
            if (bbLastBack != null) placedIds.Add(bbLastBack.Id.IntegerValue);

            double zH1_last = z0 + jackH + 135;
            var lgFLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastBack != null) placedIds.Add(lgFLastBack.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_last);
                if (lgTopLastBack != null) placedIds.Add(lgTopLastBack.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay, y0 - W, 0, L_last, z0, jackH, s, storyCount, opts);

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbLastBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_last);
                    if (tbLastBack != null) placedIds.Add(tbLastBack.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_last);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // Nếu L1 > W: cấy cột phụ tại xC = xMax - W và dựng đoạn giằng trước thu ngắn L_cut = L1 - W
            double L_cut = L1 > W ? (L1 - W) : 0;
            double xLast = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xLast, y0, z0 + jackH, opts);

                var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
                if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

                var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH1_last)), 0, "l", L_cut);
                if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3_c = z0 + jackH + 830 + sOff;
                    var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH3_c)), 0, "l", L_cut);
                    if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);

                    double zH6_c = z0 + jackH + 2180 + sOff;
                    var rF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH6_c)), 0, "l", L_cut);
                    if (rF1 != null) placedIds.Add(rF1.Id.IntegerValue);

                    double zH7_c = z0 + jackH + 2630 + sOff;
                    var rF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH7_c)), 0, "l", L_cut);
                    if (rF2 != null) placedIds.Add(rF2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);
                    }
                }
            }

            // PHẦN B: NHÁNH RẼ L2 x W mm
            double yTop = y0 + L2;
            double L_return = L2;
            double W_return = W;

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.IntegerValue);
            if (jkR2 != null) placedIds.Add(jkR2.Id.IntegerValue);

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xLast, yTop, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yTop, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_return);
                if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, y0, 90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
                if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                    if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
                var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
                if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
                var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
                if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.3: KHỚP NỐI GÓC 90° HƯỚNG -Y (CONN-GRP1-TYPE1.3 - GIẰNG SAU THU NGẮN)
        /// </summary>
        private static string ExecuteCase1_3(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double L1 = opts.L1 > 0 ? opts.L1 : 1800.0;
            double L2 = opts.L2 > 0 ? opts.L2 : 1800.0;
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

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

            double xMax = x0 + L1;
            List<double> colX = new List<double> { x0, xMax };
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            double xBay5 = x0;
            double L_bay5 = L1;

            var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Front != null) placedIds.Add(bb5Front.Id.IntegerValue);

            double zH1_5 = z0 + jackH + 135;
            var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Front != null) placedIds.Add(lgF5Front.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTop5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(topZ)), 0, "l", L_bay5);
                if (lgTop5Front != null) placedIds.Add(lgTop5Front.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_5 = z0 + jackH + 830 + sOff;
                var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_bay5);
                if (lgMid5 != null) placedIds.Add(lgMid5.Id.IntegerValue);

                double zH6_5 = z0 + jackH + 2180 + sOff;
                var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_bay5);
                if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

                double zH7_5 = z0 + jackH + 2630 + sOff;
                var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_bay5);
                if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (tbFront5 != null) placedIds.Add(tbFront5.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // CẤY CỘT TẠI X = xMax - W (MÉP SAU Y = -W) KHI L1 > W
            double L_cut = L1 > W ? (L1 - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0 - W, z0 + jackH, opts);

                var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
                if (bb5Back != null) placedIds.Add(bb5Back.Id.IntegerValue);

                var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_cut);
                if (lgF5Back != null) placedIds.Add(lgF5Back.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTop5Back != null) placedIds.Add(lgTop5Back.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay5, y0 - W, 0, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbBack5 != null) placedIds.Add(tbBack5.Id.IntegerValue);
                    }
                }
            }

            // PHẦN B: HỆ GIÀN 2 (NHÁNH RẼ -Y: L2 x W mm)
            double yBase = y0 - W;
            double L_return = L2;
            double yBot = yBase - L_return;
            double W_return = W;

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.IntegerValue);
            if (jkR2 != null) placedIds.Add(jkR2.Id.IntegerValue);

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yBot, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yBot, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBase), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBase), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC, yBot, 90, L_return, z0, jackH, s, storyCount, opts);

                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH3_R)), 90, "l", L_return);
                if (lgRM2 != null) placedIds.Add(lgRM2.Id.IntegerValue);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBase), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
                if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                    if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH6_R)), 90, "l", L_return);
                var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
                if (rR1 != null) placedIds.Add(rR1.Id.IntegerValue);
                if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", L_return);
                var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
                if (rR2 != null) placedIds.Add(rR2.Id.IntegerValue);
                if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.4: KHỚP NỐI GÓC 90° HƯỚNG -Y (CONN-GRP1-TYPE1.4 - HỖ TRỢ MỌI L1, L2, W)
        /// </summary>
        private static string ExecuteCase1_4(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double L1 = opts.L1 > 0 ? opts.L1 : 1200.0;
            double L2 = opts.L2 > 0 ? opts.L2 : 1200.0;
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

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

            double xMax = x0 + L1;
            List<double> colX = new List<double> { x0, xMax };

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            double xBay = x0;
            double L_last = L1;

            var bbLastFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(z0)), 0, "l", L_last);
            if (bbLastFront != null) placedIds.Add(bbLastFront.Id.IntegerValue);

            double zH1_last = z0 + jackH + 135;
            var lgFLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastFront != null) placedIds.Add(lgFLastFront.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(topZ)), 0, "l", L_last);
                if (lgTopLastFront != null) placedIds.Add(lgTopLastFront.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_last = z0 + jackH + 830 + sOff;
                var lgMidLast = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH3_last)), 0, "l", L_last);
                if (lgMidLast != null) placedIds.Add(lgMidLast.Id.IntegerValue);

                double zH6_last = z0 + jackH + 2180 + sOff;
                var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH6_last)), 0, "l", L_last);
                if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

                double zH7_last = z0 + jackH + 2630 + sOff;
                var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0), MmToFt(zH7_last)), 0, "l", L_last);
                if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbLastFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_last);
                    if (tbLastFront != null) placedIds.Add(tbLastFront.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_last);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // Nếu L1 > W: cấy cột phụ tại xC = xMax - W (mép sau Y = -W) và dựng đoạn thu ngắn L_cut = L1 - W
            double L_cut = L1 > W ? (L1 - W) : 0;
            double xLast = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xLast, y0 - W, z0 + jackH, opts);

                var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
                if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);

                var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(zH1_last)), 0, "l", L_cut);
                if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay, y0 - W, 0, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                    }
                }
            }

            // PHẦN B: NHÁNH RẼ -Y L2 x W mm
            double yBase = y0 - W;
            double L_return = L2;
            double yBot = yBase - L_return;
            double W_return = W;

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.IntegerValue);
            if (jkR2 != null) placedIds.Add(jkR2.Id.IntegerValue);

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xLast, yBot, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yBot, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBase), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBase), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xLast, yBot, 90, L_return, z0, jackH, s, storyCount, opts);

                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH3_R)), 90, "l", L_return);
                if (lgRM2 != null) placedIds.Add(lgRM2.Id.IntegerValue);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBase), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
                if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + pOff), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                    if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH6_R)), 90, "l", L_return);
                var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
                if (rR1 != null) placedIds.Add(rR1.Id.IntegerValue);
                if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", L_return);
                var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
                if (rR2 != null) placedIds.Add(rR2.Id.IntegerValue);
                if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
            }

            return null;
        }
    }
}
