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
        /// Thực thi tuần tự: TH thẳng Row 1 -> TH thẳng Row 2 -> Connect (Row 1-2) -> TH thẳng Row 3 -> Connect (Row 2-3) -> ... -> Khép kín vòng.
        /// Đảm bảo hoàn toàn độc lập, KHÔNG chỉnh sửa hay làm ảnh hưởng tới 6 hàm trường hợp cũ.
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
        /// Pipeline thực thi chính:
        /// Tạo TH thẳng Row 1 -> Tạo TH thẳng Row 2 -> Xử lý Connect Row 1 (nếu có Row 2) -> ... -> Khép kín vòng.
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

            // Hàm cấy cột đảm bảo không bao giờ trùng tại các giao điểm góc
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

            // 1. Tạo hệ giàn thẳng Row 1
            BuildTableStraightRun(doc, rows[0], opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);

            // 2. Duyệt các hàng tiếp theo: Dựng thẳng Row i -> Xử lý Connect giữa Row i-1 và Row i
            for (int i = 1; i < rows.Count; i++)
            {
                var rowPrev = rows[i - 1];
                var rowCur = rows[i];

                // a. Dựng hệ thẳng của Row hiện tại
                BuildTableStraightRun(doc, rowCur, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);

                // b. Xử lý Connect cho Row trước đó (chỉ xử lý vì đã có Row hiện tại)
                BuildTableCornerConnect(doc, rowPrev, rowCur, rowPrev.ConnectCase, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
            }

            // 3. XỬ LÝ VÒNG KHÉP KÍN (CLOSED LOOP):
            // Nếu có từ 2 hàng trở lên, kiểm tra xem Row cuối cùng (rows[N-1]) có khép vòng về Row đầu tiên (rows[0]) không
            if (rows.Count >= 2)
            {
                var rowLast = rows[rows.Count - 1];
                var rowFirst = rows[0];

                double dEndToStart = Math.Sqrt(Math.Pow(rowLast.EndX - rowFirst.StartX, 2) + Math.Pow(rowLast.EndY - rowFirst.StartY, 2));
                bool isClosedLoop = (dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rowLast.ConnectCase) && rowLast.ConnectCase != "None");

                if (isClosedLoop)
                {
                    string cornerCase = !string.IsNullOrEmpty(rowLast.ConnectCase) ? rowLast.ConnectCase : "TH 1.1";
                    BuildTableCornerConnect(doc, rowLast, rowFirst, cornerCase, opts, level, placedIds, ensureCol, sym1, sym4, sym5, sym6, sym7);
                }
            }

            return null;
        }

        /// <summary>
        /// DỰNG HỆ GIÀN THẲNG CHO 1 HÀNG TRONG TABLE:
        /// Dựa trên danh sách nhịp động (BayLengths) từ Table và tọa độ hướng tuyến thực tế.
        /// </summary>
        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
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

            // Vector chỉ phương dọc u và vuông góc v (vuông góc sang phải: (sin, -cos))
            double ux = cosA, uy = sinA;
            double vx = sinA, vy = -cosA;

            double startX = row.StartX;
            double startY = row.StartY;

            // Tính toán khoảng cách d tích lũy qua từng nhịp
            List<double> distances = new List<double> { 0.0 };
            double curDist = 0.0;
            foreach (var bLen in bayLens)
            {
                curDist += bLen;
                distances.Add(curDist);
            }

            int nCols = distances.Count;

            // 1. Cấy các hàng cột hai bên mép (mép trong P1 và mép ngoài P2)
            for (int k = 0; k < nCols; k++)
            {
                double d = distances[k];
                double cx1 = startX + d * ux;
                double cy1 = startY + d * uy;
                double cx2 = cx1 + W * vx;
                double cy2 = cy1 + W * vy;

                ensureCol(cx1, cy1);
                ensureCol(cx2, cy2);

                // Giằng ngang chân tại h1 (Z = jackH + 135)
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(zH1)), row.AngleDeg - 90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // Giằng đỉnh ngang nếu bật HasTopTransom
                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(topZ)), row.AngleDeg - 90, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
                }

                // Giằng ngang đỡ mâm lặp theo số tầng
                for (int s = 0; s < storyCount; s++)
                {
                    double sDeck = z0 + jackH + 1730 + s * 1800.0;
                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(sDeck)), row.AngleDeg - 90, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);
                }

                // Lan can và ván chắn đầu hồi ở hai đầu mút
                if (k == 0 || k == nCols - 1)
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

                // Giằng dọc chân Family 4 tại h1 (Z = jackH + 135)
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

                // Cấu kiện theo từng tầng: Giằng giữa h3, Giằng chéo sau F5, Mâm F6, Lan can h6/h7, Ván chắn F7
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

                    // Giằng chéo mặt sau Family 5 (nguyên tắc: ở đâu có F5 thì không có F4)
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
        /// Tách riêng biệt thành các logic khớp nối theo từng trường hợp đã kiểm chứng (TH 1.1 -> TH 2.2).
        /// Sử dụng nhịp cuối (L_last), nhịp đầu (L_first) và bề rộng thực tế của từng hàng để code tổng quát.
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
        //  CÁC FUNCTION KHỚP NỐI RIÊNG BIỆT - TỔNG QUÁT HÓA THEO KÍCH THƯỚC THẬT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TH 1.1: Khớp nối góc 90° hướng mép trái (+Y) - Mép ngoài phẳng.
        /// Cấy cột phụ lùi vào W_B, thu ngắn thanh giằng mặt trước L_cut = L_last - W_B, giằng chéo ngoài theo nhịp L_first.
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

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double L_cut = (L_last > W_B) ? (L_last - W_B) : 600.0;

            // 1. Cấy cột phụ tại mép trước của Row A lùi lại một đoạn W_B
            double cx_sub = rowA.EndX - (L_last - L_cut) * uAx;
            double cy_sub = rowA.EndY - (L_last - L_cut) * uAy;
            ensureCol(cx_sub, cy_sub);

            // Cột góc ngoài tại điểm cuối Row A
            double cx_corner = rowA.EndX + W_A * vAx;
            double cy_corner = rowA.EndY + W_A * vAy;
            ensureCol(cx_corner, cy_corner);

            // 2. Ván lót đáy Family 1 và giằng chân Family 4 cho phần thu ngắn L_cut
            double zH1 = z0 + jackH + 135;
            var bbCut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
            var lgCutFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
            if (bbCut != null) placedIds.Add(bbCut.Id.IntegerValue);
            if (lgCutFoot != null) placedIds.Add(lgCutFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgCutTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                if (lgCutTop != null) placedIds.Add(lgCutTop.Id.IntegerValue);
            }

            // 3. Giằng ngang liên kết tại góc chuyển hướng sang Row B (chiều dài W_B)
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTop != null) placedIds.Add(trTop.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                // Giằng giữa và lan can phần thu ngắn L_cut
                var lgCutMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH3)), rowA.AngleDeg, "l", L_cut);
                var railCut1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH6)), rowA.AngleDeg, "l", L_cut);
                var railCut2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH7)), rowA.AngleDeg, "l", L_cut);
                if (lgCutMid != null) placedIds.Add(lgCutMid.Id.IntegerValue);
                if (railCut1 != null) placedIds.Add(railCut1.Id.IntegerValue);
                if (railCut2 != null) placedIds.Add(railCut2.Id.IntegerValue);

                // Giằng ngang đỡ mâm góc chuyển tiếp sang Row B
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Giằng chéo Family 5 tại mép ngoài góc (hướng Row B, nhịp L_first)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, cx_corner, cy_corner, rowB.AngleDeg, L_first, z0, jackH, s, storyCount, opts);

                if (sym7 != null)
                {
                    var tbCut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(cx_sub + 66 * vAx), MmToFt(cy_sub + 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                    if (tbCut != null) placedIds.Add(tbCut.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 1.2: Khớp nối góc 90° - Ô vuông W_B x W_B (hoặc L_last x W_B).
        /// Khớp vuông góc đối đầu đầy đủ 2 đầu hồi: giằng chân, giằng mâm, giằng đỉnh và lan can an toàn.
        /// </summary>
        private static void ConnectCorner_TH1_2(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double L_first = (rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            // Cột góc trong và góc ngoài tại giao điểm
            double cx1 = rowA.EndX;
            double cy1 = rowA.EndY;
            double cx2 = cx1 + W_A * vAx;
            double cy2 = cy1 + W_A * vAy;
            ensureCol(cx1, cy1);
            ensureCol(cx2, cy2);

            // Đầu hồi 1 (Giao diện nối góc): Giằng chân, giằng đỉnh, giằng mâm chiều dài W_B
            double zH1 = z0 + jackH + 135;
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;

                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx1), MmToFt(cy1), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);

                // Giằng chéo Family 5 tại mép ngoài góc
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, cx2, cy2, rowB.AngleDeg, L_first, z0, jackH, s, storyCount, opts);
            }
        }

        /// <summary>
        /// TH 1.3: Khớp nối góc 90° hướng mép phải (-Y) - Giằng thu ngắn phía đối diện.
        /// Cấy cột phụ tại mép offset W_A, thu ngắn giằng sau L_cut = L_last - W_B, giằng chéo thu ngắn.
        /// </summary>
        private static void ConnectCorner_TH1_3(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
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

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double L_cut = (L_last > W_B) ? (L_last - W_B) : 600.0;

            // Cột phụ cấy tại mép sau (offset W_A) lùi lại một đoạn W_B
            double cx_sub = (rowA.EndX + W_A * vAx) - (L_last - L_cut) * uAx;
            double cy_sub = (rowA.EndY + W_A * vAy) - (L_last - L_cut) * uAy;
            ensureCol(cx_sub, cy_sub);

            // Cột góc trước
            ensureCol(rowA.EndX, rowA.EndY);

            // Ván lót đáy và giằng sau thu ngắn L_cut
            double zH1 = z0 + jackH + 135;
            var bbCut = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(z0)), rowA.AngleDeg, "l", L_cut);
            var lgCutFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH1)), rowA.AngleDeg, "l", L_cut);
            if (bbCut != null) placedIds.Add(bbCut.Id.IntegerValue);
            if (lgCutFoot != null) placedIds.Add(lgCutFoot.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var lgCutTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(topZ)), rowA.AngleDeg, "l", L_cut);
                if (lgCutTop != null) placedIds.Add(lgCutTop.Id.IntegerValue);
            }

            // Giằng ngang chuyển tiếp chiều dài W_B tại góc
            var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;

                // Giằng chéo thu ngắn Family 5 tại mép sau
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, cx_sub, cy_sub, rowA.AngleDeg, L_cut, z0, jackH, s, storyCount, opts);

                // Giằng đỡ mâm góc
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx_sub), MmToFt(cy_sub), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbCut = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(cx_sub - 66 * vAx), MmToFt(cy_sub - 66 * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", L_cut);
                    if (tbCut != null) placedIds.Add(tbCut.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 1.4: Khớp nối góc 90° hướng mép phải (-Y) - Ô vuông W_B x W_B.
        /// Tương tự TH 1.2 nhưng xoay sang hướng -Y.
        /// </summary>
        private static void ConnectCorner_TH1_4(Document doc, BayRowModel rowA, BayRowModel rowB, BuildOptions opts, Level level,
            List<int> placedIds, Action<double, double> ensureCol, FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7)
        {
            double jackH = GetJackHeight(opts);
            double z0 = opts.Z0;
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            double W_A = rowA.Width > 0 ? rowA.Width : 1200.0;
            double W_B = rowB.Width > 0 ? rowB.Width : 1200.0;
            double L_first = (rowB.BayLengths != null && rowB.BayLengths.Count > 0) ? rowB.BayLengths.First() : 1800.0;

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double cx1 = rowA.EndX;
            double cy1 = rowA.EndY;
            double cx2 = cx1 + W_A * vAx;
            double cy2 = cy1 + W_A * vAy;
            ensureCol(cx1, cy1);
            ensureCol(cx2, cy2);

            double zH1 = z0 + jackH + 135;
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx2), MmToFt(cy2), MmToFt(zH1)), rowB.AngleDeg - 90, "l", W_B);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx2), MmToFt(cy2), MmToFt(topZ)), rowB.AngleDeg - 90, "l", W_B);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zDeck = z0 + jackH + 1730 + sOff;

                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(cx2), MmToFt(cy2), MmToFt(zDeck)), rowB.AngleDeg - 90, "l", W_B);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);

                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, cx1, cy1, rowB.AngleDeg, L_first, z0, jackH, s, storyCount, opts);
            }
        }

        /// <summary>
        /// TH 2.1: Nhánh rẽ vươn ngoài đầu hồi giàn (Mép sau phẳng).
        /// Cột vươn ngoài xB = xA + W_ret, giằng chéo đầu hồi và mép ngoài, vách trong giật cấp L_step = L_first - W_A.
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

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double W_ret = W_B;
            double L_ret = L_first;
            double L_step = (L_first > W_A) ? (L_first - W_A) : 600.0;

            // Điểm đầu hồi Row A
            double xA = rowA.EndX;
            double yA = rowA.EndY;

            // Cột vươn ngoài vượt qua đầu hồi một đoạn W_ret
            double xB = xA + W_ret * uAx;
            double yB = yA + W_ret * uAy;

            ensureCol(xA, yA);
            ensureCol(xA + W_A * vAx, yA + W_A * vAy);
            ensureCol(xB, yB);
            ensureCol(xB + W_A * vAx, yB + W_A * vAy);

            // Giằng ngang vươn ngoài chiều dài W_ret
            double zH1 = z0 + jackH + 135;
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA + W_A * vAy), MmToFt(zH1)), rowA.AngleDeg, "l", W_ret);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA + W_A * vAy), MmToFt(topZ)), rowA.AngleDeg, "l", W_ret);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                // 1. Giằng dọc vách trong giật cấp (chiều dài L_step)
                var lgStep = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH3)), rowB.AngleDeg, "l", L_step);
                var rStep1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH6)), rowB.AngleDeg, "l", L_step);
                var rStep2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH7)), rowB.AngleDeg, "l", L_step);
                if (lgStep != null) placedIds.Add(lgStep.Id.IntegerValue);
                if (rStep1 != null) placedIds.Add(rStep1.Id.IntegerValue);
                if (rStep2 != null) placedIds.Add(rStep2.Id.IntegerValue);

                // 2. Giằng chéo Family 5: đầu hồi đáy và mép ngoài bên phải
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yA + W_A * vAy, rowA.AngleDeg, W_ret, z0, jackH, s, storyCount, opts);
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xB, yA + W_A * vAy, rowB.AngleDeg, L_ret, z0, jackH, s, storyCount, opts);

                // 3. Giằng đỡ mâm vươn ngoài
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA + W_A * vAy), MmToFt(zDeck)), rowA.AngleDeg, "l", W_ret);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbStep = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66 * uAx), MmToFt(yA + 66 * uAy), MmToFt(zDeck)), rowB.AngleDeg, "l", L_step);
                    if (tbStep != null) placedIds.Add(tbStep.Id.IntegerValue);
                }
            }
        }

        /// <summary>
        /// TH 2.2: Nhánh rẽ vươn ngoài đầu hồi giàn hướng -Y (Mép trước phẳng).
        /// Cột vươn ngoài xB = xA + W_ret hướng -Y, giằng chéo vách trong giật cấp L_step = L_first - W_A.
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

            double angRadA = rowA.AngleDeg * Math.PI / 180.0;
            double uAx = Math.Cos(angRadA), uAy = Math.Sin(angRadA);
            double vAx = Math.Sin(angRadA), vAy = -Math.Cos(angRadA);

            double W_ret = W_B;
            double L_ret = L_first;
            double L_step = (L_first > W_A) ? (L_first - W_A) : 600.0;

            double xA = rowA.EndX;
            double yA = rowA.EndY;
            double xB = xA + W_ret * uAx;
            double yB = yA + W_ret * uAy;

            ensureCol(xA, yA);
            ensureCol(xA + W_A * vAx, yA + W_A * vAy);
            ensureCol(xB, yB);
            ensureCol(xB + W_A * vAx, yB + W_A * vAy);

            double zH1 = z0 + jackH + 135;
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zH1)), rowA.AngleDeg, "l", W_ret);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);

            if (opts != null && opts.HasTopTransom)
            {
                var trTopRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(topZ)), rowA.AngleDeg, "l", W_ret);
                if (trTopRF1 != null) placedIds.Add(trTopRF1.Id.IntegerValue);
            }

            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;
                double zH3 = z0 + jackH + 830 + sOff;
                double zDeck = z0 + jackH + 1730 + sOff;
                double zH6 = z0 + jackH + 2180 + sOff;
                double zH7 = z0 + jackH + 2630 + sOff;

                // 1. Giằng chéo Family 5 tại vách trong giật cấp
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xA, yA + W_A * vAy, rowB.AngleDeg, L_step, z0, jackH, s, storyCount, opts);

                // 2. Giằng dọc ngoài tại mép ngoài bên phải xB
                var lgOut = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yB + W_A * vAy), MmToFt(zH3)), rowB.AngleDeg, "l", L_ret);
                var rOut1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yB + W_A * vAy), MmToFt(zH6)), rowB.AngleDeg, "l", L_ret);
                var rOut2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(yB + W_A * vAy), MmToFt(zH7)), rowB.AngleDeg, "l", L_ret);
                if (lgOut != null) placedIds.Add(lgOut.Id.IntegerValue);
                if (rOut1 != null) placedIds.Add(rOut1.Id.IntegerValue);
                if (rOut2 != null) placedIds.Add(rOut2.Id.IntegerValue);

                // 3. Giằng đỡ mâm vươn ngoài
                var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yA), MmToFt(zDeck)), rowA.AngleDeg, "l", W_ret);
                if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);

                if (sym7 != null)
                {
                    var tbStep = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66 * uAx), MmToFt(yA + W_A * vAy - 66 * vAy), MmToFt(zDeck)), rowB.AngleDeg, "l", L_step);
                    if (tbStep != null) placedIds.Add(tbStep.Id.IntegerValue);
                }
            }
        }
    }
}