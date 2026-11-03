const fs = require('fs');

// 1. Update ScaffoldingBuilder.Master.cs
let masterCode = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.Master.cs', 'utf8');

// Ensure signature has public and maxLegs
masterCode = masterCode.replace(
  /private static string ExecuteMaster6CornersContinuous\([^)]*\)/,
  'public static string ExecuteMaster6CornersContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds, int maxLegs = 7)'
);
masterCode = masterCode.replace(
  /public static string ExecuteMaster6CornersContinuous\(Document doc, BuildOptions opts, Level level, List<int> placedIds\)/,
  'public static string ExecuteMaster6CornersContinuous(Document doc, BuildOptions opts, Level level, List<int> placedIds, int maxLegs = 7)'
);

// Insert maxLegs checks before each leg if not already present
if (!masterCode.includes('if (maxLegs <= 1) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 2\. LEG 2)/,
    '\r\n            if (maxLegs <= 1) return null;$1'
  );
}
if (!masterCode.includes('if (maxLegs <= 2) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 3\. LEG 3)/,
    '\r\n            if (maxLegs <= 2) return null;$1'
  );
}
if (!masterCode.includes('if (maxLegs <= 3) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 4\. LEG 4)/,
    '\r\n            if (maxLegs <= 3) return null;$1'
  );
}
if (!masterCode.includes('if (maxLegs <= 4) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 5\. LEG 5)/,
    '\r\n            if (maxLegs <= 4) return null;$1'
  );
}
if (!masterCode.includes('if (maxLegs <= 5) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 6\. LEG 6)/,
    '\r\n            if (maxLegs <= 5) return null;$1'
  );
}
if (!masterCode.includes('if (maxLegs <= 6) return null;')) {
  masterCode = masterCode.replace(
    /(\r?\n\s*\/\/ =+\r?\n\s*\/\/ 7\. LEG 7)/,
    '\r\n            if (maxLegs <= 6) return null;$1'
  );
}

fs.writeFileSync('RevitScaffoldingTool/ScaffoldingBuilder.Master.cs', masterCode, 'utf8');
console.log('Updated ScaffoldingBuilder.Master.cs successfully.');

// 2. Update ScaffoldingBuilder.TableRun.cs
let tableCode = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', 'utf8');

const targetCheck = `            int nRows = rows.Count;`;
const replacementCheck = `            int nRows = rows.Count;

            // KIỂM TRA CHUỖI GÓC NỐI LIÊN TỤC THEO MASTER (6 TRƯỜNG HỢP LIÊN TỤC):
            // Kế thừa 100% chuẩn ALINCO từ ExecuteMaster6CornersContinuous (Master.cs)
            bool isContinuousMaster = false;
            if (nRows >= 2 && rows[0].ConnectCase == "TH 1.1")
            {
                isContinuousMaster = true;
                string[] expectedCases = { "TH 1.1", "TH 1.2", "TH 1.3", "TH 1.4", "TH 2.1", "TH 2.2" };
                for (int c = 0; c < Math.Min(nRows - 1, expectedCases.Length); c++)
                {
                    if (rows[c].ConnectCase != expectedCases[c])
                    {
                        isContinuousMaster = false;
                        break;
                    }
                }
            }

            if (isContinuousMaster)
            {
                opts.X0 = rows[0].StartX;
                opts.Y0 = rows[0].StartY;
                if (rows[0].StartZ != 0) opts.Z0 = rows[0].StartZ;
                return ExecuteMaster6CornersContinuous(doc, opts, level, placedIds, nRows);
            }`;

if (!tableCode.includes('isContinuousMaster')) {
  tableCode = tableCode.replace(targetCheck, replacementCheck);
  fs.writeFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', tableCode, 'utf8');
  console.log('Updated ScaffoldingBuilder.TableRun.cs successfully.');
} else {
  console.log('ScaffoldingBuilder.TableRun.cs already contains isContinuousMaster.');
}
