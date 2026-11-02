using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitScaffoldingTool.ViewModels;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// Điểm vào Transaction cho chế độ Run Table Pipeline.
        /// Kế thừa 100% logic cấu kiện từ 6 TH đã kiểm chứng trong Cases1.cs và Cases2.cs,
        /// thay thế toàn bộ số kích thước cứng bằng công thức động: L_last, L_first, W_A, W_B, L_cut, L_step.
        /// </summary>
        public static BuildResult ExecuteTableBuild(Document doc, BuildOptions opts, List<BayRowModel> rows)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = new BuildResult();

            if (doc == null)
            {
                res.Message = "Revit Document không hợp lệ!";
                return res;
            }

            if (rows == null || rows.Count == 0)
            {
                res.Message = "Bảng dữ liệu Table rỗng!";
                return res;
            }

            using (var tx = new Transaction(doc, "Dung Gian Giao - Table Pipeline (Thang & Connect)"))
            {
                var fOps = tx.GetFailureHandlingOptions();
                fOps.SetFailuresPreprocessor(new WarningDiscarder());
                tx.SetFailureHandlingOptions(fOps);

                tx.Start();

                try
                {
                    Level defaultLevel = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .OrderBy(l => l.Elevation)
                        .FirstOrDefault();

                    List<int> placedIds = new List<int>();
                    string err = ExecuteTablePipeline(doc, opts, rows, defaultLevel, placedIds);
                    if (!string.IsNullOrEmpty(err))
                    {
                        tx.RollBack();
                        res.Message = "Lỗi Table Pipeline: " + err;
                        return res;
                    }

                    tx.Commit();
                    sw.Stop();
                    res.Success = true;
                    res.TotalElements = placedIds.Count;
                    res.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                    res.Message = string.Format("Hoàn thành thi công {0} hệ giàn theo Table Pipeline ({1} cấu kiện, {2:F0} ms)",
                        rows.Count, placedIds.Count, res.ElapsedMs);
                    return res;
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    res.Message = "Lỗi ngoại lệ Table Pipeline: " + ex.Message;
                    return res;
                }
            }
        }

        /// <summary>
        /// Pipeline thực thi tuần tự:
        /// 1) Nếu 1 Row: Dựng thẳng + nếu có Connect thì dựng góc và nhánh rẽ mẫu (chuẩn xác như Button).
        /// 2) Nếu nhiều Row: Dựng TH thẳng Row 1 -> TH thẳng Row 2 -> Connect (Row 1 -> Row 2) -> ... -> Khép kín vòng.
        /// </summary>
        private static string ExecuteTablePipeline(Document doc, BuildOptions opts, List<BayRowModel> rows, Level level, List<int> placedIds)
        {
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

            double jackH = GetJackHeight(opts);
            HashSet<string> placedCols = new HashSet<string>();

            Action<double, double> ensureCol = (cx, cy) =>
            {
                string key = string.Format("{0:F0}_{1:F0}", Math.Round(cx), Math.Round(cy));
                if (!placedCols.Contains(key))
                {
                    placedCols.Add(key);
                    var jk = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(opts.Z0)), 0, "h", jackH);
                    if (jk != null) placedIds.Add(jk.Id.IntegerValue);
                    PlaceConfiguredPosts(doc, sym3, level, placedIds, cx, cy, opts.Z0 + jackH, opts);
                }
            };

            HashSet<string> placedTransoms = new HashSet<string>();
            Action<double, double, double, double, double> placeTransom = (tx, ty, tz, trot, tlen) =>
            {
                string key = string.Format("{0:F0}_{1:F0}_{2:F0}_{3:F0}", Math.Round(tx), Math.Round(ty), Math.Round(tz), Math.Round(trot));
                if (!placedTransoms.Contains(key))
                {
                    placedTransoms.Add(key);
                    var tr = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(tz)), trot, "l", tlen);
                    if (tr != null) placedIds.Add(tr.Id.IntegerValue);
                }
            };

            int nRows = rows.Count;

            // TRƯỜNG HỢP 1: CHỈ CÓ 1 HÀNG TRONG TABLE (Người dùng test 1 hàng đơn lẻ)
            if (nRows == 1)
            {
                var rowSingle = rows[0];
                bool hasCorner = !string.IsNullOrEmpty(rowSingle.ConnectCase) && rowSingle.ConnectCase != "None";
                if (!hasCorner)
                {
                    // Dựng thẳng bình thường đóng kín 2 đầu
                    BuildTableStraightRun(doc, rowSingle, opts, level, placedIds, ensureCol, placeTransom, sym1, sym4, sym5, sym6, sym7,
                        openStart: false, hasCornerConnect: false);
                }
                else
                {
                    // Standalone test: Dựng các nhịp trước + Dựng nhịp góc cuối + Nhánh rẽ mẫu (100% giống kết quả Button)
                    BuildTableStraightRun(doc, rowSingle, opts, level, placedIds, ensureCol, placeTransom, sym1, sym4, sym5, sym6, sym7,
                        openStart: false, hasCornerConnect: true);

                    BuildTableCornerConnect(doc, rowSingle, null, rowSingle.ConnectCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
                return null;
            }

            // TRƯỜNG HỢP 2: CÓ TỪ 2 HÀNG TRỞ LÊN (Thi công liên tục nhiều Leg theo đúng quy trình)
            bool isClosedLoop = false;
            var rLast = rows[nRows - 1];
            var rFirst = rows[0];
            double dEndToStart = Math.Sqrt(Math.Pow(rLast.EndX - rFirst.StartX, 2) + Math.Pow(rLast.EndY - rFirst.StartY, 2));
            isClosedLoop = (dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rLast.ConnectCase) && rLast.ConnectCase != "None");

            // THI CÔNG LIÊN TỤC NHIỀU ROW DỰA TRÊN DATA BẢNG TABLE (100% ĐỘNG THEO BayLengths, Width, ConnectCase):
            // Kế thừa logic hình học từ Master 6 Corners: Mỗi hàng kế tiếp tự động bắt đầu từ cửa ngõ của góc nối hàng trước.
            BuildTableChainedContinuous(doc, opts, rows, level, placedIds, ensureCol, placeTransom,
                sym1, sym2, sym3, sym4, sym5, sym6, sym7, isClosedLoop);

            return null;
        }

        /// <summary>
        /// DỰNG CÁC NHỊP THẲNG TIÊU CHUẨN CỦA 1 HÀNG TRONG TABLE:
        /// Nếu hasCornerConnect = true, nhịp cuối cùng sẽ do hàm Connect chuyên trách xử lý hoàn thiện.
        /// </summary>
        /// <summary>
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
                    else if (cCase == "TH 2.1")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);

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

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        double L_first = (rowNext != null && rowNext.BayLengths != null && rowNext.BayLengths.Count > 0) ? rowNext.BayLengths[0] : 1800.0;
                        double pExtOutX = p1MaxX + nextW * curUx;
                        double pExtOutY = p1MaxY + nextW * curUy;
                        double pExtTopOutX = pExtOutX + L_first * curVx;
                        double pExtTopOutY = pExtOutY + L_first * curVy;
                        double pExtTopInX = p1MaxX + L_first * curVx;
                        double pExtTopInY = p1MaxY + L_first * curVy;

                        ensureCol(pExtOutX, pExtOutY);
                        ensureCol(pExtTopOutX, pExtTopOutY);
                        ensureCol(pExtTopInX, pExtTopInY);

                        placeTransom(p1MaxX, p1MaxY, zH1, curRot, nextW);
                        placeTransom(p1MaxX, p1MaxY, zDeck, curRot, nextW);
                        var rS1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH6)), curRot, "l", nextW);
                        var rS2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1MaxX), MmToFt(p1MaxY), MmToFt(zH7)), curRot, "l", nextW);
                        if (rS1 != null) placedIds.Add(rS1.Id.IntegerValue);
                        if (rS2 != null) placedIds.Add(rS2.Id.IntegerValue);

                        var bbExtE = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pExtOutX), MmToFt(pExtOutY), MmToFt(z0)), curRot + 90, "l", L_first);
                        var lgFExtE = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtOutX), MmToFt(pExtOutY), MmToFt(zH1)), curRot + 90, "l", L_first);
                        if (bbExtE != null) placedIds.Add(bbExtE.Id.IntegerValue);
                        if (lgFExtE != null) placedIds.Add(lgFExtE.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, pExtOutX, pExtOutY, curRot + 90, L_first, z0, jackH, opts);

                        double L_in = Math.Max(0, L_first - curW);
                        if (L_in > 0)
                        {
                            var bbInS = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(z0)), curRot + 90, "l", L_in);
                            var lgFInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH1)), curRot + 90, "l", L_in);
                            var lgMInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH3)), curRot + 90, "l", L_in);
                            var rInS1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH6)), curRot + 90, "l", L_in);
                            var rInS2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH7)), curRot + 90, "l", L_in);
                            if (bbInS != null) placedIds.Add(bbInS.Id.IntegerValue);
                            if (lgFInS != null) placedIds.Add(lgFInS.Id.IntegerValue);
                            if (lgMInS != null) placedIds.Add(lgMInS.Id.IntegerValue);
                            if (rInS1 != null) placedIds.Add(rInS1.Id.IntegerValue);
                            if (rInS2 != null) placedIds.Add(rInS2.Id.IntegerValue);
                        }

                        placeTransom(pExtTopInX, pExtTopInY, zH1, curRot, nextW);
                        placeTransom(pExtTopInX, pExtTopInY, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pExtTopInX, pExtTopInY, topZ, curRot, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1MaxX + 345 * curUx), MmToFt(p1MaxY + 345 * curUy), MmToFt(zDeck)), curRot + 90, "l", L_first);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pExtOutX - 345 * curUx), MmToFt(pExtOutY - 345 * curUy), MmToFt(zDeck)), curRot + 90, "l", L_first);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        curOutX = pExtTopOutX;
                        curOutY = pExtTopOutY;
                        curInX = pExtTopInX;
                        curInY = pExtTopInY;
                        curUx = curVx; curUy = curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot + 90.0;
                        curW = nextW;
                    }
                    else if (cCase == "TH 2.2")
                    {
                        double p1MaxX = p1StartX + L_last * curUx;
                        double p1MaxY = p1StartY + L_last * curUy;
                        double p2MaxX = p2StartX + L_last * curUx;
                        double p2MaxY = p2StartY + L_last * curUy;

                        ensureCol(p1MaxX, p1MaxY);
                        ensureCol(p2MaxX, p2MaxY);

                        var bbOut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(z0)), curRot, "l", L_last);
                        var lgFOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p1StartX), MmToFt(p1StartY), MmToFt(zH1)), curRot, "l", L_last);
                        if (bbOut != null) placedIds.Add(bbOut.Id.IntegerValue);
                        if (lgFOut != null) placedIds.Add(lgFOut.Id.IntegerValue);
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, p1StartX, p1StartY, curRot, L_last, z0, jackH, opts);

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

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p1StartX + 345 * curVx), MmToFt(p1StartY + 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2StartX - 345 * curVx), MmToFt(p2StartY - 345 * curVy), MmToFt(zDeck)), curRot, "l", L_last);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        double L_first = (rowNext != null && rowNext.BayLengths != null && rowNext.BayLengths.Count > 0) ? rowNext.BayLengths[0] : 1800.0;
                        double pExtTopInX = p2MaxX + nextW * curUx;
                        double pExtTopInY = p2MaxY + nextW * curUy;
                        double pExtOutEndX = p2MaxX + L_first * (-curVx);
                        double pExtOutEndY = p2MaxY + L_first * (-curVy);
                        double pExtTopOutX = pExtTopInX + L_first * (-curVx);
                        double pExtTopOutY = pExtTopInY + L_first * (-curVy);

                        ensureCol(pExtTopInX, pExtTopInY);
                        ensureCol(pExtTopOutX, pExtTopOutY);
                        ensureCol(pExtOutEndX, pExtOutEndY);

                        placeTransom(p2MaxX, p2MaxY, zH1, curRot, nextW);
                        placeTransom(p2MaxX, p2MaxY, zDeck, curRot, nextW);
                        var rF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH6)), curRot, "l", nextW);
                        var rF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(p2MaxX), MmToFt(p2MaxY), MmToFt(zH7)), curRot, "l", nextW);
                        if (rF1 != null) placedIds.Add(rF1.Id.IntegerValue);
                        if (rF2 != null) placedIds.Add(rF2.Id.IntegerValue);

                        var bbExtT = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(z0)), curRot - 90, "l", L_first);
                        var lgFExtT = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH1)), curRot - 90, "l", L_first);
                        var lgMExtT = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH3)), curRot - 90, "l", L_first);
                        var rExtT1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH6)), curRot - 90, "l", L_first);
                        var rExtT2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pExtTopInX), MmToFt(pExtTopInY), MmToFt(zH7)), curRot - 90, "l", L_first);
                        if (bbExtT != null) placedIds.Add(bbExtT.Id.IntegerValue);
                        if (lgFExtT != null) placedIds.Add(lgFExtT.Id.IntegerValue);
                        if (lgMExtT != null) placedIds.Add(lgMExtT.Id.IntegerValue);
                        if (rExtT1 != null) placedIds.Add(rExtT1.Id.IntegerValue);
                        if (rExtT2 != null) placedIds.Add(rExtT2.Id.IntegerValue);

                        double L_in = Math.Max(0, L_first - curW);
                        if (L_in > 0)
                        {
                            double pInStartX = p1MaxX;
                            double pInStartY = p1MaxY;
                            var bbInS = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(pInStartX), MmToFt(pInStartY), MmToFt(z0)), curRot - 90, "l", L_in);
                            var lgFInS = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(pInStartX), MmToFt(pInStartY), MmToFt(zH1)), curRot - 90, "l", L_in);
                            if (bbInS != null) placedIds.Add(bbInS.Id.IntegerValue);
                            if (lgFInS != null) placedIds.Add(lgFInS.Id.IntegerValue);
                            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, pInStartX, pInStartY, curRot - 90, L_in, z0, jackH, opts);
                        }

                        placeTransom(pExtOutEndX, pExtOutEndY, zH1, curRot, nextW);
                        placeTransom(pExtOutEndX, pExtOutEndY, zDeck, curRot, nextW);
                        if (opts != null && opts.HasTopTransom) placeTransom(pExtOutEndX, pExtOutEndY, topZ, curRot, nextW);

                        if (sym6 != null)
                        {
                            var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(p2MaxX - 345 * curUx), MmToFt(p2MaxY - 345 * curUy), MmToFt(zDeck)), curRot - 90, "l", L_first);
                            var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pExtTopInX + 345 * curUx), MmToFt(pExtTopInY + 345 * curUy), MmToFt(zDeck)), curRot - 90, "l", L_first);
                            if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                            if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                        }

                        curOutX = pExtTopOutX;
                        curOutY = pExtTopOutY;
                        curInX = pExtOutEndX;
                        curInY = pExtOutEndY;
                        curUx = -curVx; curUy = -curVy;
                        curVx = -curUy; curVy = curUx;
                        curRot = curRot - 90.0;
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

        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool openStart, bool hasCornerConnect)
        {
            var bayLens = (row.BayLengths != null && row.BayLengths.Count > 0)
                ? row.BayLengths
                : new List<double> { 1800 };

            double W = row.Width > 0 ? row.Width : (opts.BayWidth > 0 ? opts.BayWidth : 1200.0);
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double angleRad = row.AngleDeg * Math.PI / 180.0;
            double ux = Math.Cos(angleRad), uy = Math.Sin(angleRad);
            double vx = Math.Sin(angleRad), vy = -Math.Cos(angleRad);

            double startX = row.StartX;
            double startY = row.StartY;

            List<double> distances = new List<double> { 0.0 };
            double curDist = 0.0;
            foreach (var bLen in bayLens)
            {
                curDist += bLen;
                distances.Add(curDist);
            }

            int nBaysTotal = bayLens.Count;
            // Nếu có corner connect ở cuối, nhịp cuối sẽ do hàm Connect xử lý
            int nStandardBays = hasCornerConnect ? Math.Max(0, nBaysTotal - 1) : nBaysTotal;
            int nColsToBuild = nStandardBays + 1;

            // 1. Dựng cột và giằng ngang cho các nhịp tiêu chuẩn
            for (int k = 0; k < nColsToBuild; k++)
            {
                double d = distances[k];
                double cx1 = startX + d * ux;
                double cy1 = startY + d * uy;
                double cx2 = cx1 + W * vx;
                double cy2 = cy1 + W * vy;

                ensureCol(cx1, cy1);
                ensureCol(cx2, cy2);

                double zH1 = z0 + jackH + 135;
                placeTransom(cx1, cy1, zH1, row.AngleDeg - 90, W);

                if (opts != null && opts.HasTopTransom)
                {
                    placeTransom(cx1, cy1, topZ, row.AngleDeg - 90, W);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sDeck = z0 + jackH + 1730 + s * 1800.0;
                    placeTransom(cx1, cy1, sDeck, row.AngleDeg - 90, W);
                }

                // Lan can đầu hồi: Chỉ đặt ở cột đầu tiên nếu không bị mở nối góc
                if (k == 0 && !openStart)
                {
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double sH6 = z0 + jackH + 2180 + sOff;
                        double sH7 = z0 + jackH + 2630 + sOff;
                        double sDeck = z0 + jackH + 1730 + sOff;

                        var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sH6)), row.AngleDeg - 90, "l", W);
                        var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sH7)), row.AngleDeg - 90, "l", W);
                        if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);
                        if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                        if (sym7 != null)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sDeck)), row.AngleDeg - 90, "l", W);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }
                }

                // Lan can đuôi: Nếu không có corner connect ở cuối và là cột cuối cùng
                if (!hasCornerConnect && k == nColsToBuild - 1)
                {
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double sH6 = z0 + jackH + 2180 + sOff;
                        double sH7 = z0 + jackH + 2630 + sOff;
                        double sDeck = z0 + jackH + 1730 + sOff;

                        var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sH6)), row.AngleDeg - 90, "l", W);
                        var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sH7)), row.AngleDeg - 90, "l", W);
                        if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);
                        if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                        if (sym7 != null)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sDeck)), row.AngleDeg - 90, "l", W);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }
                }
            }

            // 2. Dựng cấu kiện dọc cho các nhịp tiêu chuẩn
            for (int b = 0; b < nStandardBays; b++)
            {
                double dStart = distances[b];
                double bLen = bayLens[b];

                double px1 = startX + dStart * ux;
                double py1 = startY + dStart * uy;
                double px2 = px1 + W * vx;
                double py2 = py1 + W * vy;

                var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(z0)), row.AngleDeg, "l", bLen);
                var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(z0)), row.AngleDeg, "l", bLen);
                if (bb1 != null) placedIds.Add(bb1.Id.IntegerValue);
                if (bb2 != null) placedIds.Add(bb2.Id.IntegerValue);

                double zH1 = z0 + jackH + 135;
                var lgFoot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH1)), row.AngleDeg, "l", bLen);
                var lgFoot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(zH1)), row.AngleDeg, "l", bLen);
                if (lgFoot1 != null) placedIds.Add(lgFoot1.Id.IntegerValue);
                if (lgFoot2 != null) placedIds.Add(lgFoot2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(topZ)), row.AngleDeg, "l", bLen);
                    var lgTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(topZ)), row.AngleDeg, "l", bLen);
                    if (lgTop1 != null) placedIds.Add(lgTop1.Id.IntegerValue);
                    if (lgTop2 != null) placedIds.Add(lgTop2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH3)), row.AngleDeg, "l", bLen);
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH6)), row.AngleDeg, "l", bLen);
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH7)), row.AngleDeg, "l", bLen);
                    if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px2, py2, row.AngleDeg, bLen, z0, jackH, s, storyCount, opts);

                    if (sym6 != null)
                    {
                        double pDeck1_x = px1 + 345 * vx;
                        double pDeck1_y = py1 + 345 * vy;
                        double pDeck2_x = px2 - 345 * vx;
                        double pDeck2_y = py2 - 345 * vy;

                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pDeck1_x), MmToFt(pDeck1_y), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pDeck2_x), MmToFt(pDeck2_y), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px1 + 66 * vx), MmToFt(py1 + 66 * vy), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px2 - 66 * vx), MmToFt(py2 - 66 * vy), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                    }
                }
            }
        }

        /// <summary>
        /// XỬ LÝ KHỚP NỐI CORNER CONNECT GIỮA ROW TRƯỚC VÀ ROW SAU:
        /// Áp dụng 100% danh mục cấu kiện chuẩn ALINCO từ Cases1.cs và Cases2.cs.
        /// isStandalone = true: Dựng cả nhánh rẽ mẫu (cho test 1 row đơn lẻ).
        /// isStandalone = false: Chỉ xử lý góc nối & mở thông cửa sang rowNext, không dựng trùng lặp rowNext.
        /// </summary>
        private static void BuildTableCornerConnect(Document doc, BayRowModel rowPrev, BayRowModel rowNext, string connectCase,
            BuildOptions opts, Level level, List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            switch (connectCase)
            {
                case "TH 1.1":
                    ConnectCorner_TH1_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                case "TH 1.2":
                    ConnectCorner_TH1_2(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                case "TH 1.3":
                    ConnectCorner_TH1_3(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                case "TH 1.4":
                    ConnectCorner_TH1_4(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                case "TH 2.1":
                    ConnectCorner_TH2_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                case "TH 2.2":
                    ConnectCorner_TH2_2(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
                default:
                    ConnectCorner_TH1_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, placeTransom, sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CÁC FUNCTION KHỚP NỐI GÓC - 100% CẤU KIỆN CHUẨN TỪ CASES1 VÀ CASES2
        //  ÁP DỤNG CÔNG THỨC KÍCH THƯỚC ĐỘNG: L_last, L_first, W_A, W_B, L_cut, L_step
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TH 1.1: Khớp nối góc 90° hướng +Y (Mép ngoài phẳng).
        /// Kế thừa 100% cấu kiện ExecuteCase1_1: Cấy cột phụ tại X_C, giằng mặt trước thu ngắn L_cut = L_last - W_B.
        /// </summary>
        private static void ConnectCorner_TH1_1(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_cut = (L_last > W_B) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            // Điểm bắt đầu của nhịp cuối dãy A
            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;

            // Tọa độ cột cấy xC và cột ngoài xMax
            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            if (L_cut > 0)
            {
                ensureCol(xC, yC);
            }
            ensureCol(xMax, yMax);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            // 1. NHỊP CUỐI DÃY A - MẶT SAU (offset W_A): Đầy đủ nhịp L_last
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;
            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_last, z0, jackH, s, storyCount, opts);

                double zDeck = z0 + jackH + 1730 + sOff;
                if (sym7 != null)
                {
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + 345 * vAx), MmToFt(yStartLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - 345 * vAx), MmToFt(yBack - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. NHỊP CUỐI DÃY A - MẶT TRƯỚC: Thu ngắn về L_cut (nếu L_cut > 0)
            if (L_cut > 0)
            {
                var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
                var lgFFron = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
                if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);
                if (lgFFron != null) placedIds.Add(lgFFron.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopF = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                    if (lgTopF != null) placedIds.Add(lgTopF.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;

                    var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_cut);
                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_cut);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_cut);
                    if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xStartLast + 66 * vAx), MmToFt(yStartLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                        if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    }
                }
            }

            // 3. CỬA NGÕ MỞ THÔNG SANG ROW B & SHARED TRANSOM TẠI CỬA NGÕ (dọc Row A)
            double rotReturn = rowA.AngleDeg;
            placeTransom(xC, yC, zH1, rotReturn, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xC, yC, topZ, rotReturn, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xC, yC, sDeck, rotReturn, W_B);
            }

            // Đóng kín đầu hồi cuối dãy A (tại xMax across W_A)
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                placeTransom(xMax, yMax, zDeck, rowA.AngleDeg - 90, W_A);

                var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rowA.AngleDeg - 90, "l", W_A);
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rowA.AngleDeg - 90, "l", W_A);
                if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zDeck)), rowA.AngleDeg - 90, "l", W_A);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }
            }

            // 4. NẾU LÀ CHẾ ĐỘ STANDALONE (TEST 1 ROW): DỰNG THÊM 1 NHỊP RẼ MẪU CHO ROW B
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA + Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg + 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xTopC = xC + L_first * uBx;
                double yTopC = yC + L_first * uBy;
                double xTopMax = xMax + L_first * uBx;
                double yTopMax = yMax + L_first * uBy;

                ensureCol(xTopC, yTopC);
                ensureCol(xTopMax, yTopMax);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH3)), rotDegB, "l", L_first);
                    if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, yMax, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + 345 * vBx), MmToFt(yC + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345 * vBx), MmToFt(yMax - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66 * vBx), MmToFt(yC + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66 * vBx), MmToFt(yMax - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 1.2: Khớp nối góc 90° - Ô vuông W_B x W_B (hoặc L_last x W_B).
        /// Kế thừa 100% cấu kiện ExecuteCase1_2 và hỗ trợ kích thước động L_cut = max(0, L_last - W_B).
        /// </summary>
        private static void ConnectCorner_TH1_2(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1200.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1200.0;
            double L_cut = (L_last > W_B) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            if (L_cut > 0)
            {
                ensureCol(xC, yC);
            }
            ensureCol(xMax, yMax);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            // 1. Mặt sau nhịp cuối Row A (offset W_A)
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;
            var bbLastBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbLastBack != null) placedIds.Add(bbLastBack.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFLastBack != null) placedIds.Add(lgFLastBack.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopLastBack != null) placedIds.Add(lgTopLastBack.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_last, z0, jackH, s, storyCount, opts);

                double zDeck = z0 + jackH + 1730 + sOff;
                if (sym7 != null)
                {
                    var tbLastBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbLastBack != null) placedIds.Add(tbLastBack.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + 345 * vAx), MmToFt(yStartLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - 345 * vAx), MmToFt(yBack - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. Mặt trước nhịp cuối Row A: Thu ngắn về L_cut (nếu L_cut > 0)
            if (L_cut > 0)
            {
                var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
                if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

                var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
                if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                    if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_cut);
                    var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_cut);
                    var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_cut);
                    if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                    if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                    if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xStartLast + 66 * vAx), MmToFt(yStartLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                        if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    }
                }
            }

            // 3. Shared Transom tại cửa ngõ rẽ sang Row B (từ xC đến xMax dọc rowA.AngleDeg)
            placeTransom(xC, yC, zH1, rowA.AngleDeg, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xC, yC, topZ, rowA.AngleDeg, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xC, yC, sDeck, rowA.AngleDeg, W_B);
            }

            // 4. Đóng kín đầu hồi cuối dãy A (tại xMax across W_A)
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                placeTransom(xMax, yMax, zDeck, rowA.AngleDeg - 90, W_A);

                var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rowA.AngleDeg - 90, "l", W_A);
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rowA.AngleDeg - 90, "l", W_A);
                if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zDeck)), rowA.AngleDeg - 90, "l", W_A);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }
            }

            // 5. Standalone return branch
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA + Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg + 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xTop1 = xC + L_first * uBx;
                double yTop1 = yC + L_first * uBy;
                double xTop2 = xMax + L_first * uBx;
                double yTop2 = yMax + L_first * uBy;

                ensureCol(xTop1, yTop1);
                ensureCol(xTop2, yTop2);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, yMax, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + 345 * vBx), MmToFt(yC + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345 * vBx), MmToFt(yMax - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66 * vBx), MmToFt(yC + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66 * vBx), MmToFt(yMax - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 1.3: Khớp nối góc 90° hướng -Y (Giằng thu ngắn phía đối diện).
        /// Kế thừa 100% cấu kiện ExecuteCase1_3.
        /// </summary>
        private static void ConnectCorner_TH1_3(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_cut = (L_last > W_B) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xMax, yMax);
            if (L_cut > 0)
            {
                ensureCol(xC + W_A * vAx, yC + W_A * vAy);
            }
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            // 1. MẶT TRƯỚC (Y = 0): Đầy đủ nhịp L_last
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_last);
                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_last);
                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_last);
                if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xStartLast + 66 * vAx), MmToFt(yStartLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    double xBack_s = xStartLast + W_A * vAx;
                    double yBack_s = yStartLast + W_A * vAy;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + 345 * vAx), MmToFt(yStartLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack_s - 345 * vAx), MmToFt(yBack_s - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. MẶT SAU (offset W_A): Thu ngắn về L_cut (nếu L_cut > 0)
            if (L_cut > 0)
            {
                double xBack = xStartLast + W_A * vAx;
                double yBack = yStartLast + W_A * vAy;
                var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
                var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
                if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
                if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                    if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                    }
                }
            }

            // 3. CỬA NGÕ MỞ THÔNG SANG ROW B & SHARED TRANSOM TẠI CỬA NGÕ (dọc Row A)
            double xC_back = xC + W_A * vAx;
            double yC_back = yC + W_A * vAy;
            double rotReturn = rowA.AngleDeg;
            placeTransom(xC_back, yC_back, zH1, rotReturn, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xC_back, yC_back, topZ, rotReturn, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xC_back, yC_back, sDeck, rotReturn, W_B);
            }

            // Đóng kín đầu hồi cuối dãy A (tại xMax across W_A)
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                placeTransom(xMax, yMax, zDeck, rowA.AngleDeg - 90, W_A);

                var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rowA.AngleDeg - 90, "l", W_A);
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rowA.AngleDeg - 90, "l", W_A);
                if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zDeck)), rowA.AngleDeg - 90, "l", W_A);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }
            }

            // Standalone return branch
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA - Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg - 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xMax_back = xMax + W_A * vAx;
                double yMax_back = yMax + W_A * vAy;
                double xTopC = xC_back + L_first * uBx;
                double yTopC = yC_back + L_first * uBy;
                double xTopMax = xMax_back + L_first * uBx;
                double yTopMax = yMax_back + L_first * uBy;

                ensureCol(xTopC, yTopC);
                ensureCol(xTopMax, yTopMax);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH3)), rotDegB, "l", L_first);
                    if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax_back, yMax_back, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC_back + 345 * vBx), MmToFt(yC_back + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax_back - 345 * vBx), MmToFt(yMax_back - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC_back + 66 * vBx), MmToFt(yC_back + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax_back - 66 * vBx), MmToFt(yMax_back - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 1.4: Khớp nối góc 90° hướng -Y (Ô vuông W_B x W_B hoặc L_last x W_B).
        /// Kế thừa 100% cấu kiện ExecuteCase1_4 và hỗ trợ kích thước động L_cut = max(0, L_last - W_B).
        /// </summary>
        private static void ConnectCorner_TH1_4(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1200.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1200.0;
            double L_cut = (L_last > W_B) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            double xC_back = xC + W_A * vAx;
            double yC_back = yC + W_A * vAy;
            double xMax_back = xMax + W_A * vAx;
            double yMax_back = yMax + W_A * vAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xMax, yMax);
            if (L_cut > 0)
            {
                ensureCol(xC_back, yC_back);
            }
            ensureCol(xMax_back, yMax_back);

            // 1. Mặt trước nhịp cuối Row A (đầy đủ nhịp L_last)
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_last);
                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_last);
                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_last);
                if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xStartLast + 66 * vAx), MmToFt(yStartLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                }

                if (sym6 != null)
                {
                    double xBack_s = xStartLast + W_A * vAx;
                    double yBack_s = yStartLast + W_A * vAy;
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + 345 * vAx), MmToFt(yStartLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack_s - 345 * vAx), MmToFt(yBack_s - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. Mặt sau nhịp cuối Row A: Thu ngắn về L_cut (nếu L_cut > 0)
            if (L_cut > 0)
            {
                double xBack = xStartLast + W_A * vAx;
                double yBack = yStartLast + W_A * vAy;
                var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
                var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
                if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
                if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                    if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_cut, z0, jackH, s, storyCount, opts);

                    if (sym7 != null)
                    {
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                    }
                }
            }

            // 3. Shared Transom tại cửa ngõ rẽ sang Row B (mặt sau dọc Row A)
            placeTransom(xC_back, yC_back, zH1, rowA.AngleDeg, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xC_back, yC_back, topZ, rowA.AngleDeg, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xC_back, yC_back, sDeck, rowA.AngleDeg, W_B);
            }

            // 4. Đóng kín đầu hồi cuối dãy A (tại xMax across W_A)
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                placeTransom(xMax, yMax, zDeck, rowA.AngleDeg - 90, W_A);

                var rEnd1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rowA.AngleDeg - 90, "l", W_A);
                var rEnd2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rowA.AngleDeg - 90, "l", W_A);
                if (rEnd1 != null) placedIds.Add(rEnd1.Id.IntegerValue);
                if (rEnd2 != null) placedIds.Add(rEnd2.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zDeck)), rowA.AngleDeg - 90, "l", W_A);
                    if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                }
            }

            // 5. Standalone return branch
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA - Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg - 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xTop1 = xC_back + L_first * uBx;
                double yTop1 = yC_back + L_first * uBy;
                double xTop2 = xMax_back + L_first * uBx;
                double yTop2 = yMax_back + L_first * uBy;

                ensureCol(xTop1, yTop1);
                ensureCol(xTop2, yTop2);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax_back, yMax_back, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC_back + 345 * vBx), MmToFt(yC_back + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax_back - 345 * vBx), MmToFt(yMax_back - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC_back + 66 * vBx), MmToFt(yC_back + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax_back - 66 * vBx), MmToFt(yMax_back - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 2.1: Khớp nối góc 90° - Vươn ra ngoài (Outer Protrusion +Y).
        /// Kế thừa 100% cấu kiện ExecuteCase2_1.
        /// </summary>
        private static void ConnectCorner_TH2_1(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_step = (L_first > W_A) ? (L_first - W_A) : 600.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xLast = rowA.StartX + dStartLast * uAx;
            double yLast = rowA.StartY + dStartLast * uAy;
            double xMax = xLast + L_last * uAx;
            double yMax = yLast + L_last * uAy;

            ensureCol(xLast, yLast);
            ensureCol(xMax, yMax);
            ensureCol(xLast + W_A * vAx, yLast + W_A * vAy);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            // Dãy A nhịp cuối: Mặt trước và mặt sau
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

            double xBack = xLast + W_A * vAx;
            double yBack = yLast + W_A * vAy;
            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
            if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
                if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_last);
                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_last);
                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_last);
                if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_last, z0, jackH, s, storyCount, opts);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + 345 * vAx), MmToFt(yLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - 345 * vAx), MmToFt(yBack - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66 * vAx), MmToFt(yLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }
            }

            // Shared Transom tại cửa ngõ vươn sang Row B
            double rotReturn = (rowB != null) ? (rowB.AngleDeg - 90) : (rowA.AngleDeg);
            placeTransom(xMax, yMax, zH1, rotReturn, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax, yMax, topZ, rotReturn, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xMax, yMax, sDeck, rotReturn, W_B);
            }

            // Standalone return branch
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA + Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg + 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xTop1 = xMax + L_first * uBx;
                double yTop1 = yMax + L_first * uBy;
                double x2 = xMax + W_B * vBx;
                double y2 = yMax + W_B * vBy;
                double xTop2 = x2 + L_first * uBx;
                double yTop2 = y2 + L_first * uBy;

                ensureCol(x2, y2);
                ensureCol(xTop1, yTop1);
                ensureCol(xTop2, yTop2);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, x2, y2, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax + 345 * vBx), MmToFt(yMax + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x2 - 345 * vBx), MmToFt(y2 - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax + 66 * vBx), MmToFt(yMax + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x2 - 66 * vBx), MmToFt(y2 - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 2.2: Khớp nối góc 90° - Vươn ra ngoài hướng ngược lại (Outer Protrusion -Y).
        /// Kế thừa 100% cấu kiện ExecuteCase2_2.
        /// </summary>
        private static void ConnectCorner_TH2_2(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool isStandalone)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = (rowB != null && rowB.Width > 0) ? rowB.Width : (rowA.Width > 0 ? rowA.Width : 1200.0);
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xLast = rowA.StartX + dStartLast * uAx;
            double yLast = rowA.StartY + dStartLast * uAy;
            double xMax = xLast + L_last * uAx;
            double yMax = yLast + L_last * uAy;

            ensureCol(xLast, yLast);
            ensureCol(xMax, yMax);
            ensureCol(xLast + W_A * vAx, yLast + W_A * vAy);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            // Dãy A nhịp cuối
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            double zH1 = z0 + jackH + 135;
            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

            double xBack = xLast + W_A * vAx;
            double yBack = yLast + W_A * vAy;
            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            var lgFBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);
            if (lgFBack != null) placedIds.Add(lgFBack.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgTopFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                var lgTopBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(topZ)), rowA.AngleDeg, "l", L_last);
                if (lgTopFront != null) placedIds.Add(lgTopFront.Id.IntegerValue);
                if (lgTopBack != null) placedIds.Add(lgTopBack.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH3)), rowA.AngleDeg, "l", L_last);
                var r1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH6)), rowA.AngleDeg, "l", L_last);
                var r2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH7)), rowA.AngleDeg, "l", L_last);
                if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);
                if (r1 != null) placedIds.Add(r1.Id.IntegerValue);
                if (r2 != null) placedIds.Add(r2.Id.IntegerValue);

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xBack, yBack, rowA.AngleDeg, L_last, z0, jackH, s, storyCount, opts);

                if (sym6 != null)
                {
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + 345 * vAx), MmToFt(yLast + 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - 345 * vAx), MmToFt(yBack - 345 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66 * vAx), MmToFt(yLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }
            }

            // Shared Transom tại cửa ngõ vươn sang Row B
            double xMax_back = xMax + W_A * vAx;
            double yMax_back = yMax + W_A * vAy;
            double rotReturn = (rowB != null) ? (rowB.AngleDeg - 90) : (rowA.AngleDeg);
            placeTransom(xMax_back, yMax_back, zH1, rotReturn, W_B);

            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xMax_back, yMax_back, topZ, rotReturn, W_B);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xMax_back, yMax_back, sDeck, rotReturn, W_B);
            }

            // Standalone return branch
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA - Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg - 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);

                double xTop1 = xMax_back + L_first * uBx;
                double yTop1 = yMax_back + L_first * uBy;
                double x2 = xMax_back + W_B * vBx;
                double y2 = yMax_back + W_B * vBy;
                double xTop2 = x2 + L_first * uBx;
                double yTop2 = y2 + L_first * uBy;

                ensureCol(x2, y2);
                ensureCol(xTop1, yTop1);
                ensureCol(xTop2, yTop2);

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(z0)), rotDegB, "l", L_first);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(z0)), rotDegB, "l", L_first);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH1)), rotDegB, "l", L_first);
                var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(zH1)), rotDegB, "l", L_first);
                var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH1)), rotDegB - 90, "l", W_B);
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(x2), MmToFt(y2), MmToFt(topZ)), rotDegB, "l", L_first);
                    var trTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(topZ)), rotDegB - 90, "l", W_B);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    if (trTopRF2 != null) placedIds.Add(trTopRF2.Id.IntegerValue);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, x2, y2, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                    if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax_back + 345 * vBx), MmToFt(yMax_back + 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(x2 - 345 * vBx), MmToFt(y2 - 345 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax_back + 66 * vBx), MmToFt(yMax_back + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(x2 - 66 * vBx), MmToFt(y2 - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH7)), rotDegB, "l", L_first);
                    var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
                    if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                }
            }
        }
    }
}
