using System;
using System.Collections.Generic;
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

        // Vị trí, Chân chống & Cao độ Z
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

        // Tùy chọn chạy theo dữ liệu nhập (From data)
        [ObservableProperty] private bool _isFromData = false;

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
                s.Save();
            }
            catch
            {
            }
        }

        private BuildOptions CreateOptions(int mode)
        {
            SaveSettings();

            if (!IsFromData)
            {
                // Chế độ Default (không dùng data nhập, chạy chuẩn 100% theo milestone kiểm chứng)
                return new BuildOptions
                {
                    X0 = 0.0,
                    Y0 = 0.0,
                    Z0 = 0.0,
                    BayWidth = 1200.0,
                    JackHeight = 250.0,
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
                // Chế độ From Data (chạy với toàn bộ dữ liệu cấu hình trên UI)
                return new BuildOptions
                {
                    X0 = 0.0,
                    Y0 = 0.0,
                    Z0 = ElevationZ,
                    BayWidth = BayWidth > 0 ? BayWidth : 1200.0,
                    JackHeight = JackHeight,
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
                BuildOptions opts = CreateOptions(mode);
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
