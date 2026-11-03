const http = require('http');

function sendTask(action, params) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({ action, ...params });
    const req = http.request({
      hostname: '127.0.0.1',
      port: 4848,
      path: '/execute',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      }
    }, res => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => resolve(JSON.parse(body)));
    });
    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function main() {
  const ping = await sendTask('ping');
  console.log('Ping:', ping);
}
main();
