const http = require('http');
const fs = require('fs');
const path = require('path');
const https = require('https');
const WebSocket = require('ws');

function proxyRequest(targetUrl, res, headers) {
  const url = new URL(targetUrl);
  const options = {
    hostname: url.hostname,
    path: url.pathname + url.search,
    method: 'GET',
    headers: headers || {}
  };
  const proto = url.protocol === 'https:' ? https : http;
  const req2 = proto.request(options, (res2) => {
    res.writeHead(res2.statusCode, res2.headers);
    res2.pipe(res);
  });
  req2.on('error', (err) => {
    console.error('Proxy error:', err.message);
    res.writeHead(502);
    res.end('Proxy error');
  });
  req2.end();
}

const server = http.createServer((req, res) => {
  // CORS headers for all responses
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.url === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(fs.readFileSync(path.join(__dirname, 'index.html')));
  } else if (req.url.startsWith('/api/yadisk')) {
    // Proxy for Yandex.Disk API
    const urlObj = new URL(req.url, `http://${req.headers.host}`);
    const publicKey = urlObj.searchParams.get('public_key');
    if (!publicKey) {
      res.writeHead(400);
      res.end(JSON.stringify({ error: 'Missing public_key' }));
      return;
    }
    const apiUrl = `https://cloud-api.yandex.net/v1/disk/public/resources/download?public_key=${encodeURIComponent(publicKey)}`;
    https.get(apiUrl, (apiRes) => {
      let data = '';
      apiRes.on('data', chunk => data += chunk);
      apiRes.on('end', () => {
        res.writeHead(apiRes.statusCode, { 'Content-Type': 'application/json' });
        res.end(data);
      });
    }).on('error', (err) => {
      console.error('Yandex API error:', err.message);
      res.writeHead(502);
      res.end(JSON.stringify({ error: err.message }));
    });
  } else if (req.url.startsWith('/proxy')) {
    // Generic proxy for video files (bypass CORS)
    const urlObj = new URL(req.url, `http://${req.headers.host}`);
    const targetUrl = urlObj.searchParams.get('url');
    if (!targetUrl) {
      res.writeHead(400);
      res.end('Missing url parameter');
      return;
    }
    proxyRequest(targetUrl, res);
  } else if (req.url.startsWith('/movies/')) {
    // Stream video files with range support
    const filePath = path.join(__dirname, decodeURIComponent(req.url));
    fs.stat(filePath, (err, stats) => {
      if (err || !stats.isFile()) {
        res.writeHead(404);
        res.end('Not found');
        return;
      }
      const ext = path.extname(filePath).toLowerCase();
      const contentTypes = {
        '.mp4': 'video/mp4',
        '.webm': 'video/webm',
        '.ogg': 'video/ogg',
        '.m3u8': 'application/vnd.apple.mpegurl',
        '.ts': 'video/mp2t'
      };
      const contentType = contentTypes[ext] || 'application/octet-stream';
      const range = req.headers.range;
      if (range) {
        const parts = range.replace(/bytes=/, '').split('-');
        const start = parseInt(parts[0], 10);
        const end = parts[1] ? parseInt(parts[1], 10) : stats.size - 1;
        const chunksize = end - start + 1;
        res.writeHead(206, {
          'Content-Range': `bytes ${start}-${end}/${stats.size}`,
          'Accept-Ranges': 'bytes',
          'Content-Length': chunksize,
          'Content-Type': contentType
        });
        fs.createReadStream(filePath, { start, end }).pipe(res);
      } else {
        res.writeHead(200, {
          'Content-Length': stats.size,
          'Content-Type': contentType
        });
        fs.createReadStream(filePath).pipe(res);
      }
    });
  } else if (req.url.startsWith('/image/') || req.url.startsWith('/milanaImage/')) {
    const filePath = path.join(__dirname, decodeURIComponent(req.url));
    const ext = path.extname(filePath);
    const contentTypes = {
      '.png': 'image/png',
      '.jpg': 'image/jpeg',
      '.jpeg': 'image/jpeg',
      '.gif': 'image/gif',
      '.svg': 'image/svg+xml',
      '.webp': 'image/webp'
    };
    const contentType = contentTypes[ext] || 'application/octet-stream';
    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404);
        res.end('Not found');
      } else {
        res.writeHead(200, { 'Content-Type': contentType });
        res.end(data);
      }
    });
  } else {
    res.writeHead(404);
    res.end();
  }
});

