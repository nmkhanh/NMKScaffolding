const http = require('http');

function post(method, args) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      jsonrpc: "2.0",
      id: Date.now(),
      method: "tools/call",
      params: {
        name: method,
        arguments: args
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
        try {
          const parsed = JSON.parse(body);
          // Revit MCP JSON-RPC returns result
          resolve(parsed.result || parsed);
        } catch (e) {
          resolve(body);
        }
      });
    });

    req.on("error", reject);
    req.write(data);
    req.end();
  });
}

async function main() {
  const ids = ["87965", "88144", "88301", "88492", "88647", "88798", "88910"];
  for (const id of ids) {
    const pRes = await post("get_element_parameters", { ElementId: id });
    const lRes = await post("get_element_location", { ElementId: id });
    
    // Check structure
    const params = (pRes.parameters && pRes.parameters.instance) ? pRes.parameters.instance : [];
    const loc = (lRes.location && lRes.location.curve) ? lRes.location.curve : null;

    const findP = (name) => {
      const p = params.find(x => x.name === name);
      return p ? p.displayValue : "";
    };

    const len = findP("Length");
    const w = findP("W");
    const bays = [];
    for (let i = 1; i <= 10; i++) {
      const g = params.find(x => x.name === `Group_${i}`);
      if (g && g.value === 1) {
        const n = params.find(x => x.name === `Group_${i}_N`)?.value || 1;
        const l = params.find(x => x.name === `Group_${i}_L`)?.displayValue || "";
        bays.push(`${n}x${l}`);
      }
    }

    const s = loc?.start ? `(${Math.round(loc.start.x * 304.8)}, ${Math.round(loc.start.y * 304.8)})` : "";
    const e = loc?.end ? `(${Math.round(loc.end.x * 304.8)}, ${Math.round(loc.end.y * 304.8)})` : "";

    console.log(`ID ${id}: Start ${s} -> End ${e} | Length: ${len} | W: ${w} | Bays: ${bays.join(", ")}`);
  }
}

main().catch(console.error);
