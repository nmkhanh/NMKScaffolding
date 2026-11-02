const fs = require('fs');

function patch(filePath, replacements) {
    let text = fs.readFileSync(filePath, 'utf8');
    for (const [target, repl] of replacements) {
        if (!text.includes(target)) {
            console.error(`Target not found in ${filePath}: ${target.slice(0, 40)}...`);
        } else {
            text = text.replace(target, repl);
        }
    }
    fs.writeFileSync(filePath, text, 'utf8');
}

// 1. F_ElementOperations.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ElementOperations.cs", [
    [
        "symbol.Category.Id.Value == (long)category",
        "#if R22 || R23\n            symbol.Category.Id.IntegerValue == (int)category\n#else\n            symbol.Category.Id.Value == (long)category\n#endif"
    ]
]);

// 2. F_Parameters.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Parameters.cs", [
    [
        "var element = doc.GetElement(new ElementId(id));",
        "#if R22 || R23\n            var element = doc.GetElement(new ElementId((int)id));\n#else\n            var element = doc.GetElement(new ElementId(id));\n#endif"
    ],
    [
        "parameter.Set(new ElementId(ToLongParameterValue(value)));",
        "#if R22 || R23\n                    parameter.Set(new ElementId((int)ToLongParameterValue(value)));\n#else\n                    parameter.Set(new ElementId(ToLongParameterValue(value)));\n#endif"
    ]
]);

// 3. F_Rebar.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Rebar.cs", [
    [
        "coupler.RotationAngle = request.RotationAngleDegrees.Value * Math.PI / 180.0;\r\n                        changed.Add(\"rotationAngleDegrees\");",
        "#if !R22\r\n                        coupler.RotationAngle = request.RotationAngleDegrees.Value * Math.PI / 180.0;\r\n                        changed.Add(\"rotationAngleDegrees\");\r\n#endif"
    ],
    [
        "rotationAngle = Safe(() => coupler.RotationAngle, 0.0),",
        "#if !R22\r\n                rotationAngle = Safe(() => coupler.RotationAngle, 0.0),\r\n#else\r\n                rotationAngle = 0.0,\r\n#endif"
    ]
]);

// 4. F_Selection.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Selection.cs", [
    [
        "$\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.Value))}\"",
        "#if R22 || R23\r\n                            $\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.IntegerValue))}\"\r\n#else\r\n                            $\"[RevitMCP][SELECT] selected ids: {string.Join(\", \", validIds.Select(e => e.Value))}\"\r\n#endif"
    ],
    [
        "selectedIds = validIds.Select(e => e.Value).ToList(),",
        "#if R22 || R23\r\n                    selectedIds = validIds.Select(e => (long)e.IntegerValue).ToList(),\r\n#else\r\n                    selectedIds = validIds.Select(e => e.Value).ToList(),\r\n#endif"
    ]
]);

// 5. F_ViewOverrides.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewOverrides.cs", [
    [
        "Math.Clamp(request.Transparency.Value, 0, 100)",
        "Math.Max(0, Math.Min(100, request.Transparency.Value))"
    ],
    [
        "return Enum.Parse<TEnum>(name);",
        "return (TEnum)Enum.Parse(typeof(TEnum), name);"
    ]
]);

// 6. F_ViewSheets.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewSheets.cs", [
    [
        "var elementId = new ElementId(id);",
        "#if R22 || R23\r\n            var elementId = new ElementId((int)id);\r\n#else\r\n            var elementId = new ElementId(id);\r\n#endif"
    ],
    [
        "new ElementId(long.Parse(viewTemplateId.Trim()))",
        "#if R22 || R23\r\n                    new ElementId((int)long.Parse(viewTemplateId.Trim()))\r\n#else\r\n                    new ElementId(long.Parse(viewTemplateId.Trim()))\r\n#endif"
    ]
]);

// 7. F_ViewSheetSets.cs
patch("c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewSheetSets.cs", [
    [
        "var currentViews = existing.Views.Cast<View>().ToDictionary(v => v.Id.Value);",
        "#if R22 || R23\r\n                var currentViews = existing.Views.Cast<View>().ToDictionary(v => (long)v.Id.IntegerValue);\r\n#else\r\n                var currentViews = existing.Views.Cast<View>().ToDictionary(v => v.Id.Value);\r\n#endif"
    ],
    [
        "if (remove) currentViews.Remove(view.Id.Value);\r\n                    else currentViews[view.Id.Value] = view;",
        "#if R22 || R23\r\n                    if (remove) currentViews.Remove((long)view.Id.IntegerValue);\r\n                    else currentViews[(long)view.Id.IntegerValue] = view;\r\n#else\r\n                    if (remove) currentViews.Remove(view.Id.Value);\r\n                    else currentViews[view.Id.Value] = view;\r\n#endif"
    ],
    [
        "views[view.Id.Value] = view;",
        "#if R22 || R23\r\n                views[(long)view.Id.IntegerValue] = view;\r\n#else\r\n                views[view.Id.Value] = view;\r\n#endif"
    ],
    [
        "views[sheet.Id.Value] = sheet;",
        "#if R22 || R23\r\n                views[(long)sheet.Id.IntegerValue] = sheet;\r\n#else\r\n                views[sheet.Id.Value] = sheet;\r\n#endif"
    ]
]);

console.log('All files processed.');
