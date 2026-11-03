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
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
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
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(tbX), MmToFt(yBack), MmToFt(zDeck)), 90, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.IntegerValue);
                    }
                }
            };

            // Đóng đầu hồi phương Y (tại Y = yGable, X từ xLeft đến xLeft + W, rot = 0)
            Action<double, double, bool> CloseEndGableY = (xLeft, yGable, isBottomEnd) =>
            {
                PlaceSharedTransom(xLeft, yGable, 0, W);
                var bb = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(z0)), 0, "l", W);
                if (bb != null) placedIds.Add(bb.Id.IntegerValue);

                double tbY = isBottomEnd ? (yGable + 66.0) : (yGable - 66.0);
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH6)), 0, "l", W);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yGable), MmToFt(zH7)), 0, "l", W);
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft), MmToFt(tbY), MmToFt(zDeck)), 0, "l", W);
                        if (tb != null) placedIds.Add(tb.Id.IntegerValue);
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
                if (bbL != null) placedIds.Add(bbL.Id.IntegerValue);
                if (bbR != null) placedIds.Add(bbR.Id.IntegerValue);

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.IntegerValue);
                if (lgFR != null) placedIds.Add(lgFR.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yStart), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.IntegerValue);
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830.0 + sOff;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    double zH6 = z0 + jackH + 2180.0 + sOff;
                    double zH7 = z0 + jackH + 2630.0 + sOff;

                    var lgML = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH3)), 90, "l", L_bay);
                    if (lgML != null) placedIds.Add(lgML.Id.IntegerValue);

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, yStart, 90, L_bay, z0, jackH, s, storyCount, opts);

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yStart), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH6)), 90, "l", L_bay);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yStart), MmToFt(zH7)), 90, "l", L_bay);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
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
                if (bbL != null) placedIds.Add(bbL.Id.IntegerValue);
                if (bbR != null) placedIds.Add(bbR.Id.IntegerValue);

                var lgFL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                var lgFR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH1)), 90, "l", L_bay);
                if (lgFL != null) placedIds.Add(lgFL.Id.IntegerValue);
                if (lgFR != null) placedIds.Add(lgFR.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(topZ)), 90, "l", L_bay);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.IntegerValue);
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.IntegerValue);
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
                    if (lgMR != null) placedIds.Add(lgMR.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66.0), MmToFt(yEnd), MmToFt(zDeck)), 90, "l", L_bay);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                    }

                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH6)), 90, "l", L_bay);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(yEnd), MmToFt(zH7)), 90, "l", L_bay);
                    if (rR1 != null) placedIds.Add(rR1.Id.IntegerValue);
                    if (rR2 != null) placedIds.Add(rR2.Id.IntegerValue);
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
                if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
                if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L0_last);
                    if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast0, curYBack, 0, L0_last, z0, jackH, s, storyCount, opts);
                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L0_last);
                        if (tb != null) placedIds.Add(tb.Id.IntegerValue);
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L0_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L0_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
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
                    if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);
                    if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L_cut1);
                        if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
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
                        if (lgM != null) placedIds.Add(lgM.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast0), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L_cut1);
                            if (tb != null) placedIds.Add(tb.Id.IntegerValue);
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
                if (bbL != null) placedIds.Add(bbL.Id.IntegerValue);
                if (lgFL != null) placedIds.Add(lgFL.Id.IntegerValue);
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yBayLast1), MmToFt(topZ)), 90, "l", L1_last);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.IntegerValue);
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
                    if (lgML != null) placedIds.Add(lgML.Id.IntegerValue);
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight1 - pOff), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L1_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
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
                    if (bbR != null) placedIds.Add(bbR.Id.IntegerValue);
                    if (lgFR != null) placedIds.Add(lgFR.Id.IntegerValue);
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight1), MmToFt(yBayLast1), MmToFt(topZ)), 90, "l", L_cut2);
                        if (lgTopR != null) placedIds.Add(lgTopR.Id.IntegerValue);
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight1, yBayLast1, 90, L_cut2, z0, jackH, s, storyCount, opts);
                        if (sym7 != null)
                        {
                            var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight1 - 66.0), MmToFt(yBayLast1), MmToFt(zDeck)), 90, "l", L_cut2);
                            if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
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
                if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);
                if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront), MmToFt(topZ)), 0, "l", L2_last);
                    if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
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
                    if (lgM != null) placedIds.Add(lgM.Id.IntegerValue);
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront - 66.0), MmToFt(zDeck)), 0, "l", L2_last);
                        if (tb != null) placedIds.Add(tb.Id.IntegerValue);
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYFront - pOff), MmToFt(zDeck)), 0, "l", L2_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack + pOff), MmToFt(zDeck)), 0, "l", L2_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
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
                    if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
                    if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack), MmToFt(topZ)), 0, "l", L_cut3);
                        if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
                    }
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBayLast2, curYBack, 0, L_cut3, z0, jackH, s, storyCount, opts);
                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBayLast2), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", L_cut3);
                            if (tb != null) placedIds.Add(tb.Id.IntegerValue);
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
                if (bbL != null) placedIds.Add(bbL.Id.IntegerValue);
                if (lgFL != null) placedIds.Add(lgFL.Id.IntegerValue);
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curXLeft), MmToFt(yMin3), MmToFt(topZ)), 90, "l", L3_last);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730.0 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, curXLeft, yMin3, 90, L3_last, z0, jackH, s, storyCount, opts);
                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curXLeft + 66.0), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                    }
                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(curXLeft + pOff), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight3 - pOff), MmToFt(yMin3), MmToFt(zDeck)), 90, "l", L3_last);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
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
                    if (bbR != null) placedIds.Add(bbR.Id.IntegerValue);
                    if (lgFR != null) placedIds.Add(lgFR.Id.IntegerValue);
                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight3), MmToFt(yC4), MmToFt(topZ)), 90, "l", L_cut4);
                        if (lgTopR != null) placedIds.Add(lgTopR.Id.IntegerValue);
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
                        if (lgM != null) placedIds.Add(lgM.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight3 - 66.0), MmToFt(yC4), MmToFt(zDeck)), 90, "l", L_cut4);
                            if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
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
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(zH1)), 90, "l", L5_first);
                if (lgRF_B != null) placedIds.Add(lgRF_B.Id.IntegerValue);
                if (L_A5 > 10.0)
                {
                    var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(z0)), 90, "l", L_A5);
                    var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(zH1)), 90, "l", L_A5);
                    if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                    if (lgRF_A != null) placedIds.Add(lgRF_A.Id.IntegerValue);
                }

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(curYBack), MmToFt(topZ)), 90, "l", L5_first);
                    if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.IntegerValue);
                    if (L_A5 > 10.0)
                    {
                        var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(curYFront), MmToFt(topZ)), 90, "l", L_A5);
                        if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.IntegerValue);
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
                        if (lgRM_A != null) placedIds.Add(lgRM_A.Id.IntegerValue);
                        if (rA1 != null) placedIds.Add(rA1.Id.IntegerValue);
                        if (rA2 != null) placedIds.Add(rA2.Id.IntegerValue);
                    }

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - pOff), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(curYBack + 66.0), MmToFt(zDeck)), 0, "l", W);
                        var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66.0), MmToFt(curYBack), MmToFt(zDeck)), 90, "l", L5_first);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                        if (tbEndB != null) placedIds.Add(tbEndB.Id.IntegerValue);
                        if (L_A5 > 10.0)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(curYFront), MmToFt(zDeck)), 90, "l", L_A5);
                            if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
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
                if (bbC6_F != null) placedIds.Add(bbC6_F.Id.IntegerValue);
                if (bbC6_T != null) placedIds.Add(bbC6_T.Id.IntegerValue);

                var lgFC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH1)), 0, "l", L6_first);
                if (lgFC6_T != null) placedIds.Add(lgFC6_T.Id.IntegerValue);

                if (L_A6 > 10.0)
                {
                    var bbC6_In = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(z0)), 0, "l", L_A6);
                    var lgFC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(zH1)), 0, "l", L_A6);
                    if (bbC6_In != null) placedIds.Add(bbC6_In.Id.IntegerValue);
                    if (lgFC6_In != null) placedIds.Add(lgFC6_In.Id.IntegerValue);
                }

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(topZ)), 0, "l", L6_first);
                    if (lgTopC6_T != null) placedIds.Add(lgTopC6_T.Id.IntegerValue);
                    if (L_A6 > 10.0)
                    {
                        var lgTopC6_In = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(topZ)), 0, "l", L_A6);
                        if (lgTopC6_In != null) placedIds.Add(lgTopC6_In.Id.IntegerValue);
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
                    if (lgMC6_F != null) placedIds.Add(lgMC6_F.Id.IntegerValue);
                    if (rC6_F1 != null) placedIds.Add(rC6_F1.Id.IntegerValue);
                    if (rC6_F2 != null) placedIds.Add(rC6_F2.Id.IntegerValue);

                    var lgMC6_T = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH3)), 0, "l", L6_first);
                    var rC6_T1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH6)), 0, "l", L6_first);
                    var rC6_T2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yTop6), MmToFt(zH7)), 0, "l", L6_first);
                    if (lgMC6_T != null) placedIds.Add(lgMC6_T.Id.IntegerValue);
                    if (rC6_T1 != null) placedIds.Add(rC6_T1.Id.IntegerValue);
                    if (rC6_T2 != null) placedIds.Add(rC6_T2.Id.IntegerValue);

                    if (L_A6 > 10.0)
                    {
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yBot6, 0, L_A6, z0, jackH, s, storyCount, opts);
                        var rIn2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yBot6), MmToFt(zH7)), 0, "l", L_A6);
                        if (rIn2 != null) placedIds.Add(rIn2.Id.IntegerValue);
                    }

                    if (sym6 != null)
                    {
                        var p1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yBot6 + pOff), MmToFt(zDeck)), 0, "l", L6_first);
                        var p2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - pOff), MmToFt(zDeck)), 0, "l", L6_first);
                        if (p1 != null) { SetParam(p1, "w", pw); placedIds.Add(p1.Id.IntegerValue); }
                        if (p2 != null) { SetParam(p2, "w", pw); placedIds.Add(p2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66.0), MmToFt(yBot6), MmToFt(zDeck)), 90, "l", W);
                        var tbT = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yTop6 - 66.0), MmToFt(zDeck)), 0, "l", L6_first);
                        if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                        if (tbT != null) placedIds.Add(tbT.Id.IntegerValue);
                        if (L_A6 > 10.0)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB), MmToFt(yBot6 + 66.0), MmToFt(zDeck)), 0, "l", L_A6);
                            if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
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
    }
}
