# QUY TRÌNH XỬ LÝ TRƯỜNG HỢP 1.0 (TH 1.0) STEP-BY-STEP

Tài liệu này phân rã toàn bộ logic hình học và mã nguồn C# thực thi của **Trường Hợp 1.0: Dãy thẳng nhiều nhịp tiêu chuẩn (MultiBay Straight)** theo từng bước chi tiết (`step-by-step`), kèm giải thích tọa độ, tham số và đối chiếu cấu kiện để bạn dễ dàng rà soát và chỉnh sửa.

---

## 1. Sơ Đồ Nguyên Lý Hình Học & Tọa Độ TH 1.0

```
     Y (+)
      ▲
      │  (x0, y0)         (x1, y0)         (x2, y0)         (xMax, y0)
   y0 ┼──[Cột 0,0]────────[Cột 1,0]────────[Cột 2,0]────────[Cột n,0]─────► X (+)
      │     │    Nhịp 1      │    Nhịp 2      │    Nhịp k      │
      │     │   BayLen[0]    │   BayLen[1]    │   BayLen[k]    │
      │     │  (Mặt trước)   │  (Mặt trước)   │  (Mặt trước)   │
      │     │                │                │                │
y0 - W┼──[Cột 0,1]────────[Cột 1,1]────────[Cột 2,1]────────[Cột n,1]
      │  (x0, y0-W)       (x1, y0-W)       (x2, y0-W)       (xMax, y0-W)
      │  (Đầu hồi trái)                                     (Đầu hồi phải)
```

### Các Thông Số Đầu Vào:
- `x0, y0, z0`: Tọa độ gốc bắt đầu của dãy giàn giáo (mặc định `0, 0, 0`).
- `BayLengths`: Danh sách chiều dài các nhịp dọc theo trục X (ví dụ: `1800, 1200, 1800, 1200, 1800` hoặc chuỗi phân tích `1800,2x1200,...`).
- `W`: Bề rộng giàn giáo (mặc định `1200` mm).
- `jackH`: Chiều cao kích chân (mặc định `250` mm).
- `storyCount`: Số tầng giàn giáo (mặc định `1` hoặc lấy từ Data).

### Công Thức Tọa Độ Then Chốt:
1. **Lưới cột theo phương X**:
   $$x_0 = \text{X0}, \quad x_k = x_{k-1} + L_{\text{bay}}[k-1], \quad x_{\text{Max}} = x_n$$
2. **Hàng cột trước và hàng cột sau**:
   - Hàng cột trước: $Y = y_0$ (mặt lan can an toàn).
   - Hàng cột sau: $Y = y_0 - W$ (mặt giằng chéo chịu lực).
3. **Mặt đầu hồi (Transom / Gable Ends)**:
   - Đầu hồi bắt đầu: $X = x_0$ ($k = 0$).
   - Đầu hồi kết thúc: $X = x_{\text{Max}}$ ($k = n$).
   - Cả 2 đầu hồi được khóa kín bằng giằng chân $h_1$, giằng ngang đỡ mâm $h_5$, lan can an toàn kép $h_6, h_7$, và ván chắn biên Family 7.

---

## 2. Mã Nguồn C# Chi Tiết Từng Bước (Bóc Tách Từ ScaffoldingBuilder.Cases2.cs)

