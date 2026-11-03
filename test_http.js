const http = require('http');

const data = JSON.stringify({
  jsonrpc: "2.0",
  id: 1,
  method: "tools/call",
  params: {
    name: "get_element_parameters",
    arguments: { ElementId: "87965" }
  }
});

const req = http.request({
  hostname: "localhost",
  port: 5000,
  path: "/",
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Content-Length": Buffer.byteLength(data)
  }
}, res => {
  let body = "";
  res.on("data", chunk => body += chunk);
  res.on("end", () => {
    console.log("Raw body:", body.substring(0, 300));
  });
});
req.write(data);
req.end();
