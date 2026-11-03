using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// MASTER MỚI: Thực thi nối tiếp (Continue) tuần tự 6 trường hợp góc nối TH 1.1 -> TH 1.2 -> TH 1.3 -> TH 1.4 -> TH 2.1 -> TH 2.2
        /// dựa trên trạng thái 6 CheckBox (CheckCase11 .. CheckCase22) và 7 chuỗi nhịp động L0 .. L6 (kèm bề rộng W).
        /// </summary>
        public static string ExecuteMaster6CornersContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds, int maxLegs = 7)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
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

            List<double> l0Bays = (opts.L0Bays != null && opts.L0Bays.Count > 0) ? opts.L0Bays : new List<double> { 1800, 1800 };
            List<double> l1Bays = (opts.L1Bays != null && opts.L1Bays.Count > 0) ? opts.L1Bays : new List<double> { 1800, 1200 };
            List<double> l2Bays = (opts.L2Bays != null && opts.L2Bays.Count > 0) ? opts.L2Bays : new List<double> { 1800, 1800 };
            List<double> l3Bays = (opts.L3Bays != null && opts.L3Bays.Count > 0) ? opts.L3Bays : new List<double> { 1800, 1200 };
            List<double> l4Bays = (opts.L4Bays != null && opts.L4Bays.Count > 0) ? opts.L4Bays : new List<double> { 1800, 1800 };
            List<double> l5Bays = (opts.L5Bays != null && opts.L5Bays.Count > 0) ? opts.L5Bays : new List<double> { 1800, 1800, 1800 };
            List<double> l6Bays = (opts.L6Bays != null && opts.L6Bays.Count > 0) ? opts.L6Bays : new List<double> { 1800, 1800, 1800 };

            bool c1 = opts.CheckCase11;
            bool c2 = opts.CheckCase12;
            bool c3 = opts.CheckCase13;
            bool c4 = opts.CheckCase14;
            bool c5 = opts.CheckCase21;
            bool c6 = opts.CheckCase22;

            if (!c1 && !c2 && !c3 && !c4 && !c5 && !c6)
            {
                return "Vui lòng tích chọn ít nhất 1 trường hợp (TH 1.1 -> TH 2.2) để chạy Master.";
            }

            // Nếu chỉ chọn duy nhất TH 2.2 mà không chọn TH 2.1 trước đó -> chạy trực tiếp TH 2.2 (L5 => L6)
            if (!c1 && !c2 && !c3 && !c4 && !c5 && c6)
            {
                opts.L1Bays = l5Bays;
                opts.L2Bays = l6Bays;
                return ExecuteCase2_2(doc, opts, level, placedIds);
            }

            double zH1 = z0 + jackH + 135;

            HashSet<string> placedCols = new HashSet<string>();
            Action<double, double> EnsureCol = (cx, cy) =>
            {
                string key = string.Format("{0:F0}_{1:F0}", cx, cy);
                if (!placedCols.Contains(key))
                {
                    placedCols.Add(key);
                    var jk = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(z0)), 0, "h", jackH);
                    if (jk != null) placedIds.Add(jk.Id.GetIdValue());

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
                    if (fFoot != null) placedIds.Add(fFoot.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var fDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(sDeck)), trot, "l", tlen);
                        if (fDeck != null) placedIds.Add(fDeck.Id.GetIdValue());
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var fTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(topZ)), trot, "l", tlen);
                        if (fTop != null) placedIds.Add(fTop.Id.GetIdValue());
                    }
                }
            };

            // Đóng đầu hồi phương X (tại X = xGable, Y từ yBack đến yBack + W, rot = 90)
            Action<double, double, bool> CloseEndGableX = (xGable, yBack, isLeftStart) =>
            {
                PlaceSharedTransom(xGable, yBack, 90, W);
                double tbX = isLeftStart ? (xGable + 66.0) : (xGable - 66.0);
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xGable), MmToFt(yBack), MmToFt(zH6)), 90, "l", W);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xGable), MmToFt(yBack), MmToFt(zH7)), 90, "l", W);
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(tbX), MmToFt(yBack), MmToFt(zDeck)), 90, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                }
            };

            // Đóng đầu hồi phương Y (tại Y = yGable, X từ xLeft đến xLeft + W, rot = 0)
            Action<double, double, bool> CloseEndGableY = (xLeft, yGable, isBottomEnd) =>
            {
                PlaceSharedTransom(xLeft, yGable, 0, W);
                var bb = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(z0)), 0, "l", W);
                if (bb != null) placedIds.Add(bb.Id.GetIdValue());

                double tbY = isBottomEnd ? (yGable + 66.0) : (yGable - 66.0);
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH6)), 0, "l", W);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH7)), 0, "l", W);
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft), MmToFt(tbY), MmToFt(zDeck)), 0, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                }
            };

            // Dựng 1 nhịp thẳng chuẩn theo hướng +X (Y in [yBack, yFront])
            Action<double, double, double> BuildBayPlusX = (xStart, yBack, L_bay) =>
            {
                double yFront = yBack + W;
                EnsureCol(xStart, yBack); EnsureCol(xStart, yFront);
                EnsureCol(xStart + L_bay, yBack); EnsureCol(xStart + L_bay, yFront);
                PlaceSharedTransom(xStart, yBack, 90, W);
                PlaceSharedTransom(xStart + L_bay, yBack, 90, W);
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, xStart, yFront, z0, W, jackH, L_bay, true, opts);
            };

            // Dựng 1 nhịp thẳng chuẩn theo hướng +Y (X in [xLeft, xRight], từ yStart lên yStart + L_bay)
            Action<double, double, double> BuildBayPlusY = (xLeft, yStart, L_bay) =>
            {
                double xRight = xLeft + W;
                double yNext = yStart + L_bay;
                EnsureCol(xLeft, yStart); EnsureCol(xRight, yStart);
                EnsureCol(xLeft, yNext); EnsureCol(xRight, yNext);
                PlaceSharedTransom(xLeft, yStart, 0, W);
                PlaceSharedTransom(xLeft, yNext, 0, W);

                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(z0)), 90, "l", L_bay);
                var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(z0)), 90, "l", L_bay);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH3)), 90, "l", L_bay);
                    if (lgML != null) placedIds.Add(lgML.Id.GetIdValue());

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, yStart, 90, L_bay, z0, jackH, s, storyCount, opts);

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH6)), 90, "l", L_bay);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH7)), 90, "l", L_bay);
                    if (rL1 != null) placedIds.Add(rL1.Id.GetIdValue());
                    if (rL2 != null) placedIds.Add(rL2.Id.GetIdValue());
                }
            };

            // Dựng 1 nhịp thẳng chuẩn theo hướng -Y (X in [xLeft, xRight], từ yStart xuống yStart - L_bay)
            Action<double, double, double> BuildBayMinusY = (xLeft, yStart, L_bay) =>
            {
                double xRight = xLeft + W;
                double yEnd = yStart - L_bay;
                EnsureCol(xLeft, yStart); EnsureCol(xRight, yStart);
                EnsureCol(xLeft, yEnd); EnsureCol(xRight, yEnd);
                PlaceSharedTransom(xLeft, yStart, 0, W);
                PlaceSharedTransom(xLeft, yEnd, 0, W);

                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(z0)), 90, "l", L_bay);
                var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(z0)), 90, "l", L_bay);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xLeft, yEnd, 90, L_bay, z0, jackH, s, storyCount, opts);

                    var lgMR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH3)), 90, "l", L_bay);
                    if (lgMR != null) placedIds.Add(lgMR.Id.GetIdValue());

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                    }

                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH6)), 90, "l", L_bay);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH7)), 90, "l", L_bay);
                    if (rR1 != null) placedIds.Add(rR1.Id.GetIdValue());
                    if (rR2 != null) placedIds.Add(rR2.Id.GetIdValue());
                }
            };

            // Biến trạng thái tọa độ liên tục qua các trường hợp
            double curX = x0;
            double curYBack = y0 - W;
            double curYFront = y0;
            double curXLeft = x0;
            double curYStart = y0;
            bool hasStarted = false;

            // =========================================================================
            // BƯỚC 1: CHECK 1 -> TH 1.1 (L0 => L1: Hướng +X rẽ +Y mép ngoài phẳng)
            // =========================================================================
            if (c1)
            {
                hasStarted = true;
                CloseEndGableX(curX, curYBack, true);

                for (int i = 0; i < l0Bays.Count - 1; i++)
                {
                    BuildBayPlusX(curX, curYBack, l0Bays[i]);
                    curX += l0Bays[i];
                }

                // Nhịp cuối của L0 là Góc 1 (TH 1.1)
                double L0_last = l0Bays[l0Bays.Count - 1];
                double xBayLast0 = curX;
                double xMax0 = xBayLast0 + L0_last;

                EnsureCol(xBayLast0, curYBack); EnsureCol(xBayLast0, curYFront);
                EnsureCol(xMax0, curYBack); EnsureCol(xMax0, curYFront);
                PlaceSharedTransom(xBayLast0, curYBack, 90, W);
                CloseEndGableX(xMax0, curYBack, false);

                // Mép sau curYBack: giữ nguyên L0_last với Giằng chéo Family 5
                var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack), MmToFt(z0)), 0, "l", L0_last);
                var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack), MmToFt(zH1)), 0, "l", L0_last);
                if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                if (lgFBack != null) placedIds.Add(lgFBack.Id.GetIdValue());
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L0_last);
                    if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast0, curYBack, 0, L0_last, z0, jackH, s, storyCount, opts);
                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L0_last);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L0_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L0_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }
                }

                // Mép trước curYFront: cắt ngắn L_cut1 = max(0, L0_last - W), mở cửa ngõ W sang +Y (L1)
                double xC1 = xMax0 - W;
                double L_cut1 = L0_last > W ? (L0_last - W) : 0.0;
                EnsureCol(xC1, curYFront);
                if (L_cut1 > 10.0)
                {
                    var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(z0)), 0, "l", L_cut1);
                    var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(zH1)), 0, "l", L_cut1);
                    if (bbFront != null) placedIds.Add(bbFront.Id.GetIdValue());
                    if (lgFFront != null) placedIds.Add(lgFFront.Id.GetIdValue());
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L_cut1);
                        if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;
                        var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(zH3)), 0, "l", L_cut1);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(zH6)), 0, "l", L_cut1);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(zH7)), 0, "l", L_cut1);
                        if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                        if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                        if (r2 != null) placedIds.Add(r2.Id.GetIdValue());
                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L_cut1);
                            if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                        }
                    }
                }

                PlaceSharedTransom(xC1, curYFront, 0, W);
                curXLeft = xC1;
                curYStart = curYFront;

                // Nếu Check 2 (TH 1.2) KHÔNG bật -> Dựng toàn bộ nhịp L1 theo hướng +Y rồi đóng đầu hồi kết thúc
                if (!c2)
                {
                    foreach (var bLen in l1Bays)
                    {
                        BuildBayPlusY(curXLeft, curYStart, bLen);
                        curYStart += bLen;
                    }
                    CloseEndGableY(curXLeft, curYStart, false);
                    return null;
                }
            }

            // =========================================================================
            // BƯỚC 2: CHECK 2 -> TH 1.2 CONTINUE (L1 => L2: Hướng +Y rẽ +X)
            // =========================================================================
            if (c2)
            {
                if (!hasStarted)
                {
                    hasStarted = true;
                    CloseEndGableY(curXLeft, curYStart, true);
                }

                for (int i = 0; i < l1Bays.Count - 1; i++)
                {
                    BuildBayPlusY(curXLeft, curYStart, l1Bays[i]);
                    curYStart += l1Bays[i];
                }

                // Nhịp cuối của L1 là Góc 2 (TH 1.2: rẽ +X)
                double L1_last = l1Bays[l1Bays.Count - 1];
                double yBayLast1 = curYStart;
                double yMax1 = yBayLast1 + L1_last;
                double xRight1 = curXLeft + W;

                EnsureCol(curXLeft, yBayLast1); EnsureCol(xRight1, yBayLast1);
                EnsureCol(curXLeft, yMax1); EnsureCol(xRight1, yMax1);
                PlaceSharedTransom(curXLeft, yBayLast1, 0, W);
                CloseEndGableY(curXLeft, yMax1, false);

                // Vách trái bên trong (X = curXLeft): Lan can Family 4 suốt L1_last
                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(z0)), 90, "l", L1_last);
                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(zH1)), 90, "l", L1_last);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(topZ)), 90, "l", L1_last);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(zH3)), 90, "l", L1_last);
                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(zH6)), 90, "l", L1_last);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(zH7)), 90, "l", L1_last);
                    if (lgML != null) placedIds.Add(lgML.Id.GetIdValue());
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight1 - pOff), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }
                }

                // Vách phải (X = xRight1): mở cửa ngõ W sang +X (L2), cắt ngắn L_cut2 nếu L1_last > W
                double yC2 = yMax1 - W;
                double L_cut2 = L1_last > W ? (L1_last - W) : 0.0;
                EnsureCol(xRight1, yC2);
                if (L_cut2 > 10.0)
                {
                    var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight1), MmToFt(yBayLast1), MmToFt(z0)), 90, "l", L_cut2);
                    var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight1), MmToFt(yBayLast1), MmToFt(zH1)), 90, "l", L_cut2);
                    if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                    if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight1), MmToFt(yBayLast1), MmToFt(topZ)), 90, "l", L_cut2);
                        if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight1, yBayLast1, 90, L_cut2, z0, jackH, s, storyCount, opts);
                        if (sym7 != null)
                        {
                            var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight1 - 66.0), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L_cut2);
                            if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                        }
                    }
                }

                PlaceSharedTransom(xRight1, yC2, 90, W);
                curX = xRight1;
                curYBack = yC2;
                curYFront = yMax1;

                if (!c3)
                {
                    foreach (var bLen in l2Bays)
                    {
                        BuildBayPlusX(curX, curYBack, bLen);
                        curX += bLen;
                    }
                    CloseEndGableX(curX, curYBack, false);
                    return null;
                }
            }

            // =========================================================================
            // BƯỚC 3: CHECK 3 -> TH 1.3 CONTINUE (L2 => L3: Hướng +X rẽ -Y giằng chéo thu ngắn)
            // =========================================================================
            if (c3)
            {
                if (!hasStarted)
                {
                    hasStarted = true;
                    CloseEndGableX(curX, curYBack, true);
                }

                for (int i = 0; i < l2Bays.Count - 1; i++)
                {
                    BuildBayPlusX(curX, curYBack, l2Bays[i]);
                    curX += l2Bays[i];
                }

                // Nhịp cuối của L2 là Góc 3 (TH 1.3: rẽ -Y)
                double L2_last = l2Bays[l2Bays.Count - 1];
                double xBayLast2 = curX;
                double xMax2 = xBayLast2 + L2_last;

                EnsureCol(xBayLast2, curYBack); EnsureCol(xBayLast2, curYFront);
                EnsureCol(xMax2, curYBack); EnsureCol(xMax2, curYFront);
                PlaceSharedTransom(xBayLast2, curYBack, 90, W);
                CloseEndGableX(xMax2, curYBack, false);

                // Mép trên curYFront: giữ nguyên L2_last với Lan can Family 4
                var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(z0)), 0, "l", L2_last);
                var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(zH1)), 0, "l", L2_last);
                if (bbFront != null) placedIds.Add(bbFront.Id.GetIdValue());
                if (lgFFront != null) placedIds.Add(lgFFront.Id.GetIdValue());
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L2_last);
                    if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(zH3)), 0, "l", L2_last);
                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(zH6)), 0, "l", L2_last);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(zH7)), 0, "l", L2_last);
                    if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L2_last);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L2_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L2_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }
                }

                // Mép dưới curYBack: cắt ngắn L_cut3 với GIẰNG CHÉO THU NGẮN Family 5, mở cửa ngõ W sang -Y (L3)
                double xC3 = xMax2 - W;
                double L_cut3 = L2_last > W ? (L2_last - W) : 0.0;
                EnsureCol(xC3, curYBack);
                if (L_cut3 > 10.0)
                {
                    var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack), MmToFt(z0)), 0, "l", L_cut3);
                    var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack), MmToFt(zH1)), 0, "l", L_cut3);
                    if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                    if (lgFBack != null) placedIds.Add(lgFBack.Id.GetIdValue());
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L_cut3);
                        if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast2, curYBack, 0, L_cut3, z0, jackH, s, storyCount, opts);
                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L_cut3);
                            if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                        }
                    }
                }

                PlaceSharedTransom(xC3, curYBack, 0, W);
                curXLeft = xC3;
                curYStart = curYBack;

                if (!c4)
                {
                    foreach (var bLen in l3Bays)
                    {
                        BuildBayMinusY(curXLeft, curYStart, bLen);
                        curYStart -= bLen;
                    }
                    CloseEndGableY(curXLeft, curYStart, true);
                    return null;
                }
            }

            // =========================================================================
            // BƯỚC 4: CHECK 4 -> TH 1.4 CONTINUE (L3 => L4: Hướng -Y rẽ +X)
            // =========================================================================
            if (c4)
            {
                if (!hasStarted)
                {
                    hasStarted = true;
                    CloseEndGableY(curXLeft, curYStart, false);
                }

                for (int i = 0; i < l3Bays.Count - 1; i++)
                {
                    BuildBayMinusY(curXLeft, curYStart, l3Bays[i]);
                    curYStart -= l3Bays[i];
                }

                // Nhịp cuối của L3 là Góc 4 (TH 1.4: rẽ +X)
                double L3_last = l3Bays[l3Bays.Count - 1];
                double yBayLast3 = curYStart;
                double yMin3 = yBayLast3 - L3_last;
                double xRight3 = curXLeft + W;

                EnsureCol(curXLeft, yBayLast3); EnsureCol(xRight3, yBayLast3);
                EnsureCol(curXLeft, yMin3); EnsureCol(xRight3, yMin3);
                PlaceSharedTransom(curXLeft, yBayLast3, 0, W);
                CloseEndGableY(curXLeft, yMin3, true);

                // Vách trong bên trái (X = curXLeft): Giằng chéo Family 5 suốt L3_last
                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin3), MmToFt(z0)), 90, "l", L3_last);
                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin3), MmToFt(zH1)), 90, "l", L3_last);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin3), MmToFt(topZ)), 90, "l", L3_last);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, curXLeft, yMin3, 90, L3_last, z0, jackH, s, storyCount, opts);
                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight3 - pOff), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }
                }

                // Vách phải (X = xRight3): mở cửa ngõ W ở đáy [yMin3, yC4] sang +X (L4), cắt ngắn L_cut4 nếu L3_last > W
                double yC4 = yMin3 + W;
                double L_cut4 = L3_last > W ? (L3_last - W) : 0.0;
                EnsureCol(xRight3, yC4);
                if (L_cut4 > 10.0)
                {
                    var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(z0)), 90, "l", L_cut4);
                    var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(zH1)), 90, "l", L_cut4);
                    if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                    if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(topZ)), 90, "l", L_cut4);
                        if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;
                        var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(zH3)), 90, "l", L_cut4);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(zH6)), 90, "l", L_cut4);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(zH7)), 90, "l", L_cut4);
                        if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                        if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                        if (r2 != null) placedIds.Add(r2.Id.GetIdValue());
                        if (sym7 != null)
                        {
                            var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight3 - 66.0), MmToFt(yC4), MmToFt(zDeck)), 90, "l", L_cut4);
                            if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                        }
                    }
                }

                PlaceSharedTransom(xRight3, yMin3, 90, W);
                curX = xRight3;
                curYBack = yMin3;
                curYFront = yC4;

                if (!c5)
                {
                    foreach (var bLen in l4Bays)
                    {
                        BuildBayPlusX(curX, curYBack, bLen);
                        curX += bLen;
                    }
                    CloseEndGableX(curX, curYBack, false);
                    return null;
                }
            }

            // =========================================================================
            // BƯỚC 5: CHECK 5 -> TH 2.1 CONTINUE (L4 => L5: Hướng +X vươn góc ngoài +Y)
            // =========================================================================
            if (c5)
            {
                if (!hasStarted)
                {
                    hasStarted = true;
                    CloseEndGableX(curX, curYBack, true);
                }

                // Dựng toàn bộ các nhịp thẳng của L4
                foreach (var bLen in l4Bays)
                {
                    BuildBayPlusX(curX, curYBack, bLen);
                    curX += bLen;
                }

                // Góc 5 (TH 2.1: Nhánh rẽ vươn ngoài tại X in [xA, xB = xA + W], Y từ curYBack lên yTop5 = curYBack + L5[0])
                double xA = curX;
                double xB = xA + W;
                double L5_first = l5Bays[0];
                double yTop5 = curYBack + L5_first;
                double L_A5 = (yTop5 - curYFront > 10.0) ? (yTop5 - curYFront) : 0.0;

                EnsureCol(xB, curYBack); EnsureCol(xB, yTop5);
                if (L_A5 > 10.0) EnsureCol(xA, yTop5);

                PlaceSharedTransom(xA, curYBack, 0, W);
                PlaceSharedTransom(xA, yTop5, 0, W);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYBack), MmToFt(z0)), 0, "l", W);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(z0)), 90, "l", L5_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
                if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

                var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(zH1)), 90, "l", L5_first);
                if (lgRF_B != null) placedIds.Add(lgRF_B.Id.GetIdValue());
                if (L_A5 > 10.0)
                {
                    var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(z0)), 90, "l", L_A5);
                    var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH1)), 90, "l", L_A5);
                    if (bbIn != null) placedIds.Add(bbIn.Id.GetIdValue());
                    if (lgRF_A != null) placedIds.Add(lgRF_A.Id.GetIdValue());
                }

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(topZ)), 90, "l", L5_first);
                    if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.GetIdValue());
                    if (L_A5 > 10.0)
                    {
                        var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(topZ)), 90, "l", L_A5);
                        if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.GetIdValue());
                    }
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    // Mép sau phẳng curYBack & Mép ngoài phải xB: Giằng chéo Family 5 đúng chuẩn TH 2.1
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, curYBack, 0, W, z0, jackH, s, storyCount, opts);
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, curYBack, 90, L5_first, z0, jackH, s, storyCount, opts);

                    if (L_A5 > 10.0)
                    {
                        var lgRM_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH3)), 90, "l", L_A5);
                        var rA1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH6)), 90, "l", L_A5);
                        var rA2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH7)), 90, "l", L_A5);
                        if (lgRM_A != null) placedIds.Add(lgRM_A.Id.GetIdValue());
                        if (rA1 != null) placedIds.Add(rA1.Id.GetIdValue());
                        if (rA2 != null) placedIds.Add(rA2.Id.GetIdValue());
                    }

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", W);
                        var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66.0), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        if (tbBack != null) placedIds.Add(tbBack.Id.GetIdValue());
                        if (tbEndB != null) placedIds.Add(tbEndB.Id.GetIdValue());
                        if (L_A5 > 10.0)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(curYFront), MmToFt(zDeck)), 90, "l", L_A5);
                            if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                        }
                    }
                }

                // Dựng các nhịp tiếp theo (1 .. l5Bays.Count - 1) của L5 theo hướng +Y
                curXLeft = xA;
                curYStart = yTop5;
                for (int j = 1; j < l5Bays.Count; j++)
                {
                    BuildBayPlusY(curXLeft, curYStart, l5Bays[j]);
                    curYStart += l5Bays[j];
                }

                if (!c6)
                {
                    CloseEndGableY(curXLeft, curYStart, false);
                    return null;
                }
            }

            // =========================================================================
            // BƯỚC 6: CHECK 6 -> TH 2.2 CONTINUE (L5 => L6: Vươn góc ngoài sang +X)
            // =========================================================================
            if (c6)
            {
                double xA = curXLeft;
                double xB = xA + W;
                double yBot6 = curYStart;
                double yTop6 = yBot6 + W;
                double L6_first = l6Bays[0];
                double xRight6 = xA + L6_first;
                double L_A6 = (xRight6 - xB > 10.0) ? (xRight6 - xB) : 0.0;

                EnsureCol(xA, yTop6); EnsureCol(xRight6, yTop6);
                if (L_A6 > 10.0) EnsureCol(xRight6, yBot6);

                PlaceSharedTransom(xA, yBot6, 90, W);
                PlaceSharedTransom(xRight6, yBot6, 90, W);

                // Mép trước phẳng X = xA (Y: yBot6 -> yTop6, L = W): Lan can kép + giằng giữa h3 chuẩn TH 2.2
                var bbC6_F = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(z0)), 90, "l", W);
                var bbC6_T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(z0)), 0, "l", L6_first);
                if (bbC6_F != null) placedIds.Add(bbC6_F.Id.GetIdValue());
                if (bbC6_T != null) placedIds.Add(bbC6_T.Id.GetIdValue());

                var lgFC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH1)), 0, "l", L6_first);
                if (lgFC6_T != null) placedIds.Add(lgFC6_T.Id.GetIdValue());

                if (L_A6 > 10.0)
                {
                    var bbC6_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(z0)), 0, "l", L_A6);
                    var lgFC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(zH1)), 0, "l", L_A6);
                    if (bbC6_In != null) placedIds.Add(bbC6_In.Id.GetIdValue());
                    if (lgFC6_In != null) placedIds.Add(lgFC6_In.Id.GetIdValue());
                }

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(topZ)), 0, "l", L6_first);
                    if (lgTopC6_T != null) placedIds.Add(lgTopC6_T.Id.GetIdValue());
                    if (L_A6 > 10.0)
                    {
                        var lgTopC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(topZ)), 0, "l", L_A6);
                        if (lgTopC6_In != null) placedIds.Add(lgTopC6_In.Id.GetIdValue());
                    }
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgMC6_F = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH3)), 90, "l", W);
                    var rC6_F1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH6)), 90, "l", W);
                    var rC6_F2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH7)), 90, "l", W);
                    if (lgMC6_F != null) placedIds.Add(lgMC6_F.Id.GetIdValue());
                    if (rC6_F1 != null) placedIds.Add(rC6_F1.Id.GetIdValue());
                    if (rC6_F2 != null) placedIds.Add(rC6_F2.Id.GetIdValue());

                    var lgMC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH3)), 0, "l", L6_first);
                    var rC6_T1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH6)), 0, "l", L6_first);
                    var rC6_T2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH7)), 0, "l", L6_first);
                    if (lgMC6_T != null) placedIds.Add(lgMC6_T.Id.GetIdValue());
                    if (rC6_T1 != null) placedIds.Add(rC6_T1.Id.GetIdValue());
                    if (rC6_T2 != null) placedIds.Add(rC6_T2.Id.GetIdValue());

                    if (L_A6 > 10.0)
                    {
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yBot6, 0, L_A6, z0, jackH, s, storyCount, opts);
                    }

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yBot6 + pOff), MmToFt(zDeck)), 0, "l", L6_first);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - pOff), MmToFt(zDeck)), 0, "l", L6_first);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(yBot6), MmToFt(zDeck)), 90, "l", W);
                        var tbT = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - 66.0), MmToFt(zDeck)), 0, "l", L6_first);
                        if (tbF != null) placedIds.Add(tbF.Id.GetIdValue());
                        if (tbT != null) placedIds.Add(tbT.Id.GetIdValue());
                        if (L_A6 > 10.0)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB), MmToFt(yBot6 + 66.0), MmToFt(zDeck)), 0, "l", L_A6);
                            if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                        }
                    }
                }

                // Dựng các nhịp tiếp theo (1 .. l6Bays.Count - 1) của L6 theo hướng +X và đóng đầu hồi kết thúc
                curX = xRight6;
                curYBack = yBot6;
                for (int j = 1; j < l6Bays.Count; j++)
                {
                    BuildBayPlusX(curX, curYBack, l6Bays[j]);
                    curX += l6Bays[j];
                }
                CloseEndGableX(curX, curYBack, false);
            }

            return null;
        }

        public static string NormalizeConnectCase(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "TH 0";
            string s = raw.Trim().ToUpperInvariant().Replace(" ", "");
            if (s == "TH0" || s == "0" || s == "NONE") return "TH 0";
            if (s.Contains("1.1")) return "TH 1.1";
            if (s.Contains("1.2")) return "TH 1.2";
            if (s.Contains("1.3")) return "TH 1.3";
            if (s.Contains("1.4")) return "TH 1.4";
            if (s.Contains("2.1")) return "TH 2.1";
            if (s.Contains("2.2")) return "TH 2.2";
            return "TH 0";
        }

        private enum LegDir { PlusX, PlusY, MinusY }

        /// <summary>
        /// BUTTON RUN THEO BẢNG TABLE:
        /// Tương tự Button Master nhưng đọc trực tiếp danh sách hàng trong Table:
        /// - Row 1 như là L0, Row 2 như là L1, ...
        /// - Cột Connect của mỗi hàng chọn trực tiếp hàm góc nối TH nào để chạy (TH 1.1 .. TH 2.2), không ép chạy tuần tự 6 TH.
        /// - Hàng cuối nếu có connect với Row 1 (Connect != TH 0) thì nối góc về Row 1; còn không thì Connect là TH 0 (bỏ qua connect, đóng đầu hồi).
        /// </summary>
        public static string ExecuteTableRunContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            if (opts == null || opts.TableRows == null || opts.TableRows.Count == 0)
            {
                return "Bảng hệ giàn trống! Vui lòng Quét (Select) hoặc kiểm tra các hàng trong bảng trước khi bấm Run.";
            }

            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);
            double zH1 = z0 + jackH + 135;

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

            HashSet<string> placedCols = new HashSet<string>();
            Action<double, double> EnsureCol = (cx, cy) =>
            {
                string key = string.Format("{0:F0}_{1:F0}", cx, cy);
                if (!placedCols.Contains(key))
                {
                    placedCols.Add(key);
                    var jk = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(z0)), 0, "h", jackH);
                    if (jk != null) placedIds.Add(jk.Id.GetIdValue());

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
                    if (fFoot != null) placedIds.Add(fFoot.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var fDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(sDeck)), trot, "l", tlen);
                        if (fDeck != null) placedIds.Add(fDeck.Id.GetIdValue());
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var fTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(topZ)), trot, "l", tlen);
                        if (fTop != null) placedIds.Add(fTop.Id.GetIdValue());
                    }
                }
            };

            Action<double, double, bool> CloseEndGableX = (xGable, yBack, isLeftStart) =>
            {
                PlaceSharedTransom(xGable, yBack, 90, W);
                double tbX = isLeftStart ? (xGable + 66.0) : (xGable - 66.0);
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xGable), MmToFt(yBack), MmToFt(zH6)), 90, "l", W);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xGable), MmToFt(yBack), MmToFt(zH7)), 90, "l", W);
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(tbX), MmToFt(yBack), MmToFt(zDeck)), 90, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                }
            };

            Action<double, double, bool> CloseEndGableY = (xLeft, yGable, isBottomEnd) =>
            {
                PlaceSharedTransom(xLeft, yGable, 0, W);
                var bb = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(z0)), 0, "l", W);
                if (bb != null) placedIds.Add(bb.Id.GetIdValue());

                double tbY = isBottomEnd ? (yGable + 66.0) : (yGable - 66.0);
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH6)), 0, "l", W);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH7)), 0, "l", W);
                    if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                    if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft), MmToFt(tbY), MmToFt(zDeck)), 0, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                    }
                }
            };

            Action<double, double, double> BuildBayPlusX = (xStart, yBack, L_bay) =>
            {
                double yFront = yBack + W;
                EnsureCol(xStart, yBack); EnsureCol(xStart, yFront);
                EnsureCol(xStart + L_bay, yBack); EnsureCol(xStart + L_bay, yFront);
                PlaceSharedTransom(xStart, yBack, 90, W);
                PlaceSharedTransom(xStart + L_bay, yBack, 90, W);
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, xStart, yFront, z0, W, jackH, L_bay, true, opts);
            };

            Action<double, double, double> BuildBayPlusY = (xLeft, yStart, L_bay) =>
            {
                double xRight = xLeft + W;
                double yNext = yStart + L_bay;
                EnsureCol(xLeft, yStart); EnsureCol(xRight, yStart);
                EnsureCol(xLeft, yNext); EnsureCol(xRight, yNext);
                PlaceSharedTransom(xLeft, yStart, 0, W);
                PlaceSharedTransom(xLeft, yNext, 0, W);

                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(z0)), 90, "l", L_bay);
                var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(z0)), 90, "l", L_bay);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH3)), 90, "l", L_bay);
                    if (lgML != null) placedIds.Add(lgML.Id.GetIdValue());

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, yStart, 90, L_bay, z0, jackH, s, storyCount, opts);

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH6)), 90, "l", L_bay);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH7)), 90, "l", L_bay);
                    if (rL1 != null) placedIds.Add(rL1.Id.GetIdValue());
                    if (rL2 != null) placedIds.Add(rL2.Id.GetIdValue());
                }
            };

            Action<double, double, double> BuildBayMinusY = (xLeft, yStart, L_bay) =>
            {
                double xRight = xLeft + W;
                double yEnd = yStart - L_bay;
                EnsureCol(xLeft, yStart); EnsureCol(xRight, yStart);
                EnsureCol(xLeft, yEnd); EnsureCol(xRight, yEnd);
                PlaceSharedTransom(xLeft, yStart, 0, W);
                PlaceSharedTransom(xLeft, yEnd, 0, W);

                var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(z0)), 90, "l", L_bay);
                var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(z0)), 90, "l", L_bay);
                if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xLeft, yEnd, 90, L_bay, z0, jackH, s, storyCount, opts);

                    var lgMR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH3)), 90, "l", L_bay);
                    if (lgMR != null) placedIds.Add(lgMR.Id.GetIdValue());

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                    }

                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH6)), 90, "l", L_bay);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH7)), 90, "l", L_bay);
                    if (rR1 != null) placedIds.Add(rR1.Id.GetIdValue());
                    if (rR2 != null) placedIds.Add(rR2.Id.GetIdValue());
                }
            };

            List<TableRowOption> rows = opts.TableRows;
            int nRows = rows.Count;
            bool lastConnectsToFirst = (nRows >= 2) && (NormalizeConnectCase(rows[nRows - 1].ConnectCase) != "TH 0");

            bool hasAnyInstanceCoords = false;
            for (int rIdx = 0; rIdx < nRows; rIdx++)
            {
                if (rows[rIdx].HasCoords)
                {
                    hasAnyInstanceCoords = true;
                    break;
                }
            }

            if (hasAnyInstanceCoords)
            {
                return ExecuteTableRunByInstanceCoords(
                    doc, opts, level, placedIds,
                    sym1, sym2, sym3, sym4, sym5, sym6, sym7,
                    rows, z0, W, pw, pOff, jackH, storyCount, topZ, zH1, lastConnectsToFirst);
            }

            double curX = x0;
            double curYBack = y0 - W;
            double curYFront = y0;
            double curXLeft = x0;
            double curYStart = y0;
            LegDir curDir = LegDir.PlusX;
            int startBayIdx = 0;
            bool hasStarted = false;

            for (int i = 0; i < nRows; i++)
            {
                List<double> curBays = (rows[i].BayLengths != null && rows[i].BayLengths.Count > 0)
                    ? rows[i].BayLengths
                    : new List<double> { 1800.0 };

                string conn = NormalizeConnectCase(rows[i].ConnectCase);

                // Nếu chỉ có 1 hàng trong bảng, hoặc đây là hàng cuối cùng mà Connect == TH 0 -> Bỏ qua connect, dựng nốt các nhịp và đóng đầu hồi
                bool hasConnect = (conn != "TH 0") && (nRows >= 2) && (i < nRows - 1 || lastConnectsToFirst);

                if (!hasStarted)
                {
                    hasStarted = true;
                    if (!lastConnectsToFirst)
                    {
                        CloseEndGableX(curX, curYBack, true);
                    }
                    else
                    {
                        PlaceSharedTransom(curX, curYBack, 90, W);
                    }
                }

                if (!hasConnect)
                {
                    if (curDir == LegDir.PlusX)
                    {
                        for (int b = startBayIdx; b < curBays.Count; b++)
                        {
                            BuildBayPlusX(curX, curYBack, curBays[b]);
                            curX += curBays[b];
                        }
                        CloseEndGableX(curX, curYBack, false);
                    }
                    else if (curDir == LegDir.PlusY)
                    {
                        for (int b = startBayIdx; b < curBays.Count; b++)
                        {
                            BuildBayPlusY(curXLeft, curYStart, curBays[b]);
                            curYStart += curBays[b];
                        }
                        CloseEndGableY(curXLeft, curYStart, false);
                    }
                    else
                    {
                        for (int b = startBayIdx; b < curBays.Count; b++)
                        {
                            BuildBayMinusY(curXLeft, curYStart, curBays[b]);
                            curYStart -= curBays[b];
                        }
                        CloseEndGableY(curXLeft, curYStart, true);
                    }
                    startBayIdx = 0;
                    hasStarted = false;
                    curDir = LegDir.PlusX;
                    if (i + 1 < nRows && rows[i + 1].HasCoords)
                    {
                        curX = rows[i + 1].StartX;
                        curYFront = rows[i + 1].StartY;
                        curYBack = curYFront - W;
                        curXLeft = curX;
                        curYStart = curYFront;
                    }
                    else
                    {
                        curX += W * 2.0;
                        curXLeft = curX;
                    }
                    continue;
                }

                TableRowOption nextRow = (i < nRows - 1) ? rows[i + 1] : rows[0];
                List<double> nextBays = (nextRow.BayLengths != null && nextRow.BayLengths.Count > 0)
                    ? nextRow.BayLengths
                    : new List<double> { 1800.0 };

                // =========================================================================
                // NHÓM 1: TH 1.1 HOẶC TH 1.2 (Góc thu trong: +X -> +Y hoặc +Y/-Y -> +X)
                // =========================================================================
                if (conn == "TH 1.1" || conn == "TH 1.2")
                {
                    if (curDir == LegDir.PlusX)
                    {
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayPlusX(curX, curYBack, curBays[b]);
                            curX += curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double xBayLast = curX;
                        double xMax = xBayLast + L_last;

                        EnsureCol(xBayLast, curYBack); EnsureCol(xBayLast, curYFront);
                        EnsureCol(xMax, curYBack); EnsureCol(xMax, curYFront);
                        PlaceSharedTransom(xBayLast, curYBack, 90, W);
                        CloseEndGableX(xMax, curYBack, false);

                        var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(z0)), 0, "l", L_last);
                        var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(zH1)), 0, "l", L_last);
                        if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                        if (lgFBack != null) placedIds.Add(lgFBack.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L_last);
                            if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, curYBack, 0, L_last, z0, jackH, s, storyCount, opts);
                            if (sym7 != null)
                            {
                                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L_last);
                                if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double xC1 = xMax - W;
                        double L_cut1 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xC1, curYFront);
                        if (L_cut1 > 10.0)
                        {
                            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(z0)), 0, "l", L_cut1);
                            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH1)), 0, "l", L_cut1);
                            if (bbFront != null) placedIds.Add(bbFront.Id.GetIdValue());
                            if (lgFFront != null) placedIds.Add(lgFFront.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L_cut1);
                                if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zH3 = z0 + jackH + 830.0 + sOff;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                double zH6 = z0 + jackH + 2180.0 + sOff;
                                double zH7 = z0 + jackH + 2630.0 + sOff;
                                var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH3)), 0, "l", L_cut1);
                                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH6)), 0, "l", L_cut1);
                                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH7)), 0, "l", L_cut1);
                                if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                                if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                                if (r2 != null) placedIds.Add(r2.Id.GetIdValue());
                                if (sym7 != null)
                                {
                                    var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L_cut1);
                                    if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xC1, curYFront, 0, W);
                        curXLeft = xC1;
                        curYStart = curYFront;
                        curDir = LegDir.PlusY;
                        startBayIdx = 0;
                    }
                    else if (curDir == LegDir.PlusY)
                    {
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayPlusY(curXLeft, curYStart, curBays[b]);
                            curYStart += curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double yBayLast = curYStart;
                        double yMax = yBayLast + L_last;
                        double xRight = curXLeft + W;

                        EnsureCol(curXLeft, yBayLast); EnsureCol(xRight, yBayLast);
                        EnsureCol(curXLeft, yMax); EnsureCol(xRight, yMax);
                        PlaceSharedTransom(curXLeft, yBayLast, 0, W);
                        CloseEndGableY(curXLeft, yMax, false);

                        var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(z0)), 90, "l", L_last);
                        var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH1)), 90, "l", L_last);
                        if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                        if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(topZ)), 90, "l", L_last);
                            if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH3)), 90, "l", L_last);
                            var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH6)), 90, "l", L_last);
                            var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH7)), 90, "l", L_last);
                            if (lgML != null) placedIds.Add(lgML.Id.GetIdValue());
                            if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                            if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                            if (sym7 != null)
                            {
                                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double yC2 = yMax - W;
                        double L_cut2 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xRight, yC2);
                        if (L_cut2 > 10.0)
                        {
                            var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(z0)), 90, "l", L_cut2);
                            var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(zH1)), 90, "l", L_cut2);
                            if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                            if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(topZ)), 90, "l", L_cut2);
                                if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, yBayLast, 90, L_cut2, z0, jackH, s, storyCount, opts);
                                if (sym7 != null)
                                {
                                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_cut2);
                                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xRight, yC2, 90, W);
                        curX = xRight;
                        curYBack = yC2;
                        curYFront = yMax;
                        curDir = LegDir.PlusX;
                        startBayIdx = 0;
                    }
                    else
                    {
                        // Khi curDir == MinusY rẽ sang +X
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayMinusY(curXLeft, curYStart, curBays[b]);
                            curYStart -= curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double yBayLast = curYStart;
                        double yMin = yBayLast - L_last;
                        double xRight = curXLeft + W;

                        EnsureCol(curXLeft, yBayLast); EnsureCol(xRight, yBayLast);
                        EnsureCol(curXLeft, yMin); EnsureCol(xRight, yMin);
                        PlaceSharedTransom(curXLeft, yBayLast, 0, W);
                        CloseEndGableY(curXLeft, yMin, true);

                        var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(z0)), 90, "l", L_last);
                        var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(zH1)), 90, "l", L_last);
                        if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                        if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(topZ)), 90, "l", L_last);
                            if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, curXLeft, yMin, 90, L_last, z0, jackH, s, storyCount, opts);
                            if (sym7 != null)
                            {
                                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double yC4 = yMin + W;
                        double L_cut4 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xRight, yC4);
                        if (L_cut4 > 10.0)
                        {
                            var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(z0)), 90, "l", L_cut4);
                            var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH1)), 90, "l", L_cut4);
                            if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                            if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(topZ)), 90, "l", L_cut4);
                                if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zH3 = z0 + jackH + 830.0 + sOff;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                double zH6 = z0 + jackH + 2180.0 + sOff;
                                double zH7 = z0 + jackH + 2630.0 + sOff;
                                var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH3)), 90, "l", L_cut4);
                                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH6)), 90, "l", L_cut4);
                                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH7)), 90, "l", L_cut4);
                                if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                                if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                                if (r2 != null) placedIds.Add(r2.Id.GetIdValue());
                                if (sym7 != null)
                                {
                                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yC4), MmToFt(zDeck)), 90, "l", L_cut4);
                                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xRight, yMin, 90, W);
                        curX = xRight;
                        curYBack = yMin;
                        curYFront = yC4;
                        curDir = LegDir.PlusX;
                        startBayIdx = 0;
                    }
                }
                // =========================================================================
                // NHÓM 2: TH 1.3 HOẶC TH 1.4 (Góc thu trong hướng -Y / +X)
                // =========================================================================
                else if (conn == "TH 1.3" || conn == "TH 1.4")
                {
                    if (curDir == LegDir.PlusX)
                    {
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayPlusX(curX, curYBack, curBays[b]);
                            curX += curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double xBayLast = curX;
                        double xMax = xBayLast + L_last;

                        EnsureCol(xBayLast, curYBack); EnsureCol(xBayLast, curYFront);
                        EnsureCol(xMax, curYBack); EnsureCol(xMax, curYFront);
                        PlaceSharedTransom(xBayLast, curYBack, 90, W);
                        CloseEndGableX(xMax, curYBack, false);

                        var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(z0)), 0, "l", L_last);
                        var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH1)), 0, "l", L_last);
                        if (bbFront != null) placedIds.Add(bbFront.Id.GetIdValue());
                        if (lgFFront != null) placedIds.Add(lgFFront.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L_last);
                            if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH3)), 0, "l", L_last);
                            var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH6)), 0, "l", L_last);
                            var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront), MmToFt(zH7)), 0, "l", L_last);
                            if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                            if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                            if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                            if (sym7 != null)
                            {
                                var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L_last);
                                if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double xC3 = xMax - W;
                        double L_cut3 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xC3, curYBack);
                        if (L_cut3 > 10.0)
                        {
                            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(z0)), 0, "l", L_cut3);
                            var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(zH1)), 0, "l", L_cut3);
                            if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                            if (lgFBack != null) placedIds.Add(lgFBack.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L_cut3);
                                if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast, curYBack, 0, L_cut3, z0, jackH, s, storyCount, opts);
                                if (sym7 != null)
                                {
                                    var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L_cut3);
                                    if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xC3, curYBack, 0, W);
                        curXLeft = xC3;
                        curYStart = curYBack;
                        curDir = LegDir.MinusY;
                        startBayIdx = 0;
                    }
                    else if (curDir == LegDir.MinusY)
                    {
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayMinusY(curXLeft, curYStart, curBays[b]);
                            curYStart -= curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double yBayLast = curYStart;
                        double yMin = yBayLast - L_last;
                        double xRight = curXLeft + W;

                        EnsureCol(curXLeft, yBayLast); EnsureCol(xRight, yBayLast);
                        EnsureCol(curXLeft, yMin); EnsureCol(xRight, yMin);
                        PlaceSharedTransom(curXLeft, yBayLast, 0, W);
                        CloseEndGableY(curXLeft, yMin, true);

                        var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(z0)), 90, "l", L_last);
                        var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(zH1)), 90, "l", L_last);
                        if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                        if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin), MmToFt(topZ)), 90, "l", L_last);
                            if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, curXLeft, yMin, 90, L_last, z0, jackH, s, storyCount, opts);
                            if (sym7 != null)
                            {
                                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yMin), MmToFt(zDeck)), 90, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double yC4 = yMin + W;
                        double L_cut4 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xRight, yC4);
                        if (L_cut4 > 10.0)
                        {
                            var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(z0)), 90, "l", L_cut4);
                            var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH1)), 90, "l", L_cut4);
                            if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                            if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(topZ)), 90, "l", L_cut4);
                                if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zH3 = z0 + jackH + 830.0 + sOff;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                double zH6 = z0 + jackH + 2180.0 + sOff;
                                double zH7 = z0 + jackH + 2630.0 + sOff;
                                var lgM = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH3)), 90, "l", L_cut4);
                                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH6)), 90, "l", L_cut4);
                                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yC4), MmToFt(zH7)), 90, "l", L_cut4);
                                if (lgM != null) placedIds.Add(lgM.Id.GetIdValue());
                                if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                                if (r2 != null) placedIds.Add(r2.Id.GetIdValue());
                                if (sym7 != null)
                                {
                                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yC4), MmToFt(zDeck)), 90, "l", L_cut4);
                                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xRight, yMin, 90, W);
                        curX = xRight;
                        curYBack = yMin;
                        curYFront = yC4;
                        curDir = LegDir.PlusX;
                        startBayIdx = 0;
                    }
                    else
                    {
                        // Khi curDir == PlusY rẽ sang +X
                        for (int b = startBayIdx; b < curBays.Count - 1; b++)
                        {
                            BuildBayPlusY(curXLeft, curYStart, curBays[b]);
                            curYStart += curBays[b];
                        }

                        double L_last = curBays[curBays.Count - 1];
                        double yBayLast = curYStart;
                        double yMax = yBayLast + L_last;
                        double xRight = curXLeft + W;

                        EnsureCol(curXLeft, yBayLast); EnsureCol(xRight, yBayLast);
                        EnsureCol(curXLeft, yMax); EnsureCol(xRight, yMax);
                        PlaceSharedTransom(curXLeft, yBayLast, 0, W);
                        CloseEndGableY(curXLeft, yMax, false);

                        var bbL = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(z0)), 90, "l", L_last);
                        var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH1)), 90, "l", L_last);
                        if (bbL != null) placedIds.Add(bbL.Id.GetIdValue());
                        if (lgFL != null) placedIds.Add(lgFL.Id.GetIdValue());
                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(topZ)), 90, "l", L_last);
                            if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH3)), 90, "l", L_last);
                            var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH6)), 90, "l", L_last);
                            var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast), MmToFt(zH7)), 90, "l", L_last);
                            if (lgML != null) placedIds.Add(lgML.Id.GetIdValue());
                            if (r1 != null) placedIds.Add(r1.Id.GetIdValue());
                            if (r2 != null) placedIds.Add(r2.Id.GetIdValue());

                            if (sym7 != null)
                            {
                                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                            }
                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_last);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }
                        }

                        double yC2 = yMax - W;
                        double L_cut2 = L_last > W ? (L_last - W) : 0.0;
                        EnsureCol(xRight, yC2);
                        if (L_cut2 > 10.0)
                        {
                            var bbR = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(z0)), 90, "l", L_cut2);
                            var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(zH1)), 90, "l", L_cut2);
                            if (bbR != null) placedIds.Add(bbR.Id.GetIdValue());
                            if (lgFR != null) placedIds.Add(lgFR.Id.GetIdValue());
                            if (opts != null && opts.HasTopTransom)
                            {
                                var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yBayLast), MmToFt(topZ)), 90, "l", L_cut2);
                                if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                            }
                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, yBayLast, 90, L_cut2, z0, jackH, s, storyCount, opts);
                                if (sym7 != null)
                                {
                                    var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yBayLast), MmToFt(zDeck)), 90, "l", L_cut2);
                                    if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());
                                }
                            }
                        }

                        PlaceSharedTransom(xRight, yC2, 90, W);
                        curX = xRight;
                        curYBack = yC2;
                        curYFront = yMax;
                        curDir = LegDir.PlusX;
                        startBayIdx = 0;
                    }
                }
                // =========================================================================
                // NHÓM 3: TH 2.1 (Góc vươn ngoài +X -> +Y hoặc +Y/-Y -> +X)
                // =========================================================================
                else if (conn == "TH 2.1")
                {
                    if (curDir == LegDir.PlusX)
                    {
                        for (int b = startBayIdx; b < curBays.Count; b++)
                        {
                            BuildBayPlusX(curX, curYBack, curBays[b]);
                            curX += curBays[b];
                        }

                        double xA = curX;
                        double xB = xA + W;
                        double L_next_first = nextBays[0];
                        double yTop5 = curYBack + L_next_first;
                        double L_A5 = (yTop5 - curYFront > 10.0) ? (yTop5 - curYFront) : 0.0;

                        EnsureCol(xB, curYBack); EnsureCol(xB, yTop5);
                        if (L_A5 > 10.0) EnsureCol(xA, yTop5);

                        PlaceSharedTransom(xA, curYBack, 0, W);
                        PlaceSharedTransom(xA, yTop5, 0, W);

                        var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYBack), MmToFt(z0)), 0, "l", W);
                        var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(z0)), 90, "l", L_next_first);
                        if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
                        if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

                        var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(zH1)), 90, "l", L_next_first);
                        if (lgRF_B != null) placedIds.Add(lgRF_B.Id.GetIdValue());
                        if (L_A5 > 10.0)
                        {
                            var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(z0)), 90, "l", L_A5);
                            var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH1)), 90, "l", L_A5);
                            if (bbIn != null) placedIds.Add(bbIn.Id.GetIdValue());
                            if (lgRF_A != null) placedIds.Add(lgRF_A.Id.GetIdValue());
                        }

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(topZ)), 90, "l", L_next_first);
                            if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.GetIdValue());
                            if (L_A5 > 10.0)
                            {
                                var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(topZ)), 90, "l", L_A5);
                                if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.GetIdValue());
                            }
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, curYBack, 0, W, z0, jackH, s, storyCount, opts);
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, curYBack, 90, L_next_first, z0, jackH, s, storyCount, opts);

                            if (L_A5 > 10.0)
                            {
                                var lgRM_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH3)), 90, "l", L_A5);
                                var rA1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH6)), 90, "l", L_A5);
                                var rA2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH7)), 90, "l", L_A5);
                                if (lgRM_A != null) placedIds.Add(lgRM_A.Id.GetIdValue());
                                if (rA1 != null) placedIds.Add(rA1.Id.GetIdValue());
                                if (rA2 != null) placedIds.Add(rA2.Id.GetIdValue());
                            }

                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L_next_first);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L_next_first);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }

                            if (sym7 != null)
                            {
                                var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", W);
                                var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66.0), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L_next_first);
                                if (tbBack != null) placedIds.Add(tbBack.Id.GetIdValue());
                                if (tbEndB != null) placedIds.Add(tbEndB.Id.GetIdValue());
                                if (L_A5 > 10.0)
                                {
                                    var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(curYFront), MmToFt(zDeck)), 90, "l", L_A5);
                                    if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                                }
                            }
                        }

                        curXLeft = xA;
                        curYStart = yTop5;
                        curDir = LegDir.PlusY;
                        startBayIdx = 1;
                    }
                    else
                    {
                        // Khi curDir == PlusY hoặc MinusY vươn ngoài sang +X
                        bool isFromMinusY = (curDir == LegDir.MinusY);
                        if (!isFromMinusY)
                        {
                            for (int b = startBayIdx; b < curBays.Count; b++)
                            {
                                BuildBayPlusY(curXLeft, curYStart, curBays[b]);
                                curYStart += curBays[b];
                            }
                        }
                        else
                        {
                            for (int b = startBayIdx; b < curBays.Count; b++)
                            {
                                BuildBayMinusY(curXLeft, curYStart, curBays[b]);
                                curYStart -= curBays[b];
                            }
                        }

                        double xA = curXLeft;
                        double xB = xA + W;
                        double yBot6 = isFromMinusY ? (curYStart - W) : curYStart;
                        double yTop6 = isFromMinusY ? curYStart : (curYStart + W);
                        double yOuter6 = isFromMinusY ? yBot6 : yTop6;
                        double yInner6 = isFromMinusY ? yTop6 : yBot6;
                        double tbOuterY = isFromMinusY ? (yBot6 + 66.0) : (yTop6 - 66.0);
                        double tbInnerY = isFromMinusY ? (yTop6 - 66.0) : (yBot6 + 66.0);
                        double L6_first = nextBays[0];
                        double xRight6 = xA + L6_first;
                        double L_A6 = (xRight6 - xB > 10.0) ? (xRight6 - xB) : 0.0;

                        EnsureCol(xA, yOuter6); EnsureCol(xRight6, yOuter6);
                        if (L_A6 > 10.0) EnsureCol(xRight6, yInner6);

                        PlaceSharedTransom(xA, yBot6, 90, W);
                        PlaceSharedTransom(xRight6, yBot6, 90, W);

                        var bbC6_F = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(z0)), 90, "l", W);
                        var bbC6_T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(z0)), 0, "l", L6_first);
                        if (bbC6_F != null) placedIds.Add(bbC6_F.Id.GetIdValue());
                        if (bbC6_T != null) placedIds.Add(bbC6_T.Id.GetIdValue());

                        var lgFC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(zH1)), 0, "l", L6_first);
                        if (lgFC6_T != null) placedIds.Add(lgFC6_T.Id.GetIdValue());

                        if (L_A6 > 10.0)
                        {
                            var bbC6_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(z0)), 0, "l", L_A6);
                            var lgFC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(zH1)), 0, "l", L_A6);
                            if (bbC6_In != null) placedIds.Add(bbC6_In.Id.GetIdValue());
                            if (lgFC6_In != null) placedIds.Add(lgFC6_In.Id.GetIdValue());
                        }

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(topZ)), 0, "l", L6_first);
                            if (lgTopC6_T != null) placedIds.Add(lgTopC6_T.Id.GetIdValue());
                            if (L_A6 > 10.0)
                            {
                                var lgTopC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(topZ)), 0, "l", L_A6);
                                if (lgTopC6_In != null) placedIds.Add(lgTopC6_In.Id.GetIdValue());
                            }
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yBot6, 90, W, z0, jackH, s, storyCount, opts);
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yOuter6, 0, L6_first, z0, jackH, s, storyCount, opts);

                            if (L_A6 > 10.0)
                            {
                                var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(zH3)), 0, "l", L_A6);
                                var rIn1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(zH6)), 0, "l", L_A6);
                                var rIn2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(zH7)), 0, "l", L_A6);
                                if (lgMIn != null) placedIds.Add(lgMIn.Id.GetIdValue());
                                if (rIn1 != null) placedIds.Add(rIn1.Id.GetIdValue());
                                if (rIn2 != null) placedIds.Add(rIn2.Id.GetIdValue());
                            }

                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yBot6 + pOff), MmToFt(zDeck)), 0, "l", L6_first);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - pOff), MmToFt(zDeck)), 0, "l", L6_first);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }

                            if (sym7 != null)
                            {
                                var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(yBot6), MmToFt(zDeck)), 90, "l", W);
                                var tbT = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(tbOuterY), MmToFt(zDeck)), 0, "l", L6_first);
                                if (tbF != null) placedIds.Add(tbF.Id.GetIdValue());
                                if (tbT != null) placedIds.Add(tbT.Id.GetIdValue());
                                if (L_A6 > 10.0)
                                {
                                    var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB), MmToFt(tbInnerY), MmToFt(zDeck)), 0, "l", L_A6);
                                    if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                                }
                            }
                        }

                        curX = xRight6;
                        curYBack = yBot6;
                        curYFront = yTop6;
                        curDir = LegDir.PlusX;
                        startBayIdx = 1;
                    }
                }
                // =========================================================================
                // NHÓM 4: TH 2.2 (Góc vươn ngoài có giằng chéo vách giật cấp trong, không giằng dọc chỗ giằng chéo)
                // =========================================================================
                else if (conn == "TH 2.2")
                {
                    if (curDir == LegDir.PlusY || curDir == LegDir.MinusY)
                    {
                        bool isFromMinusY = (curDir == LegDir.MinusY);
                        if (!isFromMinusY)
                        {
                            for (int b = startBayIdx; b < curBays.Count; b++)
                            {
                                BuildBayPlusY(curXLeft, curYStart, curBays[b]);
                                curYStart += curBays[b];
                            }
                        }
                        else
                        {
                            for (int b = startBayIdx; b < curBays.Count; b++)
                            {
                                BuildBayMinusY(curXLeft, curYStart, curBays[b]);
                                curYStart -= curBays[b];
                            }
                        }

                        double xA = curXLeft;
                        double xB = xA + W;
                        double yBot6 = isFromMinusY ? (curYStart - W) : curYStart;
                        double yTop6 = isFromMinusY ? curYStart : (curYStart + W);
                        double yOuter6 = isFromMinusY ? yBot6 : yTop6;
                        double yInner6 = isFromMinusY ? yTop6 : yBot6;
                        double tbOuterY = isFromMinusY ? (yBot6 + 66.0) : (yTop6 - 66.0);
                        double tbInnerY = isFromMinusY ? (yTop6 - 66.0) : (yBot6 + 66.0);
                        double L6_first = nextBays[0];
                        double xRight6 = xA + L6_first;
                        double L_A6 = (xRight6 - xB > 10.0) ? (xRight6 - xB) : 0.0;

                        EnsureCol(xA, yOuter6); EnsureCol(xRight6, yOuter6);
                        if (L_A6 > 10.0) EnsureCol(xRight6, yInner6);

                        PlaceSharedTransom(xA, yBot6, 90, W);
                        PlaceSharedTransom(xRight6, yBot6, 90, W);

                        var bbC6_F = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(z0)), 90, "l", W);
                        var bbC6_T = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(z0)), 0, "l", L6_first);
                        if (bbC6_F != null) placedIds.Add(bbC6_F.Id.GetIdValue());
                        if (bbC6_T != null) placedIds.Add(bbC6_T.Id.GetIdValue());

                        var lgFC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(zH1)), 0, "l", L6_first);
                        if (lgFC6_T != null) placedIds.Add(lgFC6_T.Id.GetIdValue());

                        if (L_A6 > 10.0)
                        {
                            var bbC6_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(z0)), 0, "l", L_A6);
                            var lgFC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(zH1)), 0, "l", L_A6);
                            if (bbC6_In != null) placedIds.Add(bbC6_In.Id.GetIdValue());
                            if (lgFC6_In != null) placedIds.Add(lgFC6_In.Id.GetIdValue());
                        }

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(topZ)), 0, "l", L6_first);
                            if (lgTopC6_T != null) placedIds.Add(lgTopC6_T.Id.GetIdValue());
                            if (L_A6 > 10.0)
                            {
                                var lgTopC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yInner6), MmToFt(topZ)), 0, "l", L_A6);
                                if (lgTopC6_In != null) placedIds.Add(lgTopC6_In.Id.GetIdValue());
                            }
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            var lgMC6_F = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH3)), 90, "l", W);
                            var rC6_F1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH6)), 90, "l", W);
                            var rC6_F2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot6), MmToFt(zH7)), 90, "l", W);
                            if (lgMC6_F != null) placedIds.Add(lgMC6_F.Id.GetIdValue());
                            if (rC6_F1 != null) placedIds.Add(rC6_F1.Id.GetIdValue());
                            if (rC6_F2 != null) placedIds.Add(rC6_F2.Id.GetIdValue());

                            var lgMC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(zH3)), 0, "l", L6_first);
                            var rC6_T1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(zH6)), 0, "l", L6_first);
                            var rC6_T2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yOuter6), MmToFt(zH7)), 0, "l", L6_first);
                            if (lgMC6_T != null) placedIds.Add(lgMC6_T.Id.GetIdValue());
                            if (rC6_T1 != null) placedIds.Add(rC6_T1.Id.GetIdValue());
                            if (rC6_T2 != null) placedIds.Add(rC6_T2.Id.GetIdValue());

                            if (L_A6 > 10.0)
                            {
                                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yInner6, 0, L_A6, z0, jackH, s, storyCount, opts);
                            }

                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yBot6 + pOff), MmToFt(zDeck)), 0, "l", L6_first);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - pOff), MmToFt(zDeck)), 0, "l", L6_first);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }

                            if (sym7 != null)
                            {
                                var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(yBot6), MmToFt(zDeck)), 90, "l", W);
                                var tbT = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(tbOuterY), MmToFt(zDeck)), 0, "l", L6_first);
                                if (tbF != null) placedIds.Add(tbF.Id.GetIdValue());
                                if (tbT != null) placedIds.Add(tbT.Id.GetIdValue());
                                if (L_A6 > 10.0)
                                {
                                    var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB), MmToFt(tbInnerY), MmToFt(zDeck)), 0, "l", L_A6);
                                    if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                                }
                            }
                        }

                        curX = xRight6;
                        curYBack = yBot6;
                        curYFront = yTop6;
                        curDir = LegDir.PlusX;
                        startBayIdx = 1;
                    }
                    else
                    {
                        // Khi curDir == PlusX vươn ngoài sang -Y (như ExecuteCase2_2)
                        for (int b = startBayIdx; b < curBays.Count; b++)
                        {
                            BuildBayPlusX(curX, curYBack, curBays[b]);
                            curX += curBays[b];
                        }

                        double xA = curX;
                        double xB = xA + W;
                        double L_next_first = nextBays[0];
                        double yBot = curYFront - L_next_first;
                        double L_A = (curYBack - yBot > 10.0) ? (curYBack - yBot) : 0.0;

                        EnsureCol(xB, curYFront); EnsureCol(xB, yBot);
                        if (L_A > 10.0) EnsureCol(xA, yBot);

                        PlaceSharedTransom(xA, curYFront, 0, W);
                        PlaceSharedTransom(xA, yBot, 0, W);

                        var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(z0)), 0, "l", W);
                        var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(z0)), 90, "l", L_next_first);
                        if (bbR1 != null) placedIds.Add(bbR1.Id.GetIdValue());
                        if (bbR2 != null) placedIds.Add(bbR2.Id.GetIdValue());

                        var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zH1)), 90, "l", L_next_first);
                        if (lgRF_B != null) placedIds.Add(lgRF_B.Id.GetIdValue());
                        if (L_A > 10.0)
                        {
                            var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(z0)), 90, "l", L_A);
                            var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(zH1)), 90, "l", L_A);
                            if (bbIn != null) placedIds.Add(bbIn.Id.GetIdValue());
                            if (lgRF_A != null) placedIds.Add(lgRF_A.Id.GetIdValue());
                        }

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_next_first);
                            if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.GetIdValue());
                            if (L_A > 10.0)
                            {
                                var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yBot), MmToFt(topZ)), 90, "l", L_A);
                                if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.GetIdValue());
                            }
                        }

                        for (int s = 0; s < storyCount; s++)
                        {
                            double sOff = s * 1800.0;
                            double zH3 = z0 + jackH + 830.0 + sOff;
                            double zDeck = z0 + jackH + 1730.0 + sOff;
                            double zH6 = z0 + jackH + 2180.0 + sOff;
                            double zH7 = z0 + jackH + 2630.0 + sOff;

                            var lgMFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH3)), 0, "l", W);
                            var rFront1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH6)), 0, "l", W);
                            var rFront2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH7)), 0, "l", W);
                            if (lgMFront != null) placedIds.Add(lgMFront.Id.GetIdValue());
                            if (rFront1 != null) placedIds.Add(rFront1.Id.GetIdValue());
                            if (rFront2 != null) placedIds.Add(rFront2.Id.GetIdValue());

                            var lgMEndB = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zH3)), 90, "l", L_next_first);
                            var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zH6)), 90, "l", L_next_first);
                            var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot), MmToFt(zH7)), 90, "l", L_next_first);
                            if (lgMEndB != null) placedIds.Add(lgMEndB.Id.GetIdValue());
                            if (rEnd1 != null) placedIds.Add(rEnd1.Id.GetIdValue());
                            if (rEnd2 != null) placedIds.Add(rEnd2.Id.GetIdValue());

                            if (L_A > 10.0)
                            {
                                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yBot, 90, L_A, z0, jackH, s, storyCount, opts);
                            }

                            if (sym6 != null)
                            {
                                var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + pOff), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_next_first);
                                var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - pOff), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_next_first);
                                if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.GetIdValue()); }
                                if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.GetIdValue()); }
                            }

                            if (sym7 != null)
                            {
                                var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", W);
                                var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66.0), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_next_first);
                                if (tbFront != null) placedIds.Add(tbFront.Id.GetIdValue());
                                if (tbEndB != null) placedIds.Add(tbEndB.Id.GetIdValue());
                                if (L_A > 10.0)
                                {
                                    var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(yBot), MmToFt(zDeck)), 90, "l", L_A);
                                    if (tbIn != null) placedIds.Add(tbIn.Id.GetIdValue());
                                }
                            }
                        }

                        curXLeft = xA;
                        curYStart = yBot;
                        curDir = LegDir.MinusY;
                        startBayIdx = 1;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Thực thi Table Run theo đúng tọa độ thực tế (StartX, StartY, AngleDeg) của từng instance Review_DetailLine đã quét trong Revit,
        /// không bao giờ tự ý kéo về (0, 0).
        /// </summary>
        private static string ExecuteTableRunByInstanceCoords(
            Document doc, BuildOptions opts, Level level, List<int> placedIds,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4,
            FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            List<TableRowOption> rows, double z0, double defaultW, double defaultPw, double defaultPOff,
            double jackH, int storyCount, double topZ, double zH1, bool lastConnectsToFirst)
        {
            int nRows = rows.Count;
            HashSet<string> placedCols = new HashSet<string>();
            HashSet<string> placedTransoms = new HashSet<string>();
            HashSet<string> placedBaseboards = new HashSet<string>();

            Action<double, double> EnsureGlobalCol = (gx, gy) =>
            {
                double rx = Math.Round(gx / 10.0) * 10.0;
                double ry = Math.Round(gy / 10.0) * 10.0;
                string key = string.Format("{0:F0}_{1:F0}", rx, ry);
                if (!placedCols.Contains(key))
                {
                    placedCols.Add(key);
                    var jk = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(z0)), 0, "h", jackH);
                    if (jk != null) placedIds.Add(jk.Id.GetIdValue());

                    PlaceConfiguredPosts(doc, sym3, level, placedIds, gx, gy, z0 + jackH, opts);
                }
            };

            Action<double, double, double, double, double, double> PlaceSharedGlobalTransom = (gx, gy, mx, my, gRot, tlen) =>
            {
                double rmx = Math.Round(mx / 10.0) * 10.0;
                double rmy = Math.Round(my / 10.0) * 10.0;
                string key = string.Format("{0:F0}_{1:F0}", rmx, rmy);
                if (!placedTransoms.Contains(key))
                {
                    placedTransoms.Add(key);
                    var fFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(zH1)), gRot, "l", tlen);
                    if (fFoot != null) placedIds.Add(fFoot.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var fDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(sDeck)), gRot, "l", tlen);
                        if (fDeck != null) placedIds.Add(fDeck.Id.GetIdValue());
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var fTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(topZ)), gRot, "l", tlen);
                        if (fTop != null) placedIds.Add(fTop.Id.GetIdValue());
                    }
                }
            };

            Action<double, double, double, double, double, double> PlaceSharedGlobalBaseboard = (gx, gy, mx, my, gRot, blen) =>
            {
                double rmx = Math.Round(mx / 10.0) * 10.0;
                double rmy = Math.Round(my / 10.0) * 10.0;
                string key = string.Format("{0:F0}_{1:F0}", rmx, rmy);
                if (!placedBaseboards.Contains(key))
                {
                    placedBaseboards.Add(key);
                    var bb = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(z0)), gRot, "l", blen);
                    if (bb != null) placedIds.Add(bb.Id.GetIdValue());
                }
            };

            double fallbackOx = opts.X0;
            double fallbackOy = opts.Y0;
            double fallbackTheta = 0.0;

            for (int i = 0; i < nRows; i++)
            {
                TableRowOption curRow = rows[i];
                List<double> curBays = (curRow.BayLengths != null && curRow.BayLengths.Count > 0)
                    ? curRow.BayLengths
                    : new List<double> { 1800.0 };

                double W = curRow.Width > 0 ? curRow.Width : defaultW;
                double pw = GetPlankWidth(W);
                double pOff = GetPlankOffset(W);

                double ox = curRow.HasCoords ? curRow.StartX : fallbackOx;
                double oy = curRow.HasCoords ? curRow.StartY : fallbackOy;
                double thetaDeg = curRow.HasCoords ? curRow.AngleDeg : fallbackTheta;
                double rad = thetaDeg * Math.PI / 180.0;
                double cosA = Math.Cos(rad);
                double sinA = Math.Sin(rad);

                string conn = NormalizeConnectCase(curRow.ConnectCase);
                bool hasConnect = (conn != "TH 0") && (nRows >= 2) && (i < nRows - 1 || lastConnectsToFirst);

                string prevConn = "TH 0";
                TableRowOption prevRow = null;
                if (i > 0)
                {
                    prevRow = rows[i - 1];
                    prevConn = NormalizeConnectCase(prevRow.ConnectCase);
                }
                else if (lastConnectsToFirst && nRows >= 2)
                {
                    prevRow = rows[nRows - 1];
                    prevConn = NormalizeConnectCase(prevRow.ConnectCase);
                }

                TableRowOption nextRow = hasConnect ? ((i < nRows - 1) ? rows[i + 1] : rows[0]) : null;
                double wNext = (nextRow != null && nextRow.Width > 0) ? nextRow.Width : W;
                double wPrev = (prevRow != null && prevRow.Width > 0) ? prevRow.Width : W;

                Action<double, double> EnsureLocalCol = (lx, ly) =>
                {
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    EnsureGlobalCol(gx, gy);
                };

                Action<double, double, double, double> PlaceLocalTransomAt = (lx, ly, lRot, tlen) =>
                {
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    double gRot = thetaDeg + lRot;
                    double gRad = gRot * Math.PI / 180.0;
                    double mx = gx + 0.5 * tlen * Math.Cos(gRad);
                    double my = gy + 0.5 * tlen * Math.Sin(gRad);
                    PlaceSharedGlobalTransom(gx, gy, mx, my, gRot, tlen);
                };

                Action<double> PlaceLocalTransom = (lx) =>
                {
                    PlaceLocalTransomAt(lx, -W, 90.0, W);
                };

                Action<double, double, double, double> PlaceLocalBaseboard = (lx, ly, lRot, blen) =>
                {
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    double gRot = thetaDeg + lRot;
                    double gRad = gRot * Math.PI / 180.0;
                    double mx = gx + 0.5 * blen * Math.Cos(gRad);
                    double my = gy + 0.5 * blen * Math.Sin(gRad);
                    PlaceSharedGlobalBaseboard(gx, gy, mx, my, gRot, blen);
                };

                Action<FamilySymbol, double, double, double, double, string, double> PlaceLocal = (sym, lx, ly, zMm, lRot, pName, pVal) =>
                {
                    if (sym == null) return;
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    double gRot = thetaDeg + lRot;
                    var inst = PlaceInstance(doc, sym, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(zMm)), gRot, pName, pVal);
                    if (inst != null) placedIds.Add(inst.Id.GetIdValue());
                };

                Action<double, double, double, double, double, double> PlaceLocalPlank = (lx, ly, zMm, lRot, plen, pWidth) =>
                {
                    if (sym6 == null) return;
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    double gRot = thetaDeg + lRot;
                    var inst = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(gx), MmToFt(gy), MmToFt(zMm)), gRot, "l", plen);
                    if (inst != null)
                    {
                        SetParam(inst, "w", pWidth);
                        placedIds.Add(inst.Id.GetIdValue());
                    }
                };

                Action<double, double, double, double, int> PlaceLocalBraces = (lx, ly, lRot, bLen, s) =>
                {
                    if (sym5 == null) return;
                    double gx = ox + lx * cosA - ly * sinA;
                    double gy = oy + lx * sinA + ly * cosA;
                    double gRot = thetaDeg + lRot;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, gx, gy, gRot, bLen, z0, jackH, s, storyCount, opts);
                };

                Action<double, bool> CloseLocalGable = (lxGable, isStartGable) =>
                {
                    EnsureLocalCol(lxGable, -W);
                    EnsureLocalCol(lxGable, 0.0);
                    PlaceLocalTransom(lxGable);
                    PlaceLocalBaseboard(lxGable, -W, 90.0, W);
                    double tbLx = isStartGable ? (lxGable + 66.0) : (lxGable - 66.0);
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        PlaceLocal(sym4, lxGable, -W, zH6, 90.0, "l", W);
                        PlaceLocal(sym4, lxGable, -W, zH7, 90.0, "l", W);
                        PlaceLocal(sym7, tbLx, -W, zDeck, 90.0, "l", W);
                    }
                };

                Action<double, double> BuildLocalStandardBay = (lxStart, L_bay) =>
                {
                    EnsureLocalCol(lxStart, -W); EnsureLocalCol(lxStart, 0.0);
                    EnsureLocalCol(lxStart + L_bay, -W); EnsureLocalCol(lxStart + L_bay, 0.0);
                    PlaceLocalTransom(lxStart);
                    PlaceLocalTransom(lxStart + L_bay);

                    PlaceLocalBaseboard(lxStart, 0.0, 0.0, L_bay);
                    PlaceLocalBaseboard(lxStart, -W, 0.0, L_bay);

                    PlaceLocal(sym4, lxStart, 0.0, zH1, 0.0, "l", L_bay);
                    PlaceLocal(sym4, lxStart, -W, zH1, 0.0, "l", L_bay);

                    if (opts != null && opts.HasTopTransom)
                    {
                        PlaceLocal(sym4, lxStart, 0.0, topZ, 0.0, "l", L_bay);
                        PlaceLocal(sym4, lxStart, -W, topZ, 0.0, "l", L_bay);
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        PlaceLocal(sym4, lxStart, 0.0, zH3, 0.0, "l", L_bay);
                        PlaceLocal(sym4, lxStart, 0.0, zH6, 0.0, "l", L_bay);
                        PlaceLocal(sym4, lxStart, 0.0, zH7, 0.0, "l", L_bay);

                        PlaceLocalBraces(lxStart, -W, 0.0, L_bay, s);

                        PlaceLocalPlank(lxStart, -pOff, zDeck, 0.0, L_bay, pw);
                        PlaceLocalPlank(lxStart, -W + pOff, zDeck, 0.0, L_bay, pw);

                        PlaceLocal(sym7, lxStart, -66.0, zDeck, 0.0, "l", L_bay);
                        PlaceLocal(sym7, lxStart, -W + 66.0, zDeck, 0.0, "l", L_bay);
                    }
                };

                int startBayIdx = 0;
                double curLx = 0.0;

                // 1. Xử lý đầu vào (lx = 0) của hàng i dựa trên kết nối prevConn từ hàng trước
                if (prevConn == "TH 0")
                {
                    CloseLocalGable(0.0, true);
                }
                else if (prevConn == "TH 2.1")
                {
                    // Nhịp đầu tiên (bay 0) của hàng hiện tại là nhịp góc vươn ngoài TH 2.1 (Mép sau phẳng)
                    // Chuẩn hóa 100% đồng bộ với ExecuteCase2_1:
                    // - Đầu hồi lối vào (lx = 0): đặt giằng chéo Family 5 từ (0, 0) đến (0, -W) với góc -90° (tương đương xA, yBack góc 0°)
                    // - Vách ngoài (ly = -W): đặt giằng chéo Family 5 góc 0° suốt chiều dài L_first
                    // - Vách giật cấp trong (ly = 0, nếu L_A > 10): đặt lan can an toàn Family 4 (zH1, zH3, zH6, zH7, topZ)
                    double L_first = curBays[0];
                    double L_A = (L_first - wPrev > 10.0) ? (L_first - wPrev) : 0.0;

                    EnsureLocalCol(0.0, -W); EnsureLocalCol(0.0, 0.0);
                    EnsureLocalCol(L_first, -W); EnsureLocalCol(L_first, 0.0);
                    if (L_A > 10.0) EnsureLocalCol(wPrev, 0.0);

                    // Transoms và Baseboards chân cột (h1 và z0)
                    PlaceLocalTransomAt(0.0, 0.0, -90.0, W);
                    PlaceLocalTransom(L_first);
                    PlaceLocalBaseboard(0.0, 0.0, -90.0, W);

                    PlaceLocalBaseboard(0.0, -W, 0.0, L_first);
                    PlaceLocal(sym4, 0.0, -W, zH1, 0.0, "l", L_first);

                    if (L_A > 10.0)
                    {
                        PlaceLocalBaseboard(wPrev, 0.0, 0.0, L_A);
                        PlaceLocal(sym4, wPrev, 0.0, zH1, 0.0, "l", L_A);
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        PlaceLocal(sym4, 0.0, 0.0, topZ, -90.0, "l", W);
                        PlaceLocal(sym4, L_first, 0.0, topZ, -90.0, "l", W);
                        PlaceLocal(sym4, 0.0, -W, topZ, 0.0, "l", L_first);
                        if (L_A > 10.0)
                        {
                            PlaceLocal(sym4, wPrev, 0.0, topZ, 0.0, "l", L_A);
                        }
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        // 1. Giằng chéo Family 5: đầu hồi xA -> xB (góc -90°) và vách ngoài xB (góc 0°)
                        PlaceLocalBraces(0.0, 0.0, -90.0, W, s);
                        PlaceLocalBraces(0.0, -W, 0.0, L_first, s);

                        // 2. Lan can an toàn Family 4 tại vách giật cấp trong
                        if (L_A > 10.0)
                        {
                            PlaceLocal(sym4, wPrev, 0.0, zH3, 0.0, "l", L_A);
                            PlaceLocal(sym4, wPrev, 0.0, zH6, 0.0, "l", L_A);
                            PlaceLocal(sym4, wPrev, 0.0, zH7, 0.0, "l", L_A);
                        }

                        // 3. Giằng đỡ mâm tại zDeck
                        PlaceLocal(sym4, 0.0, 0.0, zDeck, -90.0, "l", W);
                        PlaceLocal(sym4, L_first, 0.0, zDeck, -90.0, "l", W);

                        // 4. Mâm sàn thao tác Family 6
                        PlaceLocalPlank(0.0, -pOff, zDeck, 0.0, L_first, pw);
                        PlaceLocalPlank(0.0, -W + pOff, zDeck, 0.0, L_first, pw);

                        // 5. Ván chắn biên Family 7
                        PlaceLocal(sym7, 66.0, 0.0, zDeck, -90.0, "l", W);
                        PlaceLocal(sym7, 0.0, -W + 66.0, zDeck, 0.0, "l", L_first);
                        if (L_A > 10.0)
                        {
                            PlaceLocal(sym7, wPrev, -66.0, zDeck, 0.0, "l", L_A);
                        }
                    }

                    startBayIdx = 1;
                    curLx = L_first;
                }
                else if (prevConn == "TH 2.2")
                {
                    // Nhịp đầu tiên (bay 0) của hàng hiện tại là nhịp góc vươn ngoài TH 2.2 (Mép trước phẳng)
                    // Chuẩn hóa 100% đồng bộ với ExecuteCase2_2:
                    // - Vách ngoài mép trước phẳng (ly = 0): dùng lan can an toàn Family 4
                    // - Vách giật cấp trong (ly = -W, nếu L_A > 10): dùng giằng chéo Family 5 (không giằng dọc chỗ giằng chéo)
                    double L_first = curBays[0];
                    double L_A = (L_first - wPrev > 10.0) ? (L_first - wPrev) : 0.0;

                    EnsureLocalCol(0.0, -W); EnsureLocalCol(0.0, 0.0);
                    EnsureLocalCol(L_first, -W); EnsureLocalCol(L_first, 0.0);
                    if (L_A > 10.0) EnsureLocalCol(wPrev, -W);

                    PlaceLocalTransomAt(0.0, 0.0, -90.0, W);
                    PlaceLocalTransom(L_first);
                    PlaceLocalBaseboard(0.0, 0.0, -90.0, W);

                    PlaceLocalBaseboard(0.0, 0.0, 0.0, L_first);
                    PlaceLocal(sym4, 0.0, 0.0, zH1, 0.0, "l", L_first);

                    if (L_A > 10.0)
                    {
                        PlaceLocalBaseboard(wPrev, -W, 0.0, L_A);
                        PlaceLocal(sym4, wPrev, -W, zH1, 0.0, "l", L_A);
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        PlaceLocal(sym4, 0.0, 0.0, topZ, -90.0, "l", W);
                        PlaceLocal(sym4, L_first, 0.0, topZ, -90.0, "l", W);
                        PlaceLocal(sym4, 0.0, 0.0, topZ, 0.0, "l", L_first);
                        if (L_A > 10.0)
                        {
                            PlaceLocal(sym4, wPrev, -W, topZ, 0.0, "l", L_A);
                        }
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        // Lan can đầu hồi đối diện lối vào
                        PlaceLocal(sym4, 0.0, 0.0, zH3, -90.0, "l", W);
                        PlaceLocal(sym4, 0.0, 0.0, zH6, -90.0, "l", W);
                        PlaceLocal(sym4, 0.0, 0.0, zH7, -90.0, "l", W);

                        // Lan can vách ngoài mép trước phẳng (ly = 0)
                        PlaceLocal(sym4, 0.0, 0.0, zH3, 0.0, "l", L_first);
                        PlaceLocal(sym4, 0.0, 0.0, zH6, 0.0, "l", L_first);
                        PlaceLocal(sym4, 0.0, 0.0, zH7, 0.0, "l", L_first);

                        // Giằng chéo Family 5 tại vách trong giật cấp (không giằng dọc chỗ giằng chéo)
                        if (L_A > 10.0)
                        {
                            PlaceLocalBraces(wPrev, -W, 0.0, L_A, s);
                        }

                        // Giằng đỡ mâm tại zDeck
                        PlaceLocal(sym4, 0.0, 0.0, zDeck, -90.0, "l", W);
                        PlaceLocal(sym4, L_first, 0.0, zDeck, -90.0, "l", W);

                        // Mâm sàn thao tác Family 6
                        PlaceLocalPlank(0.0, -pOff, zDeck, 0.0, L_first, pw);
                        PlaceLocalPlank(0.0, -W + pOff, zDeck, 0.0, L_first, pw);

                        // Ván chắn biên Family 7
                        PlaceLocal(sym7, 66.0, 0.0, zDeck, -90.0, "l", W);
                        PlaceLocal(sym7, 0.0, -66.0, zDeck, 0.0, "l", L_first);
                        if (L_A > 10.0)
                        {
                            PlaceLocal(sym7, wPrev, -W + 66.0, zDeck, 0.0, "l", L_A);
                        }
                    }

                    startBayIdx = 1;
                    curLx = L_first;
                }
                else
                {
                    // Đầu vào từ góc thu trong (TH 1.1 .. TH 1.4)
                    EnsureLocalCol(0.0, -W);
                    EnsureLocalCol(0.0, 0.0);
                    PlaceLocalTransom(0.0);
                }

                // 2. Xử lý thân và cuối của hàng i theo kết nối conn đi sang hàng i+1
                if (!hasConnect)
                {
                    for (int b = startBayIdx; b < curBays.Count; b++)
                    {
                        BuildLocalStandardBay(curLx, curBays[b]);
                        curLx += curBays[b];
                    }
                    CloseLocalGable(curLx, false);
                }
                else if (conn == "TH 2.1" || conn == "TH 2.2")
                {
                    // Với góc vươn ngoài (TH 2.1 / TH 2.2), dựng hết các nhịp thẳng của hàng hiện tại và để mở đầu cuối (curLx) làm cửa ngõ sang hàng tiếp theo
                    for (int b = startBayIdx; b < curBays.Count; b++)
                    {
                        BuildLocalStandardBay(curLx, curBays[b]);
                        curLx += curBays[b];
                    }
                    PlaceLocalTransom(curLx);
                }
                else
                {
                    // Với góc thu trong (TH 1.1, TH 1.2, TH 1.3, TH 1.4): dựng các nhịp thẳng trước nhịp cuối, nhịp cuối cắt mở cửa ngõ W_next
                    for (int b = startBayIdx; b < curBays.Count - 1; b++)
                    {
                        BuildLocalStandardBay(curLx, curBays[b]);
                        curLx += curBays[b];
                    }

                    if (startBayIdx < curBays.Count)
                    {
                        double L_last = curBays[curBays.Count - 1];
                        double xBayLast = curLx;
                        double xMax = xBayLast + L_last;
                        double xC = xMax - wNext;
                        double L_cut = L_last > wNext ? (L_last - wNext) : 0.0;

                        bool cutFrontEdge = (conn == "TH 1.1" || conn == "TH 1.2");
                        if (nextRow != null && nextRow.HasCoords && curRow.HasCoords)
                        {
                            double nRad = nextRow.AngleDeg * Math.PI / 180.0;
                            double crossNext = cosA * Math.Sin(nRad) - sinA * Math.Cos(nRad);
                            if (Math.Abs(crossNext) > 0.5)
                            {
                                cutFrontEdge = (crossNext > 0.0);
                            }
                        }

                        EnsureLocalCol(xBayLast, -W); EnsureLocalCol(xBayLast, 0.0);
                        EnsureLocalCol(xMax, -W); EnsureLocalCol(xMax, 0.0);
                        PlaceLocalTransom(xBayLast);
                        CloseLocalGable(xMax, false);

                        if (cutFrontEdge)
                        {
                            // Mép sau (ly = -W) giữ nguyên L_last với Giằng chéo Family 5; Mép trước (ly = 0) cắt L_cut
                            PlaceLocalBaseboard(xBayLast, -W, 0.0, L_last);
                            PlaceLocal(sym4, xBayLast, -W, zH1, 0.0, "l", L_last);
                            if (opts != null && opts.HasTopTransom)
                            {
                                PlaceLocal(sym4, xBayLast, -W, topZ, 0.0, "l", L_last);
                            }

                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                PlaceLocalBraces(xBayLast, -W, 0.0, L_last, s);
                                PlaceLocal(sym7, xBayLast, -W + 66.0, zDeck, 0.0, "l", L_last);
                                PlaceLocalPlank(xBayLast, -pOff, zDeck, 0.0, L_last, pw);
                                PlaceLocalPlank(xBayLast, -W + pOff, zDeck, 0.0, L_last, pw);
                            }

                            EnsureLocalCol(xC, 0.0);
                            PlaceLocalTransomAt(xC, 0.0, 0.0, wNext);
                            if (L_cut > 10.0)
                            {
                                PlaceLocalBaseboard(xBayLast, 0.0, 0.0, L_cut);
                                PlaceLocal(sym4, xBayLast, 0.0, zH1, 0.0, "l", L_cut);
                                if (opts != null && opts.HasTopTransom)
                                {
                                    PlaceLocal(sym4, xBayLast, 0.0, topZ, 0.0, "l", L_cut);
                                }
                                for (int s = 0; s < storyCount; s++)
                                {
                                    double sOff = s * 1800.0;
                                    double zH3 = z0 + jackH + 830.0 + sOff;
                                    double zDeck = z0 + jackH + 1730.0 + sOff;
                                    double zH6 = z0 + jackH + 2180.0 + sOff;
                                    double zH7 = z0 + jackH + 2630.0 + sOff;
                                    PlaceLocal(sym4, xBayLast, 0.0, zH3, 0.0, "l", L_cut);
                                    PlaceLocal(sym4, xBayLast, 0.0, zH6, 0.0, "l", L_cut);
                                    PlaceLocal(sym4, xBayLast, 0.0, zH7, 0.0, "l", L_cut);
                                    PlaceLocal(sym7, xBayLast, -66.0, zDeck, 0.0, "l", L_cut);
                                }
                            }
                        }
                        else
                        {
                            // Mép trước (ly = 0) giữ nguyên L_last với Lan can Family 4; Mép sau (ly = -W) cắt L_cut
                            PlaceLocalBaseboard(xBayLast, 0.0, 0.0, L_last);
                            PlaceLocal(sym4, xBayLast, 0.0, zH1, 0.0, "l", L_last);
                            if (opts != null && opts.HasTopTransom)
                            {
                                PlaceLocal(sym4, xBayLast, 0.0, topZ, 0.0, "l", L_last);
                            }

                            for (int s = 0; s < storyCount; s++)
                            {
                                double sOff = s * 1800.0;
                                double zH3 = z0 + jackH + 830.0 + sOff;
                                double zDeck = z0 + jackH + 1730.0 + sOff;
                                double zH6 = z0 + jackH + 2180.0 + sOff;
                                double zH7 = z0 + jackH + 2630.0 + sOff;
                                PlaceLocal(sym4, xBayLast, 0.0, zH3, 0.0, "l", L_last);
                                PlaceLocal(sym4, xBayLast, 0.0, zH6, 0.0, "l", L_last);
                                PlaceLocal(sym4, xBayLast, 0.0, zH7, 0.0, "l", L_last);
                                PlaceLocal(sym7, xBayLast, -66.0, zDeck, 0.0, "l", L_last);
                                PlaceLocalPlank(xBayLast, -pOff, zDeck, 0.0, L_last, pw);
                                PlaceLocalPlank(xBayLast, -W + pOff, zDeck, 0.0, L_last, pw);
                            }

                            EnsureLocalCol(xC, -W);
                            PlaceLocalTransomAt(xC, -W, 0.0, wNext);
                            if (L_cut > 10.0)
                            {
                                PlaceLocalBaseboard(xBayLast, -W, 0.0, L_cut);
                                PlaceLocal(sym4, xBayLast, -W, zH1, 0.0, "l", L_cut);
                                if (opts != null && opts.HasTopTransom)
                                {
                                    PlaceLocal(sym4, xBayLast, -W, topZ, 0.0, "l", L_cut);
                                }
                                for (int s = 0; s < storyCount; s++)
                                {
                                    double sOff = s * 1800.0;
                                    double zDeck = z0 + jackH + 1730.0 + sOff;
                                    PlaceLocalBraces(xBayLast, -W, 0.0, L_cut, s);
                                    PlaceLocal(sym7, xBayLast, -W + 66.0, zDeck, 0.0, "l", L_cut);
                                }
                            }
                        }

                        curLx = xMax;
                    }
                }

                // Cập nhật fallbackOx, fallbackOy, fallbackTheta phòng trường hợp người dùng thêm dòng thủ công sau dòng quét
                if (!hasConnect)
                {
                    fallbackOx = ox + (curLx + W * 2.0) * cosA;
                    fallbackOy = oy + (curLx + W * 2.0) * sinA;
                    fallbackTheta = thetaDeg;
                }
                else if (conn == "TH 1.1" || conn == "TH 1.2")
                {
                    double nextLx = curLx - wNext;
                    fallbackOx = ox + nextLx * cosA;
                    fallbackOy = oy + nextLx * sinA;
                    fallbackTheta = thetaDeg + 90.0;
                }
                else if (conn == "TH 1.3" || conn == "TH 1.4")
                {
                    fallbackOx = ox + curLx * cosA - (-W) * sinA;
                    fallbackOy = oy + curLx * sinA + (-W) * cosA;
                    fallbackTheta = thetaDeg - 90.0;
                }
                else if (conn == "TH 2.1")
                {
                    fallbackOx = ox + curLx * cosA - (-W) * sinA;
                    fallbackOy = oy + curLx * sinA + (-W) * cosA;
                    fallbackTheta = thetaDeg + 90.0;
                }
                else if (conn == "TH 2.2")
                {
                    double nextLx = curLx + wNext;
                    fallbackOx = ox + nextLx * cosA;
                    fallbackOy = oy + nextLx * sinA;
                    fallbackTheta = thetaDeg - 90.0;
                }
            }

            return null;
        }
    }
}
