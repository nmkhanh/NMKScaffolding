using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Async;
using AppSettings = RevitScaffoldingTool.Properties.Settings;

namespace RevitScaffoldingTool.ViewModels
{
    public partial class ScaffoldingViewModel : ObservableObject
    {
        private readonly UIApplication _uiApp;

        // Bảng hệ giàn (DataGrid - Cột Trái)
        public ObservableCollection<BayRowModel> BayRows { get; } = new ObservableCollection<BayRowModel>();
        [ObservableProperty] private BayRowModel _selectedBayRow;
        public List<string> ConnectCases { get; } = new List<string> { "TH 0", "TH 1.1", "TH 1.2", "TH 1.3", "TH 1.4", "TH 2.1", "TH 2.2" };

        // Vị trí, Chân chống & Cao độ Z (Cột Giữa - Data)
        [ObservableProperty] private double _elevationZ = 0.0;
        [ObservableProperty] private double _jackHeight = 250.0;
        [ObservableProperty] private double _bayWidth = 1200.0;
        [ObservableProperty] private string _familyFolder = @"H:\My Drive\Family\New folder\Child";

        // 6 ô cấu hình cột chống từ dưới lên trên (mặc định = 0)
        [ObservableProperty] private double _post1Height = 0.0;
        [ObservableProperty] private double _post2Height = 0.0;
        [ObservableProperty] private int _post3Count = 0;
        [ObservableProperty] private double _post3Height = 0.0;
        [ObservableProperty] private double _post4Height = 0.0;
        [ObservableProperty] private double _post5Height = 0.0;

        // Số tầng & Giằng đỉnh
        [ObservableProperty] private int _storyCount = 1;
        [ObservableProperty] private bool _hasTopTransom = false;

        // Tùy chọn chạy theo dữ liệu nhập (From data) — ÁP DỤNG RIÊNG CHO NHÓM NÚT BÊN PHẢI
        [ObservableProperty] private bool _isFromData = true;

        // 7 chuỗi nhịp L0..L6 và bề rộng W cho nhóm Button bên phải:
        // TH 1.1: L0 => L1 | TH 1.2: L1 => L2 | TH 1.3: L2 => L3 | TH 1.4: L3 => L4 | TH 2.1: L4 => L5 | TH 2.2: L5 => L6
        [ObservableProperty] private string _buttonL0 = "1800,1800";
        [ObservableProperty] private string _buttonL1 = "1800,1200";
        [ObservableProperty] private string _buttonL2 = "1800,1800";
        [ObservableProperty] private string _buttonL3 = "1800,1200";
        [ObservableProperty] private string _buttonL4 = "1800,1800";
        [ObservableProperty] private string _buttonL5 = "1800,2x1800";
        [ObservableProperty] private string _buttonL6 = "1800,2x1800";
        [ObservableProperty] private double _buttonW = 1200.0;

        // 6 CheckBox cho 6 trường hợp (mặc định true) dùng cho nút Master chạy nối tiếp (Continue)
        [ObservableProperty] private bool _checkCase11 = true;
        [ObservableProperty] private bool _checkCase12 = true;
        [ObservableProperty] private bool _checkCase13 = true;
        [ObservableProperty] private bool _checkCase14 = true;
        [ObservableProperty] private bool _checkCase21 = true;
        [ObservableProperty] private bool _checkCase22 = true;

        // Trạng thái thực thi
        [ObservableProperty] private string _statusMessage = "Sẵn sàng thi công giàn giáo ALINCO (Dark Theme).";
        [ObservableProperty] private bool _isBusy = false;

        public ScaffoldingViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            LoadSettings();
            InitDefaultTableRows();
        }

        private void InitDefaultTableRows()
        {
            BayRows.Clear();
            string[] defaultBays = new string[] { ButtonL0, ButtonL1, ButtonL2, ButtonL3, ButtonL4, ButtonL5, ButtonL6 };
            string[] defaultConnects = new string[] { "TH 1.1", "TH 1.2", "TH 1.3", "TH 1.4", "TH 2.1", "TH 2.2", "TH 0" };
            for (int i = 0; i < defaultBays.Length; i++)
            {
                string expr = !string.IsNullOrWhiteSpace(defaultBays[i]) ? defaultBays[i] : "1800";
                BayRows.Add(new BayRowModel
                {
                    Stt = i + 1,
                    ElementId = "L" + i,
                    BayList = expr,
                    ConnectCase = defaultConnects[i],
                    Width = ButtonW > 0 ? ButtonW : 1200.0,
                    BayLengths = ScaffoldingBuilder.ParseBayLengths(expr, 1800.0)
                });
            }
            if (BayRows.Count > 0) SelectedBayRow = BayRows[0];
        }