const wss = new WebSocket.Server({ server });
const rooms = new Map(); // roomId -> Map<ws, { name, mode }>

function broadcastUsers(room) {
  if (!rooms.has(room)) return;
  const peers = rooms.get(room);
  const users = Array.from(peers.values()).map(v => v.name);
  peers.forEach((data, peer) => {
    if (peer.readyState === WebSocket.OPEN) {
      peer.send(JSON.stringify({ type: 'room-users', room, users }));
    }
  });
}

function getOtherPeer(room, ws) {
  const peers = rooms.get(room);
  if (!peers) return null;
  for (const [peer, data] of peers) {
    if (peer !== ws && peer.readyState === WebSocket.OPEN) return peer;
  }
  return null;
}

wss.on('connection', (ws) => {
  let currentRoom = null;

  ws.on('message', (raw) => {
    let msg;
    try { msg = JSON.parse(raw); } catch { return; }

    if (msg.type === 'join') {
      currentRoom = msg.room;
      const userName = msg.name || 'Гость';
      const userMode = msg.mode || 'call';
      if (!rooms.has(currentRoom)) rooms.set(currentRoom, new Map());
      const peers = rooms.get(currentRoom);

      // Лимит 2 участника — предотвращаем хаос сигналинга
      if (peers.size >= 2) {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify({ type: 'error', message: 'Комната заполнена (макс. 2 участника)' }));
        }
        ws.close();
        return;
      }

      console.log(`[join] ${userName} -> ${currentRoom} mode=${userMode} (было участников: ${peers.size})`);

      // Если в комнате уже есть участник — запрашиваем у него состояние видео
      const otherPeer = getOtherPeer(currentRoom, ws);
      if (otherPeer && otherPeer.readyState === WebSocket.OPEN) {
        otherPeer.send(JSON.stringify({ type: 'video:sync-request', room: currentRoom }));
      }

      peers.forEach((data, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify({ type: 'peer-joined', room: currentRoom, name: userName }));
        }
      });

      peers.set(ws, { name: userName, mode: userMode });
      broadcastUsers(currentRoom);
    } else if (['offer', 'answer', 'ice-candidate'].includes(msg.type)) {
      const peers = rooms.get(msg.room);
      if (!peers) return;
      console.log(`[signal] ${msg.type} in room ${msg.room} from ${peers.get(ws)?.name || '?'}`);
      peers.forEach((data, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify({ type: msg.type, room: msg.room, payload: msg.payload }));
        }
      });
    } else if (msg.type.startsWith('video:')) {
      // Ретранслируем все video:* сообщения другому участнику
      const peers = rooms.get(msg.room);
      if (!peers) return;
      peers.forEach((data, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify(msg));
        }
      });
    } else if (msg.type === 'chat') {
      const peers = rooms.get(msg.room);
      if (!peers) return;
      peers.forEach((data, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify(msg));
        }
      });
    } else if (msg.type === 'camera-state') {
      const peers = rooms.get(msg.room);
      if (!peers) return;
      peers.forEach((data, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify(msg));
        }
      });
    }
  });

  ws.on('close', () => {
    if (currentRoom && rooms.has(currentRoom)) {
      const peers = rooms.get(currentRoom);
      peers.delete(ws);
      console.log(`[leave] ${currentRoom} (осталось: ${peers.size})`);
      peers.forEach((data, peer) => {
        if (peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify({ type: 'peer-left', room: currentRoom }));
        }
      });
      broadcastUsers(currentRoom);
      if (peers.size === 0) rooms.delete(currentRoom);
    }
  });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log('Server running on port ' + PORT));
