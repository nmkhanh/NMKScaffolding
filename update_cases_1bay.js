const fs = require('fs');

function updateFile(filePath, transforms) {
    let raw = fs.readFileSync(filePath, 'utf8');
    let isCrlf = raw.includes('\r\n');
    let text = raw.replace(/\r\n/g, '\n');

    for (const { name, search, replace } of transforms) {
        if (typeof search === 'string') {
            if (!text.includes(search)) {
                console.error(`ERROR in ${filePath}: transform '${name}' failed to find target string`);
                process.exit(1);
            }
            text = text.replace(search, replace);
            console.log(`[OK] Applied ${name}`);
        } else if (search instanceof RegExp) {
            if (!search.test(text)) {
                console.error(`ERROR in ${filePath}: transform '${name}' failed to match regex`);
                process.exit(1);
            }
            text = text.replace(search, replace);
            console.log(`[OK] Applied ${name}`);
        }
    }

    if (isCrlf) {
        text = text.replace(/\n/g, '\r\n');
    }
    fs.writeFileSync(filePath, text, 'utf8');
    console.log(`Successfully updated ${filePath}`);
}

// 1. Cases1.cs
updateFile('RevitScaffoldingTool/ScaffoldingBuilder.Cases1.cs', [
    {
        name: 'Case 1.1: 5 bays -> 1 bay',
        search: /List<double> colX = new List<double> \{ x0, x0 \+ 1800, x0 \+ 3000, x0 \+ 4800, x0 \+ 6000, x0 \+ 7800 \};[\s\S]*?double xBay5 = colX\[4\]; \/\/ X = 6000\s*\n\s*double L_bay5 = 1800;/,
        replace: `List<double> colX = new List<double> { x0, x0 + 1800 };

            // 1. Dựng 2 hàng cột dãy chính bằng helper chuẩn hóa (đầu hồi trái k=0 và đầu hồi phải k=1)
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            // 2. Cấu kiện Nhịp 1 dãy chính (X: x0 -> x0 + 1800, L = 1800 mm)
            double xBay5 = x0;
            double L_bay5 = 1800;`
    },
    {
        name: 'Case 1.1: xC and xMax',
        search: /double xC = x0 \+ 6600;([\s\S]*?)double xMax = x0 \+ 7800;/,
        replace: 'double xC = x0 + 600;$1double xMax = x0 + 1800;'
    },
    {
        name: 'Case 1.2: mainBays -> { 1200 }',
        search: 'List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200 };',
        replace: 'List<double> mainBays = new List<double> { 1200 };'
    },
    {
        name: 'Case 1.3: 5 bays -> 1 bay',
        search: /List<double> colX = new List<double> \{ x0, x0 \+ 1800, x0 \+ 3000, x0 \+ 4800, x0 \+ 6000, x0 \+ 7800 \};[\s\S]*?double xBay5 = colX\[4\]; \/\/ X = 6000\s*\n\s*double L_bay5 = 1800;/,
        replace: `List<double> colX = new List<double> { x0, x0 + 1800 };
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7, colX, y0, z0, W, jackH, storyCount, topZ, (k, n) => (k == 0 || k == n - 1), opts);

            double xBay5 = x0;
            double L_bay5 = 1800;`
    },
    {
        name: 'Case 1.3: xC and xMax',
        search: /double xC = x0 \+ 6600;([\s\S]*?)double xMax = x0 \+ 7800;/,
        replace: 'double xC = x0 + 600;$1double xMax = x0 + 1800;'
    },
    {
        name: 'Case 1.4: mainBays -> { 1200 }',
        search: 'List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200 };',
        replace: 'List<double> mainBays = new List<double> { 1200 };'
    }
]);

// 2. Cases2.cs
updateFile('RevitScaffoldingTool/ScaffoldingBuilder.Cases2.cs', [
    {
        name: 'Case 2.1: mainBays -> { 1800 }',
        search: 'List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };',
        replace: 'List<double> mainBays = new List<double> { 1800 };'
    },
    {
        name: 'Case 2.1: xA = x0 + 1800, xB = x0 + 3000',
        search: `            double xA = x0 + 7800;\n            double xB = x0 + 9000;`,
        replace: `            double xA = x0 + 1800;\n            double xB = x0 + 3000;`
    },
    {
        name: 'Case 2.2: mainBays -> { 1800 }',
        search: 'List<double> mainBays = new List<double> { 1800, 1200, 1800, 1200, 1800 };',
        replace: 'List<double> mainBays = new List<double> { 1800 };'
    },
    {
        name: 'Case 2.2: xA = x0 + 1800, xB = x0 + 3000',
        search: `            double xA = x0 + 7800;\n            double xB = x0 + 9000;`,
        replace: `            double xA = x0 + 1800;\n            double xB = x0 + 3000;`
    }
]);
