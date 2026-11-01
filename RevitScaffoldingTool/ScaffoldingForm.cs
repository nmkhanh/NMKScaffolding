using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Panel = System.Windows.Forms.Panel;
using TextBox = System.Windows.Forms.TextBox;
using GroupBox = System.Windows.Forms.GroupBox;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;

namespace RevitScaffoldingTool
{
    public class ScaffoldingEventHandler : IExternalEventHandler
    {
        public BuildOptions Options { get; set; }
        public Action<BuildResult> OnCompleted { get; set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                Document doc = uiapp.ActiveUIDocument != null ? uiapp.ActiveUIDocument.Document : null;
                if (doc == null)
                {
                    var err = new BuildResult { Success = false, Message = "Không tìm thấy Document Revit đang mở!" };
                    if (OnCompleted != null) OnCompleted(err);
                    return;
                }

                BuildOptions opts = Options ?? new BuildOptions();
                BuildResult res = ScaffoldingBuilder.ExecuteBuild(doc, opts);

                if (OnCompleted != null)
                {
                    OnCompleted(res);
                }
            }
            catch (Exception ex)
            {
                var err = new BuildResult { Success = false, Message = "Lỗi thực thi: " + ex.Message };
                if (OnCompleted != null) OnCompleted(err);
            }
        }

        public string GetName()
        {
            return "ScaffoldingEventHandler";
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ScaffoldingCommand : IExternalCommand
    {
        private static ScaffoldingForm _currentForm = null;
        private static ExternalEvent _extEvent = null;
        private static ScaffoldingEventHandler _handler = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_handler == null)
                {
                    _handler = new ScaffoldingEventHandler();
                    _extEvent = ExternalEvent.Create(_handler);
                }

                if (_currentForm == null || _currentForm.IsDisposed)
                {
                    _currentForm = new ScaffoldingForm(_extEvent, _handler);
                    _currentForm.Show(); // Modeless Show
                }
                else
                {
                    _currentForm.BringToFront();
                    _currentForm.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class ScaffoldingForm : System.Windows.Forms.Form
    {
        private readonly ExternalEvent _extEvent;
        private readonly ScaffoldingEventHandler _handler;

        private TextBox txtX0;
        private TextBox txtY0;
        private TextBox txtBayWidth;
        private TextBox txtJackHeight;
        private TextBox txtMultiBays;

        private Button btnRunMultiStraight;
        private Button btnRunCase1_1;
        private Button btnRunCase1_2;
        private Button btnRunCase1_3;
        private Button btnRunCase1_4;
        private Button btnRunCase2_1;
        private Button btnRunCase2_2;
        private Button btnRunMaster6Corners;
        private Label lblStatus;
        private ToolTip toolTip;

        public ScaffoldingForm(ExternalEvent extEvent, ScaffoldingEventHandler handler)
        {
            _extEvent = extEvent;
            _handler = handler;
            _handler.OnCompleted = (res) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateStatus(res)));
                }
                else
                {
                    UpdateStatus(res);
                }
            };

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ Thống Dựng Giàn Giáo Nêm Tiêu Chuẩn ALINCO";
            this.Size = new Size(580, 810);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(248, 250, 252);

