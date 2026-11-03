const fs = require('fs');
const d = JSON.parse(fs.readFileSync('all_4_instances_params.json', 'utf8'));
for (let id in d) {
  const inst = d[id];
  console.log(`\n=== INSTANCE ${id} ===`);
  const instParams = inst.parameters.instance;
  for (let p of instParams) {
    if (['W', 'Length', 'Comments', 'Comments', 'Mark'].includes(p.name)) {
      console.log(`  ${p.name}: ${p.displayValue || p.value}`);
    }
    if (p.name.startsWith('Group_') && p.value === 1) {
      console.log(`  ${p.name}: ${p.value}`);
      const n = instParams.find(x => x.name === `${p.name}_N`);
      const l = instParams.find(x => x.name === `${p.name}_L`);
      console.log(`    -> N: ${n ? n.value : 'N/A'}, L: ${l ? l.displayValue : 'N/A'}`);
    }
  }
}
