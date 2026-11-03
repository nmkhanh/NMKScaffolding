using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitFamilyMcpBridge
{
    public class App : IExternalApplication
    {
        public static McpHttpServer Server { get; set; }
        public static McpExternalEventHandler Handler { get; set; }
        public static ExternalEvent ExtEvent { get; set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Initialize External Event Handler for Revit UI thread dispatch
                Handler = new McpExternalEventHandler();
                ExtEvent = ExternalEvent.Create(Handler);

                // 2. Start HTTP MCP Bridge Server on port 4848
                Server = new McpHttpServer(Handler, ExtEvent, 4848);
                Server.Start();

                // 3. Create Ribbon Tab and Panel in Revit 2022
                string tabName = "BIM AI";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Tab might already exist if loaded in multi-session
                }

                RibbonPanel panel = null;
                try
                {
                    panel = application.CreateRibbonPanel(tabName, "MCP Server Bridge");
                }
                catch
                {
                    panel = application.CreateRibbonPanel("MCP Server Bridge");
                }

                string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

                // Button 1: Bridge Status
                PushButtonData btnStatusData = new PushButtonData(
                    "btnBridgeStatus",
                    "Bridge Status\n(Port 4848)",
                    thisAssemblyPath,
                    "RevitFamilyMcpBridge.CmdBridgeStatus"
                );
                btnStatusData.ToolTip = "Kiểm tra trạng thái kết nối giữa Revit 2022 và AI MCP Server.";

                // Button 2: Toggle Server
                PushButtonData btnToggleData = new PushButtonData(
                    "btnToggleServer",
                    "Restart\nBridge",
                    thisAssemblyPath,
                    "RevitFamilyMcpBridge.CmdRestartServer"
                );
                btnToggleData.ToolTip = "Khởi động lại máy chủ lắng nghe HTTP MCP Bridge.";

                // Button 3: Open Library
                PushButtonData btnLibraryData = new PushButtonData(
                    "btnOpenLibrary",
                    "Open Family\nLibrary",
                    thisAssemblyPath,
                    "RevitFamilyMcpBridge.CmdOpenLibrary"
                );
                btnLibraryData.ToolTip = "Mở thư mục lưu trữ Family được tạo bởi AI.";

                panel.AddItem(btnStatusData);
                panel.AddItem(btnToggleData);
                panel.AddItem(btnLibraryData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit Family MCP Bridge", "Lỗi khởi động Add-in: " + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                if (Server != null)
                {
                    Server.Stop();
                }
                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }
    }
}
