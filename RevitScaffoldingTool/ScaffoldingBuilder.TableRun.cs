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

            // KIỂM TRA VÒNG KHÉP KÍN (CLOSED LOOP):
            bool isClosedLoop = false;
            var rLast = rows[nRows - 1];
            var rFirst = rows[0];
            double dEndToStart = Math.Sqrt(Math.Pow(rLast.EndX - rFirst.StartX, 2) + Math.Pow(rLast.EndY - rFirst.StartY, 2));
            isClosedLoop = (nRows >= 3 && dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rLast.ConnectCase) && rLast.ConnectCase != "None" && nRows >= 2);

            // THI CÔNG TOÀN BỘ CÁC ROW VÀ GÓC NỐI TRONG BẢNG TABLE (100% TỰ CUSTOM CHIỀU DÀI):
            for (int i = 0; i < nRows; i++)
            {
                var rowCur = rows[i];
                var rowNext = (i < nRows - 1) ? rows[i + 1] : (isClosedLoop ? rows[0] : null);
                bool hasNext = (rowNext != null) && (!string.IsNullOrEmpty(rowCur.ConnectCase) && rowCur.ConnectCase != "None");
                bool isFirstRow = (i == 0);
                bool hasSoloConnect = (nRows == 1 && !string.IsNullOrEmpty(rowCur.ConnectCase) && rowCur.ConnectCase != "None");

                // 1. Dựng các nhịp thẳng tiêu chuẩn của rowCur
                int startBay = isFirstRow ? 0 : 1;
                BuildTableStraightRun(doc, rowCur, opts, level, placedIds, ensureCol, placeTransom,
                    sym1, sym4, sym5, sym6, sym7,
                    openStart: !isFirstRow || isClosedLoop,
                    hasCornerConnect: hasNext || hasSoloConnect,
                    startBayIndex: startBay);

                // 2. Góc nối giữa nhịp cuối rowCur và nhịp đầu rowNext:
                if (hasNext)
                {
                    string cCase = !string.IsNullOrEmpty(rowCur.ConnectCase) ? rowCur.ConnectCase : "TH 1.1";
                    BuildTableCornerConnect(doc, rowCur, rowNext, cCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
                else if (hasSoloConnect)
                {
                    // Chế độ 1 hàng đơn lẻ có ConnectCase: Dựng nhịp cuối + nhịp rẽ mẫu
                    BuildTableCornerConnect(doc, rowCur, null, rowCur.ConnectCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
            }

            return null;
        }

        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool openStart, bool hasCornerConnect, int startBayIndex = 0)
        {
            var bayLens = (row.BayLengths != null && row.BayLengths.Count > 0)
                ? row.BayLengths
                : new List<double> { 1800 };

            double W = row.Width > 0 ? row.Width : (opts.BayWidth > 0 ? opts.BayWidth : 1200.0);
            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
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

            if (nStandardBays < startBayIndex && hasCornerConnect) return;

            // 1. Dựng cột và giằng ngang cho các nhịp tiêu chuẩn (bao gồm cột đầu k = 0)
            for (int k = startBayIndex; k < nColsToBuild; k++)
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

                // Lan can đầu hồi: Chỉ đặt ở cột đầu tiên nếu không bị mở nối góc và bắt đầu từ bay 0
                if (k == 0 && !openStart && startBayIndex == 0)
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
            for (int b = startBayIndex; b < nStandardBays; b++)
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
                        double pDeck1_x = px1 + pOff * vx;
                        double pDeck1_y = py1 + pOff * vy;
                        double pDeck2_x = px2 - pOff * vx;
                        double pDeck2_y = py2 - pOff * vy;

                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pDeck1_x), MmToFt(pDeck1_y), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(pDeck2_x), MmToFt(pDeck2_y), MmToFt(zDeck)), row.AngleDeg, "l", bLen);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.IntegerValue); }
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_cut = (L_last > W_B + 10.0) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xBack, yBack);
            if (L_cut > 10.0)
            {
                ensureCol(xC, yC);
            }
            ensureCol(xMax, yMax);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            double zH1 = z0 + jackH + 135;
            placeTransom(xStartLast, yStartLast, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xStartLast, yStartLast, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                placeTransom(xStartLast, yStartLast, z0 + jackH + 1730 + s * 1800.0, rowA.AngleDeg - 90, W_A);
            }

            // 1. NHỊP CUỐI DÃY A - MẶT SAU (offset W_A): Đầy đủ nhịp L_last
            var bbBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbBack != null) placedIds.Add(bbBack.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + pOffA * vAx), MmToFt(yStartLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. NHỊP CUỐI DÃY A - MẶT TRƯỚC: Thu ngắn về L_cut (nếu L_cut > 10)
            if (L_cut > 10.0)
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

            // 4. DỰNG NHỊP ĐẦU CỦA ROW B (NHÁNH RẼ +90°)
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA + Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg + 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                double vBx = Math.Sin(angRadB), vBy = -Math.Cos(angRadB);
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

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
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                placeTransom(xTopC, yTopC, zH1, rotDegB - 90, W_B);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_first);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    placeTransom(xTopC, yTopC, topZ, rotDegB - 90, W_B);
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

                    placeTransom(xTopC, yTopC, zDeck, rotDegB - 90, W_B);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOffB * vBx), MmToFt(yC + pOffB * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOffB * vBx), MmToFt(yMax - pOffB * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66 * vBx), MmToFt(yC + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66 * vBx), MmToFt(yMax - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTopC - 66 * uBx), MmToFt(yTopC - 66 * uBy), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH7)), rotDegB, "l", L_first);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);

                    if (closeReturnEnd)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                    }
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1200.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1200.0;
            double L_cut = (L_last > W_B + 10.0) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xBack, yBack);
            if (L_cut > 10.0)
            {
                ensureCol(xC, yC);
            }
            ensureCol(xMax, yMax);
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            double zH1 = z0 + jackH + 135;
            placeTransom(xStartLast, yStartLast, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xStartLast, yStartLast, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                placeTransom(xStartLast, yStartLast, z0 + jackH + 1730 + s * 1800.0, rowA.AngleDeg - 90, W_A);
            }

            // 1. Mặt sau nhịp cuối Row A (offset W_A)
            var bbLastBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBack), MmToFt(yBack), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbLastBack != null) placedIds.Add(bbLastBack.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + pOffA * vAx), MmToFt(yStartLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. Mặt trước nhịp cuối Row A: Thu ngắn về L_cut (nếu L_cut > 10)
            if (L_cut > 10.0)
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
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

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
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                placeTransom(xTop1, yTop1, zH1, rotDegB - 90, W_B);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_first);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    placeTransom(xTop1, yTop1, topZ, rotDegB - 90, W_B);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xMax, yMax, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    placeTransom(xTop1, yTop1, zDeck, rotDegB - 90, W_B);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + pOffB * vBx), MmToFt(yC + pOffB * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - pOffB * vBx), MmToFt(yMax - pOffB * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66 * vBx), MmToFt(yC + 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66 * vBx), MmToFt(yMax - 66 * vBy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1 - 66 * uBx), MmToFt(yTop1 - 66 * uBy), MmToFt(zDeck)), rotDegB - 90, "l", W_B);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH7)), rotDegB, "l", L_first);
                    if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
                    if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);

                    if (closeReturnEnd)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rotDegB - 90, "l", W_B);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rotDegB - 90, "l", W_B);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                    }
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_cut = (L_last > W_B + 10.0) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xBack, yBack);
            ensureCol(xMax, yMax);
            if (L_cut > 10.0)
            {
                ensureCol(xC + W_A * vAx, yC + W_A * vAy);
            }
            ensureCol(xMax + W_A * vAx, yMax + W_A * vAy);

            double zH1 = z0 + jackH + 135;
            placeTransom(xStartLast, yStartLast, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xStartLast, yStartLast, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                placeTransom(xStartLast, yStartLast, z0 + jackH + 1730 + s * 1800.0, rowA.AngleDeg - 90, W_A);
            }

            // 1. MẶT TRƯỚC (Y = 0): Đầy đủ nhịp L_last
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + pOffA * vAx), MmToFt(yStartLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. MẶT SAU (offset W_A): Thu ngắn về L_cut (nếu L_cut > 10)
            if (L_cut > 10.0)
            {
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

            // 4. DỰNG NHÁNH RẼ HƯỚNG -90° (Kế thừa chuẩn xác ExecuteCase1_3)
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA - Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg - 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

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
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                placeTransom(xTopC, yTopC, zH1, rowA.AngleDeg, W_B);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    placeTransom(xTopC, yTopC, topZ, rowA.AngleDeg, W_B);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    // Mép phải nhánh rẽ (xMax_back): Giằng giữa h3 + lan can kép h6/h7
                    var lgRM2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH3)), rotDegB, "l", L_first);
                    if (lgRM2 != null) placedIds.Add(lgRM2.Id.IntegerValue);

                    // Mép trái nhánh rẽ (xC_back): Giằng chéo Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC_back, yC_back, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    placeTransom(xTopC, yTopC, zDeck, rowA.AngleDeg, W_B);

                    // Vector ngang từ C_back sang Max_back là uA = (uAx, uAy)
                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC_back + pOffB * uAx), MmToFt(yC_back + pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax_back - pOffB * uAx), MmToFt(yMax_back - pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC_back + 66 * uAx), MmToFt(yC_back + 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax_back - 66 * uAx), MmToFt(yMax_back - 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTopC - 66 * uBx), MmToFt(yTopC - 66 * uBy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }

                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH7)), rotDegB, "l", L_first);
                    if (rR1 != null) placedIds.Add(rR1.Id.IntegerValue);
                    if (rR2 != null) placedIds.Add(rR2.Id.IntegerValue);

                    if (closeReturnEnd)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH6)), rowA.AngleDeg, "l", W_B);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTopC), MmToFt(yTopC), MmToFt(zH7)), rowA.AngleDeg, "l", W_B);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                    }
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1200.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1200.0;
            double L_cut = (L_last > W_B + 10.0) ? (L_last - W_B) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xStartLast = rowA.StartX + dStartLast * uAx;
            double yStartLast = rowA.StartY + dStartLast * uAy;
            double xBack = xStartLast + W_A * vAx;
            double yBack = yStartLast + W_A * vAy;

            double xC = xStartLast + L_cut * uAx;
            double yC = yStartLast + L_cut * uAy;
            double xMax = xStartLast + L_last * uAx;
            double yMax = yStartLast + L_last * uAy;

            double xC_back = xC + W_A * vAx;
            double yC_back = yC + W_A * vAy;
            double xMax_back = xMax + W_A * vAx;
            double yMax_back = yMax + W_A * vAy;

            ensureCol(xStartLast, yStartLast);
            ensureCol(xBack, yBack);
            ensureCol(xMax, yMax);
            if (L_cut > 10.0)
            {
                ensureCol(xC_back, yC_back);
            }
            ensureCol(xMax_back, yMax_back);

            double zH1 = z0 + jackH + 135;
            placeTransom(xStartLast, yStartLast, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xStartLast, yStartLast, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                placeTransom(xStartLast, yStartLast, z0 + jackH + 1730 + s * 1800.0, rowA.AngleDeg - 90, W_A);
            }

            // 1. Mặt trước nhịp cuối Row A (đầy đủ nhịp L_last)
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xStartLast), MmToFt(yStartLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xStartLast + pOffA * vAx), MmToFt(yStartLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }
            }

            // 2. Mặt sau nhịp cuối Row A: Thu ngắn về L_cut (nếu L_cut > 10)
            if (L_cut > 10.0)
            {
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

            // 5. Standalone return branch (Kế thừa chuẩn xác ExecuteCase1_4)
            if (isStandalone)
            {
                double angRadB = (rowB != null) ? (rowB.AngleDeg * Math.PI / 180.0) : (angRadA - Math.PI / 2.0);
                double rotDegB = (rowB != null) ? rowB.AngleDeg : (rowA.AngleDeg - 90.0);
                double uBx = Math.Cos(angRadB), uBy = Math.Sin(angRadB);
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

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
                if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
                if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);
                placeTransom(xTop1, yTop1, zH1, rowA.AngleDeg, W_B);

                if (opts != null && opts.HasTopTransom)
                {
                    var lgTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC_back), MmToFt(yC_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    var lgTopRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    if (lgTopRF1 != null) placedIds.Add(lgTopRF1.Id.IntegerValue);
                    if (lgTopRF2 != null) placedIds.Add(lgTopRF2.Id.IntegerValue);
                    placeTransom(xTop1, yTop1, topZ, rowA.AngleDeg, W_B);
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    // Mép trái nhánh rẽ (xC_back): Giằng chéo Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xC_back, yC_back, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    placeTransom(xTop1, yTop1, zDeck, rowA.AngleDeg, W_B);

                    // Vector ngang từ C_back sang Max_back là uA = (uAx, uAy)
                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC_back + pOffB * uAx), MmToFt(yC_back + pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax_back - pOffB * uAx), MmToFt(yMax_back - pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC_back + 66 * uAx), MmToFt(yC_back + 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax_back - 66 * uAx), MmToFt(yMax_back - 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                        if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xTop1 - 66 * uBx), MmToFt(yTop1 - 66 * uBy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                        }
                    }

                    // Mép phải nhánh rẽ (xMax_back): Lan can kép h6/h7
                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH6)), rotDegB, "l", L_first);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax_back), MmToFt(yMax_back), MmToFt(zH7)), rotDegB, "l", L_first);
                    if (rR1 != null) placedIds.Add(rR1.Id.IntegerValue);
                    if (rR2 != null) placedIds.Add(rR2.Id.IntegerValue);

                    if (closeReturnEnd)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH6)), rowA.AngleDeg, "l", W_B);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xTop1), MmToFt(yTop1), MmToFt(zH7)), rowA.AngleDeg, "l", W_B);
                        if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
                        if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);
                    }
                }
            }
        }

        /// <summary>
        /// TH 2.1: Khớp nối góc 90° - Vươn ra ngoài (Outer Protrusion +Y, Mép sau phẳng).
        /// Kế thừa 100% cấu kiện ExecuteCase2_1 với mọi kích thước (L_last, L_first, W_A, W_B).
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_A = (L_first > W_A + 10.0) ? (L_first - W_A) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xLast = rowA.StartX + dStartLast * uAx;
            double yLast = rowA.StartY + dStartLast * uAy;
            double xMax = xLast + L_last * uAx;
            double yMax = yLast + L_last * uAy;
            double xBack = xLast + W_A * vAx;
            double yBack = yLast + W_A * vAy;
            double xA_back = xMax + W_A * vAx;
            double yA_back = yMax + W_A * vAy;

            ensureCol(xLast, yLast);
            ensureCol(xMax, yMax);
            ensureCol(xBack, yBack);
            ensureCol(xA_back, yA_back);

            double zH1 = z0 + jackH + 135;
            placeTransom(xLast, yLast, zH1, rowA.AngleDeg - 90, W_A);
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xLast, yLast, topZ, rowA.AngleDeg - 90, W_A);
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xLast, yLast, sDeck, rowA.AngleDeg - 90, W_A);
                placeTransom(xMax, yMax, sDeck, rowA.AngleDeg - 90, W_A);
            }

            // 1. Dãy A nhịp cuối: Mặt trước và mặt sau đầy đủ L_last
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + pOffA * vAx), MmToFt(yLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66 * vAx), MmToFt(yLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }
            }

            // 2. PHẦN B: NHÁNH RẼ NGOÀI TH 2.1 (MÉP SAU PHẲNG: L_first x W_B)
            if (isStandalone)
            {
                double rotDegB = rowA.AngleDeg + 90.0;
                double uBx = -vAx, uBy = -vAy; // Hướng +90° (từ yBack lên yTop)
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

                double xB_back = xA_back + W_B * uAx;
                double yB_back = yA_back + W_B * uAy;
                double xA_top = xA_back + L_first * uBx;
                double yA_top = yA_back + L_first * uBy;
                double xB_top = xB_back + L_first * uBx;
                double yB_top = yB_back + L_first * uBy;

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA_back), MmToFt(yA_back), MmToFt(z0)), rowA.AngleDeg, "l", W_B);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA_top),  MmToFt(yA_top),  MmToFt(z0)), rowA.AngleDeg, "l", W_B);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                ensureCol(xB_back, yB_back);
                ensureCol(xB_top, yB_top);
                if (L_A > 10.0)
                {
                    ensureCol(xA_top, yA_top);
                }

                placeTransom(xA_back, yA_back, zH1, rowA.AngleDeg, W_B);
                placeTransom(xA_top,  yA_top,  zH1, rowA.AngleDeg, W_B);
                var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_back), MmToFt(yB_back), MmToFt(zH1)), rotDegB, "l", L_first);
                if (lgRF_B != null) placedIds.Add(lgRF_B.Id.IntegerValue);
                if (L_A > 10.0)
                {
                    var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH1)), rotDegB, "l", L_A);
                    if (lgRF_A != null) placedIds.Add(lgRF_A.Id.IntegerValue);
                }

                if (opts != null && opts.HasTopTransom)
                {
                    placeTransom(xA_back, yA_back, topZ, rowA.AngleDeg, W_B);
                    placeTransom(xA_top,  yA_top,  topZ, rowA.AngleDeg, W_B);
                    var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_back), MmToFt(yB_back), MmToFt(topZ)), rotDegB, "l", L_first);
                    if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.IntegerValue);
                    if (L_A > 10.0)
                    {
                        var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(topZ)), rotDegB, "l", L_A);
                        if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.IntegerValue);
                    }
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    if (L_A > 10.0)
                    {
                        var lgRM_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH3)), rotDegB, "l", L_A);
                        if (lgRM_A != null) placedIds.Add(lgRM_A.Id.IntegerValue);
                    }

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA_back, yA_back, rowA.AngleDeg, W_B, z0, jackH, s, storyCount, opts);
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB_back, yB_back, rotDegB, L_first, z0, jackH, s, storyCount, opts);

                    placeTransom(xA_back, yA_back, zDeck, rowA.AngleDeg, W_B);
                    placeTransom(xA_top,  yA_top,  zDeck, rowA.AngleDeg, W_B);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA_back + pOffB * uAx), MmToFt(yA_back + pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB_back - pOffB * uAx), MmToFt(yB_back - pOffB * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA_back + 66 * uBx), MmToFt(yA_back + 66 * uBy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                        var tbEndB = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB_back - 66 * uAx), MmToFt(yB_back - 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_first);
                        if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                        if (tbEndB != null) placedIds.Add(tbEndB.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbTop = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA_top - 66 * uBx), MmToFt(yA_top - 66 * uBy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                            if (tbTop != null) placedIds.Add(tbTop.Id.IntegerValue);
                        }

                        if (L_A > 10.0)
                        {
                            var tbInnerA = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax + 66 * uAx), MmToFt(yMax + 66 * uAy), MmToFt(zDeck)), rotDegB, "l", L_A);
                            if (tbInnerA != null) placedIds.Add(tbInnerA.Id.IntegerValue);
                        }
                    }

                    if (closeReturnEnd)
                    {
                        var rTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_top), MmToFt(yA_top), MmToFt(zH6)), rowA.AngleDeg, "l", W_B);
                        var rTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_top), MmToFt(yA_top), MmToFt(zH7)), rowA.AngleDeg, "l", W_B);
                        if (rTop1 != null) placedIds.Add(rTop1.Id.IntegerValue);
                        if (rTop2 != null) placedIds.Add(rTop2.Id.IntegerValue);
                    }

                    if (L_A > 10.0)
                    {
                        var rA_1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH6)), rotDegB, "l", L_A);
                        var rA_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yMax), MmToFt(zH7)), rotDegB, "l", L_A);
                        if (rA_1 != null) placedIds.Add(rA_1.Id.IntegerValue);
                        if (rA_2 != null) placedIds.Add(rA_2.Id.IntegerValue);
                    }
                }
            }
        }

        /// <summary>
        /// TH 2.2: Khớp nối góc 90° - Vươn ra ngoài hướng ngược lại (Outer Protrusion -Y, Mép trước phẳng).
        /// Kế thừa 100% cấu kiện ExecuteCase2_2 với mọi kích thước (L_last, L_first, W_A, W_B).
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
            double pwA = GetPlankWidth(W_A), pOffA = GetPlankOffset(W_A);
            double pwB = GetPlankWidth(W_B), pOffB = GetPlankOffset(W_B);

            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB != null && rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_A = (L_first > W_A + 10.0) ? (L_first - W_A) : 0.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double totalDistA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Sum() : L_last;
            double dStartLast = totalDistA - L_last;
            double xLast = rowA.StartX + dStartLast * uAx;
            double yLast = rowA.StartY + dStartLast * uAy;
            double xMax = xLast + L_last * uAx;
            double yMax = yLast + L_last * uAy;
            double xBack = xLast + W_A * vAx;
            double yBack = yLast + W_A * vAy;
            double xA_back = xMax + W_A * vAx;
            double yA_back = yMax + W_A * vAy;

            ensureCol(xLast, yLast);
            ensureCol(xMax, yMax);
            ensureCol(xBack, yBack);
            ensureCol(xA_back, yA_back);

            double zH1 = z0 + jackH + 135;
            placeTransom(xLast, yLast, zH1, rowA.AngleDeg - 90, W_A);
            placeTransom(xMax, yMax, zH1, rowA.AngleDeg - 90, W_A);
            if (opts != null && opts.HasTopTransom)
            {
                placeTransom(xLast, yLast, topZ, rowA.AngleDeg - 90, W_A);
                placeTransom(xMax, yMax, topZ, rowA.AngleDeg - 90, W_A);
            }
            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                placeTransom(xLast, yLast, sDeck, rowA.AngleDeg - 90, W_A);
                placeTransom(xMax, yMax, sDeck, rowA.AngleDeg - 90, W_A);
            }

            // 1. Dãy A nhịp cuối
            var bbFront = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(z0)), rowA.AngleDeg, "l", L_last);
            if (bbFront != null) placedIds.Add(bbFront.Id.IntegerValue);

            var lgFFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yLast), MmToFt(zH1)), rowA.AngleDeg, "l", L_last);
            if (lgFFront != null) placedIds.Add(lgFFront.Id.IntegerValue);

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
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + pOffA * vAx), MmToFt(yLast + pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBack - pOffA * vAx), MmToFt(yBack - pOffA * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (pl1 != null) { SetParam(pl1, "w", pwA); placedIds.Add(pl1.Id.IntegerValue); }
                    if (pl2 != null) { SetParam(pl2, "w", pwA); placedIds.Add(pl2.Id.IntegerValue); }
                }

                if (sym7 != null)
                {
                    var tbF = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66 * vAx), MmToFt(yLast + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBack - 66 * vAx), MmToFt(yBack - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_last);
                    if (tbF != null) placedIds.Add(tbF.Id.IntegerValue);
                    if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
                }
            }

            // 2. PHẦN B: NHÁNH RẼ NGOÀI TH 2.2 (MÉP TRƯỚC PHẲNG: L_first x W_B)
            if (isStandalone)
            {
                double rotUp = rowA.AngleDeg + 90.0; // Hướng từ yBot lên yFront (-vA)
                bool closeReturnEnd = (rowB == null || rowB.BayLengths == null || rowB.BayLengths.Count <= 1);

                double xA_front = xMax;
                double yA_front = yMax;
                double xB_front = xA_front + W_B * uAx;
                double yB_front = yA_front + W_B * uAy;
                double xA_bot = xA_front + L_first * vAx;
                double yA_bot = yA_front + L_first * vAy;
                double xB_bot = xB_front + L_first * vAx;
                double yB_bot = yB_front + L_first * vAy;

                var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA_bot),   MmToFt(yA_bot),   MmToFt(z0)), rowA.AngleDeg, "l", W_B);
                var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA_front), MmToFt(yA_front), MmToFt(z0)), rowA.AngleDeg, "l", W_B);
                if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
                if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

                ensureCol(xB_front, yB_front);
                ensureCol(xB_bot, yB_bot);
                if (L_A > 10.0)
                {
                    ensureCol(xA_bot, yA_bot);
                }

                placeTransom(xA_front, yA_front, zH1, rowA.AngleDeg, W_B);
                placeTransom(xA_bot,   yA_bot,   zH1, rowA.AngleDeg, W_B);
                var lgRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_bot), MmToFt(yB_bot), MmToFt(zH1)), rotUp, "l", L_first);
                if (lgRF_B != null) placedIds.Add(lgRF_B.Id.IntegerValue);
                if (L_A > 10.0)
                {
                    var lgRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot), MmToFt(yA_bot), MmToFt(zH1)), rotUp, "l", L_A);
                    if (lgRF_A != null) placedIds.Add(lgRF_A.Id.IntegerValue);
                }

                if (opts != null && opts.HasTopTransom)
                {
                    placeTransom(xA_front, yA_front, topZ, rowA.AngleDeg, W_B);
                    placeTransom(xA_bot,   yA_bot,   topZ, rowA.AngleDeg, W_B);
                    var lgTopRF_B = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_bot), MmToFt(yB_bot), MmToFt(topZ)), rotUp, "l", L_first);
                    if (lgTopRF_B != null) placedIds.Add(lgTopRF_B.Id.IntegerValue);
                    if (L_A > 10.0)
                    {
                        var lgTopRF_A = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot), MmToFt(yA_bot), MmToFt(topZ)), rotUp, "l", L_A);
                        if (lgTopRF_A != null) placedIds.Add(lgTopRF_A.Id.IntegerValue);
                    }
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgRM_Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_front), MmToFt(yA_front), MmToFt(zH3)), rowA.AngleDeg, "l", W_B);
                    var lgRM_Bot   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot),   MmToFt(yA_bot),   MmToFt(zH3)), rowA.AngleDeg, "l", W_B);
                    var lgRM_EndB  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_bot),   MmToFt(yB_bot),   MmToFt(zH3)), rotUp, "l", L_first);
                    if (lgRM_Front != null) placedIds.Add(lgRM_Front.Id.IntegerValue);
                    if (lgRM_Bot   != null) placedIds.Add(lgRM_Bot.Id.IntegerValue);
                    if (lgRM_EndB  != null) placedIds.Add(lgRM_EndB.Id.IntegerValue);

                    if (L_A > 10.0)
                    {
                        PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA_bot, yA_bot, rotUp, L_A, z0, jackH, s, storyCount, opts);
                    }

                    placeTransom(xA_front, yA_front, zDeck, rowA.AngleDeg, W_B);
                    placeTransom(xA_bot,   yA_bot,   zDeck, rowA.AngleDeg, W_B);

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA_bot + pOffB * uAx), MmToFt(yA_bot + pOffB * uAy), MmToFt(zDeck)), rotUp, "l", L_first);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB_bot - pOffB * uAx), MmToFt(yB_bot - pOffB * uAy), MmToFt(zDeck)), rotUp, "l", L_first);
                        if (pl1 != null) { SetParam(pl1, "w", pwB); placedIds.Add(pl1.Id.IntegerValue); }
                        if (pl2 != null) { SetParam(pl2, "w", pwB); placedIds.Add(pl2.Id.IntegerValue); }
                    }

                    if (sym7 != null)
                    {
                        var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA_front + 66 * vAx), MmToFt(yA_front + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                        var tbEndB  = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB_bot - 66 * uAx),   MmToFt(yB_bot - 66 * uAy),   MmToFt(zDeck)), rotUp, "l", L_first);
                        if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);
                        if (tbEndB  != null) placedIds.Add(tbEndB.Id.IntegerValue);

                        if (closeReturnEnd)
                        {
                            var tbBot = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA_bot - 66 * vAx), MmToFt(yA_bot - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_B);
                            if (tbBot != null) placedIds.Add(tbBot.Id.IntegerValue);
                        }

                        if (L_A > 10.0)
                        {
                            var tbInnerA = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA_bot + 66 * uAx), MmToFt(yA_bot + 66 * uAy), MmToFt(zDeck)), rotUp, "l", L_A);
                            if (tbInnerA != null) placedIds.Add(tbInnerA.Id.IntegerValue);
                        }
                    }

                    var rFront1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_front), MmToFt(yA_front), MmToFt(zH6)), rowA.AngleDeg, "l", W_B);
                    var rFront2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_front), MmToFt(yA_front), MmToFt(zH7)), rowA.AngleDeg, "l", W_B);
                    var rEnd1   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_bot),   MmToFt(yB_bot),   MmToFt(zH6)), rotUp, "l", L_first);
                    var rEnd2   = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB_bot),   MmToFt(yB_bot),   MmToFt(zH7)), rotUp, "l", L_first);
                    if (rFront1 != null) placedIds.Add(rFront1.Id.IntegerValue);
                    if (rFront2 != null) placedIds.Add(rFront2.Id.IntegerValue);
                    if (rEnd1   != null) placedIds.Add(rEnd1.Id.IntegerValue);
                    if (rEnd2   != null) placedIds.Add(rEnd2.Id.IntegerValue);

                    if (closeReturnEnd)
                    {
                        var rBot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot), MmToFt(yA_bot), MmToFt(zH6)), rowA.AngleDeg, "l", W_B);
                        var rBot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot), MmToFt(yA_bot), MmToFt(zH7)), rowA.AngleDeg, "l", W_B);
                        if (rBot1 != null) placedIds.Add(rBot1.Id.IntegerValue);
                        if (rBot2 != null) placedIds.Add(rBot2.Id.IntegerValue);
                    }

                    if (L_A > 10.0)
                    {
                        var rA_2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA_bot), MmToFt(yA_bot), MmToFt(zH7)), rotUp, "l", L_A);
                        if (rA_2 != null) placedIds.Add(rA_2.Id.IntegerValue);
                    }
                }
            }
        }
    }
}
