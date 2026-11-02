const fs = require('fs');

// 1. Update Net48Extensions.cs
const compatPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Compat/Net48Extensions.cs";
let compatText = fs.readFileSync(compatPath, 'utf8');
if (!compatText.includes("ElementCompat")) {
    const extraCompat = `
    internal static class ElementCompat
    {
        public static Autodesk.Revit.DB.ElementId CreateElementId(long id)
        {
#if R22 || R23
            return new Autodesk.Revit.DB.ElementId((int)id);
#else
            return new Autodesk.Revit.DB.ElementId(id);
#endif
        }

        public static long GetIdValue(this Autodesk.Revit.DB.ElementId id)
        {
#if R22 || R23
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }
`;
    // Insert before the last closing braces
    compatText = compatText.replace(/}\r?\n#endif\s*$/, extraCompat + "}\n#endif\n");
    fs.writeFileSync(compatPath, compatText, 'utf8');
    console.log("Updated Net48Extensions.cs");
}

function replaceInFile(filePath, searchStr, replaceStr) {
    let content = fs.readFileSync(filePath, 'utf8');
    if (!content.includes(searchStr)) {
        console.error(`Search string not found in ${filePath}:\n${searchStr}`);
        return false;
    }
    content = content.replace(searchStr, replaceStr);
    fs.writeFileSync(filePath, content, 'utf8');
    console.log(`Replaced in ${filePath}`);
    return true;
}

// 2. F_ElementOperations.cs
replaceInFile(
    "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ElementOperations.cs",
    "symbol.Category.Id.Value == (long)category",
    "symbol.Category.Id.GetIdValue() == (long)category"
);

// 3. F_Parameters.cs
replaceInFile(
    "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Parameters.cs",
    "var element = doc.GetElement(new ElementId(id));",
    "var element = doc.GetElement(ElementCompat.CreateElementId(id));"
);
replaceInFile(
    "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Parameters.cs",
    "parameter.Set(new ElementId(ToLongParameterValue(value)));",
    "parameter.Set(ElementCompat.CreateElementId(ToLongParameterValue(value)));"
);

// 4. F_Rebar.cs
let rebarPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Rebar.cs";
let rebarText = fs.readFileSync(rebarPath, 'utf8');
rebarText = rebarText.replace(
    /coupler\.RotationAngle = request\.RotationAngleDegrees\.Value \* Math\.PI \/ 180\.0;\r?\n\s*changed\.Add\("rotationAngleDegrees"\);/,
    `#if !R22\n                        coupler.RotationAngle = request.RotationAngleDegrees.Value * Math.PI / 180.0;\n                        changed.Add("rotationAngleDegrees");\n#endif`
);
rebarText = rebarText.replace(
    /rotationAngle = Safe\(\(\) => coupler\.RotationAngle, 0\.0\),/,
    `#if !R22\n                rotationAngle = Safe(() => coupler.RotationAngle, 0.0),\n#else\n                rotationAngle = 0.0,\n#endif`
);
fs.writeFileSync(rebarPath, rebarText, 'utf8');
console.log("Updated F_Rebar.cs");

// 5. F_Selection.cs
let selPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_Selection.cs";
let selText = fs.readFileSync(selPath, 'utf8');
selText = selText.replace(
    "validIds.Select(e => e.Value)",
    "validIds.Select(e => e.GetIdValue())"
);
selText = selText.replace(
    "selectedIds = validIds.Select(e => e.Value).ToList(),",
    "selectedIds = validIds.Select(e => e.GetIdValue()).ToList(),"
);
fs.writeFileSync(selPath, selText, 'utf8');
console.log("Updated F_Selection.cs");

// 6. F_ViewOverrides.cs
let voPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewOverrides.cs";
let voText = fs.readFileSync(voPath, 'utf8');
voText = voText.replace(
    "Math.Clamp(request.Transparency.Value, 0, 100)",
    "Math.Max(0, Math.Min(100, request.Transparency.Value))"
);
voText = voText.replace(
    "return Enum.Parse<TEnum>(name);",
    "return (TEnum)Enum.Parse(typeof(TEnum), name);"
);
fs.writeFileSync(voPath, voText, 'utf8');
console.log("Updated F_ViewOverrides.cs");

// 7. F_ViewSheets.cs
let vsPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewSheets.cs";
let vsText = fs.readFileSync(vsPath, 'utf8');
vsText = vsText.replace(
    "var elementId = new ElementId(id);",
    "var elementId = ElementCompat.CreateElementId(id);"
);
vsText = vsText.replace(
    "new ElementId(long.Parse(viewTemplateId.Trim()))",
    "ElementCompat.CreateElementId(long.Parse(viewTemplateId.Trim()))"
);
fs.writeFileSync(vsPath, vsText, 'utf8');
console.log("Updated F_ViewSheets.cs");

// 8. F_ViewSheetSets.cs
let vssPath = "c:/Users/Admin/Documents/antigravity/nifty-chandrasekhar/MCPRevit/RevitMcpAddin/Services/Functions/F_ViewSheetSets.cs";
let vssText = fs.readFileSync(vssPath, 'utf8');
vssText = vssText.replace(
    "v => v.Id.Value",
    "v => v.Id.GetIdValue()"
);
vssText = vssText.replace(
    "currentViews.Remove(view.Id.Value)",
    "currentViews.Remove(view.Id.GetIdValue())"
);
vssText = vssText.replace(
    "currentViews[view.Id.Value] = view",
    "currentViews[view.Id.GetIdValue()] = view"
);
vssText = vssText.replace(
    "views[view.Id.Value] = view;",
    "views[view.Id.GetIdValue()] = view;"
);
vssText = vssText.replace(
    "views[sheet.Id.Value] = sheet;",
    "views[sheet.Id.GetIdValue()] = sheet;"
);
fs.writeFileSync(vssPath, vssText, 'utf8');
console.log("Updated F_ViewSheetSets.cs");

console.log("All compat updates applied successfully!");
