using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitScaffoldingTool
{
    public class BuildOptions
    {
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double Z0 { get; set; }
        public double BayWidth { get; set; }
        public double JackHeight { get; set; }
        public List<double> BayLengths { get; set; }
        public int Mode { get; set; } // -1: Dãy thẳng nhiều nhịp, -2: Trường hợp 1.1 (Khớp nối góc 90° +Y)
        public string FamilyFolder { get; set; }

        public double Post1Height { get; set; }
        public double Post2Height { get; set; }
        public int Post3Count { get; set; }
        public double Post3Height { get; set; }
        public double Post4Height { get; set; }
        public double Post5Height { get; set; }
        public int StoryCount { get; set; }
        public bool HasTopTransom { get; set; }

        public BuildOptions()
        {
            X0 = 0;
            Y0 = 0;
            Z0 = 0;
            BayWidth = 1200;
            JackHeight = 250;
            BayLengths = new List<double> { 1800, 1200, 1800, 1200, 1800 };
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

    public static class ScaffoldingBuilder
    {
        public static double MmToFt(double mm) { return mm / 304.8; }

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

            string txName = opts.Mode == -8 ? "Dung Gian Giao - Master 6 Corners Lien Tuc" :
                                              opts.Mode == -2 ? "Dựng Giàn Giáo - Trường Hợp 1.1 (Đủ Giằng Chân & Giằng Đỉnh)" :
                                              "Dựng Giàn Giáo - Dãy Thẳng Nhiều Nhịp Tiêu Chuẩn";

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

                    if (opts.Mode == -8)
                    {
                        // MASTER: LIEN TUC DU 6 TRUONG HOP (MOI HE 2 NHIP GIAN)
                        string err = ExecuteMaster6CornersContinuous(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Loi Master 6 Corners: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -2)
                    {
                        // TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP PHẢI PHẲNG)
                        string err = ExecuteCase1_1(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Lỗi Trường Hợp 1.1: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -4)
                    {
                        // TRUONG HOP 1.2: KHOP NOI GOC 90 HUONG +Y (NHIP CUOI = RONG RE = 1200)
                        string err = ExecuteCase1_2(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Loi Truong Hop 1.2: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -5)
                    {
                        // TRUONG HOP 1.3: KHOP NOI GOC 90 HUONG -Y (MEP PHAI PHANG)
                        string err = ExecuteCase1_3(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Loi Truong Hop 1.3: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -6)
                    {
                        // TRUONG HOP 1.4: KHOP NOI GOC 90 HUONG -Y (NHIP CUOI = RONG RE = 1200)
                        string err = ExecuteCase1_4(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Loi Truong Hop 1.4: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -7)
                    {
                        // TRUONG HOP 2.2: KHOP NOI GOC 90 HUONG -Y (MEP TRUOC PHANG - NHOM 2)
                        string err = ExecuteCase2_2(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Loi Truong Hop 2.2: " + err;
                            return res;
                        }
                    }
                    else if (opts.Mode == -3)
                    {
                        // TRƯỜNG HỢP 2.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP SAU PHẲNG - NHÓM 2)
                        string err = ExecuteCase2_1(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Lỗi Trường Hợp 2.1: " + err;
                            return res;
                        }
                    }
                    else
                    {
                        // DÃY THẲNG NHIỀU NHỊP TIÊU CHUẨN
                        string err = ExecuteMultiBay(doc, opts, defaultLevel, placedIds);
                        if (!string.IsNullOrEmpty(err))
                        {
                            tx.RollBack();
                            res.Message = "Lỗi nhiều nhịp thẳng: " + err;
                            return res;
                        }
                    }

                    tx.Commit();

                    sw.Stop();
                    res.Success = true;
                    res.TotalElements = placedIds.Count;
                    res.ElapsedMs = sw.Elapsed.TotalMilliseconds;

                    string title = opts.Mode == -2 ? "Trường Hợp 1.1 (Khớp Nối Góc +Y Mép Phải Phẳng)" :
                                   opts.Mode == -4 ? "Truong Hop 1.2 (Khop Noi Goc +Y Nhip Cuoi 1200)" :
                                   opts.Mode == -5 ? "Truong Hop 1.3 (Khop Noi Goc -Y Mep Phai Phang)" :
                                   opts.Mode == -6 ? "Truong Hop 1.4 (Khop Noi Goc -Y Nhip Cuoi 1200)" :
                                   opts.Mode == -7 ? "Truong Hop 2.2 (Khop Noi Goc -Y Mep Truoc Phang - Nhom 2)" :
                                   opts.Mode == -3 ? "Trường Hợp 2.1 (Khớp Nối Góc +Y Mép Sau Phẳng - Nhóm 2)" :
                                   string.Format("Dãy {0} Nhịp Thẳng Tiêu Chuẩn", opts.BayLengths.Count);

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
        /// TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (CONN-GRP1-TYPE1.1)
        /// NGUYÊN TẮC:
        /// 1. Giằng chân tại h1: ĐỦ 100% ở tất cả các mặt để khóa chân cột.
        /// 2. Giằng đỉnh tại h7: ĐỦ 100% ở tất cả các mặt để khóa đỉnh hệ giàn.
        /// 3. Thân giữa (h3 và h6): Không lắp giằng Family 4 đè lên Family 5.
        /// 4. Mặt sau Nhịp 5 (Y = -1200): Vẫn dài 1800 mm đến X = 7800.
        /// 5. Mặt trước Nhịp 5 (Y = 0): Thu ngắn về 600 mm (X: 6000 -> 6600) kết nối vào cột cấy (6600, 0).
        /// 6. Cửa ngõ Y = 0 (X: 6600 -> 7800): Mở thông suốt không có lan can chắn.
        /// </summary>
        private static string ExecuteCase1_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH 5 NHỊP (NHỊP 1->4 TIÊU CHUẨN, NHỊP 5 ĐẶC BIỆT CÓ CỘT CẤY)
            // =========================================================================
            List<double> colX = new List<double> { x0, x0 + 1800, x0 + 3000, x0 + 4800, x0 + 6000, x0 + 7800 };
            int nCols = colX.Count; // 6 hàng cột

            // 1. Dựng 6 hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isOuterEnd = (k == 0 || k == nCols - 1);

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang dùng chung Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // (Tạm bỏ qua giằng đỉnh ngang tại h7 theo yêu cầu người dùng)

                // Lan can đầu hồi ngoài cùng (tại X = x0 và X = x0 + 7800) ở tầng Sàn 1: ĐẦY ĐỦ 2 TẦNG (h6 và h7)
                if (isOuterEnd)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Dựng các cấu kiện dọc cho 4 nhịp đầu (Nhịp 1 -> 4)
            List<double> first4Bays = new List<double> { 1800, 1200, 1800, 1200 };
            for (int i = 0; i < 4; i++)
            {
                double px = colX[i];
                double L_i = first4Bays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // 3. Dựng cấu kiện Nhịp 5 đặc biệt (X: 6000 -> 7800):
            // - MẶT SAU (Y = -W): Dài đủ 1800 mm từ 6000 -> 7800
            double xBay5 = x0 + 6000;
            double L_bay5 = 1800;

            // Ván lót đáy sau (Y = -W)
            var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Back != null) placedIds.Add(bb5Back.Id.IntegerValue);

            // GIẰNG CHÂN SAU (Y = -W) TẠI h1 - BẮT BUỘC ĐẦY ĐỦ ĐỂ KHÓA CHÂN CỘT
            double zH1_5 = z0 + jackH + 135;
            var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Back != null) placedIds.Add(lgF5Back.Id.IntegerValue);

            // (Tạm bỏ qua giằng đỉnh sau h7 theo yêu cầu)

            // Giằng chéo sau Family 5 (Y = -W)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH2)), 0, "l", L_bay5);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH5_brc)), 0, "l", L_bay5);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // Ván chắn biên sau (Y = -W)
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_bay5);
                if (tbBack5 != null) placedIds.Add(tbBack5.Id.IntegerValue);
            }

            // Mâm thao tác Nhịp 5: 2 mâm 1800 mm từ 6000 -> 7800
            if (sym6 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 345), MmToFt(zDeck)), 0, "l", L_bay5);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 345), MmToFt(zDeck)), 0, "l", L_bay5);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // 4. CẤY CỘT TẠI X = 6600 (MÉP TRƯỚC Y = 0)
            double xC = x0 + 6600;
            var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
            if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

            var colC = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0 + jackH)), 0, null, 0);
            if (colC != null) { ConfigureAlinco2700(colC); placedIds.Add(colC.Id.IntegerValue); }

            // 5. MẶT TRƯỚC (Y = 0) CỦA NHỊP 5: THU NGẮN VỀ 600 MM (X: 6000 -> 6600)
            double L_cut = 600;
            // Ván lót trước đoạn 600 mm
            var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_cut);
            if (bb5Front != null) placedIds.Add(bb5Front.Id.IntegerValue);

            // Giằng chân trước đoạn 600 mm tại h1
            var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_cut);
            if (lgF5Front != null) placedIds.Add(lgF5Front.Id.IntegerValue);

            // Giằng giữa trước đoạn 600 mm tại h3
            double zH3_5 = z0 + jackH + 830;
            var lgMid5 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_cut);
            if (lgMid5 != null) placedIds.Add(lgMid5.Id.IntegerValue);

            // Lan can an toàn trước đoạn 600 mm: ĐẦY ĐỦ 2 TẦNG BẢO VỆ SÀN (h6 và h7)
            double zH6_5 = z0 + jackH + 2180;
            var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_cut);
            if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

            double zH7_5 = z0 + jackH + 2630;
            var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_cut);
            if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

            // Ván chắn biên trước đoạn 600 mm tại Sàn 1
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_cut);
                if (tbFront5 != null) placedIds.Add(tbFront5.Id.IntegerValue);
            }

            // =========================================================================
            // PHẦN B: DỰNG NHỊP RẼ VUÔNG GÓC 90° VƯƠN SANG +Y (1800 x 1200 mm)
            // Tọa độ X: [xC = 6600, xMax = 7800], Y: [y0 = 0 -> yTop = 1800]
            // QUY TẮC:
            // - GIẰNG CHÂN (h1): ĐẦY ĐỦ 100% CẢ 4 MẶT
            // - GIẰNG ĐỈNH (h7): ĐẦY ĐỦ 100% CẢ 4 MẶT
            // - GIỮA THÂN: Không lắp giằng Family 4 tại mặt ngoài X = xMax (nơi có Family 5)
            // =========================================================================
            double xMax = x0 + 7800;
            double yTop = y0 + 1800;
            double L_return = 1800; // Dài theo phương Y
            double W_return = 1200; // Rộng theo phương X (7800 - 6600)

            // 1. Ván lót đáy Family 1 dọc theo phương Y (rot = 90) tại X = xC và X = xMax
            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            // 2. 2 Cụm cột ngoài mới: tại (xC, yTop) và (xMax, yTop)
            XYZ[] retColPts = new XYZ[]
            {
                new XYZ(MmToFt(xC),   MmToFt(yTop), MmToFt(z0)),
                new XYZ(MmToFt(xMax), MmToFt(yTop), MmToFt(z0))
            };
            foreach (var pt in retColPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 3. GIẰNG CHÂN FAMILY 4 TẠI h1 (Z = jackH + 135) - ĐẦY ĐỦ 100% KHÓA CHÂN:
            double zH1_R = z0 + jackH + 135;
            // - 2 Giằng dọc nhịp rẽ theo phương Y (rot = 90): CẢ VÁCH TRONG (xC) VÀ MẶT NGOÀI (xMax)
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);

            // - 2 Giằng ngang theo phương X (rot = 0): tại Y = y0 (thanh chung) và Y = yTop
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // 4. GIẰNG GIỮA h3 FAMILY 4 (Z = jackH + 830) dọc theo Y (rot = 90):
            // - CHỈ ĐẶT TẠI VÁCH TRONG X = xC (nơi KHÔNG có Family 5). Mặt ngoài X = xMax có Family 5 nên KHÔNG đặt!
            double zH3_R = z0 + jackH + 830;
            var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_return);
            if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

            // 5. GIẰNG CHÉO FAMILY 5 TẠI MẶT NGOÀI (X = xMax, rot = 90 dọc theo Y):
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH2)), 90, "l", L_return);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH5_brc)), 90, "l", L_return);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 6. Sàn Thao Tác Sàn 1 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - 2 Giằng ngang đỡ mâm theo phương X (rot = 0) tại Y = y0 và Y = yTop
            var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zDeck_R)), 0, "l", W_return);
            var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
            if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
            if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + 345), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7
            if (sym7 != null)
            {
                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ sàn (h6 và h7) - ĐẦY ĐỦ 2 TẦNG (KÉP):
            // - Đặt ở vách trong xC và đầu hồi yTop (nơi KHÔNG có Family 5)
            double zH6_R = z0 + jackH + 2180;
            var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
            var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
            if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
            if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

            double zH7_R = z0 + jackH + 2630;
            var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
            var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
            if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
            if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);

            return null;
        }

        /// <summary>
        /// TRƯỜNG HỢP 2.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP SAU PHẲNG - CONN-GRP2-TYPE2.1)
        /// NGUYÊN TẮC HÌNH HỌC:
        /// 1. Dãy chính 5 nhịp giữ nguyên hoàn toàn (1800, 1200, 1800, 1200, 1800 mm = 7800 mm).
        ///    Nhịp 5 là nhịp chuẩn đủ 1800 mm (X: 6000 -> 7800) ở cả mặt trước và mặt sau.
        /// 2. Nhánh rẽ góc vuông 90° đặt phía NGOÀI đầu hồi: X ∈ [x0 + 7800, x0 + 9000], rộng W = 1200 mm.
        /// 3. Mép sau phẳng (Flush Rear Edge): Mép sau nhánh rẽ nằm tại Y = y0 - W (Y = -1200).
        /// 4. Nhánh rẽ vươn sang +Y: Chiều dài L = 1800 mm dọc theo Y (từ Y = -1200 đến Y = +600).
        ///    Nhô cao hơn mép trước dãy chính một đoạn 600 mm (từ Y = 0 đến Y = +600).
        /// 5. Cửa ngõ giao thoa: Đoạn X = x0 + 7800 giữa Y = -1200 và Y = 0 giải phóng 100% lan can/ván chắn.
        /// 6. Giằng chéo Family 5: Bố trí ở mép ngoài bên phải X = x0 + 9000.
        ///    Theo quy tắc: Tại X = x0 + 9000 TUYỆT ĐỐI KHÔNG có Family 4 ở h3 và h6.
        /// 7. Lan can kép h6 và h7 bảo vệ đầy đủ các mép hở:
        ///    - Mép sau Y = -1200 (X: 7800 -> 9000)
        ///    - Đầu hồi trước Y = +600 (X: 7800 -> 9000)
        ///    - Vách trong nhô cao X = 7800 (Y: 0 -> 600)
        /// </summary>
        // Case 1.2 method implementation for ScaffoldingBuilder.cs
        /// <summary>
        /// TRƯỜNG HỢP 1.2: KHỚP NỐI GÓC 90° HƯỚNG +Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM - CONN-GRP1-TYPE1.2)
        /// NGUYÊN TẮC HÌNH HỌC:
        /// 1. Chiều dài nhịp cuối dãy chính bằng đúng chiều rộng nhánh rẽ (L_last = W = 1200 mm).
        ///    Nhịp 5 tạo thành ô vuông 1200 x 1200 mm (X: 6000 -> 7200, Y: -1200 -> 0).
        /// 2. Không cấy cột phụ: 2 cột mặt trước (6000, 0) và (7200, 0) dùng chung trực tiếp làm cột gốc nhánh rẽ.
        /// 3. Cửa ngõ giao thoa Y = 0 (X: 6000 -> 7200): Mở trọn vẹn 1200 mm, không có lan can/ván chắn/giằng giữa cản trở.
        /// 4. Nhánh rẽ vươn sang +Y: Kích thước 1800 x 1200 mm (X: 6000 -> 7200, Y: 0 -> 1800).
        /// 5. Mép ngoài phẳng X = 7200: Là mặt biên ngoài liên tục của cả Nhịp 5 và nhánh rẽ.
        ///    Tại mép ngoài nhánh rẽ X = 7200 (Y: 0 -> 1800): Bố trí giằng chéo Family 5, TUYỆT ĐỐI KHÔNG lắp Family 4 tại h3 và h6.
        /// 6. Giằng chân h1: Đầy đủ 100% ở tất cả các mặt đáy để khóa cứng khung giàn.
        /// </summary>
        private static string ExecuteCase1_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH VỚI NHỊP CUỐI = W (1200 MM)
            // Nhịp 1..4: 1800, 1200, 1800, 1200. Nhịp 5: 1200 mm (Square 1200x1200 mm)
            // =========================================================================
            List<double> mainBays;
            if (opts.BayLengths != null && opts.BayLengths.Count > 0)
            {
                mainBays = new List<double>(opts.BayLengths);
                mainBays[mainBays.Count - 1] = W; // Nhịp cuối luôn bằng W (1200 mm)
            }
            else
            {
                mainBays = new List<double> { 1800, 1200, 1800, 1200, W };
            }

            int nBays = mainBays.Count;
            List<double> colX = new List<double>();
            double curX = x0;
            colX.Add(curX);
            for (int i = 0; i < nBays; i++)
            {
                curX += mainBays[i];
                colX.Add(curX);
            }
            int nCols = colX.Count; // nBays + 1 hàng cột (mặc định 6 hàng: 0, 1800, 3000, 4800, 6000, 7200)

            // 1. Dựng tất cả các hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];

                // Kích chân Family 2 (Trước Y = y0 & Sau Y = y0 - W)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Lan can đầu hồi:
                // - Đầu hồi trái ngoài cùng (k = 0): Đầy đủ 2 tầng kép (h6, h7) và ván chắn biên
                // - Đầu hồi phải dãy chính (k = nCols - 1): Mép ngoài Y in [-W, 0] là mép hở sàn -> bảo vệ đầy đủ h6, h7
                if (k == 0 || k == nCols - 1)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Dựng các nhịp tiêu chuẩn ban đầu (Nhịp 0 .. nBays - 2)
            for (int i = 0; i < nBays - 1; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // 3. Dựng cấu kiện Nhịp cuối (Nhịp nBays - 1, chiều dài đúng bằng W = 1200 mm):
            double xLast = colX[nBays - 1]; // X = 6000
            double L_last = mainBays[nBays - 1]; // 1200 mm

            // --- MẶT SAU (Y = -W = -1200):
            // Ván lót đáy sau Family 1
            var bbLastBack = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_last);
            if (bbLastBack != null) placedIds.Add(bbLastBack.Id.IntegerValue);

            // Giằng chân sau Family 4 tại h1 (Khóa chân cột)
            double zH1_last = z0 + jackH + 135;
            var lgFLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastBack != null) placedIds.Add(lgFLastBack.Id.IntegerValue);

            // Giằng chéo sau Family 5 tại h2 và h5 (mặt có giằng chéo -> KHÔNG đặt Family 4 tại h3, h6)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(zH2)), 0, "l", L_last);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(zH5_brc)), 0, "l", L_last);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // Ván chắn biên sau Family 7
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbBackLast = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_last);
                if (tbBackLast != null) placedIds.Add(tbBackLast.Id.IntegerValue);
            }

            // Mâm thao tác Nhịp cuối: 2 mâm 1200 mm tại X: 6000 -> 7200
            if (sym6 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast), MmToFt(y0 - 345), MmToFt(zDeck)), 0, "l", L_last);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W + 345), MmToFt(zDeck)), 0, "l", L_last);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // --- MẶT TRƯỚC (Y = 0, X: 6000 -> 7200):
            // Cửa ngõ mở thông suốt 1200 mm sang nhánh rẽ!
            // - Giằng chân Family 4 tại h1: Khóa 2 chân cột (6000, 0) và (7200, 0) ở sát mặt đất
            var lgFLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastFront != null) placedIds.Add(lgFLastFront.Id.IntegerValue);

            // - Giằng ngang đỡ mâm Family 4 tại h5 (cao trình sàn) để đỡ mép sàn liên kết
            double zH5_last = z0 + jackH + 1730;
            var trDeckFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH5_last)), 0, "l", L_last);
            if (trDeckFront != null) placedIds.Add(trDeckFront.Id.IntegerValue);

            // (LƯU Ý QUAN TRỌNG: Tại Y = 0 từ 6000 đến 7200, TUYỆT ĐỐI KHÔNG lắp giằng giữa h3, lan can h6, h7 hay ván chắn biên để thông lối đi 100%)

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ VUÔNG GÓC 90° VƯƠN SANG +Y (1800 x 1200 mm)
            // Tọa độ X: [xLast = 6000, xMax = 7200], Y: [y0 = 0 -> yTop = 1800]
            // Không cấy cột: Chân nhánh rẽ tựa trực tiếp vào cột (6000, 0) và (7200, 0)
            // =========================================================================
            double xMax = colX[nCols - 1]; // 7200
            double L_return = 1800; // Chiều dài nhánh rẽ theo Y
            double yTop = y0 + L_return; // +1800 mm
            double W_return = W; // 1200 mm

            // 1. Ván lót đáy Family 1 dọc theo Y (rot = 90) tại X = xLast và X = xMax
            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            // 2. Chỉ cần 2 Cột ngoài mới tại đỉnh nhánh rẽ: (xLast, yTop) và (xMax, yTop)
            XYZ[] retColPts = new XYZ[]
            {
                new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(z0)),
                new XYZ(MmToFt(xMax),  MmToFt(yTop), MmToFt(z0))
            };
            foreach (var pt in retColPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 3. GIẰNG CHÂN FAMILY 4 TẠI h1 (Z = jackH + 135) - ĐẦY ĐỦ KHÓA CHÂN:
            double zH1_R = z0 + jackH + 135;
            // - 2 Giằng dọc nhánh rẽ theo Y (rot = 90): Vách trong (xLast) và Mép ngoài (xMax)
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);

            // - Giằng ngang đầu hồi đỉnh tại Y = yTop (rot = 0)
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH1_R)), 0, "l", W_return);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // 4. GIẰNG GIỮA h3 FAMILY 4 (Z = jackH + 830) dọc theo Y (rot = 90):
            // - CHỈ ĐẶT TẠI VÁCH TRONG X = xLast (nơi KHÔNG có Family 5). Mép ngoài X = xMax có Family 5 nên KHÔNG đặt!
            double zH3_R = z0 + jackH + 830;
            var lgRM1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_return);
            if (lgRM1 != null) placedIds.Add(lgRM1.Id.IntegerValue);

            // 5. GIẰNG CHÉO FAMILY 5 TẠI MÉP NGOÀI (X = xMax, rot = 90 dọc theo Y):
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH2)), 90, "l", L_return);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xMax), MmToFt(y0), MmToFt(zH5_brc)), 90, "l", L_return);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 6. Sàn Thao Tác Sàn 1 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - Giằng ngang đỡ mâm đầu hồi tại Y = yTop (rot = 0)
            var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
            if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + 345), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7
            if (sym7 != null)
            {
                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zDeck_R)), 0, "l", W_return);
                if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ sàn (h6 và h7) - ĐẦY ĐỦ 2 TẦNG (KÉP):
            // - Đặt ở vách trong xLast và đầu hồi yTop (nơi KHÔNG có Family 5)
            // - Mép ngoài xMax có Family 5 nên KHÔNG lắp Family 4 tại h6!
            double zH6_R = z0 + jackH + 2180;
            var rL1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_return);
            var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH6_R)), 0, "l", W_return);
            if (rL1 != null) placedIds.Add(rL1.Id.IntegerValue);
            if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);

            double zH7_R = z0 + jackH + 2630;
            var rL2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_return);
            var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yTop), MmToFt(zH7_R)), 0, "l", W_return);
            if (rL2 != null) placedIds.Add(rL2.Id.IntegerValue);
            if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);

            return null;
        }


        /// <summary>
        /// TRƯỜNG HỢP 1.3: KHỚP NỐI GÓC 90° HƯỚNG -Y (MÉP PHẢI PHẲNG - CONN-GRP1-TYPE1.3)
        /// HIỆU CHỈNH CHUẨN XÁC:
        /// 1. Hệ giàn 1 (Dãy chính): Nhịp 5 mép sau (Y = -1200) tại đoạn cắt 600 mm (X: 6000->6600)
        ///    THU NGẮN GIẰNG CHÉO Family 5 về 600 mm (KHÔNG đổi thành giằng dọc).
        ///    Tuyệt đối KHÔNG lắp Family 4 (h3, h6) ở đoạn 600 mm này vì đã có giằng chéo.
        /// 2. Cửa ngõ Y = -1200 (X: 6600 -> 7800): Mở thông suốt 1200 mm làm lối đi sang hệ giàn 2.
        /// 3. Hệ giàn 2 (Nhánh rẽ -Y, X in [6600, 7800], Y in [-3000, -1200]):
        ///    - Vách trong X = 6600 (xC): Là mặt giằng chéo tiếp nối từ mép sau dãy chính -> Lắp GIẰNG CHÉO Family 5 (L = 1800, H = 900).
        ///      Tuyệt đối KHÔNG lắp Family 4 tại h3 và h6 ở vách trong X = 6600.
        ///    - Mép ngoài X = 7800 (xMax): Là mặt giằng dọc / lan can tiếp nối từ mặt trước dãy chính -> Lắp GIẰNG DỌC Family 4 tại h3
        ///      và LAN CAN KÉP Family 4 tại h6, h7 (L = 1800).
        ///    - Đầu hồi đáy Y = -3000: Lắp lan can kép Family 4 tại h6, h7 và ván chắn biên Family 7 (L = 1200).
        /// 4. Giằng chân h1: Đầy đủ 100% ở tất cả các mặt đáy để khóa cứng chân cột.
        /// </summary>
        private static string ExecuteCase1_3(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH 5 NHỊP (NHỊP 1->4 TIÊU CHUẨN, NHỊP 5 CẮT MÉP SAU Y = -W)
            // =========================================================================
            List<double> colX = new List<double> { x0, x0 + 1800, x0 + 3000, x0 + 4800, x0 + 6000, x0 + 7800 };
            int nCols = colX.Count; // 6 hàng cột

            // 1. Dựng 6 hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isOuterEnd = (k == 0 || k == nCols - 1);

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Lan can đầu hồi ngoài cùng:
                // - Đầu hồi trái (k = 0): Đầy đủ 2 tầng kép h6, h7
                // - Đầu hồi phải dãy chính (k = nCols - 1): Mép ngoài Y in [-W, 0] là mép hở sàn -> bảo vệ h6, h7
                if (isOuterEnd)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Dựng các cấu kiện dọc cho 4 nhịp đầu (Nhịp 1 -> 4)
            List<double> first4Bays = new List<double> { 1800, 1200, 1800, 1200 };
            for (int i = 0; i < 4; i++)
            {
                double px = colX[i];
                double L_i = first4Bays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // 3. Dựng cấu kiện Nhịp 5 (X: 6000 -> 7800):
            double xBay5 = x0 + 6000;
            double L_bay5 = 1800;

            // - MẶT TRƯỚC (Y = 0): Nguyên vẹn 1800 mm vì nhánh rẽ đi về hướng -Y
            var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(z0)), 0, "l", L_bay5);
            if (bb5Front != null) placedIds.Add(bb5Front.Id.IntegerValue);

            double zH1_5 = z0 + jackH + 135;
            var lgF5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH1_5)), 0, "l", L_bay5);
            if (lgF5Front != null) placedIds.Add(lgF5Front.Id.IntegerValue);

            double zH3_5 = z0 + jackH + 830;
            var lgMid5Front = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH3_5)), 0, "l", L_bay5);
            if (lgMid5Front != null) placedIds.Add(lgMid5Front.Id.IntegerValue);

            double zH6_5 = z0 + jackH + 2180;
            var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH6_5)), 0, "l", L_bay5);
            if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

            double zH7_5 = z0 + jackH + 2630;
            var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0), MmToFt(zH7_5)), 0, "l", L_bay5);
            if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbFront5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_bay5);
                if (tbFront5 != null) placedIds.Add(tbFront5.Id.IntegerValue);
            }

            // Mâm thao tác Nhịp 5: 2 mâm 1800 mm từ 6000 -> 7800
            if (sym6 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - 345), MmToFt(zDeck)), 0, "l", L_bay5);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 345), MmToFt(zDeck)), 0, "l", L_bay5);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // 4. CẤY CỘT TẠI X = 6600 (MÉP SAU Y = -W)
            double xC = x0 + 6600;
            var jkC = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
            if (jkC != null) placedIds.Add(jkC.Id.IntegerValue);

            var colC = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(xC), MmToFt(y0 - W), MmToFt(z0 + jackH)), 0, null, 0);
            if (colC != null) { ConfigureAlinco2700(colC); placedIds.Add(colC.Id.IntegerValue); }

            // 5. MÉP SAU (Y = -W) CỦA NHỊP 5: THU NGẮN VỀ 600 MM (X: 6000 -> 6600)
            // QUY TẮC: THU NGẮN GIẰNG CHÉO Family 5 (L = 600 mm) - KHÔNG ĐỔI THÀNH GIẰNG DỌC!
            // VÀ TUYỆT ĐỐI KHÔNG LẮP Family 4 Ở THÂN GIỮA (h3, h6) TẠI MẶT NÀY
            double L_cut = 600;
            // Ván lót đáy sau đoạn 600 mm
            var bb5Back = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_cut);
            if (bb5Back != null) placedIds.Add(bb5Back.Id.IntegerValue);

            // Giằng chân sau đoạn 600 mm tại h1 (Khóa chân cột)
            var lgF5Back = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH1_5)), 0, "l", L_cut);
            if (lgF5Back != null) placedIds.Add(lgF5Back.Id.IntegerValue);

            // Giằng chéo sau thu ngắn về 600 mm Family 5 (Z = jackH + 380 tại h2 và jackH + 1730 tại h5)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH2)), 0, "l", L_cut);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W), MmToFt(zH5_brc)), 0, "l", L_cut);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // Ván chắn biên sau đoạn 600 mm tại cao trình sàn
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbBack5 = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xBay5), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_cut);
                if (tbBack5 != null) placedIds.Add(tbBack5.Id.IntegerValue);
            }

            // (Khoảng mở X in [6600, 7800] tại Y = -W là CỬA NGÕ mở thông suốt 100% sang hệ giàn 2)

            // =========================================================================
            // PHẦN B: HỆ GIÀN 2 (NHÁNH RẼ VUÔNG GÓC 90° VƯƠN VỀ PHÍA -Y: 1800 x 1200 mm)
            // Tọa độ X: [xC = 6600, xMax = 7800], Y: [yBase = y0 - W = -1200 -> yBot = -3000]
            // QUY TẮC CHUẨN XÁC:
            // - VÁCH TRONG X = 6600 (xC): LẮP GIẰNG CHÉO Family 5 (nối tiếp từ mặt giằng chéo dãy chính).
            //   TUYỆT ĐỐI KHÔNG lắp Family 4 (h3, h6) ở vách trong này!
            // - MÉP NGOÀI X = 7800 (xMax): LẮP GIẰNG DỌC Family 4 tại h3 và LAN CAN KÉP Family 4 tại h6, h7!
            //   KHÔNG có Family 5 ở mép ngoài này!
            // =========================================================================
            double xMax = x0 + 7800;
            double yBase = y0 - W; // -1200 mm
            double L_return = 1800; // Dài theo phương Y
            double yBot = yBase - L_return; // -3000 mm
            double W_return = 1200; // Rộng theo phương X (7800 - 6600)

            // 1. Ván lót đáy Family 1 dọc theo Y (rot = 90 từ yBot lên yBase) tại X = xC và X = xMax
            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            // 2. 2 Cột ngoài mới tại đáy nhánh rẽ: (xC, yBot) và (xMax, yBot)
            XYZ[] retColPts = new XYZ[]
            {
                new XYZ(MmToFt(xC),   MmToFt(yBot), MmToFt(z0)),
                new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(z0))
            };
            foreach (var pt in retColPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 3. GIẰNG CHÂN FAMILY 4 TẠI h1 (Z = jackH + 135) - ĐẦY ĐỦ 100% KHÓA CHÂN:
            double zH1_R = z0 + jackH + 135;
            // - 2 Giằng dọc nhánh rẽ theo Y (rot = 90 từ yBot): Vách trong (xC) và Mép ngoài (xMax)
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);

            // - 2 Giằng ngang theo X (rot = 0): tại Y = yBase (thanh chung nối cửa ngõ) và Y = yBot (đầu hồi đáy)
            var trRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBase), MmToFt(zH1_R)), 0, "l", W_return);
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (trRF1 != null) placedIds.Add(trRF1.Id.IntegerValue);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // 4. GIẰNG CHÉO FAMILY 5 TẠI VÁCH TRONG (X = xC = 6600, rot = 90 dọc theo Y từ yBot):
            // (Nơi tiếp nối từ mép sau có giằng chéo của Nhịp 5)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH2)), 90, "l", L_return);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH5_brc)), 90, "l", L_return);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 5. GIẰNG DỌC & LAN CAN FAMILY 4 TẠI MÉP NGOÀI (X = xMax = 7800, rot = 90 dọc theo Y từ yBot):
            // - Giằng giữa h3 Family 4 tại xMax
            double zH3_R = z0 + jackH + 830;
            var lgRM_Outer = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH3_R)), 90, "l", L_return);
            if (lgRM_Outer != null) placedIds.Add(lgRM_Outer.Id.IntegerValue);

            // - Lan can bảo vệ kép sàn tại xMax: h6 (mid-rail) và h7 (top handrail)
            double zH6_R = z0 + jackH + 2180;
            var railOuter1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH6_R)), 90, "l", L_return);
            if (railOuter1 != null) placedIds.Add(railOuter1.Id.IntegerValue);

            double zH7_R = z0 + jackH + 2630;
            var railOuter2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", L_return);
            if (railOuter2 != null) placedIds.Add(railOuter2.Id.IntegerValue);

            // 6. Sàn Thao Tác Sàn 1 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - 2 Giằng ngang đỡ mâm theo X (rot = 0) tại Y = yBase và Y = yBot
            var trRD1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBase), MmToFt(zDeck_R)), 0, "l", W_return);
            var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
            if (trRD1 != null) placedIds.Add(trRD1.Id.IntegerValue);
            if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90 từ yBot)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xC + 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7
            if (sym7 != null)
            {
                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC + 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ đầu hồi đáy yBot (rot = 0 qua X: xC -> xMax): ĐẦY ĐỦ 2 TẦNG (KÉP):
            var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
            var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xC), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
            if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
            if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);

            return null;
        }


                /// <summary>
        /// TRƯỜNG HỢP 1.4: KHỚP NỐI GÓC 90° HƯỚNG -Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM - CONN-GRP1-TYPE1.4)
        /// - Góc rẽ: Giống 1.3 (Vươn về phía -Y, mép phải phẳng).
        /// - Điều kiện: Giống 1.2 (Chiều dài nhịp cuối L_last = W = 1200 mm, tạo ô vuông 1200x1200 mm).
        /// - Không cấy cột: 2 cột (6000, -1200) và (7200, -1200) dùng chung trực tiếp làm cột gốc nhánh rẽ.
        /// - Cửa ngõ Y = -1200: Mở trọn vẹn 1200 mm (X: 6000 -> 7200) làm lối đi sang hệ giàn 2.
        /// - Hệ giàn 2 (Nhánh rẽ -Y, X: 6000->7200, Y: -3000->-1200):
        ///   + Vách trong X = 6000: Bố trí GIẰNG CHÉO Family 5 (tiếp nối mặt sau). KHÔNG lắp Family 4 tại h3, h6.
        ///   + Mép ngoài X = 7200: Bố trí GIẰNG DỌC Family 4 tại h3 và LAN CAN KÉP Family 4 tại h6, h7.
        ///   + Đầu hồi đáy Y = -3000: Bố trí lan can kép Family 4 (h6, h7) và ván chắn biên Family 7.
        /// - Giằng chân h1: Khóa cứng 100% tất cả các mặt chân cột.
        /// </summary>
        private static string ExecuteCase1_4(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH VỚI NHỊP CUỐI = W = 1200 MM (Ô VUÔNG 1200 x 1200 MM)
            // Nhịp 1..4: 1800, 1200, 1800, 1200. Nhịp 5: 1200 mm
            // =========================================================================
            List<double> mainBays;
            if (opts.BayLengths != null && opts.BayLengths.Count > 0)
            {
                mainBays = new List<double>(opts.BayLengths);
                mainBays[mainBays.Count - 1] = W;
            }
            else
            {
                mainBays = new List<double> { 1800, 1200, 1800, 1200, W };
            }

            int nBays = mainBays.Count;
            List<double> colX = new List<double>();
            double curX = x0;
            colX.Add(curX);
            for (int i = 0; i < nBays; i++)
            {
                curX += mainBays[i];
                colX.Add(curX);
            }
            int nCols = colX.Count; // 6 hàng cột (0, 1800, 3000, 4800, 6000, 7200)

            // 1. Dựng tất cả các hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isOuterEnd = (k == 0 || k == nCols - 1);

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Lan can đầu hồi:
                // - Đầu hồi trái ngoài cùng (k = 0): Đầy đủ 2 tầng kép h6, h7
                // - Đầu hồi phải dãy chính (k = nCols - 1): Mép ngoài Y in [-W, 0] là mép hở sàn -> bảo vệ đầy đủ h6, h7
                if (isOuterEnd)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Dựng các nhịp tiêu chuẩn ban đầu (Nhịp 0 .. nBays - 2)
            for (int i = 0; i < nBays - 1; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // 3. Dựng cấu kiện Nhịp cuối (Nhịp nBays - 1, L_last = W = 1200 mm):
            double xLast = colX[nBays - 1]; // X = 6000
            double L_last = mainBays[nBays - 1]; // 1200 mm

            // --- MẶT TRƯỚC (Y = y0 = 0, X: 6000 -> 7200, dài 1200 mm):
            // Ván lót đáy trước
            var bb5Front = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(z0)), 0, "l", L_last);
            if (bb5Front != null) placedIds.Add(bb5Front.Id.IntegerValue);

            // Giằng chân trước tại h1
            double zH1_last = z0 + jackH + 135;
            var lgFLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastFront != null) placedIds.Add(lgFLastFront.Id.IntegerValue);

            // Giằng giữa trước tại h3
            double zH3_last = z0 + jackH + 830;
            var lgMidLastFront = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH3_last)), 0, "l", L_last);
            if (lgMidLastFront != null) placedIds.Add(lgMidLastFront.Id.IntegerValue);

            // Lan can an toàn trước kép: h6 và h7
            double zH6_last = z0 + jackH + 2180;
            var railF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH6_last)), 0, "l", L_last);
            if (railF1 != null) placedIds.Add(railF1.Id.IntegerValue);

            double zH7_last = z0 + jackH + 2630;
            var railF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0), MmToFt(zH7_last)), 0, "l", L_last);
            if (railF2 != null) placedIds.Add(railF2.Id.IntegerValue);

            // Ván chắn biên trước
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_last);
                if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);
            }

            // Mâm thao tác Nhịp cuối: 2 mâm 1200 mm tại X: 6000 -> 7200
            if (sym6 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast), MmToFt(y0 - 345), MmToFt(zDeck)), 0, "l", L_last);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W + 345), MmToFt(zDeck)), 0, "l", L_last);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // --- MẶT SAU (Y = y0 - W = -1200, X: 6000 -> 7200):
            // CỬA NGÕ MỞ TRỌN VẸN 1200 MM SANG HỆ GIÀN 2 (-Y)!
            // - Giằng chân tại h1: Khóa 2 cột (6000, -1200) và (7200, -1200) ở sát mặt đất
            var lgFLastBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(zH1_last)), 0, "l", L_last);
            if (lgFLastBack != null) placedIds.Add(lgFLastBack.Id.IntegerValue);

            // - Giằng ngang đỡ mâm tại h5 (cao trình sàn) để đỡ mép sàn liên kết
            double zH5_last = z0 + jackH + 1730;
            var trDeckBack = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(y0 - W), MmToFt(zH5_last)), 0, "l", L_last);
            if (trDeckBack != null) placedIds.Add(trDeckBack.Id.IntegerValue);

            // (Lối đi working level mở 100%, không có lan can, không ván chắn biên, không giằng giữa)

            // =========================================================================
            // PHẦN B: HỆ GIÀN 2 (NHÁNH RẼ VUÔNG GÓC 90° VƯƠN VỀ PHÍA -Y: 1800 x 1200 mm)
            // Tọa độ X: [xLast = 6000, xMax = 7200], Y: [yBase = y0 - W = -1200 -> yBot = -3000]
            // Không cấy cột phụ: 2 cột gốc tựa trực tiếp vào (6000, -1200) và (7200, -1200)
            // =========================================================================
            double xMax = colX[nCols - 1]; // 7200
            double yBase = y0 - W; // -1200 mm
            double L_return = 1800; // Dài theo phương Y
            double yBot = yBase - L_return; // -3000 mm
            double W_return = W; // 1200 mm

            // 1. Ván lót đáy Family 1 dọc theo Y (rot = 90 từ yBot lên yBase) tại X = xLast và X = xMax
            var bbR1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            var bbR2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xMax),  MmToFt(yBot), MmToFt(z0)), 90, "l", L_return);
            if (bbR1 != null) placedIds.Add(bbR1.Id.IntegerValue);
            if (bbR2 != null) placedIds.Add(bbR2.Id.IntegerValue);

            // 2. Chỉ cần 2 Cột ngoài mới tại đáy nhánh rẽ: (xLast, yBot) và (xMax, yBot)
            XYZ[] retColPts = new XYZ[]
            {
                new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(z0)),
                new XYZ(MmToFt(xMax),  MmToFt(yBot), MmToFt(z0))
            };
            foreach (var pt in retColPts)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 3. GIẰNG CHÂN FAMILY 4 TẠI h1 (Z = jackH + 135) - ĐẦY ĐỦ 100% KHÓA CHÂN:
            double zH1_R = z0 + jackH + 135;
            // - 2 Giằng dọc nhánh rẽ theo Y (rot = 90 từ yBot): Vách trong (xLast) và Mép ngoài (xMax)
            var lgRF1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            var lgRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax),  MmToFt(yBot), MmToFt(zH1_R)), 90, "l", L_return);
            if (lgRF1 != null) placedIds.Add(lgRF1.Id.IntegerValue);
            if (lgRF2 != null) placedIds.Add(lgRF2.Id.IntegerValue);

            // - Giằng ngang đầu hồi đáy tại Y = yBot (rot = 0 từ xLast đến xMax)
            var trRF2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH1_R)), 0, "l", W_return);
            if (trRF2 != null) placedIds.Add(trRF2.Id.IntegerValue);

            // 4. GIẰNG CHÉO FAMILY 5 TẠI VÁCH TRONG (X = xLast = 6000, rot = 90 dọc theo Y từ yBot):
            // (Tiếp nối mặt sau có giằng chéo của dãy chính)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH2)), 90, "l", L_return);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH5_brc)), 90, "l", L_return);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 5. GIẰNG DỌC & LAN CAN FAMILY 4 TẠI MÉP NGOÀI (X = xMax = 7200, rot = 90 dọc theo Y từ yBot):
            // - Giằng giữa h3 Family 4 tại xMax
            double zH3_R = z0 + jackH + 830;
            var lgRM_Outer = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH3_R)), 90, "l", L_return);
            if (lgRM_Outer != null) placedIds.Add(lgRM_Outer.Id.IntegerValue);

            // - Lan can kép bảo vệ sàn tại xMax: h6 (mid-rail) và h7 (top handrail)
            double zH6_R = z0 + jackH + 2180;
            var railOuter1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH6_R)), 90, "l", L_return);
            if (railOuter1 != null) placedIds.Add(railOuter1.Id.IntegerValue);

            double zH7_R = z0 + jackH + 2630;
            var railOuter2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xMax), MmToFt(yBot), MmToFt(zH7_R)), 90, "l", L_return);
            if (railOuter2 != null) placedIds.Add(railOuter2.Id.IntegerValue);

            // 6. Sàn Thao Tác Sàn 1 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - Giằng ngang đỡ mâm đầu hồi tại Y = yBot (rot = 0)
            var trRD2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
            if (trRD2 != null) placedIds.Add(trRD2.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90 từ yBot)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xLast + 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xMax - 345), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7
            if (sym7 != null)
            {
                var tbL = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast + 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbR = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xMax - 66), MmToFt(yBot), MmToFt(zDeck_R)), 90, "l", L_return);
                var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zDeck_R)), 0, "l", W_return);
                if (tbL != null) placedIds.Add(tbL.Id.IntegerValue);
                if (tbR != null) placedIds.Add(tbR.Id.IntegerValue);
                if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ đầu hồi đáy yBot (rot = 0 qua X: xLast -> xMax): ĐẦY ĐỦ 2 TẦNG (KÉP):
            var rE1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH6_R)), 0, "l", W_return);
            var rE2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xLast), MmToFt(yBot), MmToFt(zH7_R)), 0, "l", W_return);
            if (rE1 != null) placedIds.Add(rE1.Id.IntegerValue);
            if (rE2 != null) placedIds.Add(rE2.Id.IntegerValue);

            return null;
        }


        private static string ExecuteCase2_1(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH 5 NHỊP NGUYÊN BẢN (KHÔNG CẮT NHỊP 5)
            // =========================================================================
            List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            int nMainBays = mainBays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var bLen in mainBays)
            {
                curX += bLen;
                colX.Add(curX);
            }
            int nCols = colX.Count; // 6 hàng cột

            // 1. Dựng 6 hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang dùng chung Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Lan can đầu hồi ngoài cùng bên trái (k == 0 tại X = x0)
                if (k == 0)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
                // Chú ý: Tại k == nCols - 1 (X = x0 + 7800), đây là cửa ngõ sang nhánh rẽ nên KHÔNG đặt lan can đầu hồi chắn lối đi!
            }

            // 2. Dựng 5 nhịp tiêu chuẩn dãy chính
            for (int i = 0; i < nMainBays; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ NGOÀI TRƯỜNG HỢP 2.1 (MÉP SAU PHẲNG)
            // Tọa độ: X ∈ [7800, 9000], Y ∈ [-1200, +600]
            // =========================================================================
            double xA = x0 + 7800;
            double xB = x0 + 9000;
            double ySouth = y0 - W; // y0 - 1200 (mép sau phẳng thẳng hàng)
            double yNorth = ySouth + 1800; // y0 + 600 (chiều dài nhịp rẽ L = 1800 mm)
            double L_ret = 1800;
            double W_ret = 1200;
            double L_innerWall = 600; // Đoạn nhô cao từ y0 đến yNorth

            // 1. 3 Cụm cột ngoài mới: (xB, ySouth), (xB, yNorth), (xA, yNorth)
            XYZ[] retCols = new XYZ[]
            {
                new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(z0)),
                new XYZ(MmToFt(xB), MmToFt(yNorth), MmToFt(z0)),
                new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(z0))
            };
            foreach (var pt in retCols)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 2. Ván lót đáy Family 1:
            // - Mép sau (Y = ySouth): Từ xA -> xB (L = 1200, rot = 0)
            var bbSouth = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(z0)), 0, "l", W_ret);
            if (bbSouth != null) placedIds.Add(bbSouth.Id.IntegerValue);

            // - Đầu hồi trước (Y = yNorth): Từ xA -> xB (L = 1200, rot = 0)
            var bbNorth = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(z0)), 0, "l", W_ret);
            if (bbNorth != null) placedIds.Add(bbNorth.Id.IntegerValue);

            // - Mép ngoài phải (X = xB): Từ ySouth -> yNorth (L = 1800, rot = 90)
            var bbEast = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(z0)), 90, "l", L_ret);
            if (bbEast != null) placedIds.Add(bbEast.Id.IntegerValue);

            // - Vách trong nhô cao (X = xA): Từ y0 -> yNorth (L = 600, rot = 90)
            var bbInner = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(y0), MmToFt(z0)), 90, "l", L_innerWall);
            if (bbInner != null) placedIds.Add(bbInner.Id.IntegerValue);

            // 3. Giằng chân Family 4 tại h1 (Z = jackH + 135) - Khóa chân 100%:
            double zH1_R = z0 + jackH + 135;
            // - Mép sau (Y = ySouth)
            var lgFSouth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH1_R)), 0, "l", W_ret);
            if (lgFSouth != null) placedIds.Add(lgFSouth.Id.IntegerValue);

            // - Đầu hồi trước (Y = yNorth)
            var lgFNorth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH1_R)), 0, "l", W_ret);
            if (lgFNorth != null) placedIds.Add(lgFNorth.Id.IntegerValue);

            // - Mép ngoài phải (X = xB)
            var lgFEast = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH1_R)), 90, "l", L_ret);
            if (lgFEast != null) placedIds.Add(lgFEast.Id.IntegerValue);

            // - Vách trong nhô cao (X = xA từ y0 -> yNorth)
            var lgFInner = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(y0), MmToFt(zH1_R)), 90, "l", L_innerWall);
            if (lgFInner != null) placedIds.Add(lgFInner.Id.IntegerValue);

            // 4. Giằng giữa Family 4 tại h3 (Z = jackH + 830):
            // - Chỉ đặt ở vách trong nhô cao xA (L = 600, rot = 90). Mép ngoài xB có Family 5 nên KHÔNG đặt!
            double zH3_R = z0 + jackH + 830;
            var lgMInner = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(y0), MmToFt(zH3_R)), 90, "l", L_innerWall);
            if (lgMInner != null) placedIds.Add(lgMInner.Id.IntegerValue);

            // 5. Giằng chéo Family 5 tại mép ngoài bên phải (X = xB, rot = 90 dọc theo Y):
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH2)), 90, "l", L_ret);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH5_brc)), 90, "l", L_ret);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 6. Sàn thao tác Sàn 1 tại h5 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - 2 Giằng ngang đỡ mâm Family 4 (rot = 0, L = 1200): tại ySouth và yNorth
            var trDSouth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zDeck_R)), 0, "l", W_ret);
            var trDNorth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zDeck_R)), 0, "l", W_ret);
            if (trDSouth != null) placedIds.Add(trDSouth.Id.IntegerValue);
            if (trDNorth != null) placedIds.Add(trDNorth.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90, L = 1800, W = 500)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7:
            if (sym7 != null)
            {
                var tbSouth = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(ySouth + 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                var tbNorth = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yNorth - 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                var tbEast = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                var tbInner = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66), MmToFt(y0), MmToFt(zDeck_R)), 90, "l", L_innerWall);
                if (tbSouth != null) placedIds.Add(tbSouth.Id.IntegerValue);
                if (tbNorth != null) placedIds.Add(tbNorth.Id.IntegerValue);
                if (tbEast != null) placedIds.Add(tbEast.Id.IntegerValue);
                if (tbInner != null) placedIds.Add(tbInner.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ kép của sàn (Family 4 tại h6 và h7):
            double zH6_R = z0 + jackH + 2180;
            double zH7_R = z0 + jackH + 2630;

            // - Mép sau (Y = ySouth, L = 1200, rot = 0):
            var rSouth1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH6_R)), 0, "l", W_ret);
            var rSouth2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH7_R)), 0, "l", W_ret);
            if (rSouth1 != null) placedIds.Add(rSouth1.Id.IntegerValue);
            if (rSouth2 != null) placedIds.Add(rSouth2.Id.IntegerValue);

            // - Đầu hồi trước (Y = yNorth, L = 1200, rot = 0):
            var rNorth1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH6_R)), 0, "l", W_ret);
            var rNorth2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH7_R)), 0, "l", W_ret);
            if (rNorth1 != null) placedIds.Add(rNorth1.Id.IntegerValue);
            if (rNorth2 != null) placedIds.Add(rNorth2.Id.IntegerValue);

            // - Vách trong nhô cao (X = xA, Y: y0 -> yNorth, L = 600, rot = 90):
            var rInner1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(y0), MmToFt(zH6_R)), 90, "l", L_innerWall);
            var rInner2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(y0), MmToFt(zH7_R)), 90, "l", L_innerWall);
            if (rInner1 != null) placedIds.Add(rInner1.Id.IntegerValue);
            if (rInner2 != null) placedIds.Add(rInner2.Id.IntegerValue);

            // Chú ý: Đoạn X = xA từ ySouth đến y0 mở thông suốt 100% làm lối đi giữa dãy chính và nhánh rẽ.

            return null;
        }

        /// <summary>
        /// DỰNG CẤU KIỆN 1 NHỊP TIÊU CHUẨN:
        /// - GIẰNG CHÂN (h1): ĐẦY ĐỦ CẢ TRƯỚC VÀ SAU (khóa chân cột)
        /// - GIẰNG ĐỈNH (h7): ĐẦY ĐỦ CẢ TRƯỚC VÀ SAU (khóa đỉnh giàn)
        /// - THÂN GIỮA (h3, h6): Chỉ lắp mặt trước, KHÔNG lắp đè lên Family 5 ở mặt sau!
        /// </summary>
        private static void BuildStandardBayElements(Document doc, Level level, List<int> placedIds,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            double px, double y0, double z0, double W, double jackH, double L_i, bool buildFrontRail)
        {
            // Ván lót đáy Family 1 (Trước & Sau)
            var bb1 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "l", L_i);
            var bb2 = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "l", L_i);
            if (bb1 != null) placedIds.Add(bb1.Id.IntegerValue);
            if (bb2 != null) placedIds.Add(bb2.Id.IntegerValue);

            // GIẰNG CHÂN DỌC TẠI h1 (Z = jackH + 135) - ĐẦY ĐỦ CẢ TRƯỚC VÀ SAU
            double zH1 = z0 + jackH + 135;
            var lgFoot1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), 0, "l", L_i);
            var lgFoot2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH1)), 0, "l", L_i);
            if (lgFoot1 != null) placedIds.Add(lgFoot1.Id.IntegerValue);
            if (lgFoot2 != null) placedIds.Add(lgFoot2.Id.IntegerValue);

            // Giằng dọc trung gian Family 4 tại h3: CHỈ ĐẶT TẠI MẶT TRƯỚC (thân giữa sau có Family 5)
            double zH3 = z0 + jackH + 830;
            var lgMid = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH3)), 0, "l", L_i);
            if (lgMid != null) placedIds.Add(lgMid.Id.IntegerValue);

            // Giằng chéo Family 5 (mặt sau Y = -W)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5 = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH2)), 0, "l", L_i);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(zH5)), 0, "l", L_i);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // Lan can mặt trước Family 4 bảo vệ sàn: ĐẦY ĐỦ 2 TẦNG (h6 và h7)
            if (buildFrontRail)
            {
                // Lan can tầng giữa tại h6 (Z = jackH + 2180)
                double zH6 = z0 + jackH + 2180;
                var railFront1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), 0, "l", L_i);
                if (railFront1 != null) placedIds.Add(railFront1.Id.IntegerValue);

                // Lan can tay vịn trên tại h7 (Z = jackH + 2630)
                double zH7 = z0 + jackH + 2630;
                var railFrontTop = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), 0, "l", L_i);
                if (railFrontTop != null) placedIds.Add(railFrontTop.Id.IntegerValue);
            }

            // Mâm thao tác Family 6 tại Sàn 1
            if (sym6 != null)
            {
                double zDeck = z0 + jackH + 1730;
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - 345), MmToFt(zDeck)), 0, "l", L_i);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 345), MmToFt(zDeck)), 0, "l", L_i);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // Ván chắn biên Family 7 tại Sàn 1
            if (sym7 != null)
            {
                double zDeck = z0 + jackH + 1730;
                if (buildFrontRail)
                {
                    var tbFront = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - 66), MmToFt(zDeck)), 0, "l", L_i);
                    if (tbFront != null) placedIds.Add(tbFront.Id.IntegerValue);
                }
                var tbBack = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0 - W + 66), MmToFt(zDeck)), 0, "l", L_i);
                if (tbBack != null) placedIds.Add(tbBack.Id.IntegerValue);
            }
        }

        /// <summary>
        /// DỰNG DÃY GIÀN GIÁO NHIỀU NHỊP THẲNG TIÊU CHUẨN:
        /// - ĐẦY ĐỦ GIẰNG CHÂN (h1) VÀ GIẰNG ĐỈNH (h7)
        /// - Thân giữa không đặt giằng đè lên Family 5
        /// </summary>
                /// <summary>
        /// TRƯỜNG HỢP 2.2: KHỚP NỐI GÓC 90° HƯỚNG -Y MÉP TRƯỚC PHẲNG (CONN-GRP2-TYPE2.2)
        /// NGUYÊN TẮC HÌNH HỌC:
        /// 1. Dãy chính 5 nhịp giữ nguyên hoàn toàn (1800, 1200, 1800, 1200, 1800 mm = 7800 mm).
        ///    Nhịp 5 là nhịp chuẩn đủ 1800 mm (X: 6000 -> 7800) ở cả mặt trước và mặt sau, không cắt ngắn.
        /// 2. Nhánh rẽ góc vuông 90° đặt phía NGOÀI đầu hồi: X in [x0 + 7800, x0 + 9000], rộng W = 1200 mm.
        /// 3. Mép trước phẳng (Flush Front Edge): Mép trước nhánh rẽ nằm tại Y = y0 (Y = 0), thẳng hàng với mặt trước dãy chính.
        /// 4. Nhánh rẽ vươn về phía -Y: Chiều dài L = 1800 mm dọc theo Y (từ Y = 0 đến Y = -1800 mm).
        ///    Nhô sâu ra phía sau mép sau dãy chính một đoạn 600 mm (từ Y = -1200 đến Y = -1800 mm).
        /// 5. Cửa ngõ giao thoa: Đoạn X = x0 + 7800 giữa Y = -1200 và Y = 0 giải phóng 100% lan can/ván chắn để thông suốt 2 sàn.
        /// 6. Giằng chéo Family 5: Bố trí ở vách trong nhô ra sau X = x0 + 7800 (Y in [-1800, -1200], L = 600 mm)
        ///    tiếp nối mặt giằng chéo sau của dãy chính.
        ///    Theo quy tắc: Tại vách trong X = 7800 TUYỆT ĐỐI KHÔNG có Family 4 ở h3 và h6.
        /// 7. Giằng dọc và Lan can kép h6, h7 tại mép ngoài bên phải X = x0 + 9000 (Y in [-1800, 0], L = 1800 mm)
        ///    tiếp nối mặt trước lan can dãy chính.
        ///    Các mép hở khác (mép trước Y = 0, đầu hồi đáy Y = -1800) được bọc kín lan can kép h6, h7.
        /// </summary>
        private static string ExecuteCase2_2(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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
            // PHẦN A: DÃY CHÍNH 5 NHỊP NGUYÊN BẢN (KHÔNG CẮT NHỊP 5)
            // =========================================================================
            List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            int nMainBays = mainBays.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var bLen in mainBays)
            {
                curX += bLen;
                colX.Add(curX);
            }
            int nCols = colX.Count; // 6 hàng cột

            // 1. Dựng 6 hàng cột dãy chính
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isLeftOuterEnd = (k == 0);

                // Kích chân Family 2 (Trước & Sau)
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3 (Trước & Sau)
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang Family 4 (rot = -90 vươn sang -Y)
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // Lan can đầu hồi trái ngoài cùng (k = 0): Đầy đủ 2 tầng kép h6, h7
                // (Đầu hồi phải k = nCols - 1 là cửa ngõ thông sang nhánh rẽ nên KHÔNG đặt lan can ở đây!)
                if (isLeftOuterEnd)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Dựng cả 5 nhịp dãy chính nguyên bản chuẩn (Nhịp 1 -> 5)
            for (int i = 0; i < nMainBays; i++)
            {
                double px = colX[i];
                double L_i = mainBays[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            // =========================================================================
            // PHẦN B: DỰNG NHÁNH RẼ NGOÀI TRƯỜNG HỢP 2.2 (MÉP TRƯỚC PHẲNG - HƯỚNG -Y)
            // Tọa độ: X in [7800, 9000], Y in [-1800, 0]
            // =========================================================================
            double xA = x0 + 7800;
            double xB = x0 + 9000;
            double yNorth = y0; // y0 = 0 (mép trước phẳng thẳng hàng)
            double ySouth = yNorth - 1800; // y0 - 1800 = -1800 mm
            double yRearMain = y0 - W; // y0 - 1200 = -1200 mm
            double L_ret = 1800;
            double W_ret = 1200;
            double L_innerWall = 600; // Đoạn nhô ra phía sau từ ySouth (-1800) đến yRearMain (-1200)

            // 1. 3 Cụm cột ngoài mới: (xB, yNorth), (xB, ySouth), (xA, ySouth)
            XYZ[] retCols = new XYZ[]
            {
                new XYZ(MmToFt(xB), MmToFt(yNorth), MmToFt(z0)),
                new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(z0)),
                new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(z0))
            };
            foreach (var pt in retCols)
            {
                var jk = PlaceInstance(doc, sym2, level, pt, 0, "h", jackH);
                if (jk != null) placedIds.Add(jk.Id.IntegerValue);

                var col = PlaceInstance(doc, sym3, level, new XYZ(pt.X, pt.Y, MmToFt(z0 + jackH)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
            }

            // 2. Ván lót đáy Family 1:
            // - Mép trước phẳng (Y = yNorth = 0): Từ xA -> xB (L = 1200, rot = 0)
            var bbNorth = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(z0)), 0, "l", W_ret);
            if (bbNorth != null) placedIds.Add(bbNorth.Id.IntegerValue);

            // - Đầu hồi đáy (Y = ySouth = -1800): Từ xA -> xB (L = 1200, rot = 0)
            var bbSouth = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(z0)), 0, "l", W_ret);
            if (bbSouth != null) placedIds.Add(bbSouth.Id.IntegerValue);

            // - Mép ngoài phải (X = xB = 9000): Từ ySouth -> yNorth (L = 1800, rot = 90)
            var bbEast = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(z0)), 90, "l", L_ret);
            if (bbEast != null) placedIds.Add(bbEast.Id.IntegerValue);

            // - Vách trong nhô ra sau (X = xA = 7800): Từ ySouth (-1800) -> yRearMain (-1200) (L = 600, rot = 90)
            var bbInner = PlaceInstance(doc, sym1, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(z0)), 90, "l", L_innerWall);
            if (bbInner != null) placedIds.Add(bbInner.Id.IntegerValue);

            // 3. Giằng chân Family 4 tại h1 (Z = jackH + 135) - Khóa chân 100%:
            double zH1_R = z0 + jackH + 135;
            // - Mép trước phẳng (Y = yNorth)
            var lgFNorth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH1_R)), 0, "l", W_ret);
            if (lgFNorth != null) placedIds.Add(lgFNorth.Id.IntegerValue);

            // - Đầu hồi đáy (Y = ySouth)
            var lgFSouth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH1_R)), 0, "l", W_ret);
            if (lgFSouth != null) placedIds.Add(lgFSouth.Id.IntegerValue);

            // - Mép ngoài phải (X = xB)
            var lgFEast = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH1_R)), 90, "l", L_ret);
            if (lgFEast != null) placedIds.Add(lgFEast.Id.IntegerValue);

            // - Vách trong nhô ra sau (X = xA từ ySouth -> yRearMain)
            var lgFInner = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH1_R)), 90, "l", L_innerWall);
            if (lgFInner != null) placedIds.Add(lgFInner.Id.IntegerValue);

            // 4. Giằng giữa Family 4 tại h3 (Z = jackH + 830):
            // - Đặt ở mép ngoài bên phải xB (L = 1800, rot = 90). Vách trong xA có Family 5 nên KHÔNG đặt!
            double zH3_R = z0 + jackH + 830;
            var lgMOuter = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH3_R)), 90, "l", L_ret);
            if (lgMOuter != null) placedIds.Add(lgMOuter.Id.IntegerValue);

            // 5. Giằng chéo Family 5 tại vách trong nhô ra sau (X = xA = 7800, rot = 90 dọc theo Y, L = 600 mm):
            // (Nơi tiếp nối từ mép sau có giằng chéo của dãy chính)
            if (sym5 != null)
            {
                double zH2 = z0 + jackH + 380;
                double zH5_brc = z0 + jackH + 1730;
                var brc1 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH2)), 90, "l", L_innerWall);
                if (brc1 != null) { SetParam(brc1, "h", 900); placedIds.Add(brc1.Id.IntegerValue); }

                var brc2 = PlaceInstance(doc, sym5, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH5_brc)), 90, "l", L_innerWall);
                if (brc2 != null) { SetParam(brc2, "h", 900); placedIds.Add(brc2.Id.IntegerValue); }
            }

            // 6. Sàn thao tác Sàn 1 tại h5 (Z = jackH + 1730):
            double zDeck_R = z0 + jackH + 1730;
            // - 2 Giằng ngang đỡ mâm Family 4 (rot = 0, L = 1200): tại yNorth và ySouth
            var trDNorth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zDeck_R)), 0, "l", W_ret);
            var trDSouth = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zDeck_R)), 0, "l", W_ret);
            if (trDNorth != null) placedIds.Add(trDNorth.Id.IntegerValue);
            if (trDSouth != null) placedIds.Add(trDSouth.Id.IntegerValue);

            // - 2 Mâm thao tác Family 6 dọc theo Y (rot = 90, L = 1800, W = 500)
            if (sym6 != null)
            {
                var pl1 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xA + 345), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                if (pl1 != null) { SetParam(pl1, "w", 500); placedIds.Add(pl1.Id.IntegerValue); }

                var pl2 = PlaceInstance(doc, sym6, level, new XYZ(MmToFt(xB - 345), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                if (pl2 != null) { SetParam(pl2, "w", 500); placedIds.Add(pl2.Id.IntegerValue); }
            }

            // - Ván chắn biên Family 7:
            if (sym7 != null)
            {
                var tbNorth = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(yNorth - 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                var tbSouth = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA), MmToFt(ySouth + 66), MmToFt(zDeck_R)), 0, "l", W_ret);
                var tbEast = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xB - 66), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_ret);
                var tbInner = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(xA + 66), MmToFt(ySouth), MmToFt(zDeck_R)), 90, "l", L_innerWall);
                if (tbNorth != null) placedIds.Add(tbNorth.Id.IntegerValue);
                if (tbSouth != null) placedIds.Add(tbSouth.Id.IntegerValue);
                if (tbEast != null) placedIds.Add(tbEast.Id.IntegerValue);
                if (tbInner != null) placedIds.Add(tbInner.Id.IntegerValue);
            }

            // 7. Lan can bảo vệ kép của sàn (Family 4 tại h6 và h7):
            double zH6_R = z0 + jackH + 2180;
            double zH7_R = z0 + jackH + 2630;

            // - Mép trước phẳng (Y = yNorth, L = 1200, rot = 0):
            var rNorth1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH6_R)), 0, "l", W_ret);
            var rNorth2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(yNorth), MmToFt(zH7_R)), 0, "l", W_ret);
            if (rNorth1 != null) placedIds.Add(rNorth1.Id.IntegerValue);
            if (rNorth2 != null) placedIds.Add(rNorth2.Id.IntegerValue);

            // - Đầu hồi đáy (Y = ySouth, L = 1200, rot = 0):
            var rSouth1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH6_R)), 0, "l", W_ret);
            var rSouth2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xA), MmToFt(ySouth), MmToFt(zH7_R)), 0, "l", W_ret);
            if (rSouth1 != null) placedIds.Add(rSouth1.Id.IntegerValue);
            if (rSouth2 != null) placedIds.Add(rSouth2.Id.IntegerValue);

            // - Mép ngoài phải (X = xB từ ySouth -> yNorth, L = 1800, rot = 90):
            // (Tiếp nối mặt trước lan can của dãy chính)
            var rEast1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH6_R)), 90, "l", L_ret);
            var rEast2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(xB), MmToFt(ySouth), MmToFt(zH7_R)), 90, "l", L_ret);
            if (rEast1 != null) placedIds.Add(rEast1.Id.IntegerValue);
            if (rEast2 != null) placedIds.Add(rEast2.Id.IntegerValue);

            // (Lưu ý: Vách trong xA đã có giằng chéo Family 5 nên TUYỆT ĐỐI KHÔNG lắp Family 4 tại h6!)

            return null;
        }


        private static string ExecuteMultiBay(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            var bayLens = opts.BayLengths;
            if (bayLens == null || bayLens.Count == 0)
            {
                bayLens = new List<double> { 1800, 1200, 1800, 1200, 1800 };
            }

            int nBays = bayLens.Count;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;

            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var bLen in bayLens)
            {
                curX += bLen;
                colX.Add(curX);
            }
            int nCols = colX.Count;

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

            // 1. Tạo các hàng cột theo X
            for (int k = 0; k < nCols; k++)
            {
                double px = colX[k];
                bool isOuterEnd = (k == 0 || k == nCols - 1);

                // Kích chân Family 2
                var jk1 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(z0)), 0, "h", jackH);
                var jk2 = PlaceInstance(doc, sym2, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(z0)), 0, "h", jackH);
                if (jk1 != null) placedIds.Add(jk1.Id.IntegerValue);
                if (jk2 != null) placedIds.Add(jk2.Id.IntegerValue);

                // Cột ALINCO 2700 Family 3
                double colZ = z0 + jackH;
                var col1 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(colZ)), 0, null, 0);
                var col2 = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(px), MmToFt(y0 - W), MmToFt(colZ)), 0, null, 0);
                if (col1 != null) { ConfigureAlinco2700(col1); placedIds.Add(col1.Id.IntegerValue); }
                if (col2 != null) { ConfigureAlinco2700(col2); placedIds.Add(col2.Id.IntegerValue); }

                // Giằng ngang dùng chung Family 4 (L = W, rot = -90):
                // - Giằng chân ngang tại h1
                double zH1 = z0 + jackH + 135;
                var trFoot = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH1)), -90, "l", W);
                if (trFoot != null) placedIds.Add(trFoot.Id.IntegerValue);

                // - Giằng ngang đỡ mâm tại h5
                double zH5 = z0 + jackH + 1730;
                var trDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                if (trDeck != null) placedIds.Add(trDeck.Id.IntegerValue);

                // (Tạm bỏ qua giằng đỉnh ngang tại h7 theo yêu cầu)

                // Lan can đầu hồi tại 2 đầu ngoài cùng: ĐẦY ĐỦ 2 TẦNG (h6 và h7)
                if (isOuterEnd)
                {
                    double zH6 = z0 + jackH + 2180;
                    var rail1 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH6)), -90, "l", W);
                    if (rail1 != null) placedIds.Add(rail1.Id.IntegerValue);

                    double zH7 = z0 + jackH + 2630;
                    var rail2 = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH7)), -90, "l", W);
                    if (rail2 != null) placedIds.Add(rail2.Id.IntegerValue);

                    if (sym7 != null)
                    {
                        var tbEnd = PlaceInstance(doc, sym7, level, new XYZ(MmToFt(px), MmToFt(y0), MmToFt(zH5)), -90, "l", W);
                        if (tbEnd != null) placedIds.Add(tbEnd.Id.IntegerValue);
                    }
                }
            }

            // 2. Tạo cấu kiện dọc từng nhịp
            for (int i = 0; i < nBays; i++)
            {
                double px = colX[i];
                double L_i = bayLens[i];
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, px, y0, z0, W, jackH, L_i, true);
            }

            return null;
        }

        private static void ConfigureAlinco2700(FamilyInstance inst)
        {
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

        private static void ConfigureAlincoColumn(FamilyInstance inst, double height)
        {
            SetParam(inst, "h", height);
            int hInt = (int)Math.Round(height);
            switch (hInt)
            {
                case 225:
                    SetParam(inst, "h1", 155);
                    for (int i = 2; i <= 15; i++) SetParam(inst, "h" + i, 0);
                    break;
                case 450:
                    SetParam(inst, "h1", 135);
                    SetParam(inst, "h2", 380);
                    for (int i = 3; i <= 15; i++) SetParam(inst, "h" + i, 0);
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

        private static void PlaceConfiguredPosts(Document doc, FamilySymbol sym3, Level level, List<int> placedIds, double cx, double cy, double baseZ, BuildOptions opts)
        {
            if (sym3 == null) return;
            bool hasCustomPosts = opts != null && (opts.Post1Height > 0 || opts.Post2Height > 0 || (opts.Post3Count > 0 && opts.Post3Height > 0) || opts.Post4Height > 0 || opts.Post5Height > 0);

            if (!hasCustomPosts)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(baseZ)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
                return;
            }

            double curZ = baseZ;
            if (opts.Post1Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post1Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post1Height); placedIds.Add(col.Id.IntegerValue); }
                curZ += opts.Post1Height;
            }
            if (opts.Post2Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post2Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post2Height); placedIds.Add(col.Id.IntegerValue); }
                curZ += opts.Post2Height;
            }
            if (opts.Post3Height > 0 && opts.Post3Count > 0)
            {
                for (int i = 0; i < opts.Post3Count; i++)
                {
                    var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post3Height);
                    if (col != null) { ConfigureAlincoColumn(col, opts.Post3Height); placedIds.Add(col.Id.IntegerValue); }
                    curZ += opts.Post3Height;
                }
            }
            if (opts.Post4Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post4Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post4Height); placedIds.Add(col.Id.IntegerValue); }
                curZ += opts.Post4Height;
            }
            if (opts.Post5Height > 0)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(curZ)), 0, "h", opts.Post5Height);
                if (col != null) { ConfigureAlincoColumn(col, opts.Post5Height); placedIds.Add(col.Id.IntegerValue); }
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

        /// <summary>
        /// ĐẶT INSTANCE CHUẨN XÁC VỊ TRÍ, CAO ĐỘ VÀ GÓC XOAY TRONG MẶT PHẲNG
        /// </summary>
        private static FamilyInstance PlaceInstance(Document doc, FamilySymbol symbol, Level level, XYZ pt, double rotDeg, string paramName, double paramValMm)
        {
            FamilyInstance inst = level != null ?
                doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.NonStructural) :
                doc.Create.NewFamilyInstance(pt, symbol, StructuralType.NonStructural);

            if (inst != null)
            {
                // 1. Khóa chuẩn xác cao độ tuyệt đối (Elevation from Level / INSTANCE_FREE_HOST_OFFSET_PARAM)
                Parameter pOffset = inst.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)
                                 ?? inst.LookupParameter("Elevation from Level")
                                 ?? inst.LookupParameter("Offset");
                if (pOffset != null && !pOffset.IsReadOnly)
                {
                    double targetOffset = (level != null) ? (pt.Z - level.Elevation) : pt.Z;
                    pOffset.Set(targetOffset);
                }

                // 2. Xoay góc trong mặt phẳng XY quanh trục Z
                if (Math.Abs(rotDeg) > 0.001)
                {
                    Line axis = Line.CreateBound(pt, new XYZ(pt.X, pt.Y, pt.Z + 10.0));
                    ElementTransformUtils.RotateElement(doc, inst.Id, axis, rotDeg * Math.PI / 180.0);
                }

                // 3. Gán kích thước chiều dài / chiều cao nếu có
                if (!string.IsNullOrEmpty(paramName) && paramValMm > 0)
                {
                    SetParam(inst, paramName, paramValMm);
                }
            }

            return inst;
        }

        private static void SetParam(FamilyInstance inst, string paramName, double valMm)
        {
            if (inst == null) return;
            Parameter p = inst.LookupParameter(paramName) ??
                          inst.LookupParameter(paramName.ToLower()) ??
                          inst.LookupParameter(paramName.ToUpper());

            if (p != null && !p.IsReadOnly)
            {
                p.Set(MmToFt(valMm));
            }
        }

        private static string ExecuteMaster6CornersContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            double jackH = opts.JackHeight > 0 ? opts.JackHeight : 250.0;

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

                    var fDeck = PlaceInstance(doc, sym4, level, new XYZ(MmToFt(tx), MmToFt(ty), MmToFt(zDeck)), trot, "l", tlen);
                    if (fDeck != null) placedIds.Add(fDeck.Id.IntegerValue);
                    if (opts != null && opts.HasTopTransom)
                    {
                        double topZ = z0 + jackH + 2630;
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
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 7200, y0 + 1200, z0, 1200, jackH, 1800, true);

            // Bay 2 of Leg 5: X in [x0 + 9000, x0 + 10800] (chuan du 1800 mm dung logic Nhom 2!)
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 9000, y0 + 1200, z0, 1200, jackH, 1800, true);

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
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 12600, y0 + 6600, z0, 1200, jackH, 1800, true);

            // Bay 2 of Leg 7: X in [x0 + 14400, x0 + 16200]
            BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7, x0 + 14400, y0 + 6600, z0, 1200, jackH, 1800, true);

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
