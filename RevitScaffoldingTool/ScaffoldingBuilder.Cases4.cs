using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 4.1: KHỚP NỐI NGÃ BA CHỮ T MIRRORED (MẶT TRONG PHÍA NGƯỢC TH3)
        /// Dãy chính chạy thẳng liên tục qua ngã ba; Nhánh rẽ B vươn sang +Y.
        /// - Nhánh rẽ B ở đoạn đầu [xJunc, xR] của nhịp ngã ba (xR = xJunc + W: Cột cấy phụ).
        /// - Đoạn sau [xR, xEnd] (600 mm) có lan can kép và giằng chân.
        /// - Cửa ngõ [xJunc, xR] mở thông suốt 1200 mm.
        /// - ĐẶC TÍNH MIRRORED (Ngược TH3):
        ///   + VÁCH PHẢI (xR): MẶT TRONG -> Lan can an toàn kép Family 4 (h6, h7), giằng giữa h3.
        ///   + VÁCH TRÁI (xJunc): MẶT NGOÀI -> Giằng chéo Family 5 (PlaceBayDiagonalBraces), ván chắn biên Family 7.
        /// </summary>
        public static string ExecuteCase4_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
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

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            int juncBayIdx = (nMainBays >= 2) ? 1 : 0;
            double xJunc = colX[juncBayIdx];
            double xR = xJunc + W;
            double bayLenJunc = l1Bays[juncBayIdx];
            double L_gap = bayLenJunc > W ? (bayLenJunc - W) : 0.0;

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

            // =========================================================================
            // PHẦN B: DỰNG CẤU KIỆN DÃY CHÍNH (ROW 0)
            // =========================================================================
            for (int i = 0; i < nMainBays; i++)
            {
                double bayLen = l1Bays[i];
                double px = colX[i];
                bool isJuncBay = (i == juncBayIdx);

                if (!isJuncBay)
                {
                    BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                            px, y0, z0, W, jackH, bayLen, true, opts);
                }
                else
                {
                    // Mặt sau (Y = y0 - W): Giằng chéo Family 5 suốt 1800 mm
                    var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", bayLen);
                    var lgH1Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", bayLen);
                    if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                    if (lgH1Back != null) placedIds.Add(lgH1Back.Id.GetIdValue());

                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", bayLen);
                        if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                    }

                    // Mặt trước (Y = y0):
                    // 1) Cửa ngõ [xJunc, xR] (W mm): Mở thông suốt, không đặt ván lót cản trở lối đi
                    var lgH1Door = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0), MmToFt(zH1)), 0, "l", W);
                    if (lgH1Door != null) placedIds.Add(lgH1Door.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var trDoorDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0), MmToFt(sDeck)), 0, "l", W);
                        if (trDoorDeck != null) placedIds.Add(trDoorDeck.Id.GetIdValue());
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopDoor = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0), MmToFt(topZ)), 0, "l", W);
                        if (lgTopDoor != null) placedIds.Add(lgTopDoor.Id.GetIdValue());
                    }

                    // 2) Đoạn bảo vệ còn lại [xR, px + bayLen] (nếu bayLen > W)
                    if (L_gap > 10.0)
                    {
                        var bbGap = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(z0)), 0, "l", L_gap);
                        var lgH1Gap = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(zH1)), 0, "l", L_gap);
                        if (bbGap != null) placedIds.Add(bbGap.Id.GetIdValue());
                        if (lgH1Gap != null) placedIds.Add(lgH1Gap.Id.GetIdValue());

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopGap = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(topZ)), 0, "l", L_gap);
                            if (lgTopGap != null) placedIds.Add(lgTopGap.Id.GetIdValue());
                        }
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        // Giằng chéo mặt sau Family 5
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, bayLen, z0, jackH, s, storyCount, opts);

                        // Mâm sàn chạy suốt nhịp
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

                        // Lan can an toàn kép đoạn gap phía trước
                        if (L_gap > 10.0)
                        {
                            var lgH3Gap = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(zH3)), 0, "l", L_gap);
                            var lgH6Gap = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(zH6)), 0, "l", L_gap);
                            var lgH7Gap = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0), MmToFt(zH7)), 0, "l", L_gap);
                            if (lgH3Gap != null) placedIds.Add(lgH3Gap.Id.GetIdValue());
                            if (lgH6Gap != null) placedIds.Add(lgH6Gap.Id.GetIdValue());
                            if (lgH7Gap != null) placedIds.Add(lgH7Gap.Id.GetIdValue());

                            if (sym7 != null)
                            {
                                var tbGap = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR), MmToFt(y0 - 66.0), MmToFt(zDeck)), 0, "l", L_gap);
                                if (tbGap != null) placedIds.Add(tbGap.Id.GetIdValue());
                            }
                        }
                    }
                }
            }

            // 2 Giằng ngang tại Cột chung và Cột cấy (Y = y0 - W -> y0)
            var trJuncFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            var trR_Foot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            if (trJuncFoot != null) placedIds.Add(trJuncFoot.Id.GetIdValue());
            if (trR_Foot != null) placedIds.Add(trR_Foot.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var trJuncTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                var trR_Top = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                if (trJuncTop != null) placedIds.Add(trJuncTop.Id.GetIdValue());
                if (trR_Top != null) placedIds.Add(trR_Top.Id.GetIdValue());
            }

            // =========================================================================
            // PHẦN C: DỰNG CỘT CHO NHÁNH RẼ B (VƯƠN SANG +Y)
            // =========================================================================
            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                curY += bLen;
                var jkLeft = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xJunc), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkLeft != null) placedIds.Add(jkLeft.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xJunc, curY, z0 + jackH, opts);

                var jkRight = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkRight != null) placedIds.Add(jkRight.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, curY, z0 + jackH, opts);
            }
            double yTop = curY;

            // =========================================================================
            // PHẦN D: DỰNG CẤU KIỆN TỪNG NHỊP CỦA NHÁNH RẼ B (MIRRORED RULES)
            // =========================================================================
            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                var lgLeft_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                var lgRight_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                if (lgLeft_H1 != null) placedIds.Add(lgLeft_H1.Id.GetIdValue());
                if (lgRight_H1 != null) placedIds.Add(lgRight_H1.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

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

                    // MẶT TRONG (xR, vách phải - MIRRORED): Giằng giữa & Lan can an toàn kép Family 4
                    var lgRight_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    var lgRight_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgRight_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgRight_H3 != null) placedIds.Add(lgRight_H3.Id.GetIdValue());
                    if (lgRight_H6 != null) placedIds.Add(lgRight_H6.Id.GetIdValue());
                    if (lgRight_H7 != null) placedIds.Add(lgRight_H7.Id.GetIdValue());

                    // MẶT NGOÀI (xJunc, vách trái - MIRRORED): Giằng chéo Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xJunc, runY, 90, bLen, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        var tbLeft = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc + 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        var tbRight = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (tbLeft != null) placedIds.Add(tbLeft.Id.GetIdValue());
                        if (tbRight != null) placedIds.Add(tbRight.Id.GetIdValue());
                    }

                    if (sym6 != null)
                    {
                        var plB1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xJunc + pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (plB1 != null) { SetParam(plB1, "w", pw); placedIds.Add(plB1.Id.GetIdValue()); }

                        var plB2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (plB2 != null) { SetParam(plB2, "w", pw); placedIds.Add(plB2.Id.GetIdValue()); }
                    }

                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY + bLen), MmToFt(zDeck)), 0, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.GetIdValue());
                }

                runY += bLen;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 0, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(topZ)), 0, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.GetIdValue());
                }
            }

            // =========================================================================
            // PHẦN E: ĐÓNG ĐẦU HỒI ĐỈNH NHÁNH RẼ (Y = yTop)
            // =========================================================================
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                double zH6 = z0 + jackH + 2180.0 + s * 1800.0;
                double zH7 = z0 + jackH + 2630.0 + s * 1800.0;

                var trTopH6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH6)), 0, "l", W);
                var trTopH7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH7)), 0, "l", W);
                if (trTopH6 != null) placedIds.Add(trTopH6.Id.GetIdValue());
                if (trTopH7 != null) placedIds.Add(trTopH7.Id.GetIdValue());

                if (sym7 != null)
                {
                    var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc), MmToFt(yTop - 66.0), MmToFt(sDeck)), 0, "l", W);
                    if (tbTop != null) placedIds.Add(tbTop.Id.GetIdValue());
                }
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 4.2: KHỚP NỐI NGÃ BA CHỮ T Ô VUÔNG W x W MIRRORED (MẶT TRONG PHÍA NGƯỢC TH3)
        /// Dãy chính chạy thẳng qua ngã ba với nhịp ngã ba bằng đúng W = 1200 mm.
        /// - 100% Cột dùng chung (không cần cấy cột phụ).
        /// - Cửa ngõ mở thông suốt 1200 mm.
        /// - ĐẶC TÍNH MIRRORED:
        ///   + VÁCH PHẢI (xR): MẶT TRONG -> Lan can an toàn kép Family 4 (h6, h7), giằng giữa h3.
        ///   + VÁCH TRÁI (xJunc): MẶT NGOÀI -> Giằng chéo Family 5 (PlaceBayDiagonalBraces), ván chắn biên Family 7.
        /// </summary>
        public static string ExecuteCase4_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> l1Bays = GetL1Bays(opts);
            if (l1Bays == null || l1Bays.Count < 3)
            {
                l1Bays = new List<double> { 1800.0, W, 1800.0 };
            }
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

            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }

            double xJunc = colX[1];
            double xR = colX[2];

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

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
                    var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", bayLen);
                    var lgH1Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", bayLen);
                    var lgH1Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", bayLen);
                    if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                    if (lgH1Back != null) placedIds.Add(lgH1Back.Id.GetIdValue());
                    if (lgH1Front != null) placedIds.Add(lgH1Front.Id.GetIdValue());

                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", bayLen);
                        var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(topZ)), 0, "l", bayLen);
                        if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                        if (lgTopFront != null) placedIds.Add(lgTopFront.Id.GetIdValue());
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double zDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var trDoorDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zDeck)), 0, "l", bayLen);
                        if (trDoorDeck != null) placedIds.Add(trDoorDeck.Id.GetIdValue());

                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, bayLen, z0, jackH, s, storyCount, opts);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                        }

                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 66.0), MmToFt(zDeck)), 0, "l", bayLen);
                            if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                        }
                    }
                }
            }

            var trJuncFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            var trR_Foot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            if (trJuncFoot != null) placedIds.Add(trJuncFoot.Id.GetIdValue());
            if (trR_Foot != null) placedIds.Add(trR_Foot.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var trJuncTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                var trR_Top = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                if (trJuncTop != null) placedIds.Add(trJuncTop.Id.GetIdValue());
                if (trR_Top != null) placedIds.Add(trR_Top.Id.GetIdValue());
            }

            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                curY += bLen;
                var jkLeft = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xJunc), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkLeft != null) placedIds.Add(jkLeft.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xJunc, curY, z0 + jackH, opts);

                var jkRight = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkRight != null) placedIds.Add(jkRight.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, curY, z0 + jackH, opts);
            }
            double yTop = curY;

            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                var lgLeft_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                var lgRight_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                if (lgLeft_H1 != null) placedIds.Add(lgLeft_H1.Id.GetIdValue());
                if (lgRight_H1 != null) placedIds.Add(lgRight_H1.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

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

                    // MẶT TRONG (xR, vách phải - MIRRORED): Giằng giữa & Lan can an toàn kép Family 4
                    var lgRight_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    var lgRight_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgRight_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgRight_H3 != null) placedIds.Add(lgRight_H3.Id.GetIdValue());
                    if (lgRight_H6 != null) placedIds.Add(lgRight_H6.Id.GetIdValue());
                    if (lgRight_H7 != null) placedIds.Add(lgRight_H7.Id.GetIdValue());

                    // MẶT NGOÀI (xJunc, vách trái - MIRRORED): Giằng chéo Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xJunc, runY, 90, bLen, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        var tbLeft = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc + 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        var tbRight = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (tbLeft != null) placedIds.Add(tbLeft.Id.GetIdValue());
                        if (tbRight != null) placedIds.Add(tbRight.Id.GetIdValue());
                    }

                    if (sym6 != null)
                    {
                        var plB1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xJunc + pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (plB1 != null) { SetParam(plB1, "w", pw); placedIds.Add(plB1.Id.GetIdValue()); }

                        var plB2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (plB2 != null) { SetParam(plB2, "w", pw); placedIds.Add(plB2.Id.GetIdValue()); }
                    }

                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY + bLen), MmToFt(zDeck)), 0, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.GetIdValue());
                }

                runY += bLen;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(zH1)), 0, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(runY), MmToFt(topZ)), 0, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.GetIdValue());
                }
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                double zH6 = z0 + jackH + 2180.0 + s * 1800.0;
                double zH7 = z0 + jackH + 2630.0 + s * 1800.0;

                var trTopH6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH6)), 0, "l", W);
                var trTopH7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xJunc), MmToFt(yTop), MmToFt(zH7)), 0, "l", W);
                if (trTopH6 != null) placedIds.Add(trTopH6.Id.GetIdValue());
                if (trTopH7 != null) placedIds.Add(trTopH7.Id.GetIdValue());

                if (sym7 != null)
                {
                    var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xJunc), MmToFt(yTop - 66.0), MmToFt(sDeck)), 0, "l", W);
                    if (tbTop != null) placedIds.Add(tbTop.Id.GetIdValue());
                }
            }

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 4.3: KHỚP NỐI NGÃ BA CHỮ T LỆCH PHẢI MIRRORED (MẶT TRONG PHÍA NGƯỢC TH3)
        /// Dãy chính chạy thẳng liên tục qua ngã ba; Nhánh rẽ B mọc từ vị trí lệch phải:
        /// - Cột mép phải (xR, y0) là CỘT DÙNG CHUNG.
        /// - Cột mép trái (xL, y0) là CỘT CẤY PHỤ tại xL = xR - W = xStart + 600 mm.
        /// - Khoảng [xStart, xL] (600 mm) có đầy đủ lan can an toàn và ván chắn biên.
        /// - Khoảng [xL, xR] (1200 mm) mở cửa ngõ thông suốt 100%.
        /// - ĐẶC TÍNH MIRRORED:
        ///   + VÁCH PHẢI (xR): MẶT TRONG -> Lan can an toàn kép Family 4 (h6, h7), giằng giữa h3.
        ///   + VÁCH TRÁI (xL): MẶT NGOÀI -> Giằng chéo Family 5 (PlaceBayDiagonalBraces), ván chắn biên Family 7.
        /// </summary>
        public static string ExecuteCase4_3(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
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

            int nMainBays = l1Bays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in l1Bays) { curX += b; colX.Add(curX); }

            double xStart = colX[1];
            double xEnd = colX[2];
            double xR = xEnd;
            double xL = xR - W;
            double cutFrontLen = xL - xStart;

            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            var jkCapped = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
            if (jkCapped != null) placedIds.Add(jkCapped.Id.GetIdValue());
            PlaceConfiguredPosts(doc, sym3, level, placedIds, xL, y0, z0 + jackH, opts);

            for (int i = 0; i < nMainBays; i++)
            {
                double bayLen = l1Bays[i];
                double px = colX[i];
                bool isJuncBay = (i == 1);

                if (!isJuncBay)
                {
                    BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                            px, y0, z0, W, jackH, bayLen, true, opts);
                }
                else
                {
                    var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", bayLen);
                    var lgH1Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", bayLen);
                    if (bbBack != null) placedIds.Add(bbBack.Id.GetIdValue());
                    if (lgH1Back != null) placedIds.Add(lgH1Back.Id.GetIdValue());

                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", bayLen);
                        if (lgTopBack != null) placedIds.Add(lgTopBack.Id.GetIdValue());
                    }

                    if (cutFrontLen > 10.0)
                    {
                        var bbFrontCut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "l", cutFrontLen);
                        var lgH1FrontCut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", cutFrontLen);
                        if (bbFrontCut != null) placedIds.Add(bbFrontCut.Id.GetIdValue());
                        if (lgH1FrontCut != null) placedIds.Add(lgH1FrontCut.Id.GetIdValue());

                        if (opts != null && opts.HasTopTransom)
                        {
                            var lgTopFrontCut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(topZ)), 0, "l", cutFrontLen);
                            if (lgTopFrontCut != null) placedIds.Add(lgTopFrontCut.Id.GetIdValue());
                        }
                    }

                    var lgH1Junc = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(zH1)), 0, "l", W);
                    if (lgH1Junc != null) placedIds.Add(lgH1Junc.Id.GetIdValue());

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                        var trDoorDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(sDeck)), 0, "l", W);
                        if (trDoorDeck != null) placedIds.Add(trDoorDeck.Id.GetIdValue());
                    }

                    if (opts != null && opts.HasTopTransom)
                    {
                        var lgTopJunc = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0), MmToFt(topZ)), 0, "l", W);
                        if (lgTopJunc != null) placedIds.Add(lgTopJunc.Id.GetIdValue());
                    }

                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double zH3 = z0 + jackH + 830.0 + sOff;
                        double zDeck = z0 + jackH + 1730.0 + sOff;
                        double zH6 = z0 + jackH + 2180.0 + sOff;
                        double zH7 = z0 + jackH + 2630.0 + sOff;

                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, bayLen, z0, jackH, s, storyCount, opts);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", bayLen);
                            if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                        }

                        if (sym7 != null)
                        {
                            var tb = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 66.0), MmToFt(zDeck)), 0, "l", bayLen);
                            if (tb != null) placedIds.Add(tb.Id.GetIdValue());
                        }

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

            var trL_Foot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            var trR_Foot_33 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(zH1)), 90, "l", W);
            if (trL_Foot != null) placedIds.Add(trL_Foot.Id.GetIdValue());
            if (trR_Foot_33 != null) placedIds.Add(trR_Foot_33.Id.GetIdValue());

            if (opts != null && opts.HasTopTransom)
            {
                var trL_Top = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                var trR_Top = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(y0 - W), MmToFt(topZ)), 90, "l", W);
                if (trL_Top != null) placedIds.Add(trL_Top.Id.GetIdValue());
                if (trR_Top != null) placedIds.Add(trR_Top.Id.GetIdValue());
            }

            double curY = y0;
            foreach (var bLen in l2Bays)
            {
                curY += bLen;
                var jkLeft = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xL), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkLeft != null) placedIds.Add(jkLeft.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xL, curY, z0 + jackH, opts);

                var jkRight = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xR), MmToFt(curY), MmToFt(z0)), 0, "h", jackH);
                if (jkRight != null) placedIds.Add(jkRight.Id.GetIdValue());
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xR, curY, z0 + jackH, opts);
            }
            double yTop = curY;

            double runY = y0;
            foreach (var bLen in l2Bays)
            {
                var lgLeft_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                var lgRight_H1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH1)), 90, "l", bLen);
                if (lgLeft_H1 != null) placedIds.Add(lgLeft_H1.Id.GetIdValue());
                if (lgRight_H1 != null) placedIds.Add(lgRight_H1.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopL = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    var lgTopR = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(topZ)), 90, "l", bLen);
                    if (lgTopL != null) placedIds.Add(lgTopL.Id.GetIdValue());
                    if (lgTopR != null) placedIds.Add(lgTopR.Id.GetIdValue());
                }

                var bbLeft = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xL), MmToFt(runY), MmToFt(z0)), 90, "l", bLen);
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

                    // MẶT TRONG (xR, vách phải - MIRRORED): Giằng giữa & Lan can an toàn kép Family 4
                    var lgRight_H3 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH3)), 90, "l", bLen);
                    var lgRight_H6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH6)), 90, "l", bLen);
                    var lgRight_H7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xR), MmToFt(runY), MmToFt(zH7)), 90, "l", bLen);
                    if (lgRight_H3 != null) placedIds.Add(lgRight_H3.Id.GetIdValue());
                    if (lgRight_H6 != null) placedIds.Add(lgRight_H6.Id.GetIdValue());
                    if (lgRight_H7 != null) placedIds.Add(lgRight_H7.Id.GetIdValue());

                    // MẶT NGOÀI (xL, vách trái - MIRRORED): Giằng chéo Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xL, runY, 90, bLen, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        var tbLeft = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xL + 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        var tbRight = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xR - 66.0), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (tbLeft != null) placedIds.Add(tbLeft.Id.GetIdValue());
                        if (tbRight != null) placedIds.Add(tbRight.Id.GetIdValue());
                    }

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xL + pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xR - pOff), MmToFt(runY), MmToFt(zDeck)), 90, "l", bLen);
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                    }

                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(runY + bLen), MmToFt(zDeck)), 0, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.GetIdValue());
                }

                runY += bLen;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(runY), MmToFt(zH1)), 0, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(runY), MmToFt(topZ)), 0, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.GetIdValue());
                }
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730.0 + s * 1800.0;
                double zH6 = z0 + jackH + 2180.0 + s * 1800.0;
                double zH7 = z0 + jackH + 2630.0 + s * 1800.0;

                var trTopH6 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(yTop), MmToFt(zH6)), 0, "l", W);
                var trTopH7 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xL), MmToFt(yTop), MmToFt(zH7)), 0, "l", W);
                if (trTopH6 != null) placedIds.Add(trTopH6.Id.GetIdValue());
                if (trTopH7 != null) placedIds.Add(trTopH7.Id.GetIdValue());

                if (sym7 != null)
                {
                    var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xL), MmToFt(yTop - 66.0), MmToFt(sDeck)), 0, "l", W);
                    if (tbTop != null) placedIds.Add(tbTop.Id.GetIdValue());
                }
            }

            return null;
        }
    }
}
