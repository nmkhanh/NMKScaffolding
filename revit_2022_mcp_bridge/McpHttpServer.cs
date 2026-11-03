using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Autodesk.Revit.UI;

namespace RevitFamilyMcpBridge
{
    public class McpHttpServer
    {
        private readonly HttpListener _listener;
        private readonly McpExternalEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private readonly int _port;
        private Thread _listenThread;
        private volatile bool _isRunning;

        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public McpHttpServer(McpExternalEventHandler handler, ExternalEvent extEvent, int port = 4848)
        {
            _handler = handler;
            _extEvent = extEvent;
            _port = port;

            _listener = new HttpListener();
            _listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", _port));
            _listener.Prefixes.Add(string.Format("http://localhost:{0}/", _port));
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;
                _listenThread = new Thread(ListenLoop);
                _listenThread.IsBackground = true;
                _listenThread.Start();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                throw new InvalidOperationException("Không thể khởi động HttpListener trên cổng " + _port + ": " + ex.Message, ex);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
            }
            catch { }
        }

        private void ListenLoop()
        {
            while (_isRunning && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                    break;
                }
                catch
                {
                    // Catch transient errors
                }
            }
        }

        private void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            try
            {
                string rawUrl = request.RawUrl ?? "/";
                string path = rawUrl.Split('?')[0].Trim().ToLowerInvariant();

                // Quick endpoints
                if (path == "/ping" || path == "/status")
                {
                    Dictionary<string, object> quickStatus = new Dictionary<string, object>();
                    quickStatus["status"] = "online";
                    quickStatus["service"] = "Revit 2022 MCP Bridge";
                    quickStatus["port"] = _port;
                    quickStatus["timestamp"] = DateTime.UtcNow.ToString("o");
                    SendJson(response, 200, quickStatus);
                    return;
                }

                // Parse POST body
                string body = "";
                if (request.HasEntityBody)
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }
                }

                Dictionary<string, object> reqData = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        reqData = _serializer.Deserialize<Dictionary<string, object>>(body) ?? new Dictionary<string, object>();
                    }
                    catch { }
                }

                string action = "";
                if (path == "/build_family" || path == "/create_family") action = "build_family";
                else if (path == "/create_ref_planes") action = "create_ref_planes";
                else if (path == "/create_parameters") action = "create_parameters";
                else if (path == "/flex_test") action = "flex_test";
                else if (reqData.ContainsKey("action")) action = reqData["action"].ToString();
                else action = path.TrimStart('/');

                // Dispatch to Revit UI Thread via ExternalEvent
                McpTask task = new McpTask(action, reqData);
                _handler.EnqueueTask(task);
                _extEvent.Raise();

                // Wait for Revit UI thread to execute
                bool finished = task.Wait(60000);

                if (!finished)
                {
                    Dictionary<string, object> timeoutRes = new Dictionary<string, object>();
                    timeoutRes["success"] = false;
                    timeoutRes["error"] = "Revit API UI thread timeout (Revit có thể đang bận hoặc đang hiển thị modal dialog).";
                    SendJson(response, 504, timeoutRes);
                    return;
                }

                Dictionary<string, object> finalRes = new Dictionary<string, object>();
                finalRes["success"] = task.Success;
                finalRes["data"] = task.Result;
                if (!task.Success)
                {
                    finalRes["error"] = task.ErrorMessage;
                }

                SendJson(response, task.Success ? 200 : 400, finalRes);
            }
            catch (Exception ex)
            {
                Dictionary<string, object> errRes = new Dictionary<string, object>();
                errRes["success"] = false;
                errRes["error"] = ex.Message;
                SendJson(response, 500, errRes);
            }
        }

        private void SendJson(HttpListenerResponse response, int statusCode, object data)
        {
            try
            {
                string json = _serializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = statusCode;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch { }
            finally
            {
                try { response.Close(); } catch { }
            }
        }
    }
}
