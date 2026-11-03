using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace RevitFamilyMcpBridge
{
    public class McpTask
    {
        public string Action { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public Dictionary<string, object> Result { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        private readonly ManualResetEvent _doneEvent = new ManualResetEvent(false);

        public McpTask(string action, Dictionary<string, object> parameters)
        {
            Action = action;
            Parameters = parameters ?? new Dictionary<string, object>();
            Result = new Dictionary<string, object>();
            Success = false;
        }

        public void Complete(bool success, Dictionary<string, object> result, string errorMessage = null)
        {
            Success = success;
            Result = result ?? new Dictionary<string, object>();
            ErrorMessage = errorMessage;
            _doneEvent.Set();
        }

        public bool Wait(int timeoutMs = 30000)
        {
            return _doneEvent.WaitOne(timeoutMs);
        }
    }
}
