using System;
using System.IO;
using System.Net;
using System.Text;

class Program
{
    static string baseUrl = "http://127.0.0.1:4848";

    static int Place(string fam, double x, double y, double z, double rot = 0, double len = 0, double w = 0, double h = 0)
    {
        string json = string.Format("{{\"familyName\":\"{0}\",\"x\":{1},\"y\":{2},\"z\":{3},\"rotDeg\":{4},\"lengthMm\":{5}}}", fam, x, y, z, rot, len);
        int id = Post(baseUrl + "/place_instance", json);

        if (len > 0) SetParam(id, "l", len);
        if (w > 0) SetParam(id, "w", w);
        if (h > 0) SetParam(id, "h", h);
        return id;
    }

    static void SetParam(int id, string paramName, double val)
    {
        string json = string.Format("{{\"elementId\":{0},\"paramName\":\"{1}\",\"value\":{2}}}", id, paramName, val);
        Post(baseUrl + "/set_parameter", json);
    }

    static int Post(string url, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "POST";
        req.ContentType = "application/json";
        req.KeepAlive = true;
        req.Timeout = 5000;
        req.ContentLength = bytes.Length;
        using (var stream = req.GetRequestStream())
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        using (var resp = req.GetResponse())
        using (var reader = new StreamReader(resp.GetResponseStream()))
        {
            string txt = reader.ReadToEnd();
            // Parse instanceId
            int idx = txt.IndexOf("\"instanceId\":");
            if (idx >= 0)
            {
                int end = txt.IndexOfAny(new char[] { ',', '}' }, idx + 13);
                if (end > idx)
                {
                    int id;
                    if (int.TryParse(txt.Substring(idx + 13, end - idx - 13).Trim(), out id)) return id;
                }
            }
        }
        return 0;
    }

    static void Main()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("=== BẮT ĐẦU DỰNG TOÀN BỘ CÁC BƯỚC (BƯỚC 2 -> 11) CỦA 1 NHỊP ===");
        int count = 0;

        // BƯỚC 2: 4 Kích chân (Family 2, h = 250 mm)
        int j1 = Place("2", 0, 0, 0, 0, 0, 0, 250);
        int j2 = Place("2", 1800, 0, 0, 0, 0, 0, 250);
        int j3 = Place("2", 0, -1200, 0, 0, 0, 0, 250);
        int j4 = Place("2", 1800, -1200, 0, 0, 0, 0, 250);
        count += 4;
        Console.WriteLine("-> Bước 2 hoàn thành: 4 Kích chân (ID: {0}, {1}, {2}, {3})", j1, j2, j3, j4);

        // BƯỚC 3: 4 Cột chống 支柱2700 ALINCO (Family 3 tại Z = 250 mm)
        double[][] colCoords = new double[][] {
            new double[] { 0, 0 }, new double[] { 1800, 0 },
            new double[] { 0, -1200 }, new double[] { 1800, -1200 }
        };
        foreach (var pt in colCoords)
        {
            int cId = Place("3", pt[0], pt[1], 250, 0);
            SetParam(cId, "h", 2700);
            SetParam(cId, "h1", 135);
            SetParam(cId, "h2", 380);
            SetParam(cId, "h3", 830);
            SetParam(cId, "h4", 1280);
            SetParam(cId, "h5", 1730);
            SetParam(cId, "h6", 2180);
            SetParam(cId, "h7", 2630);
            count++;
        }
        Console.WriteLine("-> Bước 3 hoàn thành: 4 Cột chống ALINCO 2700");

        // BƯỚC 4: 4 Giằng chân Family 4 tại h1 (Z = 385 mm)
        Place("4", 0, 0, 385, 0, 1800);       // Dọc trước
        Place("4", 0, -1200, 385, 0, 1800);   // Dọc sau
        Place("4", 0, 0, 385, 270, 1200);     // Ngang trái
        Place("4", 1800, 0, 385, 270, 1200);  // Ngang phải
        count += 4;
        Console.WriteLine("-> Bước 4 hoàn thành: 4 Giằng chân (Z = 385 mm)");

        // BƯỚC 5: Giằng dọc trước Family 4 tại h3 (Z = 1080 mm)
        Place("4", 0, 0, 1080, 0, 1800);
        count += 1;
        Console.WriteLine("-> Bước 5 hoàn thành: 1 Giằng dọc trước h3 (Z = 1080 mm)");

        // BƯỚC 6: Giằng chéo Family 5 tại h2 (Z = 630 mm, mặt sau Y = -1200)
        int br1 = Place("5", 0, -1200, 630, 0, 1800);
        SetParam(br1, "h", 900);
        count += 1;
        Console.WriteLine("-> Bước 6 hoàn thành: 1 Giằng chéo h2 (Z = 630 mm)");

        // BƯỚC 7: 2 Thanh giằng ngang đỡ mâm Family 4 tại h5 (Z = 1980 mm)
        Place("4", 0, 0, 1980, 270, 1200);
        Place("4", 1800, 0, 1980, 270, 1200);
        count += 2;
        Console.WriteLine("-> Bước 7 hoàn thành: 2 Giằng ngang đỡ mâm h5 (Z = 1980 mm)");

        // BƯỚC 8: Giằng chéo Family 5 tại h5 (Z = 1980 mm, mặt sau Y = -1200)
        int br2 = Place("5", 0, -1200, 1980, 0, 1800);
        SetParam(br2, "h", 900);
        count += 1;
        Console.WriteLine("-> Bước 8 hoàn thành: 1 Giằng chéo h5 (Z = 1980 mm)");

        // BƯỚC 9: 2 Lan can an toàn Family 4 tại h6 và h7 (Z = 2430 & 2880 mm)
        Place("4", 0, 0, 2430, 0, 1800);
        Place("4", 0, 0, 2880, 0, 1800);
        count += 2;
        Console.WriteLine("-> Bước 9 hoàn thành: 2 Lan can an toàn h6, h7");

        // BƯỚC 10: 2 Mâm thao tác Family 6 tại h5 (Z = 1980 mm)
        Place("6", 0, -345, 1980, 0, 1800, 500);
        Place("6", 0, -855, 1980, 0, 1800, 500);
        count += 2;
        Console.WriteLine("-> Bước 10 hoàn thành: 2 Mâm thao tác (W = 500 mm)");

        // BƯỚC 11: 2 Ván chắn biên Family 7 tại h5 (Z = 1980 mm)
        Place("7", 0, -66, 1980, 0, 1800);
        Place("7", 0, -1134, 1980, 0, 1800);
        count += 2;
        Console.WriteLine("-> Bước 11 hoàn thành: 2 Ván chắn biên");

        sw.Stop();
        Console.WriteLine("============================================================");
        Console.WriteLine("HOÀN TẤT DỰNG TOÀN BỘ BƯỚC 1 ĐẾN 11 TRONG {0} ms ({1:F2} s)!", sw.ElapsedMilliseconds, sw.Elapsed.TotalSeconds);
        Console.WriteLine("Tổng cấu kiện mới đã tạo: {0}", count);
        Console.WriteLine("============================================================");
    }
}
