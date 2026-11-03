const fs = require('fs');

// Verify compare_case11.js to see what it does
console.log("Reading compare_case11.js...");
const comp = fs.readFileSync('compare_case11.js', 'utf8');
console.log(comp.substring(0, 300));
