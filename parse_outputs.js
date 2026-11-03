const fs = require('fs');

function inspectJson(path) {
  const raw = fs.readFileSync(path, 'utf8');
  const data = JSON.parse(raw);
  console.log("=== Element:", data.elementId, "===");
  const inst = data.parameters.instance;
  for (const p of inst) {
    if (p.name.startsWith("Group") || p.name === "Length" || p.name === "W") {
      if (p.value === 1 || (p.value !== null && p.value !== 0 && !p.name.endsWith("_N") && !p.name.endsWith("_L"))) {
        console.log(`  ${p.name}: ${p.displayValue} (val: ${p.value})`);
      }
      if (p.name.endsWith("_N") || p.name.endsWith("_L")) {
        // print if its group is active
      }
    }
  }
  for (let i = 1; i <= 10; i++) {
    const g = inst.find(p => p.name === `Group_${i}`);
    if (g && g.value === 1) {
      const n = inst.find(p => p.name === `Group_${i}_N`)?.value;
      const l = inst.find(p => p.name === `Group_${i}_L`)?.displayValue;
      console.log(`  -> Group_${i}: ${n} x ${l}`);
    }
  }
}

inspectJson('C:/Users/Admin/.gemini/antigravity/brain/5365d727-08e0-4a5f-a070-96ffc1ffee96/.system_generated/steps/7500/output.txt');
inspectJson('C:/Users/Admin/.gemini/antigravity/brain/5365d727-08e0-4a5f-a070-96ffc1ffee96/.system_generated/steps/7520/output.txt');