Hàm xử lý `ExecuteMultiBay` được bóc tách theo 6 bước chuẩn hóa:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitScaffoldingTool
{
    public static partial class ScaffoldingBuilder
    {
        /// <summary>
        /// TRƯỜNG HỢP 1.0: DÃY THẲNG NHIỀU NHỊP TIÊU CHUẨN (MULTIBAY STRAIGHT)
        /// Dựng dải giàn giáo thẳng liên tục theo danh sách nhịp BayLengths.
        /// </summary>
        public static string ExecuteMultiBay_StepByStep(Document doc, BuildOptions opts, Level level, List<int> placedIds)
        {
            // =========================================================================
            // BƯỚC 1: KHỞI TẠO CÁC BIẾN KÍCH THƯỚC VÀ CAO ĐỘ
            // =========================================================================
            double x0 = opts.X0;
            double y0 = opts.Y0;
            double z0 = opts.Z0;
            double W  = opts.BayWidth > 0 ? opts.BayWidth : 1200.0;
            List<double> bayLens = (opts.BayLengths != null && opts.BayLengths.Count > 0) 
                                   ? opts.BayLengths 
                                   : new List<double> { 1800, 1200, 1800, 1200, 1800 };

            double jackH = GetJackHeight(opts);
            int storyCount = (opts != null && opts.StoryCount > 0) ? opts.StoryCount : 1;
            double topZ = GetTopZ(z0, jackH, storyCount, opts);

            // =========================================================================
            // BƯỚC 2: TẢI & KÍCH HOẠT CÁC FAMILY SYMBOL (FAMILY 1 ĐẾN 7)
            // =========================================================================
            var sym1 = GetOrLoadFamilySymbol(doc, "1", opts.FamilyFolder); // Base board (Ván lót đáy)
            var sym2 = GetOrLoadFamilySymbol(doc, "2", opts.FamilyFolder); // Base Jack (Kích chân)
            var sym3 = GetOrLoadFamilySymbol(doc, "3", opts.FamilyFolder); // Post (Cột chống đứng)
            var sym4 = GetOrLoadFamilySymbol(doc, "4", opts.FamilyFolder); // Ledger / Transom (Giằng ngang/dọc)
            var sym5 = GetOrLoadFamilySymbol(doc, "5", opts.FamilyFolder); // Diagonal Brace (Giằng chéo)
            var sym6 = GetOrLoadFamilySymbol(doc, "6", opts.FamilyFolder); // Plank (Mâm thao tác)
            var sym7 = GetOrLoadFamilySymbol(doc, "7", opts.FamilyFolder); // Toe Board (Ván chắn biên)

            if (sym1 == null || sym2 == null || sym3 == null || sym4 == null)
            {
                return "Thiếu Family cốt lõi trong: " + opts.FamilyFolder;
            }

            ActivateSymbol(doc, sym1);
            ActivateSymbol(doc, sym2);
            ActivateSymbol(doc, sym3);
            ActivateSymbol(doc, sym4);
            if (sym5 != null) ActivateSymbol(doc, sym5);
            if (sym6 != null) ActivateSymbol(doc, sym6);
            if (sym7 != null) ActivateSymbol(doc, sym7);

            // =========================================================================
            // BƯỚC 3: DỰNG HỆ LƯỚI CỘT CHỐNG & GIẰNG NGANG ĐẦU HỒI
            // =========================================================================
            int nBays = bayLens.Count;
            List<double> colX = new List<double> { x0 };
            double curX = x0;
            foreach (var b in bayLens) { curX += b; colX.Add(curX); }

            // Dựng lưới cột (Hàng trước Y=y0, Hàng sau Y=y0-W) và giằng ngang đầu hồi tại k=0 và k=nBays
            BuildMainColumnGrid(doc, level, placedIds, sym2, sym3, sym4, sym7,
                                colX, y0, z0, W, jackH, storyCount, topZ,
                                (k, n) => (k == 0 || k == n - 1), opts);

            // =========================================================================
            // BƯỚC 4: DỰNG CẤU KIỆN TỪNG NHỊP (STANDARD BAYS)
            // =========================================================================
            for (int i = 0; i < nBays; i++)
            {
                double xBay = colX[i];
                double bayLen = bayLens[i];

                // Dựng ván lót, giằng chân h1, giằng giữa h3, giằng đỉnh, mâm sàn h5,
                // giằng chéo mặt sau Family 5 và lan can kép mặt trước Family 4
                BuildStandardBayElements(doc, level, placedIds, sym1, sym4, sym5, sym6, sym7,
                                        xBay, y0, z0, W, jackH, bayLen, true, opts);
            }

            return string.Format("Dựng thành công Dãy thẳng TH 1.0 gồm {0} nhịp (Tổng chiều dài {1:F0} mm).", nBays, curX - x0);
        }
    }
}
```

---

## 3. Bảng Kiểm Tra Cấu Kiện & Quy Tắc Kiểm Soát Chất Lượng (Quality Checklist)

- [x] **Cột chống**: Đầy đủ kích chân ($h = 250$ mm) và chuỗi cột tại toàn bộ tọa độ $X \in \text{colX}$, hai hàng $Y = y_0$ và $Y = y_0 - W$.
- [x] **Giằng chân $h_1$**: 100% các mặt trước, sau và 2 đầu hồi được khóa cứng giằng chân.
- [x] **Mặt sau ($Y = y_0 - W$)**: Bố trí Giằng chéo Family 5 suốt chiều dài từng nhịp; tuyệt đối không lắp giằng dọc Family 4 ở cao độ $h_3$ và $h_6$.
- [x] **Mặt trước ($Y = y_0$)**: Bố trí Giằng giữa $h_3$ và Lan can an toàn kép $h_6, h_7$ Family 4; ván chắn biên Family 7.
- [x] **Hai đầu hồi**: Bịt kín bằng lan can kép $h_6, h_7$, giằng ngang đỡ mâm $h_5$ và ván chắn biên Family 7.
- [x] **Sàn thao tác**: 2 mâm thao tác Family 6 ($w = 500$ mm) liên tục suốt chiều dài dãy giàn.
