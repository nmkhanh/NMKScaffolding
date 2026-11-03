const fs = require('fs');

let tableCode = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', 'utf8');

// 1. Remove the shortcut if (isContinuousMaster)
const shortcutRegex = /\s*\/\/ KIỂM TRA CHUỖI GÓC NỐI LIÊN TỤC THEO MASTER[\s\S]*?return ExecuteMaster6CornersContinuous\(doc, opts, level, placedIds, nRows\);\s*\}/;
tableCode = tableCode.replace(shortcutRegex, '');

// 2. Replace the multi-row loop (Bước 1 & Bước 2) with the dynamic chained pipeline
const oldMultiRowLoop = `            // Bước 1: Dựng các nhịp thẳng tiêu chuẩn cho từng hàng (mở cửa ngõ góc nối nếu có hàng kế tiếp)
            for (int i = 0; i < nRows; i++)
            {
                var rowCur = rows[i];
                bool hasNext = (i < nRows - 1) || isClosedLoop;
                bool openStart = (i > 0) || isClosedLoop;

                BuildTableStraightRun(doc, rowCur, opts, level, placedIds, ensureCol, placeTransom, sym1, sym4, sym5, sym6, sym7,
                    openStart: openStart, hasCornerConnect: hasNext);
            }

            // Bước 2: Xử lý Connect giữa các hàng nối tiếp nhau (chỉ xử lý phần góc nối, không dựng trùng lặp hàng sau)
            for (int i = 0; i < nRows; i++)
            {
                bool hasNext = (i < nRows - 1) || isClosedLoop;
                if (!hasNext) continue;

                var rowPrev = rows[i];
                var rowNext = rows[(i + 1) % nRows];
                string cornerCase = !string.IsNullOrEmpty(rowPrev.ConnectCase) ? rowPrev.ConnectCase : "TH 1.1";

                BuildTableCornerConnect(doc, rowPrev, rowNext, cornerCase, opts, level, placedIds, ensureCol, placeTransom,
                    sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: false);
            }`;

const newMultiRowPipeline = `            // THI CÔNG LIÊN TỤC NHIỀU ROW DỰA TRÊN DATA BẢNG TABLE (100% ĐỘNG THEO BayLengths, Width, ConnectCase):
            // Kế thừa logic hình học từ Master 6 Corners: Mỗi hàng kế tiếp tự động bắt đầu từ cửa ngõ của góc nối hàng trước.
            BuildTableChainedContinuous(doc, opts, rows, level, placedIds, ensureCol, placeTransom,
                sym1, sym2, sym3, sym4, sym5, sym6, sym7, isClosedLoop);`;

if (tableCode.includes('// Bước 1: Dựng các nhịp thẳng tiêu chuẩn')) {
  tableCode = tableCode.replace(oldMultiRowLoop, newMultiRowPipeline);
  console.log('Replaced multi-row loop.');
}

