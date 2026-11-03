using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitFamilyMcpBridge
{
    public class McpExternalEventHandler : IExternalEventHandler
    {
        private readonly Queue<McpTask> _taskQueue = new Queue<McpTask>();
        private readonly object _lock = new object();

        public void EnqueueTask(McpTask task)
        {
            lock (_lock)
            {
                _taskQueue.Enqueue(task);
            }
        }

        public void Execute(UIApplication app)
        {
            McpTask task = null;
            lock (_lock)
            {
                if (_taskQueue.Count > 0)
                {
                    task = _taskQueue.Dequeue();
                }
            }

            if (task == null) return;

            try
            {
                Document doc = app.ActiveUIDocument != null ? app.ActiveUIDocument.Document : null;
                Dictionary<string, object> res = new Dictionary<string, object>();

                switch (task.Action)
                {
                    case "status":
                    case "ping":
                        res["revitVersion"] = app.Application.VersionNumber;
                        res["activeDoc"] = doc != null ? doc.Title : "No document open";
                        res["isFamilyDoc"] = doc != null && doc.IsFamilyDocument;
                        task.Complete(true, res);
                        break;

                    case "build_family":
                    case "create_family":
                        HandleBuildFamily(app, doc, task);
                        break;

                    case "create_ref_planes":
                        HandleCreateRefPlanes(app, doc, task);
                        break;

                    case "create_parameters":
                        HandleCreateParameters(app, doc, task);
                        break;

                    case "flex_test":
                        HandleFlexTest(app, doc, task);
                        break;

                    case "new_project":
                    case "new_metric_project":
                        HandleNewMetricProject(app, task);
                        break;

                    case "load_family":
                        HandleLoadFamily(app, doc, task);
                        break;

                    case "place_instance":
                        HandlePlaceInstance(app, doc, task);
                        break;

                    case "set_parameter":
                        HandleSetParameter(app, doc, task);
                        break;

                    case "audit_instances":
                    case "audit_scaffolding":
                    case "check_geometry":
                        HandleAuditScaffolding(app, doc, task);
                        break;

                    case "inspect_child_families":
                    case "inspect_families":
                        HandleInspectChildFamilies(app, task);
                        break;

                    default:
                        task.Complete(false, null, "Hành động '" + task.Action + "' không được hỗ trợ.");
                        break;
                }
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi thực thi trong Revit UI thread: " + ex.ToString());
            }
        }

        public string GetName()
        {
            return "RevitFamilyMcpHandler";
        }

        private static double MmToFeet(double mm)
        {
            return mm / 304.8;
        }

        private void HandleBuildFamily(UIApplication app, Document doc, McpTask task)
        {
            if (doc == null || !doc.IsFamilyDocument)
            {
                task.Complete(false, null, "Tài liệu đang mở không phải là một Family (.rfa). Hãy mở hoặc tạo một Family Document trong Revit 2022 trước khi thực hiện!");
                return;
            }

            using (Transaction t = new Transaction(doc, "BIM AI: Full Family Generation"))
            {
                t.Start();
                try
                {
                    View activeView = doc.ActiveView;
                    FamilyManager famMgr = doc.FamilyManager;
                    int planesCreated = 0;
                    int paramsCreated = 0;

                    // 1. Create Parameters
                    if (task.Parameters.ContainsKey("parameters"))
                    {
                        var paramList = task.Parameters["parameters"] as ArrayList;
                        if (paramList != null)
                        {
                            foreach (Dictionary<string, object> pData in paramList)
                            {
                                string pName = pData.ContainsKey("name") ? pData["name"].ToString() : null;
                                if (string.IsNullOrEmpty(pName)) continue;

                                bool isInst = pData.ContainsKey("isInstance") && Convert.ToBoolean(pData["isInstance"]);
                                string formula = pData.ContainsKey("formula") && pData["formula"] != null ? pData["formula"].ToString() : null;

                                FamilyParameter fp = famMgr.get_Parameter(pName);
                                if (fp == null)
                                {
                                    fp = famMgr.AddParameter(pName, BuiltInParameterGroup.PG_GEOMETRY, ParameterType.Length, isInst);
                                    paramsCreated++;
                                }

                                if (!string.IsNullOrEmpty(formula) && fp != null)
                                {
                                    try
                                    {
                                        famMgr.SetFormula(fp, formula);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    // 2. Create Reference Planes
                    if (task.Parameters.ContainsKey("referencePlanes"))
                    {
                        var planeList = task.Parameters["referencePlanes"] as ArrayList;
                        if (planeList != null)
                        {
                            foreach (Dictionary<string, object> rpData in planeList)
                            {
                                string name = rpData.ContainsKey("name") ? rpData["name"].ToString() : "RefPlane";
                                string orient = rpData.ContainsKey("orientation") ? rpData["orientation"].ToString() : "Vertical";
                                double offsetMm = rpData.ContainsKey("defaultOffsetMm") ? Convert.ToDouble(rpData["defaultOffsetMm"]) : 0;
                                double offsetFt = MmToFeet(offsetMm);

                                XYZ p1, p2;
                                if (orient.Equals("Vertical", StringComparison.OrdinalIgnoreCase))
                                {
                                    p1 = new XYZ(offsetFt, -10.0, 0.0);
                                    p2 = new XYZ(offsetFt, 10.0, 0.0);
                                }
                                else
                                {
                                    p1 = new XYZ(-10.0, offsetFt, 0.0);
                                    p2 = new XYZ(10.0, offsetFt, 0.0);
                                }

                                try
                                {
                                    ReferencePlane rp = doc.FamilyCreate.NewReferencePlane(p1, p2, XYZ.BasisZ, activeView);
                                    rp.Name = name;
                                    planesCreated++;
                                }
                                catch { }
                            }
                        }
                    }

                    t.Commit();

                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["status"] = "success";
                    res["familyTitle"] = doc.Title;
                    res["referencePlanesCreated"] = planesCreated;
                    res["parametersCreated"] = paramsCreated;
                    res["message"] = "Đã khởi tạo thành công hệ khung xương và tham số chuẩn BIM trong Revit 2022!";
                    task.Complete(true, res);
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    task.Complete(false, null, "Lỗi Transaction khi tạo Family: " + ex.Message);
                }
            }
        }

        private void HandleCreateRefPlanes(UIApplication app, Document doc, McpTask task)
        {
            if (doc == null || !doc.IsFamilyDocument)
            {
                task.Complete(false, null, "Yêu cầu mở một Family Document trong Revit 2022!");
                return;
            }

            using (Transaction t = new Transaction(doc, "BIM AI: Create Reference Planes"))
            {
                t.Start();
                try
                {
                    View activeView = doc.ActiveView;
                    int count = 0;
                    var planeList = task.Parameters.ContainsKey("referencePlanes") ? task.Parameters["referencePlanes"] as ArrayList : null;

                    if (planeList != null)
                    {
                        foreach (Dictionary<string, object> rpData in planeList)
                        {
                            string name = rpData.ContainsKey("name") ? rpData["name"].ToString() : "RefPlane";
                            string orient = rpData.ContainsKey("orientation") ? rpData["orientation"].ToString() : "Vertical";
                            double offsetMm = rpData.ContainsKey("defaultOffsetMm") ? Convert.ToDouble(rpData["defaultOffsetMm"]) : 0;
                            double offsetFt = MmToFeet(offsetMm);

                            XYZ p1, p2;
                            if (orient.Equals("Vertical", StringComparison.OrdinalIgnoreCase))
                            {
                                p1 = new XYZ(offsetFt, -10.0, 0.0);
                                p2 = new XYZ(offsetFt, 10.0, 0.0);
                            }
                            else
                            {
                                p1 = new XYZ(-10.0, offsetFt, 0.0);
                                p2 = new XYZ(10.0, offsetFt, 0.0);
                            }

                            try
                            {
                                ReferencePlane rp = doc.FamilyCreate.NewReferencePlane(p1, p2, XYZ.BasisZ, activeView);
                                rp.Name = name;
                                count++;
                            }
                            catch { }
                        }
                    }

                    t.Commit();
                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["createdCount"] = count;
                    task.Complete(true, res);
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    task.Complete(false, null, ex.Message);
                }
            }
        }

        private void HandleCreateParameters(UIApplication app, Document doc, McpTask task)
        {
            if (doc == null || !doc.IsFamilyDocument)
            {
                task.Complete(false, null, "Yêu cầu mở một Family Document trong Revit 2022!");
                return;
            }

            using (Transaction t = new Transaction(doc, "BIM AI: Add Parameters"))
            {
                t.Start();
                try
                {
                    FamilyManager famMgr = doc.FamilyManager;
                    int count = 0;
                    var paramList = task.Parameters.ContainsKey("parameters") ? task.Parameters["parameters"] as ArrayList : null;

                    if (paramList != null)
                    {
                        foreach (Dictionary<string, object> pData in paramList)
                        {
                            string pName = pData.ContainsKey("name") ? pData["name"].ToString() : null;
                            if (string.IsNullOrEmpty(pName)) continue;

                            bool isInst = pData.ContainsKey("isInstance") && Convert.ToBoolean(pData["isInstance"]);
                            string formula = pData.ContainsKey("formula") && pData["formula"] != null ? pData["formula"].ToString() : null;

                            FamilyParameter fp = famMgr.get_Parameter(pName);
                            if (fp == null)
                            {
                                fp = famMgr.AddParameter(pName, BuiltInParameterGroup.PG_GEOMETRY, ParameterType.Length, isInst);
                                count++;
                            }

                            if (!string.IsNullOrEmpty(formula) && fp != null)
                            {
                                try
                                {
                                    famMgr.SetFormula(fp, formula);
                                }
                                catch { }
                            }
                        }
                    }

                    t.Commit();
                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["createdCount"] = count;
                    task.Complete(true, res);
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    task.Complete(false, null, ex.Message);
                }
            }
        }

        private void HandleFlexTest(UIApplication app, Document doc, McpTask task)
        {
            if (doc == null || !doc.IsFamilyDocument)
            {
                task.Complete(false, null, "Yêu cầu mở một Family Document trong Revit 2022!");
                return;
            }

            using (Transaction t = new Transaction(doc, "BIM AI: Flex Test Parameters"))
            {
                t.Start();
                try
                {
                    FamilyManager famMgr = doc.FamilyManager;
                    var testValues = task.Parameters.ContainsKey("testValues") ? task.Parameters["testValues"] as Dictionary<string, object> : null;
                    List<string> testedParams = new List<string>();

                    if (testValues != null)
                    {
                        foreach (var kvp in testValues)
                        {
                            FamilyParameter fp = famMgr.get_Parameter(kvp.Key);
                            if (fp != null)
                            {
                                double valMm = Convert.ToDouble(kvp.Value);
                                famMgr.Set(fp, MmToFeet(valMm));
                                testedParams.Add(kvp.Key + ": " + valMm + " mm");
                            }
                        }
                    }

                    doc.Regenerate();
                    t.Commit();

                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["flexStatus"] = "PASSED";
                    res["testedParameters"] = testedParams;
                    res["message"] = "Flex test thành công! Hình học không bị xung đột hay đứt gãy ràng buộc.";
                    task.Complete(true, res);
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    task.Complete(false, null, "Flex test thất bại (Ràng buộc bị phá vỡ): " + ex.Message);
                }
            }
        }

        private void HandleNewMetricProject(UIApplication app, McpTask task)
        {
            try
            {
                string templatePath = @"C:\ProgramData\Autodesk\RVT 2022\Templates\English\DefaultMetric.rte";
                if (task.Parameters.ContainsKey("templatePath") && System.IO.File.Exists(task.Parameters["templatePath"].ToString()))
                {
                    templatePath = task.Parameters["templatePath"].ToString();
                }

                Document projDoc = app.Application.NewProjectDocument(templatePath);
                string childFolder = @"H:\My Drive\Family\New folder\Child";
                string outProjPath = System.IO.Path.Combine(childFolder, "Scaffolding_Project_1800x1200.rvt");
                SaveAsOptions opt = new SaveAsOptions { OverwriteExistingFile = true };
                projDoc.SaveAs(outProjPath, opt);
                app.OpenAndActivateDocument(outProjPath);

                var res = new Dictionary<string, object>();
                res["projectPath"] = outProjPath;
                res["status"] = "created";
                task.Complete(true, res);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi tạo new metric project: " + ex.Message);
            }
        }

        private void HandleLoadFamily(UIApplication app, Document doc, McpTask task)
        {
            try
            {
                if (doc == null)
                {
                    task.Complete(false, null, "Không có tài liệu nào đang mở.");
                    return;
                }

                string filePath = task.Parameters.ContainsKey("filePath") ? task.Parameters["filePath"].ToString() : "";
                if (!System.IO.File.Exists(filePath))
                {
                    task.Complete(false, null, "File không tồn tại: " + filePath);
                    return;
                }

                Family fam = null;
                using (Transaction t = new Transaction(doc, "MCP: Load Family"))
                {
                    t.Start();
                    doc.LoadFamily(filePath, out fam);
                    if (fam != null)
                    {
                        foreach (ElementId symId in fam.GetFamilySymbolIds())
                        {
                            FamilySymbol fs = doc.GetElement(symId) as FamilySymbol;
                            if (fs != null && !fs.IsActive) fs.Activate();
                        }
                    }
                    t.Commit();
                }

                var res = new Dictionary<string, object>();
                res["loaded"] = fam != null;
                res["familyName"] = fam != null ? fam.Name : System.IO.Path.GetFileNameWithoutExtension(filePath);
                task.Complete(true, res);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi load family: " + ex.Message);
            }
        }

        private void HandlePlaceInstance(UIApplication app, Document doc, McpTask task)
        {
            try
            {
                if (doc == null)
                {
                    task.Complete(false, null, "Không có tài liệu nào đang mở.");
                    return;
                }

                string famName = task.Parameters.ContainsKey("familyName") ? task.Parameters["familyName"].ToString() : "";
                string typeName = task.Parameters.ContainsKey("typeName") ? task.Parameters["typeName"].ToString() : "";
                double xMm = task.Parameters.ContainsKey("x") ? Convert.ToDouble(task.Parameters["x"]) : 0.0;
                double yMm = task.Parameters.ContainsKey("y") ? Convert.ToDouble(task.Parameters["y"]) : 0.0;
                double zMm = task.Parameters.ContainsKey("z") ? Convert.ToDouble(task.Parameters["z"]) : 0.0;
                double rotDeg = task.Parameters.ContainsKey("rotDeg") ? Convert.ToDouble(task.Parameters["rotDeg"]) : 0.0;
                double lengthMm = task.Parameters.ContainsKey("lengthMm") ? Convert.ToDouble(task.Parameters["lengthMm"]) : 0.0;

                FamilySymbol targetSym = null;
                var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>();
                foreach (var sym in collector)
                {
                    if (sym.Family != null && (sym.Family.Name.Equals(famName, StringComparison.OrdinalIgnoreCase) ||
                        sym.Family.Name.Equals(famName + ".rfa", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (string.IsNullOrEmpty(typeName) || sym.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetSym = sym;
                            break;
                        }
                    }
                }

                if (targetSym == null)
                {
                    task.Complete(false, null, "Không tìm thấy FamilySymbol cho Family: " + famName);
                    return;
                }

                FamilyInstance inst = null;
                using (Transaction t = new Transaction(doc, "MCP: Place " + famName))
                {
                    t.Start();
                    if (!targetSym.IsActive) targetSym.Activate();

                    XYZ pt = new XYZ(xMm / 304.8, yMm / 304.8, zMm / 304.8);
                    double rotRad = rotDeg * Math.PI / 180.0;
                    inst = PlaceOrientedInstance(doc, targetSym, pt, rotRad, lengthMm);
                    t.Commit();
                }

                var res = new Dictionary<string, object>();
                res["instanceId"] = inst.Id.IntegerValue;
                res["family"] = famName;
                res["x"] = xMm;
                res["y"] = yMm;
                res["z"] = zMm;
                res["rotDeg"] = rotDeg;
                res["lengthMm"] = lengthMm;
                task.Complete(true, res);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi đặt instance: " + ex.Message);
            }
        }

        private void HandleSetParameter(UIApplication app, Document doc, McpTask task)
        {
            try
            {
                if (doc == null)
                {
                    task.Complete(false, null, "Không có tài liệu nào đang mở.");
                    return;
                }

                int elemId = task.Parameters.ContainsKey("elementId") ? Convert.ToInt32(task.Parameters["elementId"]) : 0;
                string paramName = task.Parameters.ContainsKey("paramName") ? task.Parameters["paramName"].ToString() : "";
                object valObj = task.Parameters.ContainsKey("value") ? task.Parameters["value"] : null;

                Element elem = doc.GetElement(new ElementId(elemId));
                if (elem == null)
                {
                    task.Complete(false, null, "Không tìm thấy Element ID: " + elemId);
                    return;
                }

                using (Transaction t = new Transaction(doc, "MCP: Set Parameter " + paramName))
                {
                    t.Start();
                    Parameter p = elem.LookupParameter(paramName);
                    if (p == null || p.IsReadOnly)
                    {
                        t.RollBack();
                        task.Complete(false, null, "Tham số " + paramName + " không tồn tại hoặc ở chế độ ReadOnly.");
                        return;
                    }

                    if (p.StorageType == StorageType.Double)
                    {
                        p.Set(Convert.ToDouble(valObj) / 304.8);
                    }
                    else if (p.StorageType == StorageType.Integer)
                    {
                        p.Set(Convert.ToInt32(valObj));
                    }
                    else if (p.StorageType == StorageType.String)
                    {
                        p.Set(valObj.ToString());
                    }
                    t.Commit();
                }

                var res = new Dictionary<string, object>();
                res["elementId"] = elemId;
                res["paramName"] = paramName;
                res["value"] = valObj;
                task.Complete(true, res);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi set parameter: " + ex.Message);
            }
        }

        private static FamilyInstance PlaceOrientedInstance(Document doc, FamilySymbol sym, XYZ pt, double angleRad, double lengthMm = 0)
        {
            var inst = doc.Create.NewFamilyInstance(pt, sym, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            // 1. Gán offset parameter nếu có
            Parameter pOffset = inst.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)
                             ?? inst.LookupParameter("Elevation from Level")
                             ?? inst.LookupParameter("Offset");
            if (pOffset != null && !pOffset.IsReadOnly)
            {
                pOffset.Set(pt.Z);
            }

            // 2. Dùng MoveElement để đưa Z về đúng tuyệt đối pt.Z
            LocationPoint loc = inst.Location as LocationPoint;
            if (loc != null)
            {
                double deltaZ = pt.Z - loc.Point.Z;
                if (Math.Abs(deltaZ) > 0.0001)
                {
                    ElementTransformUtils.MoveElement(doc, inst.Id, new XYZ(0, 0, deltaZ));
                }
            }

            // 3. Xoay góc nếu cần
            if (Math.Abs(angleRad) > 0.001)
            {
                XYZ curPt = (inst.Location as LocationPoint) != null ? ((LocationPoint)inst.Location).Point : pt;
                Line axis = Line.CreateBound(curPt, new XYZ(curPt.X, curPt.Y, curPt.Z + 10.0));
                ElementTransformUtils.RotateElement(doc, inst.Id, axis, angleRad);
            }

            // 4. Gán kích thước chiều dài qua parameter
            if (lengthMm > 0)
            {
                Parameter p = inst.LookupParameter("Length") ?? inst.LookupParameter("d") ?? inst.LookupParameter("L");
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(lengthMm / 304.8);
                }
            }

            return inst;
        }

        private static Dictionary<string, Dictionary<string, double>> ReadAllFamilyParameters(Autodesk.Revit.ApplicationServices.Application revitApp, string childFolder)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();
            string[] familyFiles = new string[] { "1", "2", "3", "4", "5", "6", "7" };

            foreach (string fNum in familyFiles)
            {
                string rfaPath = System.IO.Path.Combine(childFolder, fNum + ".rfa");
                if (!System.IO.File.Exists(rfaPath)) continue;

                var famParams = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                Document famDoc = null;
                try
                {
                    famDoc = revitApp.OpenDocumentFile(rfaPath);
                    if (famDoc != null && famDoc.IsFamilyDocument)
                    {
                        FamilyManager fMgr = famDoc.FamilyManager;
                        FamilyType curType = fMgr.CurrentType;
                        foreach (FamilyParameter fp in fMgr.Parameters)
                        {
                            if (fp.StorageType == StorageType.Double && curType != null)
                            {
                                Double? val = curType.AsDouble(fp);
                                if (val.HasValue)
                                {
                                    famParams[fp.Definition.Name] = val.Value * 304.8;
                                }
                            }
                        }
                    }
                }
                catch { }
                finally
                {
                    if (famDoc != null)
                    {
                        try { famDoc.Close(false); } catch { }
                    }
                }
                result[fNum] = famParams;
            }
            return result;
        }

        private void HandleInspectChildFamilies(UIApplication app, McpTask task)
        {
            try
            {
                string childFolder = @"H:\My Drive\Family\New folder\Child";
                string[] familyFiles = new string[] { "1", "2", "3", "4", "5", "6", "7" };
                var report = new Dictionary<string, object>();

                foreach (string fNum in familyFiles)
                {
                    string rfaPath = System.IO.Path.Combine(childFolder, fNum + ".rfa");
                    if (!System.IO.File.Exists(rfaPath)) continue;

                    Document famDoc = null;
                    try
                    {
                        famDoc = app.Application.OpenDocumentFile(rfaPath);
                    }
                    catch { }

                    if (famDoc != null && famDoc.IsFamilyDocument)
                    {
                        var famData = new Dictionary<string, object>();
                        var pList = new List<Dictionary<string, object>>();

                        FamilyManager fMgr = famDoc.FamilyManager;
                        FamilyType curType = fMgr.CurrentType;

                        foreach (FamilyParameter fp in fMgr.Parameters)
                        {
                            var pInfo = new Dictionary<string, object>();
                            pInfo["name"] = fp.Definition.Name;
                            pInfo["isInstance"] = fp.IsInstance;
                            pInfo["formula"] = fp.Formula ?? "";

                            if (curType != null)
                            {
                                if (fp.StorageType == StorageType.Double)
                                {
                                    Double? dVal = curType.AsDouble(fp);
                                    pInfo["valueMm"] = dVal.HasValue ? Math.Round(dVal.Value * 304.8, 2) : 0;
                                }
                                else if (fp.StorageType == StorageType.Integer)
                                {
                                    pInfo["valueInt"] = curType.AsInteger(fp);
                                }
                                else if (fp.StorageType == StorageType.String)
                                {
                                    pInfo["valueString"] = curType.AsString(fp);
                                }
                            }
                            pList.Add(pInfo);
                        }

                        famData["family"] = fNum;
                        famData["currentTypeName"] = curType != null ? curType.Name : "Default";
                        famData["parameters"] = pList;
                        report[fNum] = famData;

                        famDoc.Close(false);
                    }
                }

                task.Complete(true, report);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi kiểm tra Family con: " + ex.ToString());
            }
        }

        private void HandleAuditScaffolding(UIApplication app, Document doc, McpTask task)
        {
            try
            {
                if (doc == null)
                {
                    task.Complete(false, null, "Không có tài liệu nào đang mở để kiểm tra.");
                    return;
                }

                var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance));
                var items = new List<Dictionary<string, object>>();

                double ftToMm = 304.8;
                foreach (FamilyInstance inst in collector)
                {
                    LocationPoint loc = inst.Location as LocationPoint;
                    XYZ pt = loc != null ? loc.Point : XYZ.Zero;
                    double rot = loc != null ? loc.Rotation : 0.0;
                    string famName = inst.Symbol != null && inst.Symbol.Family != null ? inst.Symbol.Family.Name : "Unknown";
                    string typeName = inst.Symbol != null ? inst.Symbol.Name : "Default";

                    Parameter pLen = inst.LookupParameter("Length") ?? inst.LookupParameter("d") ?? inst.LookupParameter("L");
                    double lenMm = pLen != null && pLen.StorageType == StorageType.Double ? pLen.AsDouble() * ftToMm : 0.0;

                    Parameter pOffset = inst.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)
                                     ?? inst.LookupParameter("Elevation from Level")
                                     ?? inst.LookupParameter("Offset");
                    double offsetMm = pOffset != null && pOffset.StorageType == StorageType.Double ? pOffset.AsDouble() * ftToMm : 0.0;

                    var item = new Dictionary<string, object>();
                    item["id"] = inst.Id.IntegerValue;
                    item["family"] = famName;
                    item["type"] = typeName;
                    item["x"] = Math.Round(pt.X * ftToMm, 1);
                    item["y"] = Math.Round(pt.Y * ftToMm, 1);
                    item["z"] = Math.Round(pt.Z * ftToMm, 1);
                    item["elevationOffsetMm"] = Math.Round(offsetMm, 1);
                    item["rotDeg"] = Math.Round(rot * 180.0 / Math.PI, 1);
                    item["lengthMm"] = Math.Round(lenMm, 1);

                    items.Add(item);
                }

                Dictionary<string, object> res = new Dictionary<string, object>();
                res["status"] = "success";
                res["docTitle"] = doc.Title;
                res["totalInstances"] = items.Count;
                res["instances"] = items;

                task.Complete(true, res);
            }
            catch (Exception ex)
            {
                task.Complete(false, null, "Lỗi audit geometry: " + ex.ToString());
            }
        }
    }
}
