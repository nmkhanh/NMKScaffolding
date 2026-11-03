using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitScaffoldingTool.ViewModels;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        // Đã xóa toàn bộ code cũ của Button Run theo yêu cầu người dùng.
        public static BuildResult ExecuteTableBuild(Document doc, BuildOptions opts, List<BayRowModel> rows)
        {
            return ExecuteBuild(doc, opts);
        }
    }
}
