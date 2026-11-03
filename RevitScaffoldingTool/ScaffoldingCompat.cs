using System;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    internal static class ScaffoldingCompat
    {
        public static int GetIdValue(this ElementId id)
        {
#if R26 || NET8_0_OR_GREATER
            return (int)id.Value;
#else
            return id.IntegerValue;
#endif
        }
    }
}
