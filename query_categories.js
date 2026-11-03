const http = require('http');

function callTool(name, args) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      jsonrpc: "2.0",
      id: Date.now(),
      method: "tools/call",
      params: {
        name: name,
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
          resolve(JSON.parse(body));
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
  const cats = ["SpecialityEquipment", "GenericModel", "DetailComponents", "StructuralFraming", "Columns", "Furniture", "MechanicalEquipment", "Site", "Entourage", "PlumbingFixtures"];
  for (const c of cats) {
    const res = await callTool("get_elements", { category: c });
    const count = res.result ? (res.result.returnedCount || (res.result.elements && res.result.elements.length)) : 0;
    if (count > 0) {
      console.log(`Category ${c}: ${count} elements`);
      if (res.result.elements && res.result.elements.length > 0) {
        console.log(`  Sample:`, res.result.elements.slice(0, 3).map(e => ({ id: e.id, name: e.name })));
      }
    }
  }
}

main().catch(console.error);
