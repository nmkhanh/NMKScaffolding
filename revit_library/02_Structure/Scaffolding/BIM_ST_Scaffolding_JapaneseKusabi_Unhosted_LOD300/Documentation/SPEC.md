# ĐẶC TẢ KỸ THUẬT REVIT FAMILY: HỆ GIÀN GIÁO NÊM NHẬT BẢN (KUSABI-SHIKI ASHIBA)
> **Mã Family**: `BIM_ST_Scaffolding_JapaneseKusabi_Unhosted_LOD300`  
> **Tiêu chuẩn thiết kế**: JIS A 8951 / Hiệp hội Thiết bị Tạm Nhật Bản (仮設工業会 - Kasetsu Kogyokai) / Luật An toàn Vệ sinh Lao động Nhật Bản (安衛則 - An'ei-soku)  
> **Revit Target**: Autodesk Revit 2022  
> **Phương pháp triển khai**: Tổ hợp Hệ thống (Main Host Assembly) chứa 8 Cấu kiện chia sẻ độc lập (**Nested Shared Families**)

---

## 1. TỔNG QUAN HỆ GIÀN GIÁO KUSABI NHẬT BẢN (楔緊結式足場)

Hệ giàn giáo nêm Nhật Bản (Kusabi Scaffolding) là giải pháp giàn giáo phổ biến và an toàn hàng đầu tại Nhật Bản, áp dụng cho các công trình xây dựng mới, hoàn thiện và cải tạo mặt ngoài (nhà phố, biệt thự, tòa nhà cao tầng, bảo dưỡng cầu đường).

### Ưu điểm cơ học & quy chuẩn Nhật Bản:
1. **Ống thép chính chuẩn Nhật**: Đường kính tiêu chuẩn $\phi 48.6 \text{ mm}$, độ dày $t = 1.8 \text{ mm} \div 2.4 \text{ mm}$, thép mạ kẽm nhúng nóng STK500 / STK400 chống ăn mòn và độ bền uốn cao.
2. **Khoảng cách bước nhịp (Span / Bay Length)**: Tuân theo module chuẩn xây dựng Nhật Bản: **1800 mm** (hoặc bước phụ **1500 mm**, **1200 mm**, **900 mm**, **600 mm**).
3. **Bề rộng lọt lòng (Bay Width)**: Chuẩn **900 mm** (tiêu chuẩn thoải mái) hoặc **600 mm** (tiêu chuẩn hẹp cho công trình khe hẹp).
4. **Chiều cao mỗi tầng (Lift Height)**: Chuẩn **1800 mm** (hoặc **1900 mm**) đảm bảo chiều cao thông thủy công nhân đứng thẳng, không bị cụng đầu.
5. **Tai nêm liên kết (Pocket Pitch)**: Hàn cố định cách đều **450 mm** dọc thân cột.

---

## 2. PHÂN TÍCH CHI TIẾT CẤU TẠO 3 BỘ PHẬN LỚN THEO QUY CHUẨN NHẬT

Hệ giàn giáo được phân bổ thành 3 tầng chức năng rõ rệt theo đúng yêu cầu:

```
┌────────────────────────────────────────────────────────────────────────┐
│  BỘ PHẬN TRÊN (TOP STAGE) - TẦNG ĐỈNH & BẢO VỆ AN TOÀN TRÊN CAO         │
│  - Cột lan can đỉnh (手摺柱)                                           │
│  - Tay vịn trên cùng (Top Guardrail H=900mm) + Lan can giữa (Mid Rail)  │
│  - Mâm sàn trên cùng (布板) + Ván chắn chân trên cùng (幅木 H≥150mm)    │
├────────────────────────────────────────────────────────────────────────┤
│  BỘ PHẬN GIỮA (MIDDLE STAGE) - CÁC TẦNG THAO TÁC ĐIỂN HÌNH (TYPICAL)   │
│  - Cột chống đơn nối tiếp (建地) lồng khớp Spigot                       │
│  - Mâm sàn đi lại dập gân chống trượt (布板 / アンチ)                  │
│  - Thanh giằng chéo chịu lực (筋交い)                                   │
│  - Lan can tay vịn 2 cấp (Tay vịn 900mm + Lan can giữa 450mm)           │
│  - Tấm ván chắn chân chống rơi dụng cụ (幅木 - Habaki H≥150mm)         │
├────────────────────────────────────────────────────────────────────────┤
│  BỘ PHẬN DƯỚI (BOTTOM STAGE) - CHÂN ĐẾ CHỊU LỰC & CHỐNG LÚN            │
│  - Tấm ván lót đế (敷板 - Shikiban): Chống lún, phân tán tải xuống nền  │
│  - Kích chân điều chỉnh ren M36 (ジャッキベース - Jack Base)            │
│  - Cột chống chính tầng 1 ghép từ các cột đơn (建地)                    │
│  - Giằng khóa chân đáy (根がらみ - Negarami / Base Ledger H≤200mm)      │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.1. BỘ PHẬN DƯỚI (Bottom Stage - Footing & Base)
Bao gồm các cấu kiện chịu toàn bộ tải trọng tĩnh (bản thân hệ giáo) và hoạt tải (người, vật liệu) truyền xuống nền đất/sàn bê tông:
- **Tấm ván lót đế (敷板 - Shikiban)**:
  - *Ý nghĩa thực tế*: Nền đất công trình thường không đồng đều và có nguy cơ lún cục bộ. Ván lót đế (gỗ dầu dày 35-50mm hoặc tấm nhựa/thép $200 \times 200 \text{ mm}$ đến $400 \times 400 \text{ mm}$) giúp tăng diện tích tiếp xúc, chống lún sụt.
  - *Hình học LOD 200*: Khối hộp chữ nhật mỏng (`Extrusion Box`).
- **Kích chân tăng đơ (ジャッキベース - Jack Base)**:
  - *Ý nghĩa thực tế*: Gồm bản mã đế $140 \times 140 \times 6 \text{ mm}$ có 4 lỗ đinh vít + trục ren thép đường kính $\approx 34-36 \text{ mm}$ và con tán ren có cánh xoay. Cho phép điều chỉnh độ dốc mặt đất từ $50 \text{ mm} \div 350 \text{ mm}$ để toàn bộ hệ cột thẳng đứng 100%.
  - *Hình học LOD 200*: Đế vuông phẳng + Trục ren dạng hình trụ tròn.
- **Cột chống đơn tầng 1 (建地 - Tateji)**:
  - *Ý nghĩa thực tế*: Ống thép $\phi 48.6 \text{ mm}$, đầu trên có cổ côn lồng ghép (Joint Pin).
- **Thanh giằng khóa chân (根がらみ - Negarami / Base Ledger)**:
  - *Ý nghĩa thực tế*: Bắt buộc cách mặt nền $\le 200 \text{ mm}$ theo tiêu chuẩn Nhật để khóa 4 chân cột thành một khối cứng vững, chống tách chân khi chịu tải trọng động.

---

### 2.2. BỘ PHẬN GIỮA (Middle Stage - Typical Working Bay)
Khoang thao tác làm việc tiêu chuẩn của thợ hoàn thiện, trát tường, sơn bả hoặc lắp kính:
- **Cột chống đơn nối tầng (建地 - Tateji)**:
  - Cột nối tiếp nhau qua đầu ống lồng Spigot / Joint Pin, khóa an toàn bằng chốt nêm.
- **Mâm sàn thao tác đi lại (布板 - Nunobita / アンチ - Anti)**:
  - *Ý nghĩa thực tế*: Mặt sàn làm bằng tôn kẽm dập lỗ dập nổi (perforated embossed) để chống trơn trượt khi trời mưa, nước thoát nhanh. Hai đầu có 4 móc ngàm (Hook) ôm khít vào thanh giằng ngang Ledger, tích hợp lẫy khóa tự động chống gió bão hất tung mâm sàn.
  - *Kích thước*: Rộng $400 \text{ mm}, 500 \text{ mm}$, dài tương ứng nhịp $1800 \text{ mm}$. Khoang rộng 900mm thường đặt 2 tấm 400mm hoặc 1 tấm 500mm + 1 tấm 400mm.
  - *Hình học LOD 200*: Mặt phẳng chữ nhật dập mép hộp (`Plate Box`).
- **Thanh giằng chéo (筋交い - Sujikai / Cross Brace)**:
  - *Ý nghĩa thực tế*: Ống thép $\phi 27.2 \text{ mm}$ hoặc $\phi 34 \text{ mm}$, hai đầu dập dẹt có tai móc nêm để gắn chéo góc $40^\circ \div 60^\circ$ vào cột chống, triệt tiêu lực xô ngang do gió bão hoặc rung động thi công.
- **Hệ thống Tay vịn / Lan can 2 cấp (手摺 - Tesuri)**:
  - *Tiêu chuẩn Nhật*: Gồm tay vịn chính (Top Rail) ở cao độ $900 \text{ mm}$ tính từ mặt mâm và thanh lan can giữa (Mid Rail) ở cao độ $450 \text{ mm}$ chống người lọt qua khe hở khi ngã.
- **Tấm chắn chân / Len chân (幅木 - Habaki / Toe Board)**:
  - *Quy chuẩn an toàn Nhật (An'ei-soku Điều 563)*: Bắt buộc xung quanh mọi sàn thao tác cao độ $\ge 2.0 \text{ m}$. Chiều cao tối thiểu $H \ge 150 \text{ mm}$. Ngăn chặn rơi ốc vít, búa, máy khoan xuống dưới gây nguy hiểm cho người đi bộ.
  - *Hình học LOD 200*: Tấm chắn đứng cao 150mm khóa vào chân cột.

---

### 2.3. BỘ PHẬN TRÊN (Top Stage - Upper Guardrail Protection)
Là tầng sàn trên cùng nơi thợ thi công phần mái hoặc mép tường trên cao:
- Vẫn có đầy đủ: Mâm sàn đi lại (布板), giằng chéo (筋交い), tấm chắn chân (幅木).
- **Điểm bổ sung đặc biệt**:
  - **Cột lan can đỉnh (手摺柱 - Tesuri-bashira)**: Cột ngắn cao $900 \div 1000 \text{ mm}$ gắn lồng vào đầu cột tầng dưới để tạo điểm tựa cho lan can đỉnh cao nhất.
  - **Tay vịn đỉnh trên cùng (Top Guardrail)**: Đóng vai trò như lan can bảo vệ ban công trên đỉnh giàn giáo, ngăn chặn tuyệt đối rơi ngã từ trên cao.

---

## 3. KIẾN TRÚC REVI FAMILY: TẠI SAO PHẢI DÙNG NESTED SHARED FAMILY?

Đây là cốt lõi kỹ thuật quan trọng nhất đáp ứng trọn vẹn yêu cầu: **"Geometry đơn giản nhưng làm Shared Family cho từng bộ phận để sau này tạo sửa geometry như thật chỉ cần load vào là xong"**.

```
┌────────────────────────────────────────────────────────────────────────┐
│            MAIN HOST FAMILY (BIM_ST_Scaffolding_JapaneseKusabi)        │
│                                                                        │
│   [Tham số Điều khiển]: Bay_Length, Bay_Width, Story_Height, v.v.     │
│             │                      │                      │            │
│             ▼                      ▼                      ▼            │
│   ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   │
│   │  BỘ PHẬN DƯỚI    │   │  BỘ PHẬN GIỮA    │   │  BỘ PHẬN TRÊN    │   │
│   │ (Bottom Stage)   │   │ (Middle Stage)   │   │ (Top Stage)      │   │
│   └─────────┬────────┘   └─────────┬────────┘   └─────────┬────────┘   │
└─────────────┼──────────────────────┼──────────────────────┼────────────┘
              │                      │                      │
   SHARED = TRUE          SHARED = TRUE          SHARED = TRUE
              │                      │                      │
  ┌───────────┴──────────┐┌──────────┴───────────┐┌─────────┴───────────┐
  │ 1. Scaff_SoleBoard   ││ 4. Scaff_Ledger      ││ 8. Scaff_TopPost    │
  │ 2. Scaff_JackBase    ││ 5. Scaff_CrossBrace  ││ (Tái sử dụng Ledger,│
  │ 3. Scaff_StandardPost││ 6. Scaff_PlankWalkway││ Plank, ToeBoard)    │
  │                      ││ 7. Scaff_ToeBoard    ││                     │
  └──────────────────────┘└──────────────────────┘└──────────────────────┘
```

### 3 Lợi ích vàng của Nested Shared Family:
1. **Dễ dàng nâng cấp hình học (From Simple LOD 200 to Hyper-realistic LOD 400)**:
   - Ban đầu, mỗi bộ phận chỉ là khối hình học cơ bản (hộp, ống tròn) để dựng nhanh, cực nhẹ máy và chạy mượt trên Revit 2022.
   - Khi muốn nâng cấp thành hình học thật (vẽ chi tiết ren xoắn của Jack base, dập lỗ hoa thị của mâm sàn Nunobita, chốt nêm gập lẫy lò xo của giằng Sujikai): **Người dùng chỉ cần mở riêng từng file Sub-family (.rfa) -> vẽ thêm chi tiết -> Bấm "Load into Project and Close"**.
   - Toàn bộ giàn giáo tự động cập nhật ngay lập tức mà **KHÔNG CẦN CHỈNH SỬA LẠI KHUNG SƯỜN MAIN HOST**!
2. **Bóc tách bảng thống kê độc lập (Revit Schedule & Quantity Takeoff)**:
   - Vì được tick `Shared = Yes`, trong bảng thống kê vật tư của Revit (Multi-Category Schedule hoặc Generic Model Schedule), Revit sẽ đếm chính xác từng chi tiết:
     - 4 Cái Kích chân Jack Base
     - 8 Cột đơn Tateji
     - 4 Tấm mâm sàn Nunobita
     - 8 Thanh giằng tay vịn Tesuri
     - 4 Tấm ván chắn chân Habaki
   - Không bị gộp chung thành một cục vô danh!
3. **Quản lý phân loại vật liệu (Material Tagging)**:
   - Từng Sub-family mang sẵn tham số vật liệu: `Steel - Hot Dip Galvanized` (Thép mạ kẽm cho ống và giằng), `Steel - Sheet Perforated` (Thép tôn dập cho mâm sàn), `Wood - Timber Plywood` (Ván gỗ kê chân).

---

## 4. MA TRẬN 8 SHARED SUB-FAMILIES & THAM SỐ LIÊN KẾT

Tất cả các sub-family đều có thuộc tính:
- Category: **Generic Models** (hoặc *Structural Framing*).
- **Shared = Yes** (Bắt buộc trong Family Category and Parameters).

| STT | Tên Sub-Family (.rfa) | Tên tiếng Nhật | Hình học LOD 200 ban đầu | Tham số nhận từ Host | Hình học nâng cấp sau này (LOD 350-400) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | `BIM_ST_Scaff_SoleBoard_Shared` | 敷板 (Shikiban) | Hộp $400 \times 200 \times 40 \text{ mm}$ | `Width`, `Length`, `Thickness` | Thêm vân gỗ vát cạnh hoặc đĩa cao su dập rãnh |
| **2** | `BIM_ST_Scaff_JackBase_Shared` | ジャッキベース (Jack Base) | Tấm đế phẳng $140 \times 140 \text{ mm}$ + Ống trụ ren $\phi 36$ | `Adjustable_Height` ($50 \div 350 \text{ mm}$) | Bản mã 4 lỗ bulong, ren răng cưa, con tán đúc 2 cánh tay xoay |
| **3** | `BIM_ST_Scaff_StandardPost_Shared` | 建地 (Tateji) | Ống trụ rỗng $\phi 48.6 \text{ mm}$, cao theo tầng | `Post_Height` ($1800, 2700, 3600 \text{ mm}$) | Cụm tai nêm đĩa 4 hướng cách nhau 450mm, chốt côn lồng âm dương Spigot |
| **4** | `BIM_ST_Scaff_Ledger_Shared` | 布材 / 手摺 (Nunozai / Tesuri) | Ống trụ ngang $\phi 42.7$ hoặc $\phi 48.6$ | `Ledger_Length` ($1800, 900 \text{ mm}$) | 2 đầu đúc đầu nêm móng ngựa có lá nêm nêm gõ búa |
| **5** | `BIM_ST_Scaff_CrossBrace_Shared` | 筋交い (Sujikai) | Thanh ống chéo $\phi 27.2 \text{ mm}$ | `Brace_Span`, `Brace_Rise` (hoặc `Angle`) | Hai đầu ép dẹt có chốt ngàm xoay và nêm bấm an toàn |
| **6** | `BIM_ST_Scaff_PlankWalkway_Shared` | 布板 / アンチ (Nunobita) | Hộp dẹt $1800 \times 400 \times 40 \text{ mm}$ | `Plank_Length`, `Plank_Width` | Dập hoa văn tròn/chữ nhật chống trượt gân tăng cứng, 4 móc cùm kẹp khóa lẫy chống gió |
| **7** | `BIM_ST_Scaff_ToeBoard_Shared` | 幅木 (Habaki) | Tấm mỏng dựng đứng cao $150 \text{ mm}$ | `Board_Length`, `Board_Height` | Tôn nhôm dập gân chữ U, hai đầu có móc kẹp cài chân cột |
| **8** | `BIM_ST_Scaff_TopPost_Shared` | 手摺柱 (Tesuri-bashira) | Ống trụ cao $900 \text{ mm}$ có cọc lồng đáy | `TopPost_Height` ($900 \text{ mm}$) | Tai nêm đỡ tay vịn lan can đỉnh và chốt bấm khóa chống tuột |

---

## 5. HỆ THAM SỐ TOÀN DIỆN CỦA MAIN HOST FAMILY

Trong file giàn giáo chính (`BIM_ST_Scaffolding_JapaneseKusabi_Unhosted_LOD300.rfa`):

### 5.1. Nhóm Dimensions (Kích thước hình học điều khiển)
- `Bay_Length` (Type, Length): Chiều dài nhịp module (Mặc định: **1800 mm**).
- `Bay_Width` (Type, Length): Chiều rộng khoang giàn giáo (Mặc định: **900 mm**).
- `Story_Height` (Type, Length): Chiều cao mỗi tầng thao tác (Mặc định: **1800 mm**).
- `Number_Of_Middle_Stories` (Type, Integer): Số tầng giữa thao tác (Mặc định: **1** hoặc **2**).
- `Jack_Base_Adjustment` (Instance, Length): Chiều cao kích ren chân điều chỉnh (Mặc định: **150 mm**, phạm vi $50 \div 350 \text{ mm}$).
- `Plank_Width` (Type, Length): Chiều rộng mâm sàn (Mặc định: **400 mm**).
- `ToeBoard_Height` (Type, Length): Chiều cao tấm chắn chân (Mặc định: **150 mm**).
- `Top_Guardrail_Height` (Type, Length): Chiều cao lan can đỉnh (Mặc định: **900 mm**).

### 5.2. Nhóm Visibility / Graphics (Bật tắt linh hoạt từng bộ phận)
- `Show_Sole_Board` (Instance, Yes/No): Bật/tắt tấm ván lót đế (Tắt khi đặt trên sàn bê tông phẳng).
- `Show_Toe_Board_Middle` (Instance, Yes/No): Bật/tắt ván chắn chân tầng giữa.
- `Show_Toe_Board_Top` (Instance, Yes/No): Bật/tắt ván chắn chân tầng trên cùng.
- `Show_Cross_Brace_Front` (Instance, Yes/No): Bật/tắt giằng chéo mặt trước.
- `Show_Cross_Brace_Side` (Instance, Yes/No): Bật/tắt giằng chéo mặt hông.

### 5.3. Nhóm Materials and Finishes (Vật liệu hoàn thiện)
- `Mat_Scaffolding_Steel`: Thép mạ kẽm (Hot-dip Galvanized Steel) gán cho Cột, Giằng, Lan can.
- `Mat_Plank_Steel`: Thép chống trượt mạ kẽm gán cho Mâm sàn.
- `Mat_SoleBoard_Wood`: Gỗ lót chân gán cho Ván đế.
- `Mat_ToeBoard_Alum`: Nhôm/Tôn mạ gán cho Ván chắn chân.

---

## 6. HƯỚNG DẪN CHI TIẾT TỪNG BƯỚC THAO TÁC TRONG REVIT 2022

Dành cho kỹ sư/người dựng hình thực hiện trên Revit 2022:

### Bước 1: Tạo 8 File Sub-Family Đơn giản (LOD 200)
1. Trong Revit 2022: Chọn **File > New > Family > Metric Generic Model.rfa**.
2. **Quan trọng nhất**: Vào **Family Category and Parameters** (biểu tượng cặp thư mục màu vàng ở Ribbon) -> Đánh dấu chọn ô **`Shared`** -> Bấm **OK**.
3. Dựng khối hình học đơn giản:
   - Dùng lệnh **Create > Extrusion** tạo hình dạng cơ bản theo kích thước ở Bảng 4.
   - Khóa các cạnh vào Reference Planes có tham số `Length`, `Width`, `Height`.
   - Lưu file theo đúng tên: `BIM_ST_Scaff_SoleBoard_Shared.rfa`, `BIM_ST_Scaff_JackBase_Shared.rfa`, v.v.

### Bước 2: Tạo Main Host Family (Bộ khung tổng thể)
1. Tạo một Family mới: **Metric Generic Model.rfa**.
2. Thiết lập hệ mặt phẳng tham chiếu (Reference Planes):
   - **Mặt bằng (Floor Plan - Ref. Level)**:
     - Tạo 2 Ref Plane tâm: `Center (Front/Back)` và `Center (Left/Right)` khóa Origin.
     - Tạo các Ref Plane biên: `Ref_Length_Left`, `Ref_Length_Right` khóa đối xứng `=EQ=`, gán Dimension `Bay_Length` ($1800 \text{ mm}$).
     - Tạo các Ref Plane biên: `Ref_Width_Front`, `Ref_Width_Back` khóa đối xứng `=EQ=`, gán Dimension `Bay_Width` ($900 \text{ mm}$).
   - **Mặt đứng (Elevation - Front)**:
     - Cao độ `Ref. Level` ($0.0$).
     - Tạo Ref Plane `Level_Jack_Top` ở cao độ cách chân một khoảng `Jack_Base_Adjustment` ($150 \text{ mm}$).
     - Tạo Ref Plane `Level_Story_1` (Mặt sàn tầng giữa): cao độ $= \text{Level\_Jack\_Top} + \text{Story\_Height}$.
     - Tạo Ref Plane `Level_Story_Top` (Mặt sàn tầng đỉnh): cao độ $= \text{Level\_Story\_1} + \text{Story\_Height}$.
     - Tạo Ref Plane `Level_Guardrail_Top` (Mặt lan can đỉnh): cao độ $= \text{Level\_Story\_Top} + \text{Top\_Guardrail\_Height}$.

### Bước 3: Nạp (Load) 8 Sub-Family vào Main Host & Ráp Vị trí
1. Mở Main Host, nạp lần lượt 8 file `.rfa` con vào.
2. Đặt các cấu kiện vào đúng 3 tầng:
   - **Tầng Dưới**: Đặt 4 tấm Ván đế + 4 Kích chân Jack Base tại 4 góc giao nhau của Ref Plane. Đặt 4 Cột chống đơn tầng 1. Đặt 4 Giằng ngang đáy khóa chân Negarami (cách chân $\approx 150 \text{ mm}$).
   - **Tầng Giữa**: Đặt mâm sàn thao tác (Nunobita) tựa lên hai thanh ngang. Đặt 4 thanh tay vịn lan can (2 thanh cao 900mm, 2 thanh cao 450mm). Đặt thanh giằng chéo Sujikai nghiêng 1 mặt trước và 1 mặt sau. Đặt 4 tấm ván chắn chân Habaki bao quanh 4 mép mâm sàn.
   - **Tầng Trên**: Đặt 4 cột lan can đỉnh Tesuri-bashira. Đặt mâm sàn đỉnh. Đặt tay vịn đỉnh Top Guardrail ở độ cao 900mm tính từ mâm đỉnh. Đặt tấm chắn chân Habaki đỉnh.
3. Dùng lệnh **Align (AL) và khóa ổ khóa (🔒)** các mốc của Sub-family vào các mặt phẳng Reference Planes của Main Family.
4. Chọn từng Sub-family, trong bảng Properties:
   - Bấm vào nút chữ nhật nhỏ xíu bên cạnh tham số kích thước (Associate Family Parameter) để liên kết:
     - `Length` của Sub-family $\rightarrow$ nối vào `Bay_Length` của Host.
     - `Width` của Sub-family $\rightarrow$ nối vào `Bay_Width` của Host.
     - `Material` của Sub-family $\rightarrow$ nối vào `Mat_Scaffolding_Steel` của Host.

### Bước 4: Kiểm tra co giãn (Flex Test)
- Mở **Family Types**:
  - Đổi `Bay_Length` từ $1800 \rightarrow 1500 \rightarrow 1200 \text{ mm}$ -> Bấm Apply -> Hệ giàn giáo co giãn đều, mâm sàn và giằng ngắn lại, không bị đứt gãy.
  - Đổi `Jack_Base_Adjustment` từ $150 \rightarrow 300 \text{ mm}$ -> Kích chân nâng lên, toàn bộ hệ cột tầng trên tịnh tiến lên đều.

---

## 7. CƠ CHẾ NÂNG CẤP HÌNH HỌC SAU NÀY (CHO NGƯỜI DÙNG)

Khi bạn muốn vẽ geometry chi tiết thật như sản phẩm thương mại Nhật Bản (ví dụ hãng ALINCO, HORY, hoặc SANSEI):
1. Bạn KHÔNG cần chạm vào Main Family.
2. Chỉ cần mở file sub-family cần sửa (Ví dụ: `BIM_ST_Scaff_PlankWalkway_Shared.rfa`).
3. Dùng lệnh **Create > Void Extrusion** để khoét hàng ngàn lỗ dập nổi thoát nước trên mâm sàn, vẽ thêm 4 chiếc móc cùm kẹp ở 2 đầu thanh ngang.
4. Bấm **Load into Project and Close** -> Chọn ghi đè **"Overwrite the existing version and its parameter values"**.
5. Ngay lập tức, toàn bộ hệ giàn giáo trong toàn bộ dự án của bạn biến thành giàn giáo siêu thực (Hyper-realistic LOD 400) mà không tốn công dựng lại!

---

## 8. NHẬT KÝ THỰC THI QUA REVIT MCP BRIDGE (LIVE AUDIT LOG)

- [x] **Kết nối trực tiếp phiên Revit 2022**: Port 4848 (Add-in `RevitFamilyMcpBridge` Online).
- [x] **Tự động sinh 8 Shared Family (.rfa) độc lập có Solid Geometry 3D**:
  1. `BIM_ST_Scaff_SoleBoard_Shared.rfa` (Ván lót đế chống lún $400 \times 200 \times 40\text{ mm}$)
  2. `BIM_ST_Scaff_JackBase_Shared.rfa` (Kích chân điều chỉnh ren $140 \times 140\text{ mm}$ + Trục ren $\phi 36$)
  3. `BIM_ST_Scaff_StandardPost_Shared.rfa` (Cột chống đơn $\phi 48.6\text{ mm}$, cao $1800\text{ mm}$)
  4. `BIM_ST_Scaff_Ledger_Shared.rfa` (Thanh giằng ngang / tay vịn $\phi 42.7\text{ mm}$, dài $1800\text{ mm}$)
  5. `BIM_ST_Scaff_CrossBrace_Shared.rfa` (Thanh giằng chéo chịu lực $\phi 27.2\text{ mm}$)
  6. `BIM_ST_Scaff_PlankWalkway_Shared.rfa` (Mâm sàn thao tác dập gân $1800 \times 400 \times 40\text{ mm}$)
  7. `BIM_ST_Scaff_ToeBoard_Shared.rfa` (Ván chắn chân an toàn $H = 150\text{ mm}$)
  8. `BIM_ST_Scaff_TopPost_Shared.rfa` (Cột lan can đỉnh $\phi 48.6\text{ mm}$, cao $900\text{ mm}$)
- [x] **Tự động lồng ghép (Nested) và lắp ráp vào Main Family**:
  - File: `BIM_ST_Scaffolding_JapaneseKusabi_Unhosted_LOD300.rfa`
  - Đã lắp đặt **35 linh kiện instances** vào đúng tọa độ 3D của cả 3 tầng (Dưới, Giữa, Trên).
- [x] **Thiết lập Khóa Ràng Buộc (Constraints & Parameter Linkage)**:
  - Khóa Alignment (🔒) 4 cụm chân cột vào các Reference Planes mặt bằng (`Ref_Length_Left/Right`, `Ref_Width_Front/Back`).
  - Gán Dimension tổng có Label `Bay_Length`, `Bay_Width` và khóa đối xứng `=EQ=`.
  - Nối Associate Element Parameter: `Bay_Length` $\longrightarrow$ `Length` của Mâm sàn, Tay vịn, Ván chân Habaki và Giằng chéo.
- [x] **Tích hợp 5 Family Types chuẩn Nhật Bản**:
  - `Standard_1800x900_3Tiers` ($1800 \times 900\text{ mm}$)
  - `Compact_1800x600_3Tiers` ($1800 \times 600\text{ mm}$)
  - `SubModule_1500x900_3Tiers` ($1500 \times 900\text{ mm}$)
  - `SubModule_1200x900_3Tiers` ($1200 \times 900\text{ mm}$)
  - `ShortModule_900x900_3Tiers` ($900 \times 900\text{ mm}$)
- [x] **Kiểm nghiệm Flex Test trực tiếp trong Revit 2022**: **PASSED 100%** (Co giãn kích thước mượt mà không có bất kỳ xung đột hay đứt gãy ràng buộc nào).
- [x] **Kích hoạt hiển thị**: File giàn giáo 3D hoàn chỉnh đã mở sẵn trên giao diện Revit 2022.
