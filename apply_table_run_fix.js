const fs = require('fs');

let content = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', 'utf8');
let isCrlf = content.includes('\r\n');
content = content.replace(/\r\n/g, '\n');

// 1. Update ExecuteTablePipeline (around lines 130-170)
const oldPipelineTarget = `            int nRows = rows.Count;

            // TRƯỜNG HỢP 1: CHỈ CÓ 1 HÀNG TRONG TABLE (Người dùng test 1 hàng đơn lẻ)
            if (nRows == 1)
            {
                var rowSingle = rows[0];
                bool hasCorner = !string.IsNullOrEmpty(rowSingle.ConnectCase) && rowSingle.ConnectCase != "None";
                if (!hasCorner)
                {
                    // Dựng thẳng bình thường đóng kín 2 đầu
                    BuildTableStraightRun(doc, rowSingle, opts, level, placedIds, ensureCol, placeTransom, sym1, sym4, sym5, sym6, sym7,
                        openStart: false, hasCornerConnect: false);
                }
                else
                {
                    // Standalone test: Dựng các nhịp trước + Dựng nhịp góc cuối + Nhánh rẽ mẫu (100% giống kết quả Button)
                    BuildTableStraightRun(doc, rowSingle, opts, level, placedIds, ensureCol, placeTransom, sym1, sym4, sym5, sym6, sym7,
                        openStart: false, hasCornerConnect: true);

                    BuildTableCornerConnect(doc, rowSingle, null, rowSingle.ConnectCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
                return null;
            }

            // TRƯỜNG HỢP 2: CÓ TỪ 2 HÀNG TRỞ LÊN (Thi công liên tục nhiều Leg theo đúng quy trình)
            bool isClosedLoop = false;
            var rLast = rows[nRows - 1];
            var rFirst = rows[0];
            double dEndToStart = Math.Sqrt(Math.Pow(rLast.EndX - rFirst.StartX, 2) + Math.Pow(rLast.EndY - rFirst.StartY, 2));
            isClosedLoop = (dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rLast.ConnectCase) && rLast.ConnectCase != "None");

            // THI CÔNG LIÊN TỤC NHIỀU ROW DỰA TRÊN DATA BẢNG TABLE (100% ĐỘNG THEO BayLengths, Width, ConnectCase):
            // Kế thừa logic hình học từ Master 6 Corners: Mỗi hàng kế tiếp tự động bắt đầu từ cửa ngõ của góc nối hàng trước.
            BuildTableChainedContinuous(doc, opts, rows, level, placedIds, ensureCol, placeTransom,
                sym1, sym2, sym3, sym4, sym5, sym6, sym7, isClosedLoop);

            return null;`;

const newPipelineReplacement = `            int nRows = rows.Count;

            // KIỂM TRA VÒNG KHÉP KÍN (CLOSED LOOP):
            bool isClosedLoop = false;
            var rLast = rows[nRows - 1];
            var rFirst = rows[0];
            double dEndToStart = Math.Sqrt(Math.Pow(rLast.EndX - rFirst.StartX, 2) + Math.Pow(rLast.EndY - rFirst.StartY, 2));
            isClosedLoop = (nRows >= 3 && dEndToStart <= 3500.0) || (!string.IsNullOrEmpty(rLast.ConnectCase) && rLast.ConnectCase != "None" && nRows >= 2);

            // THI CÔNG TOÀN BỘ CÁC ROW VÀ GÓC NỐI TRONG BẢNG TABLE (100% TỰ CUSTOM CHIỀU DÀI):
            // Với mỗi hàng i nối sang hàng i + 1:
            // Lấy nhịp cuối của row[i] và nhịp đầu của row[i+1], áp dụng trường hợp connect tương tự như 6 button chuẩn hóa!
            for (int i = 0; i < nRows; i++)
            {
                var rowCur = rows[i];
                var rowNext = (i < nRows - 1) ? rows[i + 1] : (isClosedLoop ? rows[0] : null);
                bool hasNext = (rowNext != null) && (!string.IsNullOrEmpty(rowCur.ConnectCase) && rowCur.ConnectCase != "None");
                bool isFirstRow = (i == 0);

                // 1. Dựng các nhịp thẳng tiêu chuẩn của rowCur (nếu có các nhịp giữa nBays > 1)
                // - Hàng đầu tiên (i == 0): bắt đầu từ nhịp 0.
                // - Hàng từ thứ 2 trở đi (i > 0): nhịp 0 đã được góc nối trước dựng, nên bắt đầu từ nhịp 1!
                int startBay = isFirstRow ? 0 : 1;
                BuildTableStraightRun(doc, rowCur, opts, level, placedIds, ensureCol, placeTransom,
                    sym1, sym4, sym5, sym6, sym7,
                    openStart: !isFirstRow || isClosedLoop,
                    hasCornerConnect: hasNext,
                    startBayIndex: startBay);

                // 2. Góc nối giữa nhịp cuối rowCur và nhịp đầu rowNext:
                // Áp dụng đúng trường hợp connect tương tự như code vừa đổi phía trên với kích thước custom 100%!
                if (hasNext)
                {
                    string cCase = !string.IsNullOrEmpty(rowCur.ConnectCase) ? rowCur.ConnectCase : "TH 1.1";
                    BuildTableCornerConnect(doc, rowCur, rowNext, cCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
                else if (nRows == 1 && !string.IsNullOrEmpty(rowCur.ConnectCase) && rowCur.ConnectCase != "None")
                {
                    // Chế độ test 1 hàng đơn lẻ có ConnectCase: Dựng nhịp cuối + nhịp rẽ mẫu
                    BuildTableCornerConnect(doc, rowCur, null, rowCur.ConnectCase, opts, level, placedIds, ensureCol, placeTransom,
                        sym1, sym2, sym3, sym4, sym5, sym6, sym7, isStandalone: true);
                }
            }

            return null;`;

