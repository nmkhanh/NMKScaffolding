const fs = require('fs');
const raw = fs.readFileSync('all_4_instances_params.json', 'utf8');
const data = JSON.parse(raw);
const inst = data['87965'].parameters.instance;
console.log("All parameters of 87965:");
inst.forEach(p => console.log(p.name, "=", p.displayValue, `(${p.value})`));
