const fs = require('fs');
const d = JSON.parse(fs.readFileSync('all_4_instances_params.json', 'utf8'));
const inst = d['88647'];
if (inst) {
  for (let p of inst.parameters.instance) {
    if (['W', 'Length', 'Comments', 'Mark'].includes(p.name)) {
      console.log(`  ${p.name}: ${p.displayValue || p.value}`);
    }
    if (p.name.startsWith('Group_') && p.value === 1) {
      const n = inst.parameters.instance.find(x => x.name === `${p.name}_N`);
      const l = inst.parameters.instance.find(x => x.name === `${p.name}_L`);
      console.log(`  ${p.name}: N=${n ? n.value : 'N/A'}, L=${l ? l.displayValue : 'N/A'}`);
    }
  }
} else {
  console.log('88647 not found in all_4_instances_params.json');
}
