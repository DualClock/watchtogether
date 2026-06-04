const http = require('http');
const fs = require('fs');
const path = require('path');
const WebSocket = require('ws');

const server = http.createServer((req, res) => {
  if (req.url === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(fs.readFileSync(path.join(__dirname, 'index.html')));
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
const rooms = new Map(); // roomId -> Map<ws, name>

function broadcastUsers(room) {
  if (!rooms.has(room)) return;
  const peers = rooms.get(room);
  const users = Array.from(peers.values());
  peers.forEach((name, peer) => {
    if (peer.readyState === WebSocket.OPEN) {
      peer.send(JSON.stringify({ type: 'room-users', room, users }));
    }
  });
}

wss.on('connection', (ws) => {
  let currentRoom = null;

  ws.on('message', (raw) => {
    let msg;
    try { msg = JSON.parse(raw); } catch { return; }

    if (msg.type === 'join') {
      currentRoom = msg.room;
      const userName = msg.name || 'Гость';
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

      console.log(`[join] ${userName} -> ${currentRoom} (было участников: ${peers.size})`);

      peers.forEach((name, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify({ type: 'peer-joined', room: currentRoom, name: userName }));
        }
      });

      peers.set(ws, userName);
      broadcastUsers(currentRoom);
    } else if (['offer', 'answer', 'ice-candidate'].includes(msg.type)) {
      const peers = rooms.get(msg.room);
      if (!peers) return;
      console.log(`[signal] ${msg.type} in room ${msg.room} from ${peers.get(ws) || '?'}`);
      peers.forEach((name, peer) => {
        if (peer !== ws && peer.readyState === WebSocket.OPEN) {
          peer.send(JSON.stringify({ type: msg.type, room: msg.room, payload: msg.payload }));
        }
      });
    }
  });

  ws.on('close', () => {
    if (currentRoom && rooms.has(currentRoom)) {
      const peers = rooms.get(currentRoom);
      peers.delete(ws);
      console.log(`[leave] ${currentRoom} (осталось: ${peers.size})`);
      peers.forEach((name, peer) => {
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