        public void LoadSettings()
        {
            try
            {
                var s = AppSettings.Default;
                ElevationZ = s.ElevationZ;
                JackHeight = s.JackHeight > 0 ? s.JackHeight : 250.0;
                Post1Height = s.Post1Height;
                Post2Height = s.Post2Height;
                Post3Count = s.Post3Count;
                Post3Height = s.Post3Height;
                Post4Height = s.Post4Height;
                Post5Height = s.Post5Height;
                StoryCount = s.StoryCount > 0 ? s.StoryCount : 1;
                HasTopTransom = s.HasTopTransom;
                IsFromData = s.IsFromData;

                ButtonL0 = !string.IsNullOrWhiteSpace(s.ButtonL0) ? s.ButtonL0 : "1800,1800";
                ButtonL1 = !string.IsNullOrWhiteSpace(s.ButtonL1) ? s.ButtonL1 : "1800,1200";
                ButtonL2 = !string.IsNullOrWhiteSpace(s.ButtonL2) ? s.ButtonL2 : "1800,1800";
                ButtonL3 = !string.IsNullOrWhiteSpace(s.ButtonL3) ? s.ButtonL3 : "1800,1200";
                ButtonL4 = !string.IsNullOrWhiteSpace(s.ButtonL4) ? s.ButtonL4 : "1800,1800";
                ButtonL5 = !string.IsNullOrWhiteSpace(s.ButtonL5) ? s.ButtonL5 : "1800,2x1800";
                ButtonL6 = !string.IsNullOrWhiteSpace(s.ButtonL6) ? s.ButtonL6 : "1800,2x1800";
                ButtonW = s.ButtonW > 0 ? s.ButtonW : 1200.0;

                CheckCase11 = s.CheckCase11;
                CheckCase12 = s.CheckCase12;
                CheckCase13 = s.CheckCase13;
                CheckCase14 = s.CheckCase14;
                CheckCase21 = s.CheckCase21;
                CheckCase22 = s.CheckCase22;
            }
            catch
            {
                // Fallback nếu settings lần đầu chưa khởi tạo
            }
        }

