const fs = require('fs');
const readline = require('readline');
const rl = readline.createInterface({
  input: fs.createReadStream('C:/Users/Admin/.gemini/antigravity/brain/5365d727-08e0-4a5f-a070-96ffc1ffee96/.system_generated/logs/transcript.jsonl'),
  crlfDelay: Infinity
});

const targets = [6449, 6946, 6990, 7008, 7117, 7247, 7255];
rl.on('line', line => {
  for (const t of targets) {
    if (line.includes(`"step_index":${t},`)) {
      const obj = JSON.parse(line);
      console.log('=== STEP ' + t + ' ===');
      console.log(obj.content);
    }
  }
});
