using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public class WarningDiscarder : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failList = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor failure in failList)
            {
                FailureSeverity s = failure.GetSeverity();
                if (s == FailureSeverity.Warning)
                {
                    // Tự động bỏ qua và xóa cảnh báo (ví dụ cảnh báo trùng lặp cấu kiện)
                    failuresAccessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
