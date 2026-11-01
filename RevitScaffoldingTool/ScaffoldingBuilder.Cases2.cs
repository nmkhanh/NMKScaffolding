using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 2.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP2-TYPE2.1 - MÉP SAU PHẲNG)
        /// </summary>
        private static string ExecuteCase2_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
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

            List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            int nMainBays = mainBays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in mainBays) { curX += b; colX.Add(curX); }

            // Đầu hồi trái (k=0) có lan can; đầu hồi phải (k=n-1) là cửa ngõ sang nhánh rẽ nên không chặn
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0), opts);

            for (int i = 0; i < nMainBays; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true, opts);
            }

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ NGOÀI TRƯỜNG HỢP 2.1 (MÉP SAU PHẲNG)
            // =========================================================================
            double xA = x0 + 7800;
            double xB = x0 + 9000;
            double yBack = y0 - W; // -1200 mm
            double yFront = y0;     // 0 mm
            double yTop = y0 + 600; // +600 mm
            double L_ret = 1800;    // Dài theo phương Y (từ -1200 đến +600)
            double W_ret = 1200;    // Rộng theo phương X (từ 7800 đến 9000)

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBack), MmToFt(z0)), 0, "l", W_ret);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(z0)), 0, "l", W_ret);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            XYZ[] retPts = new XYZ[]
            {
                new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(z0)),
                new XYZ(MmToFt(xB), MmToFt(yTop),  MmToFt(z0)),
                new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(z0))
            };
            foreach (var pt in retPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);
            }

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yBack, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yTop,  z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xA, yTop,  z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var trRF_Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBack), MmToFt(zH1_R)), 0, "l", W_ret);
            var trRF_Top  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(zH1_R)), 0, "l", W_ret);
            var lgRF_B    = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(zH1_R)), 90, "l", L_ret);
            var lgRF_A    = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH1_R)), 90, "l", 600);
            if (trRF_Back != null) placedIds.Add(trRF_Back.Id.IntegerValue);
            if (trRF_Top  != null) placedIds.Add(trRF_Top.Id.IntegerValue);
            if (lgRF_B    != null) placedIds.Add(lgRF_B.Id.IntegerValue);
            if (lgRF_A    != null) placedIds.Add(lgRF_A.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF_Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBack), MmToFt(topZ)), 0, "l", W_ret);
                var trTopRF_Top  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(topZ)), 0, "l", W_ret);
                var lgTopRF_B    = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(topZ)), 90, "l", L_ret);
                var lgTopRF_A    = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(topZ)), 90, "l", 600);
                if (trTopRF_Back != null) placedIds.Add(trTopRF_Back.Id.IntegerValue);
                if (trTopRF_Top  != null) placedIds.Add(trTopRF_Top.Id.IntegerValue);
                if (lgTopRF_B    != null) placedIds.Add(lgTopRF_B.Id.IntegerValue);
                if (lgTopRF_A    != null) placedIds.Add(lgTopRF_A.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yBack, 0, W_ret, z0, jackH, s, storyCount, opts);

                double zH3_R = z0 + jackH + 830 + sOff;
                var lgRM_Top = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop), MmToFt(zH3_R)), 0, "l", W_ret);
                if (lgRM_Top != null) placedIds.Add(lgRM_Top.Id.IntegerValue);

                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD_Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBack), MmToFt(zDeck_R)), 0, "l", W_ret);
                var trRD_Top  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(zDeck_R)), 0, "l", W_ret);
                if (trRD_Back != null) placedIds.Add(trRD_Back.Id.IntegerValue);
                if (trRD_Top  != null) placedIds.Add(trRD_Top.Id.IntegerValue);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(yBack), MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(yBack), MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yBack + 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                    var tbTop  = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yTop - 66),  MmToFt(zDeck_R)), 0, "l", W_ret);
                    var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(yBack), MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                    if (tbTop  != null) placedIds.Add(tbTop.Id.IntegerValue);
                    if (tbEndB != null) placedIds.Add(tbEndB.Id.IntegerValue);
                }

                double zH6_R = z0 + jackH + 2180 + sOff;
                var rTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(zH6_R)), 0, "l", W_ret);
                var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(zH6_R)), 90, "l", L_ret);
                var rA_1  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH6_R)), 90, "l", 600);
                if (rTop1 != null) placedIds.Add(rTop1.Id.IntegerValue);
                if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                if (rA_1  != null) placedIds.Add(rA_1.Id.IntegerValue);

                double zH7_R = z0 + jackH + 2630 + sOff;
                var rTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop),  MmToFt(zH7_R)), 0, "l", W_ret);
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBack), MmToFt(zH7_R)), 90, "l", L_ret);
                var rA_2  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH7_R)), 90, "l", 600);
                if (rTop2 != null) placedIds.Add(rTop2.Id.IntegerValue);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);
                if (rA_2  != null) placedIds.Add(rA_2.Id.IntegerValue);
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 2.2: KHỚP NỐI GÓC 90° HƯỚNG -Y (CONN-GRP2-TYPE2.2 - MÉP TRƯỚC PHẲNG)
        /// </summary>
        private static string ExecuteCase2_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
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

            List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            int nMainBays = mainBays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in mainBays) { curX += b; colX.Add(curX); }

            // Dựng cột dãy chính: đầu hồi trái (k=0) có lan can; đầu hồi phải là cửa ngõ sang nhánh rẽ
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0), opts);

            for (int i = 0; i < nMainBays; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true, opts);
            }

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ NGOÀI TRƯỜNG HỢP 2.2 (MÉP TRƯỚC PHẲNG - HƯỚNG -Y)
            // =========================================================================
            double xA = x0 + 7800;
            double xB = x0 + 9000;
            double yFront = y0;       // 0 mm
            double yBack  = y0 - W;   // -1200 mm
            double yBot   = y0 - 1800;// -1800 mm
            double L_ret  = 1800;     // Dài theo phương Y (từ -1800 đến 0)
            double W_ret  = 1200;     // Rộng theo phương X (từ 7800 đến 9000)

            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(z0)), 0, "l", W_ret);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(z0)), 0, "l", W_ret);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            XYZ[] retPts = new XYZ[]
            {
                new XYZ(MmToFt(xB), MmToFt(yFront), MmToFt(z0)),
                new XYZ(MmToFt(xB), MmToFt(yBot),   MmToFt(z0)),
                new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(z0))
            };
            foreach (var pt in retPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);
            }

            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yFront, z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xB, yBot,   z0 + jackH, opts);
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xA, yBot,   z0 + jackH, opts);

            double zH1_R = z0 + jackH + 135;
            var trRF_Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH1_R)), 0, "l", W_ret);
            var trRF_Bot   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(zH1_R)), 0, "l", W_ret);
            var lgRF_B     = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot),   MmToFt(zH1_R)), 90, "l", L_ret);
            var lgRF_A     = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(zH1_R)), 90, "l", 600);
            if (trRF_Front != null) placedIds.Add(trRF_Front.Id.IntegerValue);
            if (trRF_Bot   != null) placedIds.Add(trRF_Bot.Id.IntegerValue);
            if (lgRF_B     != null) placedIds.Add(lgRF_B.Id.IntegerValue);
            if (lgRF_A     != null) placedIds.Add(lgRF_A.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF_Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(topZ)), 0, "l", W_ret);
                var trTopRF_Bot   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(topZ)), 0, "l", W_ret);
                var lgTopRF_B     = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot),   MmToFt(topZ)), 90, "l", L_ret);
                var lgTopRF_A     = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(topZ)), 90, "l", 600);
                if (trTopRF_Front != null) placedIds.Add(trTopRF_Front.Id.IntegerValue);
                if (trTopRF_Bot   != null) placedIds.Add(trTopRF_Bot.Id.IntegerValue);
                if (lgTopRF_B     != null) placedIds.Add(lgTopRF_B.Id.IntegerValue);
                if (lgTopRF_A     != null) placedIds.Add(lgTopRF_A.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3_R = z0 + jackH + 830 + sOff;

                // 1. Giằng dọc giữa h3:
                // - Mặt trước Y = 0 (L = 1200)
                var lgRM_Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH3_R)), 0, "l", W_ret);
                if (lgRM_Front != null) placedIds.Add(lgRM_Front.Id.IntegerValue);

                // - Đầu hồi ngoài cùng Y = -1800 (L = 1200)
                var lgRM_Bot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH3_R)), 0, "l", W_ret);
                if (lgRM_Bot != null) placedIds.Add(lgRM_Bot.Id.IntegerValue);

                // - Vách trong giật cấp X = 7800 (Y: -1800 -> -1200, L = 600)
                var lgRM_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH3_R)), 90, "l", 600);
                if (lgRM_A != null) placedIds.Add(lgRM_A.Id.IntegerValue);

                // 2. Giằng chéo Family 5: Đặt tại mặt ngoài bên phải X = 9000 (dọc theo Y từ -1800 -> 0, L = 1800)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yBot, 90, L_ret, z0, jackH, s, storyCount, opts);

                // 3. Giằng đỡ mâm tại zDeck:
                double zDeck_R = z0 + jackH + 1730 + sOff;
                var trRD_Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zDeck_R)), 0, "l", W_ret);
                var trRD_Bot   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot),   MmToFt(zDeck_R)), 0, "l", W_ret);
                if (trRD_Front != null) placedIds.Add(trRD_Front.Id.IntegerValue);
                if (trRD_Bot   != null) placedIds.Add(trRD_Bot.Id.IntegerValue);

                // 4. Mâm sàn thao tác Family 6 (L = 1800 mm theo phương Y):
                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }

                // 5. Ván chắn biên Family 7:
                if (sym7 != null)
                {
                    // - Mặt trước Y = 0 (L = 1200)
                    var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yFront - 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                    if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);

                    // - Đầu hồi ngoài cùng đáy Y = -1800 (L = 1200)
                    var tbBot   = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yBot + 66),   MmToFt(zDeck_R)), 0, "l", W_ret);
                    if (tbBot != null) placedIds.Add(tbBot.Id.IntegerValue);

                    // - Mép ngoài bên phải X = 9000 (L = 1800)
                    var tbEndB  = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(yBot),  MmToFt(zDeck_R)), 90, "l", L_ret);
                    if (tbEndB != null) placedIds.Add(tbEndB.Id.IntegerValue);

                    // - Vách trong giật cấp X = 7800 (Y: -1800 -> -1200, L = 600)
                    var tbInnerA = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", 600);
                    if (tbInnerA != null) placedIds.Add(tbInnerA.Id.IntegerValue);
                }

                // 6. Lan can an toàn kép (h6 và h7):
                double zH6_R = z0 + jackH + 2180 + sOff;
                double zH7_R = z0 + jackH + 2630 + sOff;

                // - Mặt trước Y = 0 (L = 1200)
                var rFront1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH6_R)), 0, "l", W_ret);
                var rFront2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yFront), MmToFt(zH7_R)), 0, "l", W_ret);
                if (rFront1 != null) placedIds.Add(rFront1.Id.IntegerValue);
                if (rFront2 != null) placedIds.Add(rFront2.Id.IntegerValue);

                // - Đầu hồi ngoài cùng đáy Y = -1800 (L = 1200)
                var rBot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_ret);
                var rBot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_ret);
                if (rBot1 != null) placedIds.Add(rBot1.Id.IntegerValue);
                if (rBot2 != null) placedIds.Add(rBot2.Id.IntegerValue);

                // - Mép ngoài bên phải X = 9000 (chỉ đặt tay vịn trên h7 vì h6 đã có giằng chéo Family 5)
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", L_ret);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);

                // - Vách trong giật cấp X = 7800 (Y: -1800 -> -1200, L = 600)
                var rA_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH6_R)), 90, "l", 600);
                var rA_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", 600);
                if (rA_1 != null) placedIds.Add(rA_1.Id.IntegerValue);
                if (rA_2 != null) placedIds.Add(rA_2.Id.IntegerValue);
            }

            return null;
        }

        /// <summary>
        /// DỰNG DÃY NHIỀU NHỊP THẲNG TIÊU CHUẨN
        /// </summary>
        private static string ExecuteMultiBay(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            var bayLens = opts.BayLengths;
            if (bayLens == null || bayLens.Count == 0)
            {
                bayLens = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            }

            int nBays = bayLens.Count;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = GetJackHeight(opts);
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var bLen in bayLens)
            {
                curX += bLen;
                colX.Add(curX);
            }

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

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            for (int i = 0; i < nBays; i++)
            {
                double px = colX[i];
                double L_i = bayLens[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true, opts);
            }

            return null;
        }
    }
}