            // Initialize ToolTip
            toolTip = new ToolTip
            {
                IsBalloon = true,
                ToolTipTitle = "Quy Chuẩn Kỹ Thuật & Điều Kiện Kích Hoạt",
                ToolTipIcon = ToolTipIcon.Info,
                AutoPopDelay = 30000,
                InitialDelay = 200,
                ReshowDelay = 100
            };

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            Label lblHeaderTitle = new Label
            {
                Text = "DỰNG GIÀN GIÁO TIÊU CHUẨN ALINCO",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 10),
                AutoSize = true
            };
            Label lblHeaderSub = new Label
            {
                Text = "Tự động hóa lắp dựng giàn giáo đa nhịp & khớp nối góc 90° (Rê chuột xem điều kiện)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 36),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);
            this.Controls.Add(pnlHeader);

            // Group 1: General Config
            GroupBox grpConfig = new GroupBox
            {
                Text = " THÔNG SỐ TỌA ĐỘ GỐC & KÍCH THƯỚC ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 75),
                Size = new Size(525, 125)
            };

            Label lblX0 = new Label { Text = "X0 (mm):", Location = new Point(15, 24), Size = new Size(55, 20), Font = new Font("Segoe UI", 9f) };
            txtX0 = new TextBox { Text = "0", Location = new Point(72, 21), Size = new Size(70, 23), Font = new Font("Segoe UI", 9f) };

            Label lblY0 = new Label { Text = "Y0 (mm):", Location = new Point(155, 24), Size = new Size(55, 20), Font = new Font("Segoe UI", 9f) };
            txtY0 = new TextBox { Text = "0", Location = new Point(212, 21), Size = new Size(70, 23), Font = new Font("Segoe UI", 9f) };

            Label lblW = new Label { Text = "Rộng W (mm):", Location = new Point(295, 24), Size = new Size(85, 20), Font = new Font("Segoe UI", 9f) };
            txtBayWidth = new TextBox { Text = "1200", Location = new Point(382, 21), Size = new Size(60, 23), Font = new Font("Segoe UI", 9f) };

            Label lblJackH = new Label { Text = "Cao kích h (mm):", Location = new Point(15, 56), Size = new Size(105, 20), Font = new Font("Segoe UI", 9f) };
            txtJackHeight = new TextBox { Text = "250", Location = new Point(122, 53), Size = new Size(60, 23), Font = new Font("Segoe UI", 9f) };

            Label lblMultiPrompt = new Label
            {
                Text = "Dãy nhịp (mm):",
                Location = new Point(195, 56),
                Size = new Size(90, 20),
                Font = new Font("Segoe UI", 9f)
            };
            txtMultiBays = new TextBox
            {
                Text = "1800, 1200, 1800, 1200, 1800",
                Location = new Point(290, 53),
                Size = new Size(215, 23),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblNote = new Label
            {
                Text = "* Gợi ý: Rê chuột lên từng nút bên dưới để xem điều kiện và quy chuẩn kỹ thuật.",
                Location = new Point(15, 90),
                Size = new Size(490, 20),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            grpConfig.Controls.Add(lblX0);
            grpConfig.Controls.Add(txtX0);
            grpConfig.Controls.Add(lblY0);
            grpConfig.Controls.Add(txtY0);
            grpConfig.Controls.Add(lblW);
            grpConfig.Controls.Add(txtBayWidth);
            grpConfig.Controls.Add(lblJackH);
            grpConfig.Controls.Add(txtJackHeight);
            grpConfig.Controls.Add(lblMultiPrompt);
            grpConfig.Controls.Add(txtMultiBays);
            grpConfig.Controls.Add(lblNote);
            this.Controls.Add(grpConfig);

            // Group 2: Action Buttons
            GroupBox grpActions = new GroupBox
            {
                Text = " CÁC TRƯỜNG HỢP LẮP DỰNG (CLICK ĐỂ DỰNG VÀO REVIT) ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 210),
                Size = new Size(525, 490)
            };

            // Button 1.0: Dãy thẳng nhiều nhịp tiêu chuẩn
            btnRunMultiStraight = new Button
            {
                Location = new Point(15, 22),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "1.0 - DÃY THẲNG TIÊU CHUẨN (NHIỀU NHỊP)"
            };
            btnRunMultiStraight.FlatAppearance.BorderSize = 0;
            btnRunMultiStraight.Click += (s, e) => TriggerBuild(-1);
            toolTip.SetToolTip(btnRunMultiStraight,
                "DÃY THẲNG NHIỀU NHỊP TIÊU CHUẨN (MODE 1.0):\n" +
                "• Lắp dựng hệ giàn giáo thẳng đa nhịp theo danh sách chiều dài nhập vào.\n" +
                "• Giằng chân h1: Đầy đủ 100% ở mặt trước và mặt sau để khóa chân cột.\n" +
                "• Giằng chéo Family 5: Bố trí tại mặt sau (không lắp đè Family 4 tại h3, h6).\n" +
                "• Lan can mặt trước: Đầy đủ 2 tầng lan can kép (h6, h7) bảo vệ sàn thao tác.\n" +
                "• Đầu hồi: 2 đầu hồi trái phải đều có lan can kép và ván chắn biên.");

            // Button 1.1: Khớp nối góc 90° +Y (Mép ngoài phẳng, nhịp cuối cắt 600)
            btnRunCase1_1 = new Button
            {
                Location = new Point(15, 68),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "1.1 - KHỚP GÓC +Y (MÉP NGOÀI PHẲNG - CẮT 600)"
            };
            btnRunCase1_1.FlatAppearance.BorderSize = 0;
            btnRunCase1_1.Click += (s, e) => TriggerBuild(-2);
            toolTip.SetToolTip(btnRunCase1_1,
                "TRƯỜNG HỢP 1.1: KHỚP NỐI GÓC 90° HƯỚNG +Y (MÉP NGOÀI PHẲNG - Nhịp cuối 1800 cắt 600):\n" +
                "• Điều kiện: Dãy chính rẽ góc vuông 90° vươn sang +Y tại góc cuối cùng.\n" +
                "• Mép ngoài phẳng: Đầu hồi Xmax = 7800 và mép ngoài nhánh rẽ thẳng hàng trên cùng mặt phẳng.\n" +
                "• Cắt ngắn mặt trước Nhịp 5: Cấy thêm cột tại (6600, 0), thu ngắn thanh giằng mặt trước Y = 0 về 600 mm.\n" +
                "• Mép sau Y = -1200: Giữ nguyên 1800 mm đến X = 7800.\n" +
                "• Mép ngoài X = 7800: Có Family 5 giằng chéo -> Tuyệt đối KHÔNG lắp Family 4 (h3, h6).\n" +
                "• Cửa ngõ Y = 0: Mở thông suốt 1200 mm (X: 6600 -> 7800) làm lối đi sang nhánh rẽ.");

            // Button 1.2: Khớp nối góc 90° +Y (Nhịp cuối = Rộng rẽ = 1200)
            btnRunCase1_2 = new Button
            {
                Location = new Point(15, 114),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "1.2 - KHỚP GÓC +Y (NHỊP CUỐI = RỘNG RẼ = 1200)"
            };
            btnRunCase1_2.FlatAppearance.BorderSize = 0;
            btnRunCase1_2.Click += (s, e) => TriggerBuild(-4);
            toolTip.SetToolTip(btnRunCase1_2,
                "TRƯỜNG HỢP 1.2: KHỚP NỐI GÓC 90° HƯỚNG +Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM):\n" +
                "• Điều kiện: Chiều dài nhịp cuối bằng đúng bề rộng nhánh rẽ tiếp theo (L_last = W = 1200 mm).\n" +
                "• Tạo ô vuông 1200x1200 mm: Nhịp 5 thành hình vuông chuẩn (X: 6000->7200, Y: -1200->0).\n" +
                "• Không cấy cột phụ: 2 cột mặt trước (6000, 0) và (7200, 0) dùng trực tiếp làm chân nhánh rẽ.\n" +
                "• Nhánh rẽ vươn sang +Y: Kích thước 1800x1200 mm (X: 6000->7200, Y: 0->1800).\n" +
                "• Cửa ngõ Y = 0: Mở trọn vẹn toàn bộ bề rộng 1200 mm, không có bất kỳ lan can nào chắn lối.\n" +
                "• Mép ngoài X = 7200: Có giằng chéo Family 5 -> Tuyệt đối KHÔNG lắp Family 4 ở h3 và h6.\n" +
                "• Giằng chân h1: Khóa cứng 100% tất cả các mặt chân cột.");

            // Button 1.3: Khớp nối góc 90° -Y (Mép ngoài phẳng, nhịp cuối cắt 600)
            btnRunCase1_3 = new Button
            {
                Location = new Point(15, 160),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "1.3 - KHỚP GÓC -Y (MÉP NGOÀI PHẲNG - CẮT 600)"
            };
            btnRunCase1_3.FlatAppearance.BorderSize = 0;
            btnRunCase1_3.Click += (s, e) => TriggerBuild(-5);
            toolTip.SetToolTip(btnRunCase1_3,
                "TRƯỜNG HỢP 1.3: KHỚP NỐI GÓC 90° HƯỚNG -Y (MÉP PHẢI PHẲNG - CẮT 600 MM):\n" +
                "• Hệ giàn 1 (Nhịp 5): Mép sau Y = -1200 tại đoạn cắt (6000->6600) thu ngắn GIẰNG CHÉO Family 5 về 600 mm (không lắp Family 4).\n" +
                "• Cửa ngõ Y = -1200: Mở thông suốt trọn vẹn 1200 mm (X: 6600->7800) làm lối đi sang hệ giàn 2.\n" +
                "• Hệ giàn 2 (Vách trong X = 6600): Tiếp nối mặt giằng chéo dãy chính -> Lắp GIẰNG CHÉO Family 5 (tuyệt đối không lắp Family 4 tại h3, h6).\n" +
                "• Hệ giàn 2 (Mép ngoài X = 7800): Tiếp nối mặt trước dãy chính -> Lắp GIẰNG DỌC h3 và LAN CAN KÉP h6, h7 Family 4.\n" +
                "• Đầu hồi đáy Y = -3000: Lan can kép Family 4 và ván chắn biên Family 7 khóa an toàn.\n" +
                "• Giằng chân h1: Khóa cứng 100% tất cả các mặt đáy.");

            // Button 1.4: Khớp nối góc 90° -Y (Nhịp cuối = Rộng rẽ = 1200)
            btnRunCase1_4 = new Button
            {
                Location = new Point(15, 206),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "1.4 - KHỚP GÓC -Y (NHỊP CUỐI = RỘNG RẼ = 1200)"
            };
            btnRunCase1_4.FlatAppearance.BorderSize = 0;
            btnRunCase1_4.Click += (s, e) => TriggerBuild(-6);
            toolTip.SetToolTip(btnRunCase1_4,
                "TRƯỜNG HỢP 1.4: KHỚP NỐI GÓC 90° HƯỚNG -Y (NHỊP CUỐI = RỘNG RẼ = 1200 MM):\n" +
                "• Điều kiện: Rẽ góc vuông 90° vươn về phía -Y (giống 1.3), chiều dài nhịp cuối bằng đúng bề rộng nhánh rẽ (giống 1.2, L_last = W = 1200 mm).\n" +
                "• Tạo ô vuông 1200x1200 mm: Nhịp 5 thành hình vuông chuẩn (X: 6000->7200, Y: -1200->0).\n" +
                "• Không cấy cột phụ: 2 cột mép sau (6000, -1200) và (7200, -1200) dùng trực tiếp làm chân nhánh rẽ -Y.\n" +
                "• Cửa ngõ Y = -1200: Mở trọn vẹn toàn bộ 1200 mm (X: 6000->7200) làm lối đi sang hệ giàn 2.\n" +
                "• Hệ giàn 2 (Nhánh rẽ -Y, X: 6000->7200, Y: -3000->-1200):\n" +
                "  - Vách trong X = 6000: Lắp GIẰNG CHÉO Family 5 (tiếp nối mặt sau). Tuyệt đối không lắp Family 4 (h3, h6).\n" +
                "  - Mép ngoài X = 7200: Lắp GIẰNG DỌC h3 và LAN CAN KÉP h6, h7 Family 4.\n" +
                "  - Đầu hồi đáy Y = -3000: Lan can kép Family 4 và ván chắn biên Family 7.\n" +
                "• Giằng chân h1: Khóa cứng 100% tất cả các mặt chân cột.");

            // Button 2.1: Khớp nối góc 90° +Y (Mép sau phẳng - Nhóm 2)
            btnRunCase2_1 = new Button
            {
                Location = new Point(15, 252),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(147, 51, 234),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "2.1 - KHỚP GÓC +Y (MÉP SAU PHẲNG - NHÓM 2)"
            };
            btnRunCase2_1.FlatAppearance.BorderSize = 0;
            btnRunCase2_1.Click += (s, e) => TriggerBuild(-3);
            toolTip.SetToolTip(btnRunCase2_1,
                "TRƯỜNG HỢP 2.1: KHỚP NỐI GÓC 90° HƯỚNG +Y MÉP SAU PHẲNG (CONN-GRP2-TYPE2.1):\n" +
                "• Điều kiện: Mép sau dãy chính và mép sau nhánh rẽ cùng nằm phẳng tại Y = -1200 mm.\n" +
                "• Nhánh rẽ vươn ra ngoài: Đặt tại X in [7800, 9000], dài 1800 mm theo Y (từ Y = -1200 đến Y = +600).\n" +
                "• Nhô cao hơn mặt trước: Phần vươn +Y nhô ra trước mặt trước dãy chính 600 mm (Y: 0->600).\n" +
                "• Dãy chính nguyên vẹn 100%: Toàn bộ 5 nhịp giữ nguyên chuẩn (Nhịp 5 dài 1800 mm, không cắt).\n" +
                "• Cửa ngõ thông suốt: Lối đi mở 100% qua cửa ngõ X = 7800 (Y in [-1200, 0]).\n" +
                "• Family 5 ở mép ngoài: Bố trí tại X = 9000, tuyệt đối không lắp Family 4 ở h3 và h6.");

            // Button 2.2: Khớp nối góc 90° -Y (Mép trước phẳng - Nhóm 2)
            btnRunCase2_2 = new Button
            {
                Location = new Point(15, 298),
                Size = new Size(495, 40),
                BackColor = Color.FromArgb(192, 38, 211),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = "2.2 - KHỚP GÓC -Y (MÉP TRƯỚC PHẲNG - NHÓM 2)"
            };
            btnRunCase2_2.FlatAppearance.BorderSize = 0;
            btnRunCase2_2.Click += (s, e) => TriggerBuild(-7);
            toolTip.SetToolTip(btnRunCase2_2,
                "TRƯỜNG HỢP 2.2: KHỚP NỐI GÓC 90° HƯỚNG -Y MÉP TRƯỚC PHẲNG (CONN-GRP2-TYPE2.2):\n" +
                "• Điều kiện: Giống 2.1 nhưng vươn về phía -Y. Mép trước dãy chính và mép trước nhánh rẽ cùng nằm phẳng tại Y = 0 mm.\n" +
                "• Nhánh rẽ vươn ra ngoài đầu hồi: Đặt tại X in [7800, 9000], dài 1800 mm theo Y (từ Y = 0 xuống Y = -1800 mm).\n" +
                "• Nhô ra phía sau: Phần vươn -Y nhô ra sau mép sau dãy chính một đoạn 600 mm (từ Y = -1200 xuống Y = -1800 mm).\n" +
                "• Dãy chính nguyên vẹn 100%: Toàn bộ 5 nhịp giữ nguyên chuẩn (Nhịp 5 dài 1800 mm, không cắt).\n" +
                "• Cửa ngõ thông suốt: Lối đi mở 100% qua cửa ngõ X = 7800 mm (Y in [-1200, 0 mm]).\n" +
                "• Vách trong nhô ra sau X = 7800 mm: Bố trí giằng chéo Family 5 (L = 600 mm) ở cả 2 tầng (tiếp nối giằng chéo sau), không lắp Family 4 ở h3, h6.\n" +
                "• Mép ngoài X = 9000 mm: Bố trí giằng giữa h3 và lan can kép h6, h7 Family 4 (L = 1800 mm, tiếp nối mặt trước lan can), không có Family 5.");

                                    // Button Master: Full 6 truong hop lien tuc (Moi he 2 nhip)
            btnRunMaster6Corners = new Button
            {
                Location = new Point(15, 344),
                Size = new Size(495, 46),
                BackColor = Color.FromArgb(13, 148, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Text = "â˜… MASTER - Dáº¢I LIÃŠN Tá»¤C FULL 6 TRÆ¯á»œNG Há»¢P (Má»–I Há»† 2 NHá»ŠP)"
            };
            btnRunMaster6Corners.FlatAppearance.BorderSize = 0;
            btnRunMaster6Corners.Click += (s, e) => TriggerBuild(-8);
            toolTip.SetToolTip(btnRunMaster6Corners,
                "â˜… MASTER Dáº¢I GIÃ€N GIÃO LIÃŠN Tá»¤C FULL 6 TRÆ¯á»œNG Há»¢P (Má»–I Há»† 2 NHá»ŠP):\n" +
                "â€¢ Láº¯p dá»±ng 1 chuá»—i giÃ n giÃ¡o liÃªn tá»¥c khÃ´ng ngáº¯t quÃ£ng tráº£i qua Ä‘áº§y Ä‘á»§ 6 gÃ³c ráº½ theo Ä‘Ãºng chuáº©n code cá»§a 6 button:\n" +
                "  1. GÃ³c 1 (TH 1.1): Ráº½ +Y, mÃ©p ngoÃ i pháº³ng X = x0 + 3600 (Family 5), mÃ©p trong cáº¯t 600 mm cáº¥y cá»™t (x0+2400, y0+1200), cá»­a ngÃµ thÃ´ng suá»‘t 1200 mm.\n" +
                "  2. GÃ³c 2 (TH 1.2): Ráº½ +X, nhá»‹p cuá»‘i thu vá» W = 1200 mm (Ã´ vuÃ´ng 1200x1200), Ä‘á»‰nh Ä‘áº§u há»“i Ä‘Ã³ng lan can kÃ©p, cá»­a ngÃµ 1200 mm má»Ÿ sang +X.\n" +
                "  3. GÃ³c 3 (TH 1.3): Ráº½ -Y, mÃ©p trÃªn pháº³ng, mÃ©p dÆ°á»›i cáº¯t 600 mm cÃ³ GIáº°NG CHÃ‰O THU NGáº®N Family 5 (600 mm, khÃ´ng cÃ³ Family 4), cáº¥y cá»™t (x0+6000, y0+3000), cá»­a ngÃµ má»Ÿ sang -Y.\n" +
                "  4. GÃ³c 4 (TH 1.4): Ráº½ +X, nhá»‹p cuá»‘i thu vá» W = 1200 mm (Ã´ vuÃ´ng 1200x1200), Ä‘Ã¡y Ä‘áº§u há»“i Ä‘Ã³ng lan can kÃ©p, cá»­a ngÃµ 1200 mm má»Ÿ sang +X.\n" +
                "  5. GÃ³c 5 (TH 2.1): NhÃ³m 2 - Ráº½ +Y, mÃ©p sau pháº³ng Y = y0, nhÃ¡nh ráº½ 1800x1200 vÆ°Æ¡n ngoÃ i Ä‘áº§u há»“i x0+10800, vÃ¡ch trong nhÃ´ cao cÃ³ lan can, mÃ©p ngoÃ i Family 5.\n" +
                "  6. GÃ³c 6 (TH 2.2): NhÃ³m 2 - Ráº½ +X, mÃ©p trÆ°á»›c pháº³ng X = x0 + 10800, nhÃ¡nh ráº½ vÆ°Æ¡n ngoÃ i Ä‘áº§u há»“i, vÃ¡ch trong 600 mm láº¯p GIáº°NG CHÃ‰O Family 5 (khÃ´ng cÃ³ Family 4), mÃ©p ngoÃ i cÃ³ lan can kÃ©p.\n" +
                "  7. Äoáº¡n 7 (Káº¿t thÃºc): 2 nhá»‹p chuáº©n 1800 mm, Ä‘áº§u há»“i x0+16200 khÃ³a an toÃ n 100% báº±ng lan can kÃ©p vÃ  vÃ¡n cháº·n biÃªn.\n" +
                "â€¢ Má»—i há»‡ giÃ n Ä‘Ãºng chuáº©n 2 nhá»‹p giÃ n, sÃ n thao tÃ¡c thÃ´ng suá»‘t 100% khÃ´ng bá»‹ cáº£n trá»Ÿ!");

            grpActions.Controls.Add(btnRunMultiStraight);
            grpActions.Controls.Add(btnRunCase1_1);
            grpActions.Controls.Add(btnRunCase1_2);
            grpActions.Controls.Add(btnRunCase1_3);
            grpActions.Controls.Add(btnRunCase1_4);
            grpActions.Controls.Add(btnRunCase2_1);
            grpActions.Controls.Add(btnRunCase2_2);
            grpActions.Controls.Add(btnRunMaster6Corners);
            this.Controls.Add(grpActions);

            // Status Bar at Bottom
            lblStatus = new Label
            {
                Text = "Sẵn sàng! Hãy rê chuột vào nút để xem điều kiện hoặc bấm nút để dựng vào Revit.",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblStatus);
        }

        private void TriggerBuild(int mode)
        {
            try
            {
                double x0 = 0, y0 = 0, w = 1200, jackH = 250;
                double.TryParse(txtX0.Text.Trim(), out x0);
                double.TryParse(txtY0.Text.Trim(), out y0);
                double.TryParse(txtBayWidth.Text.Trim(), out w);
                double.TryParse(txtJackHeight.Text.Trim(), out jackH);

                List<double> bays = new List<double>();
                var parts = txtMultiBays.Text.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    double v;
                    if (double.TryParse(p.Trim(), out v) && v > 0)
                    {
                        bays.Add(v);
                    }
                }
                if (bays.Count == 0)
                {
                    bays = new List<double> { 1800, 1200, 1800, 1200, 1800 };
                }

                BuildOptions opts = new BuildOptions
                {
                    X0 = x0,
                    Y0 = y0,
                    Z0 = 0,
                    BayWidth = w,
                    JackHeight = jackH,
                    BayLengths = bays,
                    Mode = mode,
                    FamilyFolder = @"C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\family"
                };

                _handler.Options = opts;
                _extEvent.Raise();

                lblStatus.Text = "Đang gửi lệnh dựng vào Revit qua ExternalEvent...";
                lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tham số: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(BuildResult res)
        {
            if (res.Success)
            {
                lblStatus.Text = res.Message;
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                lblStatus.Text = "Lỗi: " + res.Message;
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            }
        }
    }
}
