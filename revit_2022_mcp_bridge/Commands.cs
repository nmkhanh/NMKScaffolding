using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitFamilyMcpBridge
{
    [Transaction(TransactionMode.Manual)]
    public class CmdBridgeStatus : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string status = App.Server != null && App.Server.IsRunning ? "ĐANG HOẠT ĐỘNG (Listening)" : "ĐÃ DỪNG (Stopped)";
            string info = string.Format(
                "--- REVIT 2022 MCP BRIDGE ---\n\n" +
                "Trạng thái: {0}\n" +
                "Cổng kết nối: http://127.0.0.1:4848/\n" +
                "Phiên bản Revit: 2022\n\n" +
                "AI Assistant (Antigravity) có thể gửi lệnh trực tiếp qua chat để tạo Family, Reference Planes, Parameters và Flex test trực tiếp trong Revit!",
                status
            );
            TaskDialog.Show("Revit MCP Bridge Status", info);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdRestartServer : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (App.Server != null)
                {
                    App.Server.Stop();
                }

                App.Server = new McpHttpServer(App.Handler, App.ExtEvent, 4848);
                App.Server.Start();

                TaskDialog.Show("Revit MCP Bridge", "Đã khởi động lại máy chủ MCP Bridge trên cổng 4848 thành công!");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit MCP Bridge Error", "Không thể khởi động lại: " + ex.Message);
                return Result.Failed;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdOpenLibrary : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "antigravity",
                    "nifty-chandrasekhar",
                    "revit_library"
                );

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                Process.Start("explorer.exe", path);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Không thể mở thư mục: " + ex.Message);
                return Result.Failed;
            }
        }
    }
}
