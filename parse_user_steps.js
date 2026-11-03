const fs = require('fs');
const readline = require('readline');

const stream = fs.createReadStream('C:/Users/Admin/.gemini/antigravity/brain/5365d727-08e0-4a5f-a070-96ffc1ffee96/.system_generated/logs/transcript.jsonl');
const rl = readline.createInterface({ input: stream });

rl.on('line', (line) => {
  if (line.includes('"source":"USER_EXPLICIT"')) {
    try {
      const j = JSON.parse(line);
      console.log(`[Step ${j.step_index}] ${j.content}`);
    } catch (e) {}
  }
});
