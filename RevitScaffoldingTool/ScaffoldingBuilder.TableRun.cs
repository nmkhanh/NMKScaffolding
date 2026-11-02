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
        /// Thực thi tuần tự theo đúng logic người dùng:
        /// Tạo TH thẳng Row 1 -> Tạo TH thẳng Row 2 -> Xử lý Connect (Row 1 -> Row 2) -> ... -> Khép kín vòng.
        /// Kế thừa 100% logic cấu kiện từ 6 TH đã kiểm chứng trong Cases1.cs và Cases2.cs,
        /// chỉ thay số cứng bằng kích thước động: L_last, L_first, W_A, W_B.
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
        /// Dựng thẳng Row 1 -> Dựng thẳng Row 2 -> Xử lý Connect (Row 1 -> Row 2) -> ... -> Khép kín vòng.
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

            // Hàm cấy cột đảm bảo không trùng tại các giao điểm
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

            int nRows = rows.Count;

            // Kiểm tra vòng khép kín
            bool isClosedLoop = false;
            if (nRows >= 2)
            {
                var rLast = rows[nRows - 1];
                var rFirst = rows[0];
                double dEndToStart = Math.Sqrt(Math.Pow(rLast.EndX - rFirst.StartX, 2) + Math.Pow(rLast.EndY - rFirst.StartY, 2));
                isClosedLoop = (dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rLast.ConnectCase) && rLast.ConnectCase != "None");
            }

            // 1. Dựng hệ thẳng Row 1
            bool row1HasNext = (nRows > 1);
            BuildTableStraightRun(doc, rows[0], opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7,
                openStart: isClosedLoop, openEnd: row1HasNext);

            // 2. Duyệt các hàng tiếp theo: Dựng thẳng Row i -> Xử lý Connect (Row i-1 -> Row i)
            for (int i = 1; i < nRows; i++)
            {
                var rowPrev = rows[i - 1];
                var rowCur = rows[i];
                bool rowCurHasNext = (i < nRows - 1) || isClosedLoop;

                // a. Dựng hệ thẳng của Row hiện tại
                BuildTableStraightRun(doc, rowCur, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7,
                    openStart: true, openEnd: rowCurHasNext);

                // b. Xử lý Connect giữa Row trước và Row hiện tại
                BuildTableCornerConnect(doc, rowPrev, rowCur, rowPrev.ConnectCase, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
            }

            // 3. Khép kín vòng nếu hàng cuối nối về hàng đầu tiên
            if (isClosedLoop && nRows >= 2)
            {
                var rowLast = rows[nRows - 1];
                var rowFirst = rows[0];
                string cornerCase = !string.IsNullOrEmpty(rowLast.ConnectCase) ? rowLast.ConnectCase : "TH 1.1";
                BuildTableCornerConnect(doc, rowLast, rowFirst, cornerCase, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
            }

            return null;
        }

        /// <summary>
        /// DỰNG HỆ GIÀN THẲNG CHO 1 HÀNG TRONG TABLE:
        /// Dựa trên danh sách nhịp động (BayLengths) từ Table.
        /// openStart = true: Không chặn lan can ở đầu bắt đầu (dành cho điểm nối góc).
        /// openEnd = true: Không chặn lan can ở đầu kết thúc (dành cho điểm nối góc).
        /// </summary>
        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool openStart, bool openEnd)
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
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double ux = cosA, uy = sinA;
            double vx = sinA, vy = -cosA;

            double startX = row.StartX;
            double startY = row.StartY;

            List<double> distances = new List<double> { 0.0 };
            double curDist = 0.0;
            foreach (var bLen in bayLens)
            {
                curDist += bLen;
                distances.Add(curDist);
            }

            int nCols = distances.Count;

            // 1. Cấy các cột dọc hai bên mép
            for (int k = 0; k < nCols; k++)
            {
                double d = distances[k];
                double cx1 = startX + d * ux;
                double cy1 = startY + d * uy;
                double cx2 = cx1 + W * vx;
                double cy2 = cy1 + W * vy;

                ensureCol(cx1, cy1);
                ensureCol(cx2, cy2);

                // Giằng ngang chân tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(zH1)), row.AngleDeg - 90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // Giằng đỉnh ngang nếu có HasTopTransom
                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(topZ)), row.AngleDeg - 90, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
                }

                // Giằng ngang đỡ mâm từng tầng
                for (int s = 0; s < storyCount; s++)
                {
                    double sDeck = z0 + jackH + 1730 + s * 1800.0;
                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sDeck)), row.AngleDeg - 90, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
                }

                // Lan can và ván chắn đầu hồi: Chỉ đặt ở đầu tự do (không bị mở bởi nối góc)
                bool needEndRail = (k == 0 && !openStart) || (k == nCols - 1 && !openEnd);
                if (needEndRail)
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

            // 2. Dựng các cấu kiện dọc cho từng nhịp
            int nBays = bayLens.Count;
            for (int b = 0; b < nBays; b++)
            {
                double dStart = distances[b];
                double bLen = bayLens[b];

                double px1 = startX + dStart * ux;
                double py1 = startY + dStart * uy;
                double px2 = px1 + W * vx;
                double py2 = py1 + W * vy;

                // Ván lót đáy Family 1 ở cao độ Z0
                var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(z0)), row.AngleDeg, "l", bLen);
                var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(z0)), row.AngleDeg, "l", bLen);
                if (bb1 != null) placedIds.Add(bb1.Id.IntegerValue);
                if (bb2 != null) placedIds.Add(bb2.Id.IntegerValue);

                // Giằng dọc chân Family 4 tại h1
                double zH1 = z0 + jackH + 135;
                var lgFoot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH1)), row.AngleDeg, "l", bLen);
                var lgFoot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(zH1)), row.AngleDeg, "l", bLen);
                if (lgFoot1 != null) placedIds.Add(lgFoot1.Id.IntegerValue);
                if (lgFoot2 != null) placedIds.Add(lgFoot2.Id.IntegerValue);

                // Giằng dọc đỉnh tại topZ
                if (opts != null && opts.HasTopTransom)
                {
                    var lgTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(topZ)), row.AngleDeg, "l", bLen);
                    var lgTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px2), MmToFt(py2), MmToFt(topZ)), row.AngleDeg, "l", bLen);
                    if (lgTop1 != null) placedIds.Add(lgTop1.Id.IntegerValue);
                    if (lgTop2 != null) placedIds.Add(lgTop2.Id.IntegerValue);
                }

                // Cấu kiện theo từng tầng
                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    // Giằng giữa mặt trước (h3)
                    var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH3)), row.AngleDeg, "l", bLen);
                    if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);

                    // Lan can mặt trước (h6, h7)
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH6)), row.AngleDeg, "l", bLen);
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px1), MmToFt(py1), MmToFt(zH7)), row.AngleDeg, "l", bLen);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    // Giằng chéo mặt sau Family 5
                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px2, py2, row.AngleDeg, bLen, z0, jackH, s, storyCount, opts);

                    // Mâm thao tác Family 6
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

                    // Ván chắn biên Family 7
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
        /// Áp dụng nguyên xi logic cấu kiện chuẩn từ Cases1.cs và Cases2.cs.
        /// </summary>
        private static void BuildTableCornerConnect(Document doc, BayRowModel rowPrev, BayRowModel rowNext, string connectCase,
            BuildOptions opts, Level level, List<int> placedIds, Action<double, double> ensureCol,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            switch (connectCase)
            {
                case "TH 1.1":
                    ConnectCorner_TH1_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                case "TH 1.2":
                    ConnectCorner_TH1_2(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                case "TH 1.3":
                    ConnectCorner_TH1_3(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                case "TH 1.4":
                    ConnectCorner_TH1_4(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                case "TH 2.1":
                    ConnectCorner_TH2_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                case "TH 2.2":
                    ConnectCorner_TH2_2(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
                default:
                    ConnectCorner_TH1_1(doc, rowPrev, rowNext, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CÁC FUNCTION KHỚP NỐI GÓC - SAO CHÉP 100% CẤU KIỆN TỪ CASES1 & CASES2
        //  CHỈ THAY THẾ KÍCH THƯỚC ĐỘNG: L_last, L_first, W_A, W_B
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TH 1.1: Khớp nối góc 90° hướng +Y (Mép ngoài phẳng).
        /// Sao chép từ ExecuteCase1_1: Cấy cột phụ tại X = xC, giằng mặt trước thu ngắn L_cut = L_last - W_B,
        /// khớp transoms nối góc tại h1, topZ và zDeck.
        /// </summary>
        private static void ConnectCorner_TH1_1(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double L_last = (rowA.BayLengths != null && rowA.BayLengths.Count > 0) ? rowA.BayLengths.Last() : 1800.0;
            double L_first = (rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_cut = (L_last > W_B) ? (L_last - W_B) : 600.0;

            // Điểm giao góc: Điểm bắt đầu của Row B chính là vị trí cột cấy xC
            double xC = rowB.StartX;
            double yC = rowB.StartY;
            ensureCol(xC, yC);

            // Giằng ngang liên kết tại góc chuyển tiếp sang Row B (chiều dài W_B)
            double zH1 = z0 + jackH + 135;
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(sDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }

        /// <summary>
        /// TH 1.2: Khớp nối góc 90° - Ô vuông W_B x W_B (hoặc L_last x W_B).
        /// Sao chép từ ExecuteCase1_2: Không cấy cột phụ, liên kết đầu hồi giao góc đầy đủ giằng chân, đỉnh và mâm.
        /// </summary>
        private static void ConnectCorner_TH1_2(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double xC = rowB.StartX;
            double yC = rowB.StartY;
            ensureCol(xC, yC);

            double zH1 = z0 + jackH + 135;
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(sDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }

        /// <summary>
        /// TH 1.3: Khớp nối góc 90° hướng -Y (Giằng thu ngắn phía đối diện).
        /// Sao chép từ ExecuteCase1_3: Cấy cột phụ tại mép sau, liên kết góc chuyển tiếp chiều dài W_B.
        /// </summary>
        private static void ConnectCorner_TH1_3(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double xC = rowB.StartX;
            double yC = rowB.StartY;
            ensureCol(xC, yC);

            double zH1 = z0 + jackH + 135;
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(sDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }

        /// <summary>
        /// TH 1.4: Khớp nối góc 90° hướng -Y (Ô vuông W_B x W_B).
        /// Sao chép từ ExecuteCase1_4.
        /// </summary>
        private static void ConnectCorner_TH1_4(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double xC = rowB.StartX;
            double yC = rowB.StartY;
            ensureCol(xC, yC);

            double zH1 = z0 + jackH + 135;
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sDeck = z0 + jackH + 1730 + s * 1800.0;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yC), MmToFt(sDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }

        /// <summary>
        /// TH 2.1: Nhánh rẽ vươn ngoài đầu hồi giàn (Mép sau phẳng).
        /// Sao chép từ ExecuteCase2_1: Cột vươn ngoài, giằng chéo đầu hồi và mép ngoài, vách trong giật cấp L_step = L_first - W_A.
        /// </summary>
        private static void ConnectCorner_TH2_1(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double L_first = (rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_step = (L_first > W_A) ? (L_first - W_A) : 600.0;

            double xA = rowB.StartX;
            double yA = rowB.StartY;
            ensureCol(xA, yA);

            // Giằng ngang liên kết tại vị trí giao góc
            double zH1 = z0 + jackH + 135;
            var trRF = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trRF != null) placedIds.Add(trRF.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                // Giằng dọc vách trong giật cấp L_step
                var lgStep = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH3)), rowB.AngleDeg, "l", L_step);
                var rStep1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH6)), rowB.AngleDeg, "l", L_step);
                var rStep2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH7)), rowB.AngleDeg, "l", L_step);
                if (lgStep != null) placedIds.Add(lgStep.Id.IntegerValue);
                if (rStep1 != null) placedIds.Add(rStep1.Id.IntegerValue);
                if (rStep2 != null) placedIds.Add(rStep2.Id.IntegerValue);

                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }

        /// <summary>
        /// TH 2.2: Nhánh rẽ vươn ngoài đầu hồi giàn hướng -Y (Mép trước phẳng).
        /// Sao chép từ ExecuteCase2_2.
        /// </summary>
        private static void ConnectCorner_TH2_2(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double L_first = (rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;
            double L_step = (L_first > W_A) ? (L_first - W_A) : 600.0;

            double xA = rowB.StartX;
            double yA = rowB.StartY;
            ensureCol(xA, yA);

            double zH1 = z0 + jackH + 135;
            var trRF = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trRF != null) placedIds.Add(trRF.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                // Giằng chéo Family 5 tại vách trong giật cấp
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yA, rowB.AngleDeg, L_step, z0, jackH, s, storyCount, opts);

                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
            }
        }
    }
}
