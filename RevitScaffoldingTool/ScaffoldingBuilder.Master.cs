using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        private static string ExecuteMaster6CornersContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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
                return "Thieu Family cot loi trong: " + opts.FamilyFolder;
            }

            ActivateSymbol(doc, sym1);
            ActivateSymbol(doc, sym2);
            ActivateSymbol(doc, sym3);
            ActivateSymbol(doc, sym4);
            if (sym5 != null) ActivateSymbol(doc, sym5);
            if (sym6 != null) ActivateSymbol(doc, sym6);
            if (sym7 != null) ActivateSymbol(doc, sym7);

            // Cao do chuan ALINCO 2700
            double zH1 = z0 + jackH + 135;
            double zH2 = z0 + jackH + 380;
            double zH3 = z0 + jackH + 830;
            double zDeck = z0 + jackH + 1730;
            double zH5_brc = z0 + jackH + 1730;
            double zH6 = z0 + jackH + 2180;
            double zH7 = z0 + jackH + 2630;

            HashSet<string> placedCols = new HashSet<string>();
            Action<double, double> EnsureCol = (cx, cy) =>
            {
                string key = string.Format("{0:F0}_{1:F0}", cx, cy);
                if (!placedCols.Contains(key))
                {
                    placedCols.Add(key);
                    var jk = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(z0)), 0, "h", jackH);
                    if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                    PlaceConfiguredPosts(doc, sym3, level, placedIds, cx, cy, z0 + jackH, opts);
                }
            };

            HashSet<string> placedTransoms = new HashSet<string>();
            Action<double, double, double, double> PlaceSharedTransom = (tx, ty, trot, tlen) =>
            {
                string key = string.Format("{0:F0}_{1:F0}_{2:F0}", tx, ty, trot);
                if (!placedTransoms.Contains(key))
                {
                    placedTransoms.Add(key);
                    var fFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(zH1)), trot, "l", tlen);
                    if (fFoot != null) placedIds.Add(fFoot.Id.IntegerValue);

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = zDeck + s * 1800.0;
                        var fDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(sDeck)), trot, "l", tlen);
                        if (fDeck != null) placedIds.Add(fDeck.Id.IntegerValue);
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var fTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(topZ)), trot, "l", tlen);
                        if (fTop != null) placedIds.Add(fTop.Id.IntegerValue);
                    }
                }
            };

            // =========================================================================
            // 1. LEG 1 (Huong +X, Y in [y0, y0 + 1200]: Bay 1 tieu chuan, Bay 2 la Corner 1 - Case 1.1)
            // =========================================================================
            EnsureCol(x0, y0); EnsureCol(x0, y0 + 1200);
            EnsureCol(x0 + 1800, y0); EnsureCol(x0 + 1800, y0 + 1200);
            EnsureCol(x0 + 3600, y0); EnsureCol(x0 + 2400, y0 + 1200); // Cot cay tai (2400, y0+1200)
            EnsureCol(x0 + 3600, y0 + 1200); // Cot goc noi tiep sang Leg 2

            // Dau hoi bat dau gian giao tai X = x0 (dong kin)
            PlaceSharedTransom(x0, y0, 90, 1200);
            var rBeg1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH6)), 90, "l", 1200);
            var rBeg2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH7)), 90, "l", 1200);
            if (rBeg1 != null) placedIds.Add(rBeg1.Id.IntegerValue);
            if (rBeg2 != null) placedIds.Add(rBeg2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tbB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1200);
                if (tbB != null) placedIds.Add(tbB.Id.IntegerValue);
            }

            PlaceSharedTransom(x0 + 1800, y0, 90, 1200);
            // KHONG dat transom tai x0 + 2400 vi khong co cot tai (x0+2400, y0)

            // Dau hoi mep ngoai cua Leg 1 tai X = x0 + 3600 (dong kin bang lan can kep dung chuan Case 1.1)
            PlaceSharedTransom(x0 + 3600, y0, 90, 1200);
            var rEndL1_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0), MmToFt(zH6)), 90, "l", 1200);
            var rEndL1_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0), MmToFt(zH7)), 90, "l", 1200);
            if (rEndL1_1 != null) placedIds.Add(rEndL1_1.Id.IntegerValue);
            if (rEndL1_2 != null) placedIds.Add(rEndL1_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tbEndL1 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1200);
                if (tbEndL1 != null) placedIds.Add(tbEndL1.Id.IntegerValue);
            }

            // Bay 1 of Leg 1: X in [x0, x0 + 1800]
            // - Mep sau Y = y0: Family 5
            var bbL1_1S = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(z0)), 0, "l", 1800);
            var lgFL1_1S = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH1)), 0, "l", 1800);
            if (bbL1_1S != null) placedIds.Add(bbL1_1S.Id.IntegerValue);
            if (lgFL1_1S != null) placedIds.Add(lgFL1_1S.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH2)), 0, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0), MmToFt(y0), MmToFt(zH5_brc)), 0, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0), MmToFt(y0 + 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mep truoc Y = y0 + 1200: Lan can Family 4
            var bbL1_1N = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200), MmToFt(z0)), 0, "l", 1800);
            var lgFL1_1N = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200), MmToFt(zH1)), 0, "l", 1800);
            var lgML1_1N = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200), MmToFt(zH3)), 0, "l", 1800);
            var rL1_1N_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200), MmToFt(zH6)), 0, "l", 1800);
            var rL1_1N_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200), MmToFt(zH7)), 0, "l", 1800);
            if (bbL1_1N != null) placedIds.Add(bbL1_1N.Id.IntegerValue);
            if (lgFL1_1N != null) placedIds.Add(lgFL1_1N.Id.IntegerValue);
            if (lgML1_1N != null) placedIds.Add(lgML1_1N.Id.IntegerValue);
            if (rL1_1N_1 != null) placedIds.Add(rL1_1N_1.Id.IntegerValue);
            if (rL1_1N_2 != null) placedIds.Add(rL1_1N_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200 - 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mam san Bay 1
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0), MmToFt(y0 + 345), MmToFt(zDeck)), 0, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0), MmToFt(y0 + 1200 - 345), MmToFt(zDeck)), 0, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // Bay 2 of Leg 1 (CORNER 1 - CASE 1.1: re +Y, mep sau 1800 mm, mep truoc cat 600 mm)
            // - Mep sau Y = y0: Giu nguyen 1800 mm den x0 + 3600 (Family 5)
            var bbL1_2S = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0), MmToFt(z0)), 0, "l", 1800);
            var lgFL1_2S = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0), MmToFt(zH1)), 0, "l", 1800);
            if (bbL1_2S != null) placedIds.Add(bbL1_2S.Id.IntegerValue);
            if (lgFL1_2S != null) placedIds.Add(lgFL1_2S.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0), MmToFt(zH2)), 0, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0), MmToFt(zH5_brc)), 0, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mep truoc Y = y0 + 1200: Cat ngan ve 600 mm (x0 + 1800 -> x0 + 2400)
            var bbL1_c = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200), MmToFt(z0)), 0, "l", 600);
            var lgFL1_c = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200), MmToFt(zH1)), 0, "l", 600);
            var lgML1_c = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200), MmToFt(zH3)), 0, "l", 600);
            var r1_c = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200), MmToFt(zH6)), 0, "l", 600);
            var r2_c = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200), MmToFt(zH7)), 0, "l", 600);
            if (bbL1_c != null) placedIds.Add(bbL1_c.Id.IntegerValue);
            if (lgFL1_c != null) placedIds.Add(lgFL1_c.Id.IntegerValue);
            if (lgML1_c != null) placedIds.Add(lgML1_c.Id.IntegerValue);
            if (r1_c != null) placedIds.Add(r1_c.Id.IntegerValue);
            if (r2_c != null) placedIds.Add(r2_c.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200 - 66), MmToFt(zDeck)), 0, "l", 600);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Cua ngo mo thong 1200 mm tai Y = y0 + 1200 (x0 + 2400 -> x0 + 3600):
            // Shared Transom dat ca zH1 (giang chan) va zDeck (do mam cho Leg 2)!
            PlaceSharedTransom(x0 + 2400, y0 + 1200, 0, 1200);

            // - Mam san Bay 2 (L = 1800 mm)
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 345), MmToFt(zDeck)), 0, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 1800), MmToFt(y0 + 1200 - 345), MmToFt(zDeck)), 0, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // =========================================================================
            // 2. LEG 2 (Huong +Y, X in [x0 + 2400, x0 + 3600]: Bay 1 tieu chuan, Bay 2 la Corner 2 - Case 1.2)
            // QUY TAC DUNG CHUAN BUTTON 1.1 & BUTTON 1.2:
            // - MEP NGOAI PHANG (X = x0 + 3600): Bay 1 la GIANG CHEO Family 5 (dung Case 1.1)!
            //   Bay 2 la CUA NGO MO sang Leg 3 (+X) co Shared Transom (zH1 & zDeck)!
            // - VACH TRONG (X = x0 + 2400): Bay 1 & Bay 2 la LAN CAN Family 4!
            // - DINH DAU HOI (Y = y0 + 4200): Dong kin bang LAN CAN KEP h6, h7 va van chan bien!
            // =========================================================================
            EnsureCol(x0 + 2400, y0 + 3000); EnsureCol(x0 + 3600, y0 + 3000);
            EnsureCol(x0 + 2400, y0 + 4200); EnsureCol(x0 + 3600, y0 + 4200);

            PlaceSharedTransom(x0 + 2400, y0 + 3000, 0, 1200);
            PlaceSharedTransom(x0 + 2400, y0 + 4200, 0, 1200);

            // --- VACH PHAI MEP NGOAI (X = x0 + 3600): Bay 1 la GIANG CHEO Family 5, Bay 2 la CUA NGO MO sang Leg 3 ---
            // Bay 1: Y in [y0 + 1200, y0 + 3000] (1800 mm) - GIANG CHEO Family 5 dung chuan Case 1.1!
            var bbL2_1R = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 1200), MmToFt(z0)), 90, "l", 1800);
            var lgFL2_1R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 1200), MmToFt(zH1)), 90, "l", 1800);
            if (bbL2_1R != null) placedIds.Add(bbL2_1R.Id.IntegerValue);
            if (lgFL2_1R != null) placedIds.Add(lgFL2_1R.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 1200), MmToFt(zH2)), 90, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 1200), MmToFt(zH5_brc)), 90, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 3600 - 66), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }

            // Bay 2 (Y: y0 + 3000 -> y0 + 4200, 1200 mm): CUA NGO MO sang Leg 3
            // Shared Transom dat ca zH1 (giang chan) va zDeck (do mam cho Leg 3)!
            PlaceSharedTransom(x0 + 3600, y0 + 3000, 90, 1200);

            // --- VACH TRAI BEN TRONG (X = x0 + 2400): LAN CAN Family 4 xuyen suot ca Bay 1 & Bay 2 ---
            // Bay 1: Y in [y0 + 1200, y0 + 3000] (1800 mm)
            var bbL2_1L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 1200), MmToFt(z0)), 90, "l", 1800);
            var lgFL2_1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 1200), MmToFt(zH1)), 90, "l", 1800);
            var lgML2_1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 1200), MmToFt(zH3)), 90, "l", 1800);
            var rL2_1L_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 1200), MmToFt(zH6)), 90, "l", 1800);
            var rL2_1L_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 1200), MmToFt(zH7)), 90, "l", 1800);
            if (bbL2_1L != null) placedIds.Add(bbL2_1L.Id.IntegerValue);
            if (lgFL2_1L != null) placedIds.Add(lgFL2_1L.Id.IntegerValue);
            if (lgML2_1L != null) placedIds.Add(lgML2_1L.Id.IntegerValue);
            if (rL2_1L_1 != null) placedIds.Add(rL2_1L_1.Id.IntegerValue);
            if (rL2_1L_2 != null) placedIds.Add(rL2_1L_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 2400 + 66), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // Bay 2 (CORNER 2 - CASE 1.2: L = 1200 mm): Y in [y0 + 3000, y0 + 4200]
            var bbL2_2L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 3000), MmToFt(z0)), 90, "l", 1200);
            var lgFL2_2L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 3000), MmToFt(zH1)), 90, "l", 1200);
            var lgML2_2L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 3000), MmToFt(zH3)), 90, "l", 1200);
            var rL2_2L_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 3000), MmToFt(zH6)), 90, "l", 1200);
            var rL2_2L_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 3000), MmToFt(zH7)), 90, "l", 1200);
            if (bbL2_2L != null) placedIds.Add(bbL2_2L.Id.IntegerValue);
            if (lgFL2_2L != null) placedIds.Add(lgFL2_2L.Id.IntegerValue);
            if (lgML2_2L != null) placedIds.Add(lgML2_2L.Id.IntegerValue);
            if (rL2_2L_1 != null) placedIds.Add(rL2_2L_1.Id.IntegerValue);
            if (rL2_2L_2 != null) placedIds.Add(rL2_2L_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 2400 + 66), MmToFt(y0 + 3000), MmToFt(zDeck)), 90, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }

            // --- DINH DAU HOI Y = y0 + 4200 (X: x0 + 2400 -> x0 + 3600): Dong kin bang lan can kep dung Case 1.2 ---
            var bbTop2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 4200), MmToFt(z0)), 0, "l", 1200);
            var rTop2_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 4200), MmToFt(zH6)), 0, "l", 1200);
            var rTop2_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 4200), MmToFt(zH7)), 0, "l", 1200);
            if (bbTop2 != null) placedIds.Add(bbTop2.Id.IntegerValue);
            if (rTop2_1 != null) placedIds.Add(rTop2_1.Id.IntegerValue);
            if (rTop2_2 != null) placedIds.Add(rTop2_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 2400), MmToFt(y0 + 4200 - 66), MmToFt(zDeck)), 0, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }

            // --- Mam san Leg 2 ---
            if (sym6 != null)
            {
                // Bay 1 (1800 mm)
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 2400 + 345), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 3600 - 345), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }

                // Bay 2 (1200 mm)
                var p3 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 2400 + 345), MmToFt(y0 + 3000), MmToFt(zDeck)), 90, "l", 1200);
                var p4 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 3600 - 345), MmToFt(y0 + 3000), MmToFt(zDeck)), 90, "l", 1200);
                if (p3 != null) { SetParam(p3, "w", 500); placedIds.Add(p3.Id.IntegerValue); }
                if (p4 != null) { SetParam(p4, "w", 500); placedIds.Add(p4.Id.IntegerValue); }
            }

            // =========================================================================
            // 3. LEG 3 (Huong +X, Y in [y0 + 3000, y0 + 4200]: Bay 1 tieu chuan, Bay 2 la Corner 3 - Case 1.3)
            // =========================================================================
            EnsureCol(x0 + 5400, y0 + 4200); EnsureCol(x0 + 5400, y0 + 3000);
            EnsureCol(x0 + 7200, y0 + 4200); EnsureCol(x0 + 6000, y0 + 3000); // Cot mep ngoai tai 7200, cot cay tai 6000
            EnsureCol(x0 + 7200, y0 + 3000); // Cot goc noi tiep sang Leg 4

            PlaceSharedTransom(x0 + 5400, y0 + 3000, 90, 1200);
            // KHONG dat transom doc Y tai x0 + 6000 vi khong co cot tai (x0+6000, y0+4200)

            // Dau hoi mep ngoai cua Leg 3 tai X = x0 + 7200 (dong kin bang lan can kep dung chuan Case 1.3)
            PlaceSharedTransom(x0 + 7200, y0 + 3000, 90, 1200);
            var rEndL3_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 3000), MmToFt(zH6)), 90, "l", 1200);
            var rEndL3_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 3000), MmToFt(zH7)), 90, "l", 1200);
            if (rEndL3_1 != null) placedIds.Add(rEndL3_1.Id.IntegerValue);
            if (rEndL3_2 != null) placedIds.Add(rEndL3_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tbEndL3 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 3000), MmToFt(zDeck)), 90, "l", 1200);
                if (tbEndL3 != null) placedIds.Add(tbEndL3.Id.IntegerValue);
            }

            // --- MEP TREN (Y = y0 + 4200): LAN CAN Family 4 xuyen suot ca Bay 1 & Bay 2 ---
            // Bay 1: X in [x0 + 3600, x0 + 5400] (1800 mm)
            var bbL3_1T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200), MmToFt(z0)), 0, "l", 1800);
            var lgFL3_1T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200), MmToFt(zH1)), 0, "l", 1800);
            var lgML3_1T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200), MmToFt(zH3)), 0, "l", 1800);
            var rL3_1T_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200), MmToFt(zH6)), 0, "l", 1800);
            var rL3_1T_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200), MmToFt(zH7)), 0, "l", 1800);
            if (bbL3_1T != null) placedIds.Add(bbL3_1T.Id.IntegerValue);
            if (lgFL3_1T != null) placedIds.Add(lgFL3_1T.Id.IntegerValue);
            if (lgML3_1T != null) placedIds.Add(lgML3_1T.Id.IntegerValue);
            if (rL3_1T_1 != null) placedIds.Add(rL3_1T_1.Id.IntegerValue);
            if (rL3_1T_2 != null) placedIds.Add(rL3_1T_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200 - 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // Bay 2: X in [x0 + 5400, x0 + 7200] (1800 mm)
            var bbL3_2T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200), MmToFt(z0)), 0, "l", 1800);
            var lgFL3_2T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200), MmToFt(zH1)), 0, "l", 1800);
            var lgML3_2T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200), MmToFt(zH3)), 0, "l", 1800);
            var rL3_2T_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200), MmToFt(zH6)), 0, "l", 1800);
            var rL3_2T_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200), MmToFt(zH7)), 0, "l", 1800);
            if (bbL3_2T != null) placedIds.Add(bbL3_2T.Id.IntegerValue);
            if (lgFL3_2T != null) placedIds.Add(lgFL3_2T.Id.IntegerValue);
            if (lgML3_2T != null) placedIds.Add(lgML3_2T.Id.IntegerValue);
            if (rL3_2T_1 != null) placedIds.Add(rL3_2T_1.Id.IntegerValue);
            if (rL3_2T_2 != null) placedIds.Add(rL3_2T_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200 - 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }

            // --- MEP DUOI (Y = y0 + 3000): GIANG CHEO Family 5 Bay 1 (1800 mm) & GIANG CHEO THU NGAN Bay 2 (600 mm) ---
            // Bay 1 (X: x0 + 3600 -> x0 + 5400, 1800 mm): Family 5
            var bbL3_1B = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000), MmToFt(z0)), 0, "l", 1800);
            var lgFL3_1B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000), MmToFt(zH1)), 0, "l", 1800);
            if (bbL3_1B != null) placedIds.Add(bbL3_1B.Id.IntegerValue);
            if (lgFL3_1B != null) placedIds.Add(lgFL3_1B.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000), MmToFt(zH2)), 0, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000), MmToFt(zH5_brc)), 0, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000 + 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // Bay 2 (CORNER 3 - CASE 1.3: X: x0 + 5400 -> x0 + 6000, 600 mm): GIANG CHEO THU NGAN Family 5 (khong co Family 4)
            var bbL3_c = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000), MmToFt(z0)), 0, "l", 600);
            var lgFL3_c = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000), MmToFt(zH1)), 0, "l", 600);
            if (bbL3_c != null) placedIds.Add(bbL3_c.Id.IntegerValue);
            if (lgFL3_c != null) placedIds.Add(lgFL3_c.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000), MmToFt(zH2)), 0, "l", 600);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000), MmToFt(zH5_brc)), 0, "l", 600);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000 + 66), MmToFt(zDeck)), 0, "l", 600);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // Cua ngo mo 1200 mm sang -Y tai Y = y0 + 3000 (x0 + 6000 -> x0 + 7200):
            // Shared Transom dat ca zH1 (giang chan) va zDeck (do mam cho Leg 4)!
            PlaceSharedTransom(x0 + 6000, y0 + 3000, 0, 1200);

            // --- Mam san Leg 3 ---
            if (sym6 != null)
            {
                // Bay 1
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 3000 + 345), MmToFt(zDeck)), 0, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 3600), MmToFt(y0 + 4200 - 345), MmToFt(zDeck)), 0, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
                // Bay 2
                var p3 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 3000 + 345), MmToFt(zDeck)), 0, "l", 1800);
                var p4 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 5400), MmToFt(y0 + 4200 - 345), MmToFt(zDeck)), 0, "l", 1800);
                if (p3 != null) { SetParam(p3, "w", 500); placedIds.Add(p3.Id.IntegerValue); }
                if (p4 != null) { SetParam(p4, "w", 500); placedIds.Add(p4.Id.IntegerValue); }
            }

            // =========================================================================
            // 4. LEG 4 (Huong -Y, X in [x0 + 6000, x0 + 7200]: Bay 1 tieu chuan, Bay 2 la Corner 4 - Case 1.4)
            // =========================================================================
            EnsureCol(x0 + 6000, y0 + 1200); EnsureCol(x0 + 7200, y0 + 1200);
            EnsureCol(x0 + 6000, y0); EnsureCol(x0 + 7200, y0);

            PlaceSharedTransom(x0 + 6000, y0 + 1200, 0, 1200);

            // Bay 1 of Leg 4: Y in [y0 + 1200, y0 + 3000] (chuan 1800 mm theo huong -Y)
            // - Vach trong ben trai X = x0 + 6000: GIANG CHEO Family 5 (tiep noi giang cheo 600 mm cua Leg 3)
            var bbL4_1L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0 + 1200), MmToFt(z0)), 90, "l", 1800);
            var lgFL4_1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0 + 1200), MmToFt(zH1)), 90, "l", 1800);
            if (bbL4_1L != null) placedIds.Add(bbL4_1L.Id.IntegerValue);
            if (lgFL4_1L != null) placedIds.Add(lgFL4_1L.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0 + 1200), MmToFt(zH2)), 90, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0 + 1200), MmToFt(zH5_brc)), 90, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 6000 + 66), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mep ngoai ben phai X = x0 + 7200: LAN CAN Family 4
            var bbL4_1R = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 1200), MmToFt(z0)), 90, "l", 1800);
            var lgFL4_1R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 1200), MmToFt(zH1)), 90, "l", 1800);
            var lgML4_1R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 1200), MmToFt(zH3)), 90, "l", 1800);
            var rL4_1R_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 1200), MmToFt(zH6)), 90, "l", 1800);
            var rL4_1R_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 7200), MmToFt(y0 + 1200), MmToFt(zH7)), 90, "l", 1800);
            if (bbL4_1R != null) placedIds.Add(bbL4_1R.Id.IntegerValue);
            if (lgFL4_1R != null) placedIds.Add(lgFL4_1R.Id.IntegerValue);
            if (lgML4_1R != null) placedIds.Add(lgML4_1R.Id.IntegerValue);
            if (rL4_1R_1 != null) placedIds.Add(rL4_1R_1.Id.IntegerValue);
            if (rL4_1R_2 != null) placedIds.Add(rL4_1R_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 7200 - 66), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mam san Bay 1
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 6000 + 345), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 7200 - 345), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // Bay 2 of Leg 4 (CORNER 4 - CASE 1.4: L = 1200 mm tao o vuong 1200x1200, re sang +X):
            // - Vach trong ben trai X = x0 + 6000 (L = 1200 mm): GIANG CHEO Family 5
            var bbL4_2L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(z0)), 90, "l", 1200);
            var lgFL4_2L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(zH1)), 90, "l", 1200);
            if (bbL4_2L != null) placedIds.Add(bbL4_2L.Id.IntegerValue);
            if (lgFL4_2L != null) placedIds.Add(lgFL4_2L.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(zH2)), 90, "l", 1200);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(zH5_brc)), 90, "l", 1200);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 6000 + 66), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Day dau hoi Y = y0 (dong kin bang lan can kep dung logic Case 1.4)
            PlaceSharedTransom(x0 + 6000, y0, 0, 1200);
            var bbBot4 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(z0)), 0, "l", 1200);
            var rBot4_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(zH6)), 0, "l", 1200);
            var rBot4_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0), MmToFt(zH7)), 0, "l", 1200);
            if (bbBot4 != null) placedIds.Add(bbBot4.Id.IntegerValue);
            if (rBot4_1 != null) placedIds.Add(rBot4_1.Id.IntegerValue);
            if (rBot4_2 != null) placedIds.Add(rBot4_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 6000), MmToFt(y0 + 66), MmToFt(zDeck)), 0, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Cua ngo mo 1200 mm sang phai tai X = x0 + 7200 (Y in [y0, y0 + 1200]):
            // Shared Transom dat ca zH1 (giang chan) va zDeck (do mam cho Leg 5)!
            PlaceSharedTransom(x0 + 7200, y0, 90, 1200);

            // - Mam san Bay 2 (L = 1200 mm)
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 6000 + 345), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1200);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 7200 - 345), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1200);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // =========================================================================
            // 5. LEG 5 (Huong +X, Y in [y0, y0 + 1200]: Bay 1 tieu chuan, Bay 2 la Corner 5 - Case 2.1)
            // =========================================================================
            EnsureCol(x0 + 9000, y0); EnsureCol(x0 + 9000, y0 + 1200);
            EnsureCol(x0 + 10800, y0); EnsureCol(x0 + 10800, y0 + 1200);

            PlaceSharedTransom(x0 + 9000, y0, 90, 1200);
            PlaceSharedTransom(x0 + 10800, y0, 90, 1200);

            // Bay 1 of Leg 5: X in [x0 + 7200, x0 + 9000]
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 7200, y0 + 1200, z0, 1200, jackH, 1800, true, opts);

            // Bay 2 of Leg 5: X in [x0 + 9000, x0 + 10800] (chuan du 1800 mm dung logic Nhom 2!)
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 9000, y0 + 1200, z0, 1200, jackH, 1800, true, opts);

            // CORNER 5 (CASE 2.1: Nhom 2 - Re vuong goc sang +Y mep sau Y = y0 phang):
            // Nhanh re o X in [x0 + 10800, x0 + 12000], Y in [y0, y0 + 1800] (L = 1800 mm, W = 1200 mm)
            EnsureCol(x0 + 12000, y0); EnsureCol(x0 + 12000, y0 + 1800); EnsureCol(x0 + 10800, y0 + 1800);

            PlaceSharedTransom(x0 + 10800, y0, 0, 1200);
            PlaceSharedTransom(x0 + 10800, y0 + 1800, 0, 1200);

            // - Mep sau phang Y = y0 (X: x0 + 10800 -> x0 + 12000, L = 1200 mm): Lan can kep (dung Case 2.1)
            var bbC5_S = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0), MmToFt(z0)), 0, "l", 1200);
            var rC5_S1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0), MmToFt(zH6)), 0, "l", 1200);
            var rC5_S2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0), MmToFt(zH7)), 0, "l", 1200);
            if (bbC5_S != null) placedIds.Add(bbC5_S.Id.IntegerValue);
            if (rC5_S1 != null) placedIds.Add(rC5_S1.Id.IntegerValue);
            if (rC5_S2 != null) placedIds.Add(rC5_S2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 66), MmToFt(zDeck)), 0, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mep ngoai phai X = x0 + 12000 (Y: y0 -> y0 + 1800, L = 1800 mm): GIANG CHEO Family 5
            var bbC5_E = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0), MmToFt(z0)), 90, "l", 1800);
            var lgFC5_E = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0), MmToFt(zH1)), 90, "l", 1800);
            if (bbC5_E != null) placedIds.Add(bbC5_E.Id.IntegerValue);
            if (lgFC5_E != null) placedIds.Add(lgFC5_E.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0), MmToFt(zH2)), 90, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0), MmToFt(zH5_brc)), 90, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 12000 - 66), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Vach trong nho truoc X = x0 + 10800 (Y: y0 + 1200 -> y0 + 1800, L = 600 mm): Lan can
            var bbC5_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1200), MmToFt(z0)), 90, "l", 600);
            var lgFC5_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1200), MmToFt(zH1)), 90, "l", 600);
            var lgMC5_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1200), MmToFt(zH3)), 90, "l", 600);
            var rC5_In1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1200), MmToFt(zH6)), 90, "l", 600);
            var rC5_In2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1200), MmToFt(zH7)), 90, "l", 600);
            if (bbC5_In != null) placedIds.Add(bbC5_In.Id.IntegerValue);
            if (lgFC5_In != null) placedIds.Add(lgFC5_In.Id.IntegerValue);
            if (lgMC5_In != null) placedIds.Add(lgMC5_In.Id.IntegerValue);
            if (rC5_In1 != null) placedIds.Add(rC5_In1.Id.IntegerValue);
            if (rC5_In2 != null) placedIds.Add(rC5_In2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800 + 66), MmToFt(y0 + 1200), MmToFt(zDeck)), 90, "l", 600);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mam san Corner 5
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 10800 + 345), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 12000 - 345), MmToFt(y0), MmToFt(zDeck)), 90, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // =========================================================================
            // 6. LEG 6 (Huong +Y, X in [x0 + 10800, x0 + 12000]: Bay 1 tieu chuan, Bay 2 la Corner 6 - Case 2.2)
            // =========================================================================
            EnsureCol(x0 + 10800, y0 + 3600); EnsureCol(x0 + 12000, y0 + 3600);
            EnsureCol(x0 + 10800, y0 + 5400); EnsureCol(x0 + 12000, y0 + 5400);

            PlaceSharedTransom(x0 + 10800, y0 + 3600, 0, 1200);
            PlaceSharedTransom(x0 + 10800, y0 + 5400, 0, 1200);

            // Bay 1 of Leg 6: Y in [y0 + 1800, y0 + 3600]
            var bbL6_1R = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 1800), MmToFt(z0)), 90, "l", 1800);
            var lgFL6_1R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 1800), MmToFt(zH1)), 90, "l", 1800);
            if (bbL6_1R != null) placedIds.Add(bbL6_1R.Id.IntegerValue);
            if (lgFL6_1R != null) placedIds.Add(lgFL6_1R.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 1800), MmToFt(zH2)), 90, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 1800), MmToFt(zH5_brc)), 90, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 12000 - 66), MmToFt(y0 + 1800), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            var bbL6_1L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1800), MmToFt(z0)), 90, "l", 1800);
            var lgFL6_1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1800), MmToFt(zH1)), 90, "l", 1800);
            var lgML6_1L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1800), MmToFt(zH3)), 90, "l", 1800);
            var rL6_1L_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1800), MmToFt(zH6)), 90, "l", 1800);
            var rL6_1L_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 1800), MmToFt(zH7)), 90, "l", 1800);
            if (bbL6_1L != null) placedIds.Add(bbL6_1L.Id.IntegerValue);
            if (lgFL6_1L != null) placedIds.Add(lgFL6_1L.Id.IntegerValue);
            if (lgML6_1L != null) placedIds.Add(lgML6_1L.Id.IntegerValue);
            if (rL6_1L_1 != null) placedIds.Add(rL6_1L_1.Id.IntegerValue);
            if (rL6_1L_2 != null) placedIds.Add(rL6_1L_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800 + 66), MmToFt(y0 + 1800), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 10800 + 345), MmToFt(y0 + 1800), MmToFt(zDeck)), 90, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 12000 - 345), MmToFt(y0 + 1800), MmToFt(zDeck)), 90, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // Bay 2 of Leg 6: Y in [y0 + 3600, y0 + 5400] (chuan 1800 mm, nguyen ven)
            var bbL6_2R = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 3600), MmToFt(z0)), 90, "l", 1800);
            var lgFL6_2R = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 3600), MmToFt(zH1)), 90, "l", 1800);
            if (bbL6_2R != null) placedIds.Add(bbL6_2R.Id.IntegerValue);
            if (lgFL6_2R != null) placedIds.Add(lgFL6_2R.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 3600), MmToFt(zH2)), 90, "l", 1800);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 3600), MmToFt(zH5_brc)), 90, "l", 1800);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 12000 - 66), MmToFt(y0 + 3600), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            var bbL6_2L = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 3600), MmToFt(z0)), 90, "l", 1800);
            var lgFL6_2L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 3600), MmToFt(zH1)), 90, "l", 1800);
            var lgML6_2L = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 3600), MmToFt(zH3)), 90, "l", 1800);
            var rL6_2L_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 3600), MmToFt(zH6)), 90, "l", 1800);
            var rL6_2L_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 3600), MmToFt(zH7)), 90, "l", 1800);
            if (bbL6_2L != null) placedIds.Add(bbL6_2L.Id.IntegerValue);
            if (lgFL6_2L != null) placedIds.Add(lgFL6_2L.Id.IntegerValue);
            if (lgML6_2L != null) placedIds.Add(lgML6_2L.Id.IntegerValue);
            if (rL6_2L_1 != null) placedIds.Add(rL6_2L_1.Id.IntegerValue);
            if (rL6_2L_2 != null) placedIds.Add(rL6_2L_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800 + 66), MmToFt(y0 + 3600), MmToFt(zDeck)), 90, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 10800 + 345), MmToFt(y0 + 3600), MmToFt(zDeck)), 90, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 12000 - 345), MmToFt(y0 + 3600), MmToFt(zDeck)), 90, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // CORNER 6 (CASE 2.2: Nhom 2 - Re vuong goc sang +X mep truoc X = x0 + 10800 phang):
            // Nhanh re o Y in [y0 + 5400, y0 + 6600], X in [x0 + 10800, x0 + 12600] (L = 1800 mm, W = 1200 mm)
            EnsureCol(x0 + 12600, y0 + 5400); EnsureCol(x0 + 12600, y0 + 6600); EnsureCol(x0 + 10800, y0 + 6600);

            // FIX: Transom doc theo Y (rot = 90) tai X = x0 + 10800 tu y0+5400 den y0+6600
            PlaceSharedTransom(x0 + 10800, y0 + 5400, 90, 1200);
            PlaceSharedTransom(x0 + 12600, y0 + 5400, 90, 1200);

            // - Mep truoc phang X = x0 + 10800 (Y: y0 + 5400 -> y0 + 6600, L = 1200 mm): Lan can kep (dung Case 2.2)
            var bbC6_F = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 5400), MmToFt(z0)), 90, "l", 1200);
            var rC6_F1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 5400), MmToFt(zH6)), 90, "l", 1200);
            var rC6_F2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 5400), MmToFt(zH7)), 90, "l", 1200);
            if (bbC6_F != null) placedIds.Add(bbC6_F.Id.IntegerValue);
            if (rC6_F1 != null) placedIds.Add(rC6_F1.Id.IntegerValue);
            if (rC6_F2 != null) placedIds.Add(rC6_F2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800 + 66), MmToFt(y0 + 5400), MmToFt(zDeck)), 90, "l", 1200);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mep ngoai tren Y = y0 + 6600 (X: x0 + 10800 -> x0 + 12600, L = 1800 mm): LAN CAN KEP + GIANG GIUA h3
            var bbC6_T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600), MmToFt(z0)), 0, "l", 1800);
            var lgFC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600), MmToFt(zH1)), 0, "l", 1800);
            var lgMC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600), MmToFt(zH3)), 0, "l", 1800);
            var rC6_T1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600), MmToFt(zH6)), 0, "l", 1800);
            var rC6_T2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600), MmToFt(zH7)), 0, "l", 1800);
            if (bbC6_T != null) placedIds.Add(bbC6_T.Id.IntegerValue);
            if (lgFC6_T != null) placedIds.Add(lgFC6_T.Id.IntegerValue);
            if (lgMC6_T != null) placedIds.Add(lgMC6_T.Id.IntegerValue);
            if (rC6_T1 != null) placedIds.Add(rC6_T1.Id.IntegerValue);
            if (rC6_T2 != null) placedIds.Add(rC6_T2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600 - 66), MmToFt(zDeck)), 0, "l", 1800);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Vach trong nho sau Y = y0 + 5400 (X: x0 + 12000 -> x0 + 12600, L = 600 mm): GIANG CHEO Family 5 (dung fix turn 8!)
            var bbC6_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 5400), MmToFt(z0)), 0, "l", 600);
            var lgFC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 5400), MmToFt(zH1)), 0, "l", 600);
            if (bbC6_In != null) placedIds.Add(bbC6_In.Id.IntegerValue);
            if (lgFC6_In != null) placedIds.Add(lgFC6_In.Id.IntegerValue);
            if (sym5 != null)
            {
                var br1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 5400), MmToFt(zH2)), 0, "l", 600);
                var br2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 5400), MmToFt(zH5_brc)), 0, "l", 600);
                if (br1 != null) { SetParam(br1, "h", 900); placedIds.Add(br1.Id.IntegerValue); }
                if (br2 != null) { SetParam(br2, "h", 900); placedIds.Add(br2.Id.IntegerValue); }
            }
            if (sym7 != null)
            {
                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 12000), MmToFt(y0 + 5400 + 66), MmToFt(zDeck)), 0, "l", 600);
                if (tb != null) placedIds.Add(tb.Id.IntegerValue);
            }
            // - Mam san Corner 6
            if (sym6 != null)
            {
                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 5400 + 345), MmToFt(zDeck)), 0, "l", 1800);
                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x0 + 10800), MmToFt(y0 + 6600 - 345), MmToFt(zDeck)), 0, "l", 1800);
                if (p1 != null) { SetParam(p1, "w", 500); placedIds.Add(p1.Id.IntegerValue); }
                if (p2 != null) { SetParam(p2, "w", 500); placedIds.Add(p2.Id.IntegerValue); }
            }

            // =========================================================================
            // 7. LEG 7 (Doan ket thuc, Huong +X, Y in [y0 + 5400, y0 + 6600]: 2 nhip 1800 mm)
            // =========================================================================
            EnsureCol(x0 + 14400, y0 + 5400); EnsureCol(x0 + 14400, y0 + 6600);
            EnsureCol(x0 + 16200, y0 + 5400); EnsureCol(x0 + 16200, y0 + 6600);

            PlaceSharedTransom(x0 + 14400, y0 + 5400, 90, 1200);
            PlaceSharedTransom(x0 + 16200, y0 + 5400, 90, 1200);

            // Bay 1 of Leg 7: X in [x0 + 12600, x0 + 14400]
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 12600, y0 + 6600, z0, 1200, jackH, 1800, true, opts);

            // Bay 2 of Leg 7: X in [x0 + 14400, x0 + 16200]
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 14400, y0 + 6600, z0, 1200, jackH, 1800, true, opts);

            // Dau hoi ket thuc Leg 7 tai X = x0 + 16200 (dong kin bang lan can kep)
            var rEnd7_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 16200), MmToFt(y0 + 5400), MmToFt(zH6)), 90, "l", 1200);
            var rEnd7_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x0 + 16200), MmToFt(y0 + 5400), MmToFt(zH7)), 90, "l", 1200);
            if (rEnd7_1 != null) placedIds.Add(rEnd7_1.Id.IntegerValue);
            if (rEnd7_2 != null) placedIds.Add(rEnd7_2.Id.IntegerValue);
            if (sym7 != null)
            {
                var tbEnd7 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x0 + 16200), MmToFt(y0 + 5400), MmToFt(zDeck)), 90, "l", 1200);
                if (tbEnd7 != null) placedIds.Add(tbEnd7.Id.IntegerValue);
            }

            return null;
        }

    }
}

