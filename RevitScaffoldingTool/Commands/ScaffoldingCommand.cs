using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using RevitScaffoldingTool.ViewModels;
using RevitScaffoldingTool.Views;

namespace RevitScaffoldingTool
{
    internal static class ToolWindowHost
    {
        private static readonly List<Window> OpenWindows = new List<Window>();

        public static IReadOnlyList<Window> Open { get { return OpenWindows; } }

        public static Window Current { get { return OpenWindows.Count == 0 ? null : OpenWindows[OpenWindows.Count - 1]; } }

        public static void Attach(UIApplication uiapp, Window window)
        {
            OpenWindows.Add(window);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.ShowInTaskbar = true;
            new WindowInteropHelper(window).Owner = uiapp.MainWindowHandle;
            window.Closed += (s, e) => OpenWindows.Remove(window);
        }

        public static bool ActivateIfOpen(Window window)
        {
            if (window == null) return false;

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
            return true;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ScaffoldingCommand : IExternalCommand
    {
        private static ScaffoldingWindow _window = null;
        private static bool _resolverRegistered = false;

        static ScaffoldingCommand()
        {
            RegisterResolver();
        }

        private static void RegisterResolver()
        {
            if (_resolverRegistered) return;
            _resolverRegistered = true;

            string addinDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (string.IsNullOrEmpty(addinDir)) return null;
                var assemblyName = new System.Reflection.AssemblyName(args.Name).Name + ".dll";
                string fullPath = System.IO.Path.Combine(addinDir, assemblyName);
                if (System.IO.File.Exists(fullPath))
                {
                    return System.Reflection.Assembly.LoadFrom(fullPath);
                }
                return null;
            };
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                RevitTask.Initialize(uiapp);

                UIDocument uidoc = uiapp.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Revit Scaffolding Tool", "Vui lòng mở một Project Revit trước khi chạy công cụ.");
                    return Result.Cancelled;
                }

                if (ToolWindowHost.ActivateIfOpen(_window))
                {
                    return Result.Succeeded;
                }

                var vm = new ScaffoldingViewModel(uiapp);
                _window = new ScaffoldingWindow { DataContext = vm };
                _window.Closed += (s, e) => _window = null;
                ToolWindowHost.Attach(uiapp, _window);
                _window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}