const chainMethodImpl = `        /// <summary>
        /// THI CÔNG LIÊN TỤC NHIỀU HÀNG THEO CHUỖI ĐỘNG (100% DỮ LIỆU TỪ TABLE):
        /// Đọc trực tiếp BayLengths, Width, ConnectCase từ từng hàng trong Table.
        /// Chuyển tiếp góc chuẩn xác từ cửa ngõ của Row i sang Row i+1, không bao giờ hardcode kích thước.
        /// </summary>
        private static void BuildTableChainedContinuous(Document doc, BuildOptions opts, List<BayRowModel> rows, Level level, List<int> placedIds,
            Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isClosedLoop)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);
            double zH1 = z0 + jackH + 135;
            double zH3 = z0 + jackH + 830;
            double zH6 = z0 + jackH + 2180;
            double zH7 = z0 + jackH + 2630;
            double zDeck = z0 + jackH + 1730;

            int nRows = rows.Count;
            if (nRows == 0) return;

            // Tọa độ 2 cột bắt đầu hiện tại (Mép ngoài pOut và Mép trong pIn)
            double curOutX = rows[0].StartX;
            double curOutY = rows[0].StartY;
            double W0 = rows[0].Width > 0 ? rows[0].Width : 1200.0;
            double ang0Rad = rows[0].AngleDeg * Math.PI / 180.0;
            double curUx = Math.Cos(ang0Rad), curUy = Math.Sin(ang0Rad);
            double curVx = -curUy, curVy = curUx; // Pháp tuyến hướng vào mặt trong (+90 độ)
            double curInX = curOutX + W0 * curVx;
            double curInY = curOutY + W0 * curVy;
            double curRot = rows[0].AngleDeg;
            double curW = W0;

            for (int r = 0; r < nRows; r++)
            {
                var rowCur = rows[r];
                var rowNext = (r < nRows - 1) ? rows[r + 1] : (isClosedLoop ? rows[0] : null);
                bool hasNext = (rowNext != null) && (!string.IsNullOrEmpty(rowCur.ConnectCase) && rowCur.ConnectCase != "None");

                var bayLens = (rowCur.BayLengths != null && rowCur.BayLengths.Count > 0)
                    ? rowCur.BayLengths
                    : new List<double> { 1800.0 };

                int nBays = bayLens.Count;
                int nStandard = hasNext ? Math.Max(0, nBays - 1) : nBays;
                double nextW = (rowNext != null && rowNext.Width > 0) ? rowNext.Width : 1200.0;

                ensureCol(curOutX, curOutY);
                ensureCol(curInX, curInY);

                // Đầu hàng 0: Đóng kín lan can đầu hồi nếu không phải vòng khép kín
                if (r == 0 && !isClosedLoop)
                {
                    placeTransom(curOutX, curOutY, zH1, curRot + 90, curW);
                    placeTransom(curOutX, curOutY, zDeck, curRot + 90, curW);
                    if (opts != null && opts.HasTopTransom) placeTransom(curOutX, curOutY, topZ, curRot + 90, curW);

                    var rB1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curOutX), MmToFt(curOutY), MmToFt(zH6)), curRot + 90, "l", curW);
                    var rB2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(curOutX), MmToFt(curOutY), MmToFt(zH7)), curRot + 90, "l", curW);
                    if (rB1 != null) placedIds.Add(rB1.Id.IntegerValue);
                    if (rB2 != null) placedIds.Add(rB2.Id.IntegerValue);
                    if (sym7 != null)
                    {
                        var tbB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(curOutX), MmToFt(curOutY), MmToFt(zDeck)), curRot + 90, "l", curW);
                        if (tbB != null) placedIds.Add(tbB.Id.IntegerValue);
                    }
                }

                // Dựng các nhịp thẳng tiêu chuẩn của hàng r (100% bám sát bayLens từ Table)
                double runDist = 0;
                for (int b = 0; b < nStandard; b++)
                {
                    double bLen = bayLens[b];
                    double p1x = curOutX + runDist * curUx;
                    double p1y = curOutY + runDist * curUy;
                    double p2x = curInX + runDist * curUx;
                    double p2y = curInY + runDist * curUy;

                    double nextDist = runDist + bLen;
                    double p1NextX = curOutX + nextDist * curUx;
                    double p1NextY = curOutY + nextDist * curUy;
                    double p2NextX = curInX + nextDist * curUx;
                    double p2NextY = curInY + nextDist * curUy;

                    ensureCol(p1NextX, p1NextY);
                    ensureCol(p2NextX, p2NextY);

                    // Transom ngang đỡ sàn và chân tại điểm cuối nhịp
                    placeTransom(p1NextX, p1NextY, zH1, curRot + 90, curW);
                    placeTransom(p1NextX, p1NextY, zDeck, curRot + 90, curW);
                    if (opts != null && opts.HasTopTransom) placeTransom(p1NextX, p1NextY, topZ, curRot + 90, curW);

                    // Mép ngoài (p1): Ván lót F1, giằng chân F4, giằng chéo F5, ván chắn F7
                    var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1x), MmToFt(p1y), MmToFt(z0)), curRot, "l", bLen);
                    var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1x), MmToFt(p1y), MmToFt(zH1)), curRot, "l", bLen);
                    if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                    if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1x, p1y, curRot, bLen, z0, jackH, opts);
                    if (sym7 != null)
                    {
                        var tbOut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1x + 66 * curVx), MmToFt(p1y + 66 * curVy), MmToFt(zDeck)), curRot, "l", bLen);
                        if (tbOut != null) placedIds.Add(tbOut.Id.IntegerValue);
                    }

                    // Mép trong (p2): Ván lót F1, giằng chân F4, giằng giữa h3, lan can kép h6/h7, ván chắn F7
                    var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2x), MmToFt(p2y), MmToFt(z0)), curRot, "l", bLen);
                    var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2x), MmToFt(p2y), MmToFt(zH1)), curRot, "l", bLen);
                    var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2x), MmToFt(p2y), MmToFt(zH3)), curRot, "l", bLen);
                    var rIn1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2x), MmToFt(p2y), MmToFt(zH6)), curRot, "l", bLen);
                    var rIn2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2x), MmToFt(p2y), MmToFt(zH7)), curRot, "l", bLen);
                    if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                    if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                    if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                    if (rIn1 != null) placedIds.Add(rIn1.Id.IntegerValue);
                    if (rIn2 != null) placedIds.Add(rIn2.Id.IntegerValue);
                    if (sym7 != null)
                    {
                        var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p2x - 66 * curVx), MmToFt(p2y - 66 * curVy), MmToFt(zDeck)), curRot, "l", bLen);
                        if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
                    }

                    // Mâm sàn F6
                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1x + 345 * curVx), MmToFt(p1y + 345 * curVy), MmToFt(zDeck)), curRot, "l", bLen);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2x - 345 * curVx), MmToFt(p2y - 345 * curVy), MmToFt(zDeck)), curRot, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    runDist = nextDist;
                }

                // XỬ LÝ NHỊP CUỐI HOẶC KHỚP GÓC NỐI SANG HÀNG TIẾP THEO
                if (hasNext)
                {
                    double L_last = bayLens[nBays - 1];
                    double p1StartX = curOutX + runDist * curUx;
                    double p1StartY = curOutY + runDist * curUy;
                    double p2StartX = curInX + runDist * curUx;
                    double p2StartY = curInY + runDist * curUy;

                    string cCase = rowCur.ConnectCase;
                    if (cCase == "TH 1.1")
                    {
                        double L_cut = Math.Max(0, L_last - nextW);
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;
                        double pCx = p2StartX + L_cut * curUx;
                        double pCy = p2StartY + L_cut * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);
                        if (L_cut > 0) ensureCol(pCx, pCy);

                        // Đóng kín đầu hồi cuối dãy tại p1Max qua curW
                        placeTransom(p1MaxX, p1MaxY, zH1, curRot + 90, curW);
                        placeTransom(p1MaxX, p1MaxY, zDeck, curRot + 90, curW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p1MaxX, p1MaxY, topZ, curRot + 90, curW);
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH6)), curRot + 90, "l", curW);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH7)), curRot + 90, "l", curW);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbE = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zDeck)), curRot + 90, "l", curW);
                            if (tbE != null) placedIds.Add(tbE.Id.IntegerValue);
                        }

                        // Mép ngoài: Full L_last với giằng chéo F5
                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);
                        if (sym7 != null)
                        {
                            var tbOut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1StartX + 66 * curVx), MmToFt(p1StartY + 66 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (tbOut != null) placedIds.Add(tbOut.Id.IntegerValue);
                        }

                        // Mép trong: Cắt ngắn L_cut với lan can F4
                        if (L_cut > 0)
                        {
                            var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(z0)), curRot, "l", L_cut);
                            var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH1)), curRot, "l", L_cut);
                            var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH3)), curRot, "l", L_cut);
                            var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH6)), curRot, "l", L_cut);
                            var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH7)), curRot, "l", L_cut);
                            if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                            if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                            if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                            if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                            if (r2 != null) placedIds.Add(r2.Id.IntegerValue);
                            if (sym7 != null)
                            {
                                var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p2StartX - 66 * curVx), MmToFt(p2StartY - 66 * curVy), MmToFt(zDeck)), curRot, "l", L_cut);
                                if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
                            }
                        }

                        // Shared Transom mở thông cửa ngõ rộng nextW dọc theo hướng tuyến curRot
                        placeTransom(pCx, pCy, zH1, curRot, nextW);
                        placeTransom(pCx, pCy, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pCx, pCy, topZ, curRot, nextW);

                        // Mâm sàn nhịp cuối
                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        // Neo hàng sau (r + 1) trực tiếp từ cửa ngõ:
                        curOutX = p2MaxX;
                        curOutY = p2MaxY;
                        curInX = pCx;
                        curInY = pCy;
                        curUx = curVx; curUy = curVy; // Rẽ +90 độ
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 1.2")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        // Vách trong: Lan can Family 4
                        var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH1)), curRot, "l", L_last);
                        var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH3)), curRot, "l", L_last);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH6)), curRot, "l", L_last);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH7)), curRot, "l", L_last);
                        if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                        if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                        if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p2StartX - 66 * curVx), MmToFt(p2StartY - 66 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
                        }

                        // Đỉnh đầu hồi đóng kín
                        placeTransom(p2MaxX, p2MaxY, zH1, curRot - 90, curW);
                        placeTransom(p2MaxX, p2MaxY, zDeck, curRot - 90, curW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p2MaxX, p2MaxY, topZ, curRot - 90, curW);
                        var rTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH6)), curRot - 90, "l", curW);
                        var rTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH7)), curRot - 90, "l", curW);
                        if (rTop1 != null) placedIds.Add(rTop1.Id.IntegerValue);
                        if (rTop2 != null) placedIds.Add(rTop2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zDeck)), curRot - 90, "l", curW);
                            if (tbTop != null) placedIds.Add(tbTop.Id.IntegerValue);
                        }

                        // Cửa ngõ mở thông sang hàng sau tại mép ngoài p1StartX (rộng nextW)
                        placeTransom(p1StartX, p1StartY, zH1, curRot, nextW);
                        placeTransom(p1StartX, p1StartY, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p1StartX, p1StartY, topZ, curRot, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        // Neo hàng sau (rẽ -90 độ sang phải)
                        curOutX = p1MaxX;
                        curOutY = p1MaxY;
                        curInX = p1StartX;
                        curInY = p1StartY;
                        curUx = -curVx; curUy = -curVy; // Rẽ -90 độ
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot - 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 1.3")
                    {
                        double L_cut = Math.Max(0, L_last - nextW);
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;
                        double pCx = p1StartX + L_cut * curUx;
                        double pCy = p1StartY + L_cut * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);
                        if (L_cut > 0) ensureCol(pCx, pCy);

                        // Mép ngoài (p2/top): Lan can Family 4
                        var bbIn = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH1)), curRot, "l", L_last);
                        var lgMIn = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH3)), curRot, "l", L_last);
                        var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH6)), curRot, "l", L_last);
                        var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2StartX), MmToFt(p2StartY), MmToFt(zH7)), curRot, "l", L_last);
                        if (bbIn != null) placedIds.Add(bbIn.Id.IntegerValue);
                        if (lgFIn != null) placedIds.Add(lgFIn.Id.IntegerValue);
                        if (lgMIn != null) placedIds.Add(lgMIn.Id.IntegerValue);
                        if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                        if (r2 != null) placedIds.Add(r2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbIn = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p2StartX - 66 * curVx), MmToFt(p2StartY - 66 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (tbIn != null) placedIds.Add(tbIn.Id.IntegerValue);
                        }

                        // Mép dưới (p1): Giằng chéo thu ngắn Family 5
                        if (L_cut > 0)
                        {
                            var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_cut);
                            var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_cut);
                            if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                            if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_cut, z0, jackH, opts);
                            if (sym7 != null)
                            {
                                var tbOut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1StartX + 66 * curVx), MmToFt(p1StartY + 66 * curVy), MmToFt(zDeck)), curRot, "l", L_cut);
                                if (tbOut != null) placedIds.Add(tbOut.Id.IntegerValue);
                            }
                        }

                        // Cửa ngõ mở thông rộng nextW tại pCx
                        placeTransom(pCx, pCy, zH1, curRot, nextW);
                        placeTransom(pCx, pCy, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pCx, pCy, topZ, curRot, nextW);

                        // Đóng kín đầu hồi cuối dãy tại p1MaxX
                        placeTransom(p1MaxX, p1MaxY, zH1, curRot + 90, curW);
                        placeTransom(p1MaxX, p1MaxY, zDeck, curRot + 90, curW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p1MaxX, p1MaxY, topZ, curRot + 90, curW);
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH6)), curRot + 90, "l", curW);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH7)), curRot + 90, "l", curW);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbE = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zDeck)), curRot + 90, "l", curW);
                            if (tbE != null) placedIds.Add(tbE.Id.IntegerValue);
                        }

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        // Neo hàng sau (rẽ -90 độ sang -Y)
                        curOutX = pCx;
                        curOutY = pCy;
                        curInX = p1MaxX;
                        curInY = p1MaxY;
                        curUx = -curVx; curUy = -curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot - 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 1.4")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        // Vách trong: Giằng chéo Family 5
                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);
                        if (sym7 != null)
                        {
                            var tbOut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1StartX + 66 * curVx), MmToFt(p1StartY + 66 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (tbOut != null) placedIds.Add(tbOut.Id.IntegerValue);
                        }

                        // Đáy đầu hồi đóng kín
                        placeTransom(p1MaxX, p1MaxY, zH1, curRot + 90, curW);
                        placeTransom(p1MaxX, p1MaxY, zDeck, curRot + 90, curW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p1MaxX, p1MaxY, topZ, curRot + 90, curW);
                        var rB1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH6)), curRot + 90, "l", curW);
                        var rB2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH7)), curRot + 90, "l", curW);
                        if (rB1 != null) placedIds.Add(rB1.Id.IntegerValue);
                        if (rB2 != null) placedIds.Add(rB2.Id.IntegerValue);
                        if (sym7 != null)
                        {
                            var tbB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zDeck)), curRot + 90, "l", curW);
                            if (tbB != null) placedIds.Add(tbB.Id.IntegerValue);
                        }

                        // Cửa ngõ mở sang hàng sau tại p2MaxX
                        placeTransom(p2MaxX, p2MaxY, zH1, curRot + 90, nextW);
                        placeTransom(p2MaxX, p2MaxY, zDeck, curRot + 90, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(p2MaxX, p2MaxY, topZ, curRot + 90, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        // Neo hàng sau (rẽ +90 độ)
                        curOutX = p2MaxX;
                        curOutY = p2MaxY;
                        curInX = p2StartX;
                        curInY = p2StartY;
                        curUx = curVx; curUy = curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else
                    {
                        // Mặc định chuyển tiếp nếu là các trường hợp khác
                        curOutX = p1StartX + L_last * curUx;
                        curOutY = p1StartY + L_last * curUy;
                        curInX = p2StartX + L_last * curUx;
                        curInY = p2StartY + L_last * curUy;
                    }
                }
                else
                {
                    // Hàng cuối cùng không nối tiếp: Đóng kín đầu hồi cuối cùng an toàn 100%
                    double p1FinalX = curOutX + runDist * curUx;
                    double p1FinalY = curOutY + runDist * curUy;
                    placeTransom(p1FinalX, p1FinalY, zH1, curRot + 90, curW);
                    placeTransom(p1FinalX, p1FinalY, zDeck, curRot + 90, curW);
                    if (opts != null && opts.HasTopTransom) placeTransom(p1FinalX, p1FinalY, topZ, curRot + 90, curW);

                    var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1FinalX), MmToFt(p1FinalY), MmToFt(zH6)), curRot + 90, "l", curW);
                    var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1FinalX), MmToFt(p1FinalY), MmToFt(zH7)), curRot + 90, "l", curW);
                    if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                    if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);
                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(p1FinalX), MmToFt(p1FinalY), MmToFt(zDeck)), curRot + 90, "l", curW);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }
        }
`;

// Insert chainMethodImpl right before BuildTableStraightRun
tableCode = tableCode.replace('        private static void BuildTableStraightRun(', chainMethodImpl + '\r\n        private static void BuildTableStraightRun(');
fs.writeFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', tableCode, 'utf8');
console.log('Inserted BuildTableChainedContinuous into ScaffoldingBuilder.TableRun.cs.');
