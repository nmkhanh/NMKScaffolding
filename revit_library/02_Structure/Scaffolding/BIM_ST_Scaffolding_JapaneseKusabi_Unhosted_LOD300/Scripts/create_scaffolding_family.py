# -*- coding: utf-8 -*-
"""
pyRevit / Revit API Script: Automated Japanese Kusabi Scaffolding Family Setup & Audit
Chuyên ngành: Kết cấu & Biện pháp thi công tạm (Temporary Works / Scaffolding)
Tiêu chuẩn: JIS A 8951 / Kasetsu Kogyokai (仮設工業会)
Tương thích: Autodesk Revit 2022
"""

import sys
import clr

# Nạp Revit API Assemblies
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI import *

doc = __revit__.ActiveUIDocument.Document
app = __revit__.Application

def log(msg):
    print("[SCAFFOLDING-BUILDER] {}".format(msg))

def verify_family_document(doc):
    if not doc.IsFamilyDocument:
        TaskDialog.Show("Lỗi Môi Trường", "Script này chỉ chạy trong môi trường Family Editor (.rfa) của Revit 2022!")
        return False
    return True

def set_shared_flag(doc):
    """Kích hoạt cờ Shared = True cho Family hiện hành để bóc tách khối lượng và tráo đổi geometry linh hoạt."""
    try:
        family_mgr = doc.FamilyManager
        owner_family = doc.OwnerFamily
        cat = owner_family.FamilyCategory
        log("Family Category hiện tại: {}".format(cat.Name))
        
        # Thiết lập thuộc tính Shared trong Family Category and Parameters
        shared_param = owner_family.get_Parameter(BuiltInParameter.FAMILY_SHARED)
        if shared_param and not shared_param.IsReadOnly:
            t = Transaction(doc, "Set Family Shared Parameter")
            t.Start()
            shared_param.Set(1) # 1 = True (Shared)
            t.Commit()
            log("Đã bật thành công thuộc tính SHARED = TRUE cho Family!")
        else:
            log("Thuộc tính Shared đã được thiết lập hoặc Read-Only.")
    except Exception as ex:
        log("Cảnh báo khi gán Shared flag: {}".format(str(ex)))

def setup_scaffolding_parameters(doc):
    """Tự động khởi tạo bộ tham số chuẩn cho hệ giàn giáo Nhật Bản"""
    family_mgr = doc.FamilyManager
    params_to_create = [
        # (Tên, BuiltInParameterGroup, ParameterType, IsInstance, Formula/Default)
        ("Bay_Length", GroupTypeId.Geometry, SpecTypeId.Length, False, 1800.0), # mm
        ("Bay_Width", GroupTypeId.Geometry, SpecTypeId.Length, False, 900.0),   # mm
        ("Story_Height", GroupTypeId.Geometry, SpecTypeId.Length, False, 1800.0), # mm
        ("Top_Guardrail_Height", GroupTypeId.Geometry, SpecTypeId.Length, False, 900.0), # mm
        ("Plank_Width", GroupTypeId.Geometry, SpecTypeId.Length, False, 400.0), # mm
        ("ToeBoard_Height", GroupTypeId.Geometry, SpecTypeId.Length, False, 150.0), # mm
        ("Jack_Base_Adjustment", GroupTypeId.Dimensions, SpecTypeId.Length, True, 150.0), # Instance
        ("Show_Sole_Board", GroupTypeId.Visibility, SpecTypeId.Boolean.YesNo, True, 1), # Instance
        ("Show_Toe_Board_Middle", GroupTypeId.Visibility, SpecTypeId.Boolean.YesNo, True, 1),
        ("Show_Toe_Board_Top", GroupTypeId.Visibility, SpecTypeId.Boolean.YesNo, True, 1),
        ("Show_Cross_Brace_Front", GroupTypeId.Visibility, SpecTypeId.Boolean.YesNo, True, 1),
        ("Show_Cross_Brace_Side", GroupTypeId.Visibility, SpecTypeId.Boolean.YesNo, True, 1),
    ]

    log("Bắt đầu khởi tạo hệ tham số giàn giáo Kusabi...")
    t = Transaction(doc, "Init Kusabi Scaffolding Parameters")
    t.Start()
    
    count_created = 0
    for p_name, p_group, p_spec, is_inst, def_val in params_to_create:
        existing = [p for p in family_mgr.Parameters if p.Definition.Name == p_name]
        if not existing:
            try:
                # Revit 2022 ForgeTypeId API
                p = family_mgr.AddParameter(p_name, p_group, p_spec, is_inst)
                count_created += 1
                log("-> Đã tạo tham số: {} (Instance: {})".format(p_name, is_inst))
            except Exception as e:
                # Fallback Revit API cổ điển nếu có
                log("Không thể tạo tham số {}: {}".format(p_name, str(e)))
        else:
            log("-> Tham số đã tồn tại: {}".format(p_name))
            
    t.Commit()
    log("Hoàn thành! Đã tạo {} tham số mới.".format(count_created))

def main():
    if not verify_family_document(doc):
        return
    
    log("=== KHỞI ĐỘNG CÔNG CỤ THIẾT LẬP GIÀN GIÁO NHẬT BẢN (REVIT 2022) ===")
    set_shared_flag(doc)
    setup_scaffolding_parameters(doc)
    
    TaskDialog.Show("Thành Công", 
                    "Đã cấu hình xong hệ tham số và Shared Family cho Giàn giáo Nhật Bản!\n"
                    "Tham khảo tài liệu SPEC.md trong thư mục Documentation để ráp 8 bộ phận con.")

if __name__ == '__main__':
    main()
