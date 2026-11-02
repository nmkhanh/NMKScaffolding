import re

# 1. F_ElementOperations.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_ElementOperations.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "symbol.Category.Id.Value == (long)category",
    "#if R22 || R23\n            symbol.Category.Id.IntegerValue == (int)category\n#else\n            symbol.Category.Id.Value == (long)category\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 2. F_Parameters.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_Parameters.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "var element = doc.GetElement(new ElementId(id));",
    "#if R22 || R23\n            var element = doc.GetElement(new ElementId((int)id));\n#else\n            var element = doc.GetElement(new ElementId(id));\n#endif"
)
text = text.replace(
    "parameter.Set(new ElementId(ToLongParameterValue(value)));",
    "#if R22 || R23\n                    parameter.Set(new ElementId((int)ToLongParameterValue(value)));\n#else\n                    parameter.Set(new ElementId(ToLongParameterValue(value)));\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 3. F_Rebar.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_Rebar.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "coupler.RotationAngle = request.RotationAngleDegrees.Value * Math.PI / 180.0;\n                        changed.Add(\"rotationAngleDegrees\");",
    "#if !R22\n                        coupler.RotationAngle = request.RotationAngleDegrees.Value * Math.PI / 180.0;\n                        changed.Add(\"rotationAngleDegrees\");\n#endif"
)
text = text.replace(
    "rotationAngle = Safe(() => coupler.RotationAngle, 0.0),",
    "#if !R22\n                rotationAngle = Safe(() => coupler.RotationAngle, 0.0),\n#else\n                rotationAngle = 0.0,\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 4. F_Selection.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_Selection.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "$\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.Value))}\"",
    "#if R22 || R23\n                            $\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.IntegerValue))}\"\n#else\n                            $\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.Value))}\"\n#endif"
)
text = text.replace(
    "selectedIds = validIds.Select(e => e.Value).ToList(),",
    "#if R22 || R23\n                    selectedIds = validIds.Select(e => (long)e.IntegerValue).ToList(),\n#else\n                    selectedIds = validIds.Select(e => e.Value).ToList(),\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 5. F_ViewOverrides.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_ViewOverrides.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "Math.Clamp(request.Transparency.Value, 0, 100)",
    "Math.Max(0, Math.Min(100, request.Transparency.Value))"
)
text = text.replace(
    "return Enum.Parse<TEnum>(name);",
    "return (TEnum)Enum.Parse(typeof(TEnum), name);"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 6. F_ViewSheets.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_ViewSheets.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "var elementId = new ElementId(id);",
    "#if R22 || R23\n            var elementId = new ElementId((int)id);\n#else\n            var elementId = new ElementId(id);\n#endif"
)
text = text.replace(
    "new ElementId(long.Parse(viewTemplateId.Trim()))",
    "#if R22 || R23\n                    new ElementId((int)long.Parse(viewTemplateId.Trim()))\n#else\n                    new ElementId(long.Parse(viewTemplateId.Trim()))\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

# 7. F_ViewSheetSets.cs
path = r"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\MCPRevit\RevitMcpAddin\Services\Functions\F_ViewSheetSets.cs"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace(
    "var currentViews = existing.Views.Cast<View>().ToDictionary(v => v.Id.Value);",
    "#if R22 || R23\n                var currentViews = existing.Views.Cast<View>().ToDictionary(v => (long)v.Id.IntegerValue);\n#else\n                var currentViews = existing.Views.Cast<View>().ToDictionary(v => v.Id.Value);\n#endif"
)
text = text.replace(
    "if (remove) currentViews.Remove(view.Id.Value);\n                    else currentViews[view.Id.Value] = view;",
    "#if R22 || R23\n                    if (remove) currentViews.Remove((long)view.Id.IntegerValue);\n                    else currentViews[(long)view.Id.IntegerValue] = view;\n#else\n                    if (remove) currentViews.Remove(view.Id.Value);\n                    else currentViews[view.Id.Value] = view;\n#endif"
)
text = text.replace(
    "views[view.Id.Value] = view;",
    "#if R22 || R23\n                views[(long)view.Id.IntegerValue] = view;\n#else\n                views[view.Id.Value] = view;\n#endif"
)
text = text.replace(
    "views[sheet.Id.Value] = sheet;",
    "#if R22 || R23\n                views[(long)sheet.Id.IntegerValue] = sheet;\n#else\n                views[sheet.Id.Value] = sheet;\n#endif"
)
with open(path, "w", encoding="utf-8") as f:
    f.write(text)

print("All files updated for R22 compat!")
