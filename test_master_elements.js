// Count elements in ExecuteMaster6CornersContinuous
const fs = require('fs');
const content = fs.readFileSync('RevitScaffoldingTool/ScaffoldingBuilder.Master.cs', 'utf8');

// Count PlaceInstance calls
const matches = content.match(/PlaceInstance\(/g);
console.log("Total PlaceInstance calls in Master.cs:", matches ? matches.length : 0);

// Count EnsureCol calls
const colMatches = content.match(/EnsureCol\(/g);
console.log("Total EnsureCol calls in Master.cs:", colMatches ? colMatches.length : 0);

// Count PlaceSharedTransom calls
const transomMatches = content.match(/PlaceSharedTransom\(/g);
console.log("Total PlaceSharedTransom calls in Master.cs:", transomMatches ? transomMatches.length : 0);