if (!content.includes(oldPipelineTarget)) {
    console.error("ERROR: oldPipelineTarget not found!");
    process.exit(1);
}
content = content.replace(oldPipelineTarget, newPipelineReplacement);
console.log("[OK] Updated ExecuteTablePipeline");

// 2. Update BuildTableStraightRun signature and body to support startBayIndex
const oldStraightSignature = `        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool openStart, bool hasCornerConnect)`;

const newStraightSignature = `        private static void BuildTableStraightRun(Document doc, BayRowModel row, BuildOptions opts, Level level, List<int> placedIds,
            Action<double, double> ensureCol, Action<double, double, double, double, double> placeTransom,
            FamilySymbol sym1, FamilySymbol sym4, FamilySymbol sym5, FamilySymbol sym6, FamilySymbol sym7,
            bool openStart, bool hasCornerConnect, int startBayIndex = 0)`;

if (!content.includes(oldStraightSignature)) {
    console.error("ERROR: oldStraightSignature not found!");
    process.exit(1);
}
content = content.replace(oldStraightSignature, newStraightSignature);

const oldStraightColLoop = `            int nBaysTotal = bayLens.Count;
            // Nếu có corner connect ở cuối, nhịp cuối sẽ do hàm Connect xử lý
            int nStandardBays = hasCornerConnect ? Math.Max(0, nBaysTotal - 1) : nBaysTotal;
            int nColsToBuild = nStandardBays + 1;

            // 1. Dựng cột và giằng ngang cho các nhịp tiêu chuẩn
            for (int k = 0; k < nColsToBuild; k++)`;

const newStraightColLoop = `            int nBaysTotal = bayLens.Count;
            // Nếu có corner connect ở cuối, nhịp cuối sẽ do hàm Connect xử lý
            int nStandardBays = hasCornerConnect ? Math.Max(0, nBaysTotal - 1) : nBaysTotal;
            int nColsToBuild = nStandardBays + 1;

            if (nStandardBays <= startBayIndex && hasCornerConnect) return;

            // 1. Dựng cột và giằng ngang cho các nhịp tiêu chuẩn
            for (int k = startBayIndex; k < nColsToBuild; k++)`;

if (!content.includes(oldStraightColLoop)) {
    console.error("ERROR: oldStraightColLoop not found!");
    process.exit(1);
}
content = content.replace(oldStraightColLoop, newStraightColLoop);

const oldStraightStartRail = `                // Lan can đầu hồi: Chỉ đặt ở cột đầu tiên nếu không bị mở nối góc
                if (k == 0 && !openStart)`;

const newStraightStartRail = `                // Lan can đầu hồi: Chỉ đặt ở cột đầu tiên nếu không bị mở nối góc và bắt đầu từ bay 0
                if (k == 0 && !openStart && startBayIndex == 0)`;

if (!content.includes(oldStraightStartRail)) {
    console.error("ERROR: oldStraightStartRail not found!");
    process.exit(1);
}
content = content.replace(oldStraightStartRail, newStraightStartRail);

const oldStraightBayLoop = `            // 2. Dựng cấu kiện dọc cho các nhịp tiêu chuẩn
            for (int b = 0; b < nStandardBays; b++)`;

const newStraightBayLoop = `            // 2. Dựng cấu kiện dọc cho các nhịp tiêu chuẩn
            for (int b = startBayIndex; b < nStandardBays; b++)`;

if (!content.includes(oldStraightBayLoop)) {
    console.error("ERROR: oldStraightBayLoop not found!");
    process.exit(1);
}
content = content.replace(oldStraightBayLoop, newStraightBayLoop);
console.log("[OK] Updated BuildTableStraightRun with startBayIndex");

if (isCrlf) content = content.replace(/\n/g, '\r\n');
fs.writeFileSync('RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs', content, 'utf8');
console.log("Successfully updated RevitScaffoldingTool/ScaffoldingBuilder.TableRun.cs");
