using System;
using System.IO;
namespace TestPayload {
    public class Runner {
        public static int Run(string arg) {
            File.WriteAllText(@"c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\shellcode_test.txt", "SUCCESS: " + arg);
            return 123;
        }
    }
}
