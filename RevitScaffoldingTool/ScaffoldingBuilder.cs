using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitScaffoldingTool
{
    public class TableRowOption
    {
        public List<double> BayLengths { get; set; } = new List<double>();
        public string ConnectCase { get; set; } = "TH 0";
        public double Width { get; set; } = 1200.0;
        public double StartX { get; set; } = 0.0;
        public double StartY { get; set; } = 0.0;
        public double StartZ { get; set; } = 0.0;
        public double EndX { get; set; } = 0.0;
        public double EndY { get; set; } = 0.0;
        public double EndZ { get; set; } = 0.0;
        public double AngleDeg { get; set; } = 0.0;
        public bool HasCoords { get; set; } = false;
    }

    public class BuildOptions
    {
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double Z0 { get; set; }
        public double BayWidth { get; set; }
        public double JackHeight { get; set; }
        public List<double> BayLengths { get; set; }
        public double L1 { get; set; }
        public double L2 { get; set; }
        public List<double> L0Bays { get; set; }
        public List<double> L1Bays { get; set; }
        public List<double> L2Bays { get; set; }
        public List<double> L3Bays { get; set; }
        public List<double> L4Bays { get; set; }
        public List<double> L5Bays { get; set; }
        public List<double> L6Bays { get; set; }
        public List<TableRowOption> TableRows { get; set; }
        public bool CheckCase11 { get; set; }
        public bool CheckCase12 { get; set; }
        public bool CheckCase13 { get; set; }
        public bool CheckCase14 { get; set; }
        public bool CheckCase21 { get; set; }
        public bool CheckCase22 { get; set; }
        public bool CheckCase31 { get; set; }
        public bool CheckCase32 { get; set; }
        public int Mode { get; set; }
        public string FamilyFolder { get; set; }

        public double Post1Height { get; set; }
        public double Post2Height { get; set; }
        public int Post3Count { get; set; }
        public double Post3Height { get; set; }
        public double Post4Height { get; set; }
        public double Post5Height { get; set; }
        public int StoryCount { get; set; }
        public bool HasTopTransom { get; set; }
        public bool IsFromData { get; set; }

        public BuildOptions()
        {
            X0 = 0;
            Y0 = 0;
            Z0 = 0;
            BayWidth = 1200;
            JackHeight = 250;
            BayLengths = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            L1 = 1800;
            L2 = 1800;
            L0Bays = new List<double> { 1800, 1800 };
            L1Bays = new List<double> { 1800, 1200 };
            L2Bays = new List<double> { 1800, 1800 };
            L3Bays = new List<double> { 1800, 1200 };
            L4Bays = new List<double> { 1800, 1800 };
            L5Bays = new List<double> { 1800, 1800, 1800 };
            L6Bays = new List<double> { 1800, 1800, 1800 };
            TableRows = new List<TableRowOption>();
            CheckCase11 = true;
            CheckCase12 = true;
            CheckCase13 = true;
            CheckCase14 = true;
            CheckCase21 = true;
            CheckCase22 = true;
            CheckCase31 = true;
            CheckCase32 = true;
            Mode = -1;
            FamilyFolder = @"H:\My Drive\Family\New folder\Child";
            Post1Height = 0;
            Post2Height = 0;
            Post3Count = 0;
            Post3Height = 0;
            Post4Height = 0;
            Post5Height = 0;
            StoryCount = 1;
            HasTopTransom = false;
            IsFromData = false;
        }
    }

    public class BuildResult
    {
        public bool Success { get; set; }
        public int TotalElements { get; set; }
        public double ElapsedMs { get; set; }
        public string Message { get; set; }

        public BuildResult()
        {
            Success = false;
            TotalElements = 0;
            ElapsedMs = 0;
            Message = "";
        }
    }

    public static partial class ScaffoldingBuilder
    {
        public static double MmToFt(double mm) { return mm / 304.8; }

        /// <summary>
        /// Phân tích chuỗi nhịp dạng "1800,2x1200,1800,3x1200,..." thành danh sách chiều dài từng nhịp (mm).
        /// </summary>
        public static List<double> ParseBayLengths(string expr, double defaultLen = 1800.0)
        {
            var result = new List<double>();
            if (!string.IsNullOrWhiteSpace(expr))
            {
                var tokens = expr.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawToken in tokens)
                {
                    string token = rawToken.Trim();
                    if (string.IsNullOrEmpty(token)) continue;

                    int xIdx = token.IndexOfAny(new char[] { 'x', 'X', '*' });
                    if (xIdx > 0 && xIdx < token.Length - 1)
                    {
                        string countStr = token.Substring(0, xIdx).Trim();
                        string lenStr = token.Substring(xIdx + 1).Trim();
                        int count = 1;
                        if (!int.TryParse(countStr, out count) || count <= 0) count = 1;

                        double len = 0;
                        if (double.TryParse(lenStr, NumberStyles.Any, CultureInfo.InvariantCulture, out len) && len > 0)
                        {
                            for (int k = 0; k < count; k++) result.Add(len);
                        }
                    }
                    else
                    {
                        double len = 0;
                        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out len) && len > 0)
                        {
                            result.Add(len);
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                result.Add(defaultLen > 0 ? defaultLen : 1800.0);
            }
            return result;
        }

        public static List<double> GetL1Bays(BuildOptions opts)
        {
            if (opts != null && opts.L1Bays != null && opts.L1Bays.Count > 0)
            {
                return opts.L1Bays;
            }
            double l1 = (opts != null && opts.L1 > 0) ? opts.L1 : 1800.0;
            return new List<double> { l1 };
        }

        public static List<double> GetL2Bays(BuildOptions opts)
        {
            if (opts != null && opts.L2Bays != null && opts.L2Bays.Count > 0)
            {
                return opts.L2Bays;
            }
            double l2 = (opts != null && opts.L2 > 0) ? opts.L2 : 1800.0;
            return new List<double> { l2 };
        }

        public static double GetJackHeight(BuildOptions opts)
        {
            if (opts == null) return 250.0;
            if (opts.IsFromData) return opts.JackHeight;
            return opts.JackHeight > 0 ? opts.JackHeight : 250.0;
        }

        public static double GetTopZ(double z0, double jackH, int storyCount, BuildOptions opts)
        {
            if (opts != null && opts.IsFromData && GetTotalPostHeight(opts) >= 1000)
            {
                return z0 + jackH + GetTotalPostHeight(opts) - 70.0;
            }
            return z0 + jackH + 2630.0 + (storyCount - 1) * 1800.0;
        }

        public static BuildResult ExecuteBuild(Document doc, BuildOptions opts)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = new BuildResult();

            if (doc == null)
            {
                res.Message = "Revit Document không hợp lệ!";
                return res;
            }

            if (opts == null) opts = new BuildOptions();

            string txName = opts.Mode == -9 ? "Dung Gian Giao - Button Run Theo Table" :
                            opts.Mode == -8 ? "Dung Gian Giao - Master 6 Corners Lien Tuc" :
                            opts.Mode == -2 ? "Dung Gian Giao - Truong Hop 1.1 (+Y Mep Ngoai)" :
                            opts.Mode == -4 ? "Dung Gian Giao - Truong Hop 1.2 (+X Thu 1200)" :
                            opts.Mode == -5 ? "Dung Gian Giao - Truong Hop 1.3 (-Y Giangg Thu Ngan)" :
                            opts.Mode == -6 ? "Dung Gian Giao - Truong Hop 1.4 (+X Thu 1200 -Y)" :
                            opts.Mode == -7 ? "Dung Gian Giao - Truong Hop 2.2 (+X Vuon Ngoai -Y)" :
                            opts.Mode == -3 ? "Dung Gian Giao - Truong Hop 2.1 (+Y Vuon Ngoai)" :
                            "Dung Gian Giao - Day Thang Nhieu Nhip";

            using (var tx = new Transaction(doc, txName))
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

                    if (opts.Mode == -9)
                    {
                        string err = ExecuteTableRunContinuous(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Button Run (Table): " + err; return res; }
                    }
                    else if (opts.Mode == -8)
                    {
                        string err = ExecuteMaster6CornersContinuous(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Master 6 Corners: " + err; return res; }
                    }
                    else if (opts.Mode == -2)
                    {
                        string err = ExecuteCase1_1(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 1.1: " + err; return res; }
                    }
                    else if (opts.Mode == -4)
                    {
                        string err = ExecuteCase1_2(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 1.2: " + err; return res; }
                    }
                    else if (opts.Mode == -5)
                    {
                        string err = ExecuteCase1_3(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 1.3: " + err; return res; }
                    }
                    else if (opts.Mode == -6)
                    {
                        string err = ExecuteCase1_4(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 1.4: " + err; return res; }
                    }
                    else if (opts.Mode == -7)
                    {
                        string err = ExecuteCase2_2(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 2.2: " + err; return res; }
                    }
                    else if (opts.Mode == -3)
                    {
                        string err = ExecuteCase2_1(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 2.1: " + err; return res; }
                    }
                    else if (opts.Mode == -10 || opts.Mode == 7)
                    {
                        string err = ExecuteCase3_1(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 3.1: " + err; return res; }
                    }
                    else if (opts.Mode == -11 || opts.Mode == 8)
                    {
                        string err = ExecuteCase3_2(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Trường Hợp 3.2: " + err; return res; }
                    }
                    else
                    {
                        string err = ExecuteMultiBay(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err)) { tx.RollBack(); res.Message = "Lỗi Nhiều Nhịp Thẳng: " + err; return res; }
                    }

                    tx.Commit();

                    sw.Stop();
                    res.Success = true;
                    res.TotalElements = placedIds.Count;
                    res.ElapsedMs = sw.Elapsed.TotalMilliseconds;

                    string title = opts.Mode == -9 ? "BUTTON RUN (THEO BẢNG TABLE)" :
                                   opts.Mode == -8 ? "MASTER 6 CORNERS" :
                                   opts.Mode == -2 ? "Trường Hợp 1.1 (+Y Mép Ngoài Phẳng)" :
                                   opts.Mode == -4 ? "Trường Hợp 1.2 (+X Thu 1200x1200)" :
                                   opts.Mode == -5 ? "Trường Hợp 1.3 (-Y Giằng Chéo Thu Ngắn 600)" :
                                   opts.Mode == -6 ? "Trường Hợp 1.4 (+X Thu 1200x1200 -Y)" :
                                   opts.Mode == -7 ? "Trường Hợp 2.2 (+X Vươn Ngoài -Y)" :
                                   opts.Mode == -3 ? "Trường Hợp 2.1 (+Y Vươn Ngoài)" :
                                   "Dãy Thẳng Tiêu Chuẩn";

                    res.Message = string.Format("{0} hoàn tất xuất sắc! Đã tạo {1} cấu kiện. Thời gian: {2:F0} ms",
                        title, placedIds.Count, res.ElapsedMs);
                    return res;
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    sw.Stop();
                    res.Success = false;
                    res.Message = "Lỗi ngoại lệ: " + ex.Message;
                    return res;
                }
            }
        }

        /// <summary>
        /// DỰNG LƯỚI CỘT DÃY CHÍNH VÀ GIẰNG NGANG LIÊN KẾT (TẤT CẢ TRƯỜNG HỢP)
        /// </summary>
        private static void BuildMainColumnGrid(Document doc, Level level, List<int> placedIds,
            FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym7,
            List<double> colX, double y0, double z0, double W, double jackH, int storyCount, double topZ,
            Func<int, int, bool> isOuterEndFunc, BuildOptions opts)
        {
            int nCols = colX.Count;
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isOuterEnd = isOuterEndFunc != null ? isOuterEndFunc(k, nCols) : (k == 0 || k == nCols - 1);

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.GetIdValue());
                if (jk2 != null) placedIds.Add(jk2.Id.GetIdValue());

                // Cột chống Family 3 (Trước & Sau)
                PlaceConfiguredPosts(doc, sym3, level, placedIds, px, y0, z0 + jackH, opts);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, px, y0 - W, z0 + jackH, opts);

                // Giằng chân ngang tại h1 (Z = jackH + 135)
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.GetIdValue());

                // Giằng đỉnh ngang tại topZ (nếu có tùy chọn Giằng đỉnh)
                if (opts != null && opts.HasTopTransom)
                {
                    var trTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(topZ)), -90, "l", W);
                    if (trTop != null) placedIds.Add(trTop.Id.GetIdValue());
                }

                // Giằng ngang đỡ mâm lặp theo số tầng (Z = jackH + 1730 + s * 1800)
                for (int s = 0; s < storyCount; s++)
                {
                    double sDeck = z0 + jackH + 1730 + s * 1800.0;
                    var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(sDeck)), -90, "l", W);
                    if (trDeck != null) placedIds.Add(trDeck.Id.GetIdValue());
                }

                // Lan can đầu hồi ngoài cùng tại các tầng sàn
                if (isOuterEnd)
                {
                    for (int s = 0; s < storyCount; s++)
                    {
                        double sOff = s * 1800.0;
                        double sH6 = z0 + jackH + 2180 + sOff;
                        double sH7 = z0 + jackH + 2630 + sOff;
                        double sDeck = z0 + jackH + 1730 + sOff;

                        var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(sH6)), -90, "l", W);
                        if (rail1 != null) placedIds.Add(rail1.Id.GetIdValue());

                        var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(sH7)), -90, "l", W);
                        if (rail2 != null) placedIds.Add(rail2.Id.GetIdValue());

                        if (sym7 != null)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(sDeck)), -90, "l", W);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// DỰNG CÁC CẤU KIỆN DỌC TRONG 1 NHỊP TIÊU CHUẨN
        /// </summary>
        private static void BuildStandardBayElements(Document doc, Level level, List<int> placedIds,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            double px, double y0, double z0, double W, double jackH, double L_i, bool buildFrontRail,
            BuildOptions opts = null)
        {
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            // Ván lót đáy Family 1 (Trước & Sau) - đặt 1 lần ở cao độ Z0
            var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "l", L_i);
            var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_i);
            if (bb1 != null) placedIds.Add(bb1.Id.GetIdValue());
            if (bb2 != null) placedIds.Add(bb2.Id.GetIdValue());

            // Giằng chân dọc tại h1 (Z = jackH + 135) - Đầy đủ cả trước và sau
            double zH1 = z0 + jackH + 135;
            var lgFoot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", L_i);
            var lgFoot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", L_i);
            if (lgFoot1 != null) placedIds.Add(lgFoot1.Id.GetIdValue());
            if (lgFoot2 != null) placedIds.Add(lgFoot2.Id.GetIdValue());

            // Giằng đỉnh dọc tại topZ (logic i hệt giằng chân)
            if (opts != null && opts.HasTopTransom)
            {
                var lgTop1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(topZ)), 0, "l", L_i);
                var lgTop2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(topZ)), 0, "l", L_i);
                if (lgTop1 != null) placedIds.Add(lgTop1.Id.GetIdValue());
                if (lgTop2 != null) placedIds.Add(lgTop2.Id.GetIdValue());
            }

            // Lặp theo số tầng (Sàn, Giằng giữa, Giằng chéo, Mâm, Ván chắn, Lan can)
            for (int s = 0; s < storyCount; s++)
            {
                double sOff = s * 1800.0;

                // Giằng dọc trung gian Family 4 tại h3 (mặt trước)
                double zH3 = z0 + jackH + 830 + sOff;
                var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH3)), 0, "l", L_i);
                if (lgMid != null) placedIds.Add(lgMid.Id.GetIdValue());

                // Giằng chéo Family 5 (mặt sau Y = -W)
                PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, y0 - W, 0, L_i, z0, jackH, s, storyCount, opts);

                // Lan can mặt trước Family 4 bảo vệ sàn (h6 và h7)
                if (buildFrontRail)
                {
                    double zH6 = z0 + jackH + 2180 + sOff;
                    var railFront1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), 0, "l", L_i);
                    if (railFront1 != null) placedIds.Add(railFront1.Id.GetIdValue());

                    double zH7 = z0 + jackH + 2630 + sOff;
                    var railFrontTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), 0, "l", L_i);
                    if (railFrontTop != null) placedIds.Add(railFrontTop.Id.GetIdValue());
                }

                // Mâm thao tác Family 6 tại tầng sàn s (tự động co giãn theo W)
                if (sym6 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double pw = GetPlankWidth(W);
                    double pOff = GetPlankOffset(W);
                    var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - pOff), MmToFt(zDeck)), 0, "l", L_i);
                    if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }

                    var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - W + pOff), MmToFt(zDeck)), 0, "l", L_i);
                    if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                }

                // Ván chắn biên Family 7 tại tầng sàn s
                if (sym7 != null)
                {
                    double zDeck = z0 + jackH + 1730 + sOff;
                    if (buildFrontRail)
                    {
                        var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_i);
                        if (tbFront != null) placedIds.Add(tbFront.Id.GetIdValue());
                    }
                    var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_i);
                    if (tbBack != null) placedIds.Add(tbBack.Id.GetIdValue());
                }
            }
        }

        /// <summary>
        /// Dựng tiếp các nhịp thứ 2 trở đi (j = 1 .. L2Bays.Count - 1) cho nhánh rẽ hướng +Y (TH 1.1, TH 1.2, TH 2.1).
        /// </summary>
        private static void BuildReturnContinuationBaysPositiveY(Document doc, Level level, List<int> placedIds,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            double xLeft, double xRight, double yStartAfterFirstBay, double z0, double W, double jackH, int storyCount, double topZ,
            List<double> l2Bays, BuildOptions opts)
        {
            if (l2Bays == null || l2Bays.Count <= 1) return;

            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double curY = yStartAfterFirstBay;

            for (int j = 1; j < l2Bays.Count; j++)
            {
                double L_j = l2Bays[j];
                double yNext = curY + L_j;
                bool isLastBay = (j == l2Bays.Count - 1);

                var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(z0)), 90, "l", L_j);
                var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(z0)), 90, "l", L_j);
                if (bb1 != null) placedIds.Add(bb1.Id.GetIdValue());
                if (bb2 != null) placedIds.Add(bb2.Id.GetIdValue());

                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xRight), MmToFt(yNext), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.GetIdValue());
                if (jk2 != null) placedIds.Add(jk2.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xLeft, yNext, z0 + jackH, opts);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xRight, yNext, z0 + jackH, opts);

                double zH1 = z0 + jackH + 135;
                var lgF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(zH1)), 90, "l", L_j);
                var lgF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(zH1)), 90, "l", L_j);
                var trF  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH1)), 0, "l", W);
                if (lgF1 != null) placedIds.Add(lgF1.Id.GetIdValue());
                if (lgF2 != null) placedIds.Add(lgF2.Id.GetIdValue());
                if (trF  != null) placedIds.Add(trF.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgT1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(topZ)), 90, "l", L_j);
                    var lgT2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(topZ)), 90, "l", L_j);
                    var trT  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(topZ)), 0, "l", W);
                    if (lgT1 != null) placedIds.Add(lgT1.Id.GetIdValue());
                    if (lgT2 != null) placedIds.Add(lgT2.Id.GetIdValue());
                    if (trT  != null) placedIds.Add(trT.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(zH3)), 90, "l", L_j);
                    if (lgM1 != null) placedIds.Add(lgM1.Id.GetIdValue());

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xRight, curY, 90, L_j, z0, jackH, s, storyCount, opts);

                    var trD = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zDeck)), 0, "l", W);
                    if (trD != null) placedIds.Add(trD.Id.GetIdValue());

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(curY), MmToFt(zDeck)), 90, "l", L_j);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(curY), MmToFt(zDeck)), 90, "l", L_j);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66), MmToFt(curY), MmToFt(zDeck)), 90, "l", L_j);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66), MmToFt(curY), MmToFt(zDeck)), 90, "l", L_j);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                        if (isLastBay)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zDeck)), 0, "l", W);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                        }
                    }

                    var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(zH6)), 90, "l", L_j);
                    var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(zH7)), 90, "l", L_j);
                    if (rL1 != null) placedIds.Add(rL1.Id.GetIdValue());
                    if (rL2 != null) placedIds.Add(rL2.Id.GetIdValue());

                    if (isLastBay)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH6)), 0, "l", W);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH7)), 0, "l", W);
                        if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                        if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                    }
                }

                curY = yNext;
            }
        }

        /// <summary>
        /// Dựng tiếp các nhịp thứ 2 trở đi (j = 1 .. L2Bays.Count - 1) cho nhánh rẽ hướng -Y (TH 1.3, TH 1.4, TH 2.2).
        /// </summary>
        private static void BuildReturnContinuationBaysNegativeY(Document doc, Level level, List<int> placedIds,
            FamilySymbol sym1, FamilySymbol sym2, FamilySymbol sym3, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            double xLeft, double xRight, double yStartAfterFirstBay, double z0, double W, double jackH, int storyCount, double topZ,
            List<double> l2Bays, BuildOptions opts)
        {
            if (l2Bays == null || l2Bays.Count <= 1) return;

            double pw = GetPlankWidth(W);
            double pOff = GetPlankOffset(W);
            double curY = yStartAfterFirstBay;

            for (int j = 1; j < l2Bays.Count; j++)
            {
                double L_j = l2Bays[j];
                double yNext = curY - L_j;
                bool isLastBay = (j == l2Bays.Count - 1);

                var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(z0)), -90, "l", L_j);
                var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(z0)), -90, "l", L_j);
                if (bb1 != null) placedIds.Add(bb1.Id.GetIdValue());
                if (bb2 != null) placedIds.Add(bb2.Id.GetIdValue());

                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xRight), MmToFt(yNext), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.GetIdValue());
                if (jk2 != null) placedIds.Add(jk2.Id.GetIdValue());

                PlaceConfiguredPosts(doc, sym3, level, placedIds, xLeft, yNext, z0 + jackH, opts);
                PlaceConfiguredPosts(doc, sym3, level, placedIds, xRight, yNext, z0 + jackH, opts);

                double zH1 = z0 + jackH + 135;
                var lgF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(zH1)), -90, "l", L_j);
                var lgF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(zH1)), -90, "l", L_j);
                var trF  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH1)), 0, "l", W);
                if (lgF1 != null) placedIds.Add(lgF1.Id.GetIdValue());
                if (lgF2 != null) placedIds.Add(lgF2.Id.GetIdValue());
                if (trF  != null) placedIds.Add(trF.Id.GetIdValue());

                if (opts != null && opts.HasTopTransom)
                {
                    var lgT1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(curY), MmToFt(topZ)), -90, "l", L_j);
                    var lgT2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(topZ)), -90, "l", L_j);
                    var trT  = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(topZ)), 0, "l", W);
                    if (lgT1 != null) placedIds.Add(lgT1.Id.GetIdValue());
                    if (lgT2 != null) placedIds.Add(lgT2.Id.GetIdValue());
                    if (trT  != null) placedIds.Add(trT.Id.GetIdValue());
                }

                for (int s = 0; s < storyCount; s++)
                {
                    double sOff = s * 1800.0;
                    double zH3 = z0 + jackH + 830 + sOff;
                    double zDeck = z0 + jackH + 1730 + sOff;
                    double zH6 = z0 + jackH + 2180 + sOff;
                    double zH7 = z0 + jackH + 2630 + sOff;

                    var lgM2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(zH3)), -90, "l", L_j);
                    if (lgM2 != null) placedIds.Add(lgM2.Id.GetIdValue());

                    PlaceBayDiagonalBraces(doc, sym5, level, placedIds, xLeft, curY, -90, L_j, z0, jackH, s, storyCount, opts);

                    var trD = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zDeck)), 0, "l", W);
                    if (trD != null) placedIds.Add(trD.Id.GetIdValue());

                    if (sym6 != null)
                    {
                        var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLeft + pOff), MmToFt(curY), MmToFt(zDeck)), -90, "l", L_j);
                        var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xRight - pOff), MmToFt(curY), MmToFt(zDeck)), -90, "l", L_j);
                        if (pl1 != null) { SetParam(pl1, "w", pw); placedIds.Add(pl1.Id.GetIdValue()); }
                        if (pl2 != null) { SetParam(pl2, "w", pw); placedIds.Add(pl2.Id.GetIdValue()); }
                    }

                    if (sym7 != null)
                    {
                        var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft + 66), MmToFt(curY), MmToFt(zDeck)), -90, "l", L_j);
                        var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xRight - 66), MmToFt(curY), MmToFt(zDeck)), -90, "l", L_j);
                        if (tbL != null) placedIds.Add(tbL.Id.GetIdValue());
                        if (tbR != null) placedIds.Add(tbR.Id.GetIdValue());

                        if (isLastBay)
                        {
                            var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zDeck)), 0, "l", W);
                            if (tbEnd != null) placedIds.Add(tbEnd.Id.GetIdValue());
                        }
                    }

                    var rR1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(zH6)), -90, "l", L_j);
                    var rR2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xRight), MmToFt(curY), MmToFt(zH7)), -90, "l", L_j);
                    if (rR1 != null) placedIds.Add(rR1.Id.GetIdValue());
                    if (rR2 != null) placedIds.Add(rR2.Id.GetIdValue());

                    if (isLastBay)
                    {
                        var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH6)), 0, "l", W);
                        var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLeft), MmToFt(yNext), MmToFt(zH7)), 0, "l", W);
                        if (rE1 != null) placedIds.Add(rE1.Id.GetIdValue());
                        if (rE2 != null) placedIds.Add(rE2.Id.GetIdValue());
                    }
                }

                curY = yNext;
            }
        }

        public static double GetPlankWidth(double bayWidth)
        {
            double w = bayWidth > 0 ? bayWidth : 1200.0;
            return Math.Max(150.0, (w - 200.0) / 2.0);
        }

        public static double GetPlankOffset(double bayWidth)
        {
            double pw = GetPlankWidth(bayWidth);
            return 95.0 + pw / 2.0;
        }

        private static void ConfigureAlinco2700(FamilyInstance inst)
        {
            if (inst == null) return;
            SetParam(inst, "h", 2700);
            SetParam(inst, "h1", 135);
            SetParam(inst, "h2", 380);
            SetParam(inst, "h3", 830);
            SetParam(inst, "h4", 1280);
            SetParam(inst, "h5", 1730);
            SetParam(inst, "h6", 2180);
            SetParam(inst, "h7", 2630);
            for (int i = 8; i <= 15; i++) SetParam(inst, "h" + i, 0);
        }

        private static void ConfigureAlincoColumn(FamilyInstance inst, double height, bool isBottomPost = true)
        {
            if (inst == null) return;
            SetParam(inst, "h", height);

            switch ((int)height)
            {
                case 225:
                    SetParam(inst, "h1", 155);
                    for (int i = 2; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 450:
                    if (isBottomPost)
                    {
                        SetParam(inst, "h1", 135);
                        SetParam(inst, "h2", 380);
                        for (int i = 3; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    }
                    else
                    {
                        SetParam(inst, "h1", 380);
                        for (int i = 2; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    }
                    break;
                case 675:
                    SetParam(inst, "h1", 155);
                    SetParam(inst, "h2", 380);
                    SetParam(inst, "h3", 605);
                    for (int i = 4; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 900:
                    SetParam(inst, "h1", 135);
                    SetParam(inst, "h2", 380);
                    SetParam(inst, "h3", 830);
                    for (int i = 4; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 1350:
                    SetParam(inst, "h1", 135);
                    SetParam(inst, "h2", 380);
                    SetParam(inst, "h3", 830);
                    SetParam(inst, "h4", 1280);
                    for (int i = 5; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 1800:
                    SetParam(inst, "h1", 380);
                    SetParam(inst, "h2", 830);
                    SetParam(inst, "h3", 1280);
                    SetParam(inst, "h4", 1730);
                    for (int i = 5; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 2700:
                    ConfigureAlinco2700(inst);
                    break;
                case 3600:
                    SetParam(inst, "h1", 380);
                    SetParam(inst, "h2", 830);
                    SetParam(inst, "h3", 1280);
                    SetParam(inst, "h4", 1730);
                    SetParam(inst, "h5", 2180);
                    SetParam(inst, "h6", 2630);
                    SetParam(inst, "h7", 3080);
                    SetParam(inst, "h8", 3530);
                    for (int i = 9; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                default:
                    SetParam(inst, "h1", 135);
                    int idx = 2;
                    double zRos = 380;
                    while (zRos < height && idx <= 15)
                    {
                        SetParam(inst, "h" + idx, zRos);
                        zRos += 450;
                        idx++;
                    }
                    for (int i = idx; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
            }
        }

        public static void PlaceBayDiagonalBraces(Document doc, FamilySymbol sym5, Level level, List<int> placedIds,
            double px, double py, double rotDeg, double spanL, double z0, double jackH, int s, int storyCount, BuildOptions opts)
        {
            if (sym5 == null) return;

            double totalPostH = GetTotalPostHeight(opts);
            double colTopZ = z0 + jackH + totalPostH;

            if (s == 0)
            {
                double zH2 = z0 + jackH + 380.0;
                if (opts == null || !opts.IsFromData || zH2 + 900.0 <= colTopZ + 10.0)
                {
                    var brcFoot = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(px), MmToFt(py), MmToFt(zH2)), rotDeg, "l", spanL);
                    if (brcFoot != null) { SetParam(brcFoot, "h", 900.0); placedIds.Add(brcFoot.Id.GetIdValue()); }
                }
            }

            double zDeck = z0 + jackH + 1730.0 + s * 1800.0;
            if (opts == null || !opts.IsFromData || zDeck + 900.0 <= colTopZ + 10.0)
            {
                var brcDeck = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(px), MmToFt(py), MmToFt(zDeck)), rotDeg, "l", spanL);
                if (brcDeck != null) { SetParam(brcDeck, "h", 900.0); placedIds.Add(brcDeck.Id.GetIdValue()); }
            }
        }

        public static void PlaceBayDiagonalBraces(Document doc, FamilySymbol sym5, Level level, List<int> placedIds,
            double px, double py, double rotDeg, double spanL, double z0, double jackH, BuildOptions opts)
        {
            PlaceBayDiagonalBraces(doc, sym5, level, placedIds, px, py, rotDeg, spanL, z0, jackH, 0, 1, opts);
        }

        public static double GetTotalPostHeight(BuildOptions opts)
        {
            if (opts == null || !opts.IsFromData) return 2700.0;
            double total = 0.0;
            if (opts.Post1Height > 0) total += opts.Post1Height;
            if (opts.Post2Height > 0) total += opts.Post2Height;
            if (opts.Post3Height > 0 && opts.Post3Count > 0) total += (opts.Post3Height * opts.Post3Count);
            if (opts.Post4Height > 0) total += opts.Post4Height;
            if (opts.Post5Height > 0) total += opts.Post5Height;
            return total;
        }

        private static void PlaceConfiguredPosts(Document doc, FamilySymbol sym3, Level level, List<int> placedIds, double cx, double cy, double baseZ, BuildOptions opts)
        {
            if (sym3 == null) return;
            if (opts == null || !opts.IsFromData)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(baseZ)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.GetIdValue()); }
                return;
            }

            double curZ = baseZ;
            if (opts.Post1Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post1Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post1Height, true); placedIds.Add(col.Id.GetIdValue()); }
                curZ += opts.Post1Height;
            }
            if (opts.Post2Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post2Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post2Height, false); placedIds.Add(col.Id.GetIdValue()); }
                curZ += opts.Post2Height;
            }
            if (opts.Post3Height > 0 && opts.Post3Count > 0)
            {
                for (int i = 0; i < opts.Post3Count; i++)
                {
                    var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post3Height);
                    if (col != null) { ConfigureAlincoColumn(col, opts.Post3Height, false); placedIds.Add(col.Id.GetIdValue()); }
                    curZ += opts.Post3Height;
                }
            }
            if (opts.Post4Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post4Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post4Height, false); placedIds.Add(col.Id.GetIdValue()); }
                curZ += opts.Post4Height;
            }
            if (opts.Post5Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post5Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post5Height, false); placedIds.Add(col.Id.GetIdValue()); }
                curZ += opts.Post5Height;
            }
        }

        private static void ActivateSymbol(Document doc, FamilySymbol sym)
        {
            if (!sym.IsActive)
            {
                sym.Activate();
                doc.Regenerate();
            }
        }

        private static FamilyInstance PlaceInstance(Document doc, FamilySymbol symbol, Level level, XYZ pt, double rotDeg, string paramName, double paramValMm)
        {
            FamilyInstance inst = level != null ?
                doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.NonStructural) :
                doc.Create.NewFamilyInstance(pt, symbol, StructuralType.NonStructural);

            if (inst != null)
            {
                Parameter pOffset = inst.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)
                                 ?? inst.LookupParameter("Elevation from Level")
                                 ?? inst.LookupParameter("Offset");
                if (pOffset != null && !pOffset.IsReadOnly)
                {
                    double targetOffset = (level != null) ? (pt.Z - level.Elevation) : pt.Z;
                    pOffset.Set(targetOffset);
                }

                if (Math.Abs(rotDeg) > 1e-6)
                {
                    Line axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, inst.Id, axis, rotDeg * Math.PI / 180.0);
                }

                if (!string.IsNullOrEmpty(paramName))
                {
                    SetParam(inst, paramName, paramValMm);
                }
            }
            return inst;
        }

        private static void SetParam(FamilyInstance inst, string name, double mmVal)
        {
            if (inst == null) return;
            Parameter p = inst.LookupParameter(name);
            if (p != null && !p.IsReadOnly)
            {
                if (p.StorageType == StorageType.Double)
                {
                    p.Set(MmToFt(mmVal));
                }
                else if (p.StorageType == StorageType.Integer)
                {
                    p.Set((int)Math.Round(mmVal));
                }
            }
        }

        private static FamilySymbol GetOrLoadFamilySymbol(Document doc, string familyName, string folder)
        {
            var sym = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase) ||
                                     s.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
            if (sym != null) return sym;

            if (Directory.Exists(folder))
            {
                string rfaPath = Path.Combine(folder, familyName + ".rfa");
                if (File.Exists(rfaPath))
                {
                    Family fam;
                    if (doc.LoadFamily(rfaPath, out fam) && fam != null)
                    {
                        var symId = fam.GetFamilySymbolIds().FirstOrDefault();
                        if (symId != null && symId != ElementId.InvalidElementId)
                        {
                            return doc.GetElement(symId) as FamilySymbol;
                        }
                    }
                }
            }
            return null;
        }
    }
}