        public void SaveSettings()
        {
            try
            {
                var s = AppSettings.Default;
                s.ElevationZ = ElevationZ;
                s.JackHeight = JackHeight;
                s.Post1Height = Post1Height;
                s.Post2Height = Post2Height;
                s.Post3Count = Post3Count;
                s.Post3Height = Post3Height;
                s.Post4Height = Post4Height;
                s.Post5Height = Post5Height;
                s.StoryCount = StoryCount;
                s.HasTopTransom = HasTopTransom;
                s.IsFromData = IsFromData;

                s.ButtonL0 = !string.IsNullOrWhiteSpace(ButtonL0) ? ButtonL0 : "1800,1800";
                s.ButtonL1 = !string.IsNullOrWhiteSpace(ButtonL1) ? ButtonL1 : "1800,1200";
                s.ButtonL2 = !string.IsNullOrWhiteSpace(ButtonL2) ? ButtonL2 : "1800,1800";
                s.ButtonL3 = !string.IsNullOrWhiteSpace(ButtonL3) ? ButtonL3 : "1800,1200";
                s.ButtonL4 = !string.IsNullOrWhiteSpace(ButtonL4) ? ButtonL4 : "1800,1800";
                s.ButtonL5 = !string.IsNullOrWhiteSpace(ButtonL5) ? ButtonL5 : "1800,2x1800";
                s.ButtonL6 = !string.IsNullOrWhiteSpace(ButtonL6) ? ButtonL6 : "1800,2x1800";
                s.ButtonW = ButtonW;

                s.CheckCase11 = CheckCase11;
                s.CheckCase12 = CheckCase12;
                s.CheckCase13 = CheckCase13;
                s.CheckCase14 = CheckCase14;
                s.CheckCase21 = CheckCase21;
                s.CheckCase22 = CheckCase22;
                s.Save();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Tạo cấu hình thực thi cho nhóm Button bên phải:
        /// - TH 1.1 (-2): L0 => L1
        /// - TH 1.2 (-4): L1 => L2
        /// - TH 1.3 (-5): L2 => L3
        /// - TH 1.4 (-6): L3 => L4
        /// - TH 2.1 (-3): L4 => L5
        /// - TH 2.2 (-7): L5 => L6
        /// - MASTER (-8): Chạy nối tiếp (continue) các TH có CheckBox == true với L0 .. L6
        /// </summary>
        private BuildOptions CreateOptionsForButtons(int mode)
        {
            SaveSettings();

            List<double> l0Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL0, 1800.0);
            List<double> l1Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL1, 1800.0);
            List<double> l2Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL2, 1800.0);
            List<double> l3Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL3, 1800.0);
            List<double> l4Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL4, 1800.0);
            List<double> l5Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL5, 1800.0);
            List<double> l6Bays = ScaffoldingBuilder.ParseBayLengths(ButtonL6, 1800.0);

            List<double> caseL1Bays = l0Bays;
            List<double> caseL2Bays = l1Bays;

            switch (mode)
            {
                case -2: // TH 1.1: L0 => L1
                    caseL1Bays = l0Bays;
                    caseL2Bays = l1Bays;
                    break;
                case -4: // TH 1.2: L1 => L2
                    caseL1Bays = l1Bays;
                    caseL2Bays = l2Bays;
                    break;
                case -5: // TH 1.3: L2 => L3
                    caseL1Bays = l2Bays;
                    caseL2Bays = l3Bays;
                    break;
                case -6: // TH 1.4: L3 => L4
                    caseL1Bays = l3Bays;
                    caseL2Bays = l4Bays;
                    break;
                case -3: // TH 2.1: L4 => L5
                    caseL1Bays = l4Bays;
                    caseL2Bays = l5Bays;
                    break;
                case -7: // TH 2.2: L5 => L6
                    caseL1Bays = l5Bays;
                    caseL2Bays = l6Bays;
                    break;
            }

            double l1Last = caseL1Bays[caseL1Bays.Count - 1];
            double l2First = caseL2Bays[0];
            double w = ButtonW > 0 ? ButtonW : 1200.0;

            return new BuildOptions
            {
                X0 = 0.0,
                Y0 = 0.0,
                Z0 = IsFromData ? ElevationZ : 0.0,
                BayWidth = w,
                L1 = l1Last,
                L2 = l2First,
                L0Bays = l0Bays,
                L1Bays = mode == -8 ? l1Bays : caseL1Bays,
                L2Bays = mode == -8 ? l2Bays : caseL2Bays,
                L3Bays = l3Bays,
                L4Bays = l4Bays,
                L5Bays = l5Bays,
                L6Bays = l6Bays,
                CheckCase11 = CheckCase11,
                CheckCase12 = CheckCase12,
                CheckCase13 = CheckCase13,
                CheckCase14 = CheckCase14,
                CheckCase21 = CheckCase21,
                CheckCase22 = CheckCase22,
                JackHeight = IsFromData ? JackHeight : 250.0,
                BayLengths = new List<double>(caseL1Bays),
                FamilyFolder = string.IsNullOrWhiteSpace(FamilyFolder) ? @"H:\My Drive\Family\New folder\Child" : FamilyFolder,
                Mode = mode,
                IsFromData = IsFromData,
                Post1Height = IsFromData ? Post1Height : 0,
                Post2Height = IsFromData ? Post2Height : 0,
                Post3Count = IsFromData ? Post3Count : 0,
                Post3Height = IsFromData ? Post3Height : 0,
                Post4Height = IsFromData ? Post4Height : 0,
                Post5Height = IsFromData ? Post5Height : 0,
                StoryCount = IsFromData ? (StoryCount > 0 ? StoryCount : 1) : 1,
                HasTopTransom = IsFromData && HasTopTransom
            };
        }

        [RelayCommand]
        private async Task SelectInstancesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Vui lòng quét chọn các instance Review_DetailLine trong Revit...";

            try
            {
                var rows = await RevitTask.RunAsync(app =>
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    Document doc = uidoc != null ? uidoc.Document : null;
                    if (doc == null) return new List<BayRowModel>();

                    List<FamilyInstance> instances = new List<FamilyInstance>();
                    try
                    {
                        var refs = uidoc.Selection.PickObjects(ObjectType.Element, "Quét chọn các instance Review_DetailLine");
                        if (refs != null && refs.Count > 0)
                        {
                            foreach (var r in refs)
                            {
                                if (doc.GetElement(r) is FamilyInstance fi)
                                {
                                    instances.Add(fi);
                                }
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                    }

                    if (instances.Count == 0)
                    {
                        var collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                            .OfClass(typeof(FamilyInstance))
                            .Cast<FamilyInstance>()
                            .Where(fi => fi.Symbol != null && (fi.Symbol.FamilyName == "Review_DetailLine" || fi.Symbol.Name == "Review_DetailLine" || fi.Name == "Review_DetailLine"));
                        instances.AddRange(collector);
                    }

                    var resultRows = new List<BayRowModel>();
                    int idx = 1;
                    foreach (var inst in instances)
                    {
                        var bayParts = new List<string>();
                        var bayLengths = new List<double>();

                        for (int i = 1; i <= 10; i++)
                        {
                            Parameter pGroup = inst.LookupParameter("Group_" + i);
                            bool isTrue = pGroup != null && pGroup.AsInteger() == 1;
                            if (isTrue)
                            {
                                Parameter pN = inst.LookupParameter("Group_" + i + "_N");
                                Parameter pL = inst.LookupParameter("Group_" + i + "_L");

                                int n = (pN != null && pN.AsInteger() > 0) ? pN.AsInteger() : 1;
                                double lMm = 0;
                                if (pL != null && pL.HasValue)
                                {
                                    lMm = Math.Round(pL.AsDouble() * 304.8, 0);
                                }
                                else
                                {
                                    Parameter pLi = inst.LookupParameter("L" + i);
                                    if (pLi != null && pLi.HasValue) lMm = Math.Round(pLi.AsDouble() * 304.8, 0);
                                }

                                bayParts.Add(n == 1 ? string.Format("{0}", lMm) : string.Format("{0}x{1}", n, lMm));
                                for (int k = 0; k < n; k++) bayLengths.Add(lMm);
                            }
                        }

                        double len = 0;
                        Parameter pLen = inst.LookupParameter("Length");
                        if (pLen != null && pLen.HasValue) len = Math.Round(pLen.AsDouble() * 304.8, 0);

                        double w = 1200;
                        Parameter pW = inst.LookupParameter("W");
                        if (pW != null && pW.HasValue) w = Math.Round(pW.AsDouble() * 304.8, 0);

                        double sX = 0, sY = 0, sZ = 0, eX = 0, eY = 0, eZ = 0, angleDeg = 0;
                        LocationCurve locCurve = inst.Location as LocationCurve;
                        if (locCurve != null && locCurve.Curve != null)
                        {
                            XYZ p0 = locCurve.Curve.GetEndPoint(0);
                            XYZ p1 = locCurve.Curve.GetEndPoint(1);
                            sX = Math.Round(p0.X * 304.8, 1);
                            sY = Math.Round(p0.Y * 304.8, 1);
                            sZ = Math.Round(p0.Z * 304.8, 1);
                            eX = Math.Round(p1.X * 304.8, 1);
                            eY = Math.Round(p1.Y * 304.8, 1);
                            eZ = Math.Round(p1.Z * 304.8, 1);
                            XYZ dir = (p1 - p0).Normalize();
                            angleDeg = Math.Round(Math.Atan2(dir.Y, dir.X) * 180.0 / Math.PI, 1);
                        }

                        resultRows.Add(new BayRowModel
                        {
                            Stt = idx++,
                            ElementId = inst.Id.IntegerValue.ToString(),
                            BayList = bayParts.Count > 0 ? string.Join(",", bayParts) : "1800",
                            ConnectCase = "TH 0",
                            Length = len,
                            Width = w > 0 ? w : 1200,
                            BayLengths = bayLengths.Count > 0 ? bayLengths : new List<double> { 1800 },
                            StartX = sX,
                            StartY = sY,
                            StartZ = sZ,
                            EndX = eX,
                            EndY = eY,
                            EndZ = eZ,
                            AngleDeg = angleDeg
                        });
                    }

                    // Xác định Connect (TH) cho từng hàng dựa trực tiếp vào dữ liệu hình học & nhịp quét được từ Revit:
                    // - Hàng i (0 .. Count-2) xét kết nối với hàng i+1
                    // - Hàng cuối (Count-1) xét kết nối với hàng đầu (0) nếu có từ 3 hàng trở lên; nếu không nối thì trả về "TH 0"
                    for (int i = 0; i < resultRows.Count - 1; i++)
                    {
                        resultRows[i].ConnectCase = DetermineConnectCase(resultRows[i], resultRows[i + 1]);
                    }

                    if (resultRows.Count > 0)
                    {
                        var lastRow = resultRows[resultRows.Count - 1];
                        if (resultRows.Count >= 3)
                        {
                            lastRow.ConnectCase = DetermineConnectCase(lastRow, resultRows[0]);
                        }
                        else
                        {
                            lastRow.ConnectCase = "TH 0";
                        }
                    }

                    return resultRows;
                });

                BayRows.Clear();
                foreach (var r in rows)
                {
                    BayRows.Add(r);
                }

                if (BayRows.Count > 0)
                {
                    SelectedBayRow = BayRows[0];
                    if (BayRows.Count > 0) ButtonL0 = BayRows[0].BayList;
                    if (BayRows.Count > 1) ButtonL1 = BayRows[1].BayList;
                    if (BayRows.Count > 2) ButtonL2 = BayRows[2].BayList;
                    if (BayRows.Count > 3) ButtonL3 = BayRows[3].BayList;
                    if (BayRows.Count > 4) ButtonL4 = BayRows[4].BayList;
                    if (BayRows.Count > 5) ButtonL5 = BayRows[5].BayList;
                    if (BayRows.Count > 6) ButtonL6 = BayRows[6].BayList;
                    StatusMessage = string.Format("Đã quét thành công {0} hệ giàn từ Revit (Row cuối Connect = {1}).", BayRows.Count, BayRows[BayRows.Count - 1].ConnectCase);
                }
                else
                {
                    StatusMessage = "Không tìm thấy instance Review_DetailLine nào.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi quét instance: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Xác định trường hợp kết nối góc (TH 1.1, TH 1.2, TH 1.3, TH 1.4, TH 2.1, TH 2.2 hoặc TH 0)
        /// dựa trực tiếp trên dữ liệu hình học (Start, End, góc rẽ, bề rộng W) và danh sách nhịp quét được từ 2 instance Review_DetailLine.
        /// </summary>
        private static string DetermineConnectCase(BayRowModel rowA, BayRowModel rowB)
        {
            if (rowA == null || rowB == null) return "TH 0";

            double dxA = rowA.EndX - rowA.StartX;
            double dyA = rowA.EndY - rowA.StartY;
            double lenVecA = Math.Sqrt(dxA * dxA + dyA * dyA);

            double dxB = rowB.EndX - rowB.StartX;
            double dyB = rowB.EndY - rowB.StartY;
            double lenVecB = Math.Sqrt(dxB * dxB + dyB * dyB);

            if (lenVecA < 1.0 || lenVecB < 1.0) return "TH 0";

            // Vector đơn vị dọc theo dãy A (uA) và pháp tuyến phải (nA: hướng từ mép trước d=0 về mép sau d=W)
            double uAx = dxA / lenVecA;
            double uAy = dyA / lenVecA;
            double nAx = uAy;
            double nAy = -uAx;

            double uBx = dxB / lenVecB;
            double uBy = dyB / lenVecB;

            double totalBayLenA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0)
                ? rowA.BayLengths.Sum()
                : (rowA.Length > 0 ? rowA.Length : lenVecA);
            double lastBayA = (rowA.BayLengths != null && rowA.BayLengths.Count > 0)
                ? rowA.BayLengths[rowA.BayLengths.Count - 1]
                : totalBayLenA;

            double wA = rowA.Width > 0 ? rowA.Width : 1200.0;
            double wB = rowB.Width > 0 ? rowB.Width : 1200.0;

            // Tọa độ tương đối của điểm đầu dãy B trong hệ trục địa phương của dãy A
            double relX = rowB.StartX - rowA.StartX;
            double relY = rowB.StartY - rowA.StartY;
            double s = relX * uAx + relY * uAy;
            double d = relX * nAx + relY * nAy;

            // Tích có hướng 2D: cross > 0.5 => Rẽ trái (+90° CCW), cross < -0.5 => Rẽ phải (-90° CW)
            double cross = uAx * uBy - uAy * uBx;
            if (Math.Abs(cross) < 0.5) return "TH 0";

            // Kiểm tra khoảng cách ngang d phải nằm trong phạm vi bề rộng giàn A
            if (d < -wA - 400.0 || d > wA + 400.0) return "TH 0";

            // Tọa độ tâm bề rộng giàn B chiếu lên phương dọc của giàn A:
            // - Khi rẽ trái (cross > 0): pháp tuyến phải của B cùng chiều uA -> B chiếm [s, s + wB]
            // - Khi rẽ phải (cross < 0): pháp tuyến phải của B ngược chiều uA -> B chiếm [s - wB, s]
            double sMid = (cross > 0) ? (s + 0.5 * wB) : (s - 0.5 * wB);

            // Kiểm tra vị trí dọc sMid phải nằm gần cuối dãy A (trong nhịp cuối hoặc vươn ra ngoài đầu dãy A)
            if (sMid < totalBayLenA - wB - 400.0 || sMid > totalBayLenA + wB + 400.0) return "TH 0";

            bool isOuterCorner = sMid >= (totalBayLenA - 100.0);
            bool isLastBayEqualW = Math.Abs(lastBayA - wB) <= 50.0;
            bool isVerticalA = Math.Abs(uAy) > Math.Abs(uAx);

            if (isOuterCorner)
            {
                // Nhóm 2: Góc vươn ngoài đầu dãy A ([L_A, L_A + W_B])
                if (cross > 0)
                {
                    return "TH 2.1";
                }
                return (uBx + uBy >= 0.0) ? "TH 2.1" : "TH 2.2";
            }
            else
            {
                // Nhóm 1: Góc thu trong nhịp cuối của dãy A ([L_A - W_B, L_A])
                if (cross > 0)
                {
                    return (isLastBayEqualW || isVerticalA) ? "TH 1.2" : "TH 1.1";
                }
                else
                {
                    return (isLastBayEqualW || isVerticalA) ? "TH 1.4" : "TH 1.3";
                }
            }
        }

        private BuildOptions CreateOptionsForTableRun()
        {
            SaveSettings();

            double w = ButtonW > 0 ? ButtonW : 1200.0;
            var tableRows = new List<TableRowOption>();
            foreach (var r in BayRows)
            {
                if (r == null) continue;
                var bays = ScaffoldingBuilder.ParseBayLengths(r.BayList, 1800.0);
                bool hasCoords = Math.Abs(r.StartX) > 0.01 || Math.Abs(r.StartY) > 0.01 || Math.Abs(r.EndX) > 0.01 || Math.Abs(r.EndY) > 0.01;
                tableRows.Add(new TableRowOption
                {
                    BayLengths = bays,
                    ConnectCase = string.IsNullOrWhiteSpace(r.ConnectCase) ? "TH 0" : r.ConnectCase,
                    Width = r.Width > 0 ? r.Width : w,
                    StartX = r.StartX,
                    StartY = r.StartY,
                    HasCoords = hasCoords
                });
            }

            return new BuildOptions
            {
                X0 = 0.0,
                Y0 = 0.0,
                Z0 = ElevationZ,
                BayWidth = w,
                TableRows = tableRows,
                JackHeight = JackHeight > 0 ? JackHeight : 250.0,
                FamilyFolder = string.IsNullOrWhiteSpace(FamilyFolder) ? @"H:\My Drive\Family\New folder\Child" : FamilyFolder,
                Mode = -9,
                IsFromData = true,
                Post1Height = Post1Height,
                Post2Height = Post2Height,
                Post3Count = Post3Count,
                Post3Height = Post3Height,
                Post4Height = Post4Height,
                Post5Height = Post5Height,
                StoryCount = StoryCount > 0 ? StoryCount : 1,
                HasTopTransom = HasTopTransom
            };
        }

        [RelayCommand]
        private async Task RunBaysAsync()
        {
            if (IsBusy) return;
            if (BayRows.Count == 0)
            {
                StatusMessage = "Bảng hệ giàn trống! Vui lòng bấm Quét (Select) hoặc nhập dòng vào bảng trước.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Format("Đang chạy Button Run theo Bảng Table ({0} hàng)...", BayRows.Count);

            try
            {
                BuildOptions opts = CreateOptionsForTableRun();
                BuildResult res = await RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument != null ? app.ActiveUIDocument.Document : null;
                    if (doc == null)
                    {
                        return new BuildResult { Success = false, Message = "Không tìm thấy Document Revit đang mở." };
                    }
                    return ScaffoldingBuilder.ExecuteBuild(doc, opts);
                });

                StatusMessage = string.Format("[Table Run] — {0}", res.Message);
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi thi công Table Run: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task PickFaceForZAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Vui lòng chọn 1 mặt phẳng (Face) trong Revit...";

            try
            {
                double? pickedZ = await RevitTask.RunAsync(app =>
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    if (uidoc == null) return (double?)null;

                    try
                    {
                        Reference faceRef = uidoc.Selection.PickObject(ObjectType.Face, "Chọn mặt phẳng (Face) để lấy cao độ Z");
                        if (faceRef != null)
                        {
                            XYZ pt = faceRef.GlobalPoint;
                            return pt.Z * 304.8;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                    }
                    return null;
                });

                if (pickedZ.HasValue)
                {
                    ElevationZ = Math.Round(pickedZ.Value, 1);
                    SaveSettings();
                    StatusMessage = string.Format("Đã lấy cao độ Z từ Face: {0:F1} mm", ElevationZ);
                }
                else
                {
                    StatusMessage = "Đã hủy chọn Face.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi chọn Face: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunBuildModeAsync(int mode, string modeName)
        {
            if (IsBusy) return;
            IsBusy = true;
            string runType = IsFromData ? "[From Data]" : "[Default]";
            StatusMessage = string.Format("Đang thi công {0}: {1}...", runType, modeName);

            try
            {
                BuildOptions opts = CreateOptionsForButtons(mode);
                BuildResult res = await RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument != null ? app.ActiveUIDocument.Document : null;
                    if (doc == null)
                    {
                        return new BuildResult { Success = false, Message = "Không tìm thấy Document Revit đang mở." };
                    }
                    return ScaffoldingBuilder.ExecuteBuild(doc, opts);
                });

                StatusMessage = string.Format("{0} — {1}", runType, res.Message);
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi thi công: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task BuildMasterAsync() => RunBuildModeAsync(-8, "MASTER — CONTINUE CÁC TH ĐƯỢC TÍCH CHỌN (L0 -> L6)");

        [RelayCommand]
        private Task BuildCase11Async() => RunBuildModeAsync(-2, "TH 1.1 (L0 => L1): Khớp góc +Y (Mép Ngoài Phẳng)");

        [RelayCommand]
        private Task BuildCase12Async() => RunBuildModeAsync(-4, "TH 1.2 (L1 => L2): Khớp góc +X (Thu góc)");

        [RelayCommand]
        private Task BuildCase13Async() => RunBuildModeAsync(-5, "TH 1.3 (L2 => L3): Khớp góc -Y (Giằng thu ngắn)");

        [RelayCommand]
        private Task BuildCase14Async() => RunBuildModeAsync(-6, "TH 1.4 (L3 => L4): Khớp góc +X (Thu góc)");

        [RelayCommand]
        private Task BuildCase21Async() => RunBuildModeAsync(-3, "TH 2.1 (L4 => L5): Góc rẽ +Y (Vươn ngoài)");

        [RelayCommand]
        private Task BuildCase22Async() => RunBuildModeAsync(-7, "TH 2.2 (L5 => L6): Góc rẽ +X (Vươn ngoài -Y)");
    }
}
