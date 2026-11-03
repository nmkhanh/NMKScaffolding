using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP1-TYPE1.1 - MÉP NGOÀI PHẲNG)
        /// Hỗ trợ chuỗi nhịp L1Bays và L2Bays dạng "1800,2x1200,1800,3x1200,..."
        /// </summary>
        private static string ExecuteCase1_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            // PHẦN A: DÃY CHÍNH (CHUỖI NHỊP L1Bays X W)
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            double xBay5 = colX[nMainBays - 1];
            double L_bay5 = l1Bays[nMainBays - 1];

            // Ván lót đáy sau Family 1 (Y = -W)
            var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Back != null) placedIds.Add(bb5Back.Id.GetIdValue());

            // Giằng chân sau (Y = -W) tại h1
            double zH1_5 = z0 + jackH + 135;
            var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Back != null) placedIds.Add(lgF5Back.Id.GetIdValue());

            // Giằng đỉnh sau (Y = -W) tại topZ
            if (opts != null && opts.HasTopTransom)
            {
                var lgTop5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_bay5);
                if (lgTop5Back != null) placedIds.Add(lgTop5Back.Id.GetIdValue());
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
                    if (tbBack5 != null) placedIds.Add(tbBack5.Id.GetIdValue());
                }

                // Mâm thao tác dãy chính
                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }
            }

            // CẤY CỘT TẠI X = xMax - W (MÉP TRƯỚC Y = 0) KHI L_bay5 > W
            double L_cut = L_bay5 > W ? (L_bay5 - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0, z0 + jackH, opts);

                // MÉP TRƯỚC (Y = 0) THU NGẮN VỀ L_cut
                var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
                if (bb5Front != null) placedIds.Add(bb5Front.Id.GetIdValue());

                var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_cut);
                if (lgF5Front != null) placedIds.Add(lgF5Front.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTop5Front != null) placedIds.Add(lgTop5Front.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3_5 = z0 + jackH + 830 + sOff;
                    var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_cut);
                    if (lgMid5 != null) placedIds.Add(lgMid5.Id.GetIdValue());

                    double zH6_5 = z0 + jackH + 2180 + sOff;
                    var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_cut);
                    if (railF1 != null) placedIds.Add(railF1.Id.GetIdValue());

                    double zH7_5 = z0 + jackH + 2630 + sOff;
                    var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_cut);
                    if (railF2 != null) placedIds.Add(railF2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbFront5 != null) placedIds.Add(tbFront5.Id.GetIdValue());
                    }
                }
            }

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ VUÔNG GÓC 90° VƯƠN SANG +Y (CHUỖI NHỊP L2Bays X W)
            // =========================================================================
            double L_return = l2Bays[0];
            double yTop = y0 + L_return;
            double W_return = W;
            bool closeFirstReturnBay = (l2Bays.Count == 1);

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
            if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.GetIdValue());
            if (jkR2 != null) placedIds.Add(jkR2.Id.GetIdValue());

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yTop, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yTop, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.GetIdValue());
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.GetIdValue());
            if (trRF1 != null) placedIds.Add(trRF1.Id.GetIdValue());
            if (trRF2 != null) placedIds.Add(trRF2.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.GetIdValue());
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.GetIdValue());
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.GetIdValue());
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_return);
                if (lgRM1 != null) placedIds.Add(lgRM1.Id.GetIdValue());

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, y0, 90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.GetIdValue());
                if (trRD2 != null) placedIds.Add(trRD2.Id.GetIdValue());

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                    if (closeFirstReturnBay)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                    }
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
                if (rL1 != null) placedIds.Add(rL1.Id.GetIdValue());

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
                if (rL2 != null) placedIds.Add(rL2.Id.GetIdValue());

                if (closeFirstReturnBay)
                {
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
                    if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                    if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                }
            }

            BuildReturnContinuationBaysPositiveY(doc, level, placedIds, sym1, sym2, sym3, sym4, sym5, sym6, sym7,
                xC, xMax, yTop, z0, W_return, jackH, storyCount, topZ, l2Bays, opts);

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.2: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP1-TYPE1.2 - HỖ TRỢ CHUỖI NHỊP L1Bays, L2Bays)
        /// </summary>
        private static string ExecuteCase1_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            // PHẦN A: DÃY CHÍNH (CHUỖI NHỊP L1Bays X W)
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            double xBayLast = colX[nMainBays - 1];
            double L_bayLast = l1Bays[nMainBays - 1];

            var bbLastBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_bayLast);
            if (bbLastBack != null) placedIds.Add(bbLastBack.Id.GetIdValue());

            double zH1_Last = z0 + jackH + 135;
            var lgFLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(zH1_Last)), 0, "l", L_bayLast);
            if (lgFLastBack != null) placedIds.Add(lgFLastBack.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_bayLast);
                if (lgTopLastBack != null) placedIds.Add(lgTopLastBack.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, y0 - W, 0, L_bayLast, z0, jackH, s, storyCount, opts);

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbLastBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (tbLastBack != null) placedIds.Add(tbLastBack.Id.GetIdValue());
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }
            }

            // CẤY CỘT TẠI X = xMax - W (MÉP TRƯỚC Y = 0) KHI L_bayLast > W
            double L_cut = L_bayLast > W ? (L_bayLast - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0, z0 + jackH, opts);

                var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
                if (bbFront != null) placedIds.Add(bbFront.Id.GetIdValue());

                var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH1_Last)), 0, "l", L_cut);
                if (lgFFront != null) placedIds.Add(lgFFront.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3_5 = z0 + jackH + 830 + sOff;
                    var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_cut);
                    if (lgMid5 != null) placedIds.Add(lgMid5.Id.GetIdValue());

                    double zH6_5 = z0 + jackH + 2180 + sOff;
                    var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_cut);
                    if (railF1 != null) placedIds.Add(railF1.Id.GetIdValue());

                    double zH7_5 = z0 + jackH + 2630 + sOff;
                    var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_cut);
                    if (railF2 != null) placedIds.Add(railF2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbFront5 != null) placedIds.Add(tbFront5.Id.GetIdValue());
                    }
                }
            }

            // =========================================================================
            // PHẦN B: NHÁNH RẼ +Y (CHUỖI NHỊP L2Bays x W mm)
            // =========================================================================
            double L_return = l2Bays[0];
            double yTop = y0 + L_return;
            double W_return = W;
            bool closeFirstReturnBay = (l2Bays.Count == 1);

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
            if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.GetIdValue());
            if (jkR2 != null) placedIds.Add(jkR2.Id.GetIdValue());

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yTop, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yTop, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.GetIdValue());
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.GetIdValue());
            if (trRF1 != null) placedIds.Add(trRF1.Id.GetIdValue());
            if (trRF2 != null) placedIds.Add(trRF2.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(topZ)), 90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.GetIdValue());
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.GetIdValue());
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.GetIdValue());
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, y0, 90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.GetIdValue());
                if (trRD2 != null) placedIds.Add(trRD2.Id.GetIdValue());

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                    if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                    if (closeFirstReturnBay)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                    }
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
                if (rL1 != null) placedIds.Add(rL1.Id.GetIdValue());

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
                if (rL2 != null) placedIds.Add(rL2.Id.GetIdValue());

                if (closeFirstReturnBay)
                {
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
                    if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                    if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                }
            }

            BuildReturnContinuationBaysPositiveY(doc, level, placedIds, sym1, sym2, sym3, sym4, sym5, sym6, sym7,
                xC, xMax, yTop, z0, W_return, jackH, storyCount, topZ, l2Bays, opts);

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.3: KHỚP NỐI GÓC 90° HƯỚNG -Y (CONN-GRP1-TYPE1.3 - HỖ TRỢ CHUỖI NHỊP L1Bays, L2Bays)
        /// </summary>
        private static string ExecuteCase1_3(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            // PHẦN A: DÃY CHÍNH (CHUỖI NHỊP L1Bays X W)
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            double xBay5 = colX[nMainBays - 1];
            double L_bay5 = l1Bays[nMainBays - 1];

            // 1. MẶT TRƯỚC (Y = 0): ĐẦY ĐỦ L_bay5
            var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Front != null) placedIds.Add(bb5Front.Id.GetIdValue());

            double zH1_5 = z0 + jackH + 135;
            var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Front != null) placedIds.Add(lgF5Front.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTop5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(topZ)), 0, "l", L_bay5);
                if (lgTop5Front != null) placedIds.Add(lgTop5Front.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_5 = z0 + jackH + 830 + sOff;
                var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_bay5);
                if (lgMid5 != null) placedIds.Add(lgMid5.Id.GetIdValue());

                double zH6_5 = z0 + jackH + 2180 + sOff;
                var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_bay5);
                if (railF1 != null) placedIds.Add(railF1.Id.GetIdValue());

                double zH7_5 = z0 + jackH + 2630 + sOff;
                var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_bay5);
                if (railF2 != null) placedIds.Add(railF2.Id.GetIdValue());

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (tbFront5 != null) placedIds.Add(tbFront5.Id.GetIdValue());
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bay5);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }
            }

            // 2. CẤY CỘT TẠI X = xMax - W (MÉP SAU Y = -W) KHI L_bay5 > W
            double L_cut = L_bay5 > W ? (L_bay5 - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0 - W, z0 + jackH, opts);

                var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
                if (bb5Back != null) placedIds.Add(bb5Back.Id.GetIdValue());

                var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_cut);
                if (lgF5Back != null) placedIds.Add(lgF5Back.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTop5Back != null) placedIds.Add(lgTop5Back.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBay5, y0 - W, 0, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbBack5 != null) placedIds.Add(tbBack5.Id.GetIdValue());
                    }
                }
            }

            // =========================================================================
            // PHẦN B: DỰNG NHỊP RẼ VUÔNG GÓC 90° VƯƠN SANG -Y (CHUỖI NHỊP L2Bays x W mm)
            // =========================================================================
            double L_return = l2Bays[0];
            double yBack = y0 - W;
            double yBot = yBack - L_return;
            double W_return = W;
            bool closeFirstReturnBay = (l2Bays.Count == 1);

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(z0)), -90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(z0)), -90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
            if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.GetIdValue());
            if (jkR2 != null) placedIds.Add(jkR2.Id.GetIdValue());

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yBot, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yBot, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zH1_R)), -90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH1_R)), -90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.GetIdValue());
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.GetIdValue());
            if (trRF1 != null) placedIds.Add(trRF1.Id.GetIdValue());
            if (trRF2 != null) placedIds.Add(trRF2.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(topZ)), -90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(topZ)), -90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.GetIdValue());
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.GetIdValue());
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.GetIdValue());
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH3_R)), -90, "l", L_return);
                if (lgRM2 != null) placedIds.Add(lgRM2.Id.GetIdValue());

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC, yBack, -90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.GetIdValue());
                if (trRD2 != null) placedIds.Add(trRD2.Id.GetIdValue());

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                    if (closeFirstReturnBay)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                    }
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH6_R)), -90, "l", L_return);
                if (rR1 != null) placedIds.Add(rR1.Id.GetIdValue());

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH7_R)), -90, "l", L_return);
                if (rR2 != null) placedIds.Add(rR2.Id.GetIdValue());

                if (closeFirstReturnBay)
                {
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
                    if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                    if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                }
            }

            BuildReturnContinuationBaysNegativeY(doc, level, placedIds, sym1, sym2, sym3, sym4, sym5, sym6, sym7,
                xC, xMax, yBot, z0, W_return, jackH, storyCount, topZ, l2Bays, opts);

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 1.4: KHỚP NỐI GÓC 90° HƯỚNG -Y (CONN-GRP1-TYPE1.4 - HỖ TRỢ CHUỖI NHỊP L1Bays, L2Bays)
        /// </summary>
        private static string ExecuteCase1_4(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
            // PHẦN A: DÃY CHÍNH (CHUỖI NHỊP L1Bays X W)
            // =========================================================================
            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }
            double xMax = colX[nMainBays];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nMainBays - 1; i++)
            {
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, colX[i], y0, z0, W, jackH, l1Bays[i], true, opts);
            }

            double xBayLast = colX[nMainBays - 1];
            double L_bayLast = l1Bays[nMainBays - 1];

            var bbLastFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(z0)), 0, "l", L_bayLast);
            if (bbLastFront != null) placedIds.Add(bbLastFront.Id.GetIdValue());

            double zH1_Last = z0 + jackH + 135;
            var lgFLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH1_Last)), 0, "l", L_bayLast);
            if (lgFLastFront != null) placedIds.Add(lgFLastFront.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(topZ)), 0, "l", L_bayLast);
                if (lgTopLastFront != null) placedIds.Add(lgTopLastFront.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_Last = z0 + jackH + 830 + sOff;
                var lgMidLast = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH3_Last)), 0, "l", L_bayLast);
                if (lgMidLast != null) placedIds.Add(lgMidLast.Id.GetIdValue());

                double zH6_Last = z0 + jackH + 2180 + sOff;
                var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH6_Last)), 0, "l", L_bayLast);
                if (railF1 != null) placedIds.Add(railF1.Id.GetIdValue());

                double zH7_Last = z0 + jackH + 2630 + sOff;
                var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0), MmToFt(zH7_Last)), 0, "l", L_bayLast);
                if (railF2 != null) placedIds.Add(railF2.Id.GetIdValue());

                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var tbLastFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (tbLastFront != null) placedIds.Add(tbLastFront.Id.GetIdValue());
                }

                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_bayLast);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }
            }

            // CẤY CỘT TẠI X = xMax - W (MÉP SAU Y = -W) KHI L_bayLast > W
            double L_cut = L_bayLast > W ? (L_bayLast - W) : 0;
            double xC = xMax - W;
            if (L_cut > 10)
            {
                var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jkC != null) placedIds.Add(jkC.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, y0 - W, z0 + jackH, opts);

                var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
                if (bb5Back != null) placedIds.Add(bb5Back.Id.GetIdValue());

                var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(zH1_Last)), 0, "l", L_cut);
                if (lgF5Back != null) placedIds.Add(lgF5Back.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_cut);
                    if (lgTop5Back != null) placedIds.Add(lgTop5Back.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, y0 - W, 0, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        double zDeck = z0 + jackH + 1730 + sOff;
                        var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_cut);
                        if (tbBack5 != null) placedIds.Add(tbBack5.Id.GetIdValue());
                    }
                }
            }

            // =========================================================================
            // PHẦN B: NHÁNH RẼ -Y (CHUỖI NHỊP L2Bays x W mm)
            // =========================================================================
            double L_return = l2Bays[0];
            double yBack = y0 - W;
            double yBot = yBack - L_return;
            double W_return = W;
            bool closeFirstReturnBay = (l2Bays.Count == 1);

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(z0)), -90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(z0)), -90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
            if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

            var jkR1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            var jkR2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 0, "h", jackH);
            if (jkR1 != null) placedIds.Add(jkR1.Id.GetIdValue());
            if (jkR2 != null) placedIds.Add(jkR2.Id.GetIdValue());

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xC, yBot, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xMax, yBot, z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zH1_R)), -90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH1_R)), -90, "l", L_return);
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.GetIdValue());
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.GetIdValue());
            if (trRF1 != null) placedIds.Add(trRF1.Id.GetIdValue());
            if (trRF2 != null) placedIds.Add(trRF2.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(topZ)), -90, "l", L_return);
                var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(topZ)), -90, "l", L_return);
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(topZ)), 0, "l", W_return);
                var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(topZ)), 0, "l", W_return);
                if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.GetIdValue());
                if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.GetIdValue());
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.GetIdValue());
                if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.GetIdValue());
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC, yBack, -90, L_return, z0, jackH, s, storyCount, opts);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBack), MmToFt(zDeck_R)), 0, "l", W_return);
                var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (trRD1 != null) placedIds.Add(trRD1.Id.GetIdValue());
                if (trRD2 != null) placedIds.Add(trRD2.Id.GetIdValue());

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOff), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOff), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }

                if (sym7 != null)
                {
                    var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBack), MmToFt(zDeck_R)), -90, "l", L_return);
                    if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                    if (closeFirstReturnBay)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                    }
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH6_R)), -90, "l", L_return);
                if (rR1 != null) placedIds.Add(rR1.Id.GetIdValue());

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBack), MmToFt(zH7_R)), -90, "l", L_return);
                if (rR2 != null) placedIds.Add(rR2.Id.GetIdValue());

                if (closeFirstReturnBay)
                {
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
                    if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                    if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                }
            }

            BuildReturnContinuationBaysNegativeY(doc, level, placedIds, sym1, sym2, sym3, sym4, sym5, sym6, sym7,
                xC, xMax, yBot, z0, W_return, jackH, storyCount, topZ, l2Bays, opts);

            return null;
        }
    }
}
