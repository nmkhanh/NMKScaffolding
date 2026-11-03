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
        public List<string> ConnectCases { get; } = new List<string> { "TH 1.1", "TH 1.2", "TH 1.3", "TH 1.4", "TH 2.1", "TH 2.2" };

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

        // Thông số nhịp & bề rộng riêng cho nhóm Button bên phải
        [ObservableProperty] private double _buttonL1 = 1800.0;
        [ObservableProperty] private double _buttonL2 = 1800.0;
        [ObservableProperty] private double _buttonW = 1200.0;

        // Trạng thái thực thi
        [ObservableProperty] private string _statusMessage = "Sẵn sàng thi công giàn giáo ALINCO.";
        [ObservableProperty] private bool _isBusy = false;

        public ScaffoldingViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            LoadSettings();
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
                ButtonL1 = s.ButtonL1 > 0 ? s.ButtonL1 : 1800.0;
                ButtonL2 = s.ButtonL2 > 0 ? s.ButtonL2 : 1800.0;
                ButtonW = s.ButtonW > 0 ? s.ButtonW : 1200.0;
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
                s.ButtonL1 = ButtonL1;
                s.ButtonL2 = ButtonL2;
                s.ButtonW = ButtonW;
                s.Save();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Tạo cấu hình thực thi cho nhóm Button bên phải (tuân thủ Checkbox IsFromData).
        /// </summary>
        private BuildOptions CreateOptionsForButtons(int mode)
        {
            SaveSettings();

            double l1 = ButtonL1 > 0 ? ButtonL1 : 1800.0;
            double l2 = ButtonL2 > 0 ? ButtonL2 : 1800.0;
            double w = ButtonW > 0 ? ButtonW : 1200.0;

            if (!IsFromData)
            {
                // Chế độ Default (chạy theo thông số mặc định của từng trường hợp)
                return new BuildOptions
                {
                    X0 = 0.0,
                    Y0 = 0.0,
                    Z0 = 0.0,
                    BayWidth = w,
                    L1 = l1,
                    L2 = l2,
                    JackHeight = 250.0,
                    BayLengths = new List<double> { l1 },
                    FamilyFolder = string.IsNullOrWhiteSpace(FamilyFolder) ? @"H:\My Drive\Family\New folder\Child" : FamilyFolder,
                    Mode = mode,
                    IsFromData = false,
                    Post1Height = 0,
                    Post2Height = 0,
                    Post3Count = 0,
                    Post3Height = 0,
                    Post4Height = 0,
                    Post5Height = 0,
                    StoryCount = 1,
                    HasTopTransom = false
                };
            }
            else
            {
                // Chế độ From Data (chạy theo thông số cấu hình ở Cột Data giữa)
                return new BuildOptions
                {
                    X0 = 0.0,
                    Y0 = 0.0,
                    Z0 = ElevationZ,
                    BayWidth = w,
                    L1 = l1,
                    L2 = l2,
                    JackHeight = JackHeight,
                    BayLengths = new List<double> { l1 },
                    FamilyFolder = string.IsNullOrWhiteSpace(FamilyFolder) ? @"H:\My Drive\Family\New folder\Child" : FamilyFolder,
                    Mode = mode,
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
        }

        /// <summary>
        /// Tạo cấu hình thực thi cho nút Run bên Table.
        /// </summary>
        private BuildOptions CreateOptionsForTableRun(int mode, BayRowModel row)
        {
            SaveSettings();

            var lengths = (row != null && row.BayLengths != null && row.BayLengths.Count > 0)
                ? row.BayLengths
                : new List<double> { 1800, 1200, 1800, 1200, 1800 };

            double w = (row != null && row.Width > 0) ? row.Width : (BayWidth > 0 ? BayWidth : 1200.0);

            return new BuildOptions
            {
                X0 = row != null ? row.StartX : 0.0,
                Y0 = row != null ? row.StartY : 0.0,
                Z0 = ElevationZ,
                BayWidth = w,
                JackHeight = JackHeight,
                BayLengths = lengths,
                FamilyFolder = string.IsNullOrWhiteSpace(FamilyFolder) ? @"H:\My Drive\Family\New folder\Child" : FamilyFolder,
                Mode = mode,
                IsFromData = true, // Nút Run bên table luôn lấy data để dựng
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
                        // Người dùng ESC / không chọn -> Tự động quét toàn bộ trong active view
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

                                bayParts.Add(string.Format("{0}x{1}", n, lMm));
                                for (int k = 0; k < n; k++) bayLengths.Add(lMm);
                            }
                        }

                        // Gợi ý trường hợp Connect theo thứ tự ban đầu
                        string defaultCase = "TH 1.1";
                        if (idx == 2) defaultCase = "TH 1.2";
                        else if (idx == 3) defaultCase = "TH 1.3";
                        else if (idx == 4) defaultCase = "TH 1.4";
                        else if (idx == 5) defaultCase = "TH 2.1";
                        else if (idx == 6) defaultCase = "TH 2.2";

                        double len = 0;
                        Parameter pLen = inst.LookupParameter("Length");
                        if (pLen != null && pLen.HasValue) len = Math.Round(pLen.AsDouble() * 304.8, 0);

                        double w = 1200;
                        Parameter pW = inst.LookupParameter("W");
                        if (pW != null && pW.HasValue) w = Math.Round(pW.AsDouble() * 304.8, 0);

                        // Trích xuất tọa độ điểm đầu, điểm cuối và góc xoay hướng tuyến
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
                            BayList = bayParts.Count > 0 ? string.Join(", ", bayParts) : "1x1800",
                            ConnectCase = defaultCase,
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
                    StatusMessage = string.Format("Đã quét thành công {0} hệ giàn từ Revit.", BayRows.Count);
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
        /// NÚT RUN TRÊN BẢNG TABLE:
        /// Thực thi theo Pipeline chuẩn: TH thẳng Row 1 -> TH thẳng Row 2 -> Connect (Row 1-2) -> TH thẳng Row 3 -> Connect (Row 2-3) -> ...
        /// Dùng data từ Table và Cột Giữa để dựng.
        /// </summary>
        [RelayCommand]
        private async Task RunBaysAsync()
        {
            if (IsBusy) return;
            if (BayRows.Count == 0)
            {
                StatusMessage = "Vui lòng bấm 'Quét (Select)' để tải dữ liệu hệ giàn trước khi Run.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Format("Đang thi công {0} hệ giàn theo Table Pipeline (Thẳng -> Connect)...", BayRows.Count);

            try
            {
                var targetRow = SelectedBayRow ?? BayRows[0];
                BuildOptions opts = CreateOptionsForTableRun(-100, targetRow);
                var rowsList = BayRows.ToList();

                BuildResult res = await RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument != null ? app.ActiveUIDocument.Document : null;
                    if (doc == null)
                    {
                        return new BuildResult { Success = false, Message = "Không tìm thấy Document Revit đang mở." };
                    }
                    return ScaffoldingBuilder.ExecuteTableBuild(doc, opts, rowsList);
                });

                StatusMessage = res.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi thi công Table Pipeline: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static int GetModeFromConnectCase(string connectCase)
        {
            switch (connectCase)
            {
                case "TH 1.1": return -2;
                case "TH 1.2": return -4;
                case "TH 1.3": return -5;
                case "TH 1.4": return -6;
                case "TH 2.1": return -3;
                case "TH 2.2": return -7;
                default: return -2;
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
                            return pt.Z * 304.8; // Đổi sang mm
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // Người dùng nhấn ESC
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
        private Task BuildMasterAsync() => RunBuildModeAsync(-8, "MASTER - LIÊN TỤC FULL 6 TRƯỜNG HỢP");

        [RelayCommand]
        private Task BuildCase11Async() => RunBuildModeAsync(-2, "TRƯỜNG HỢP 1.1 (+Y Mép Ngoài Phẳng)");

        [RelayCommand]
        private Task BuildCase12Async() => RunBuildModeAsync(-4, "TRƯỜNG HỢP 1.2 (+X Thu 1200x1200)");

        [RelayCommand]
        private Task BuildCase13Async() => RunBuildModeAsync(-5, "TRƯỜNG HỢP 1.3 (-Y Giằng Thu Ngắn)");

        [RelayCommand]
        private Task BuildCase14Async() => RunBuildModeAsync(-6, "TRƯỜNG HỢP 1.4 (+X Thu 1200x1200)");

        [RelayCommand]
        private Task BuildCase21Async() => RunBuildModeAsync(-3, "TRƯỜNG HỢP 2.1 (+Y Vươn Ngoài)");

        [RelayCommand]
        private Task BuildCase22Async() => RunBuildModeAsync(-7, "TRƯỜNG HỢP 2.2 (+X Vươn Ngoài -Y)");
    }
}
