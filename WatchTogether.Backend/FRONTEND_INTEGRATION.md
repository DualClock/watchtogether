# Интеграция фронтенда с бекендом

## Базовый URL

```
Локально: http://localhost:5000
API: http://localhost:5000/api
SignalR: ws://localhost:5000/hubs
```

## Аутентификация

Все запросы (кроме регистрации/входа) требуют заголовок:
```
Authorization: Bearer <access_token>
```

### Регистрация
```javascript
const response = await fetch('http://localhost:5000/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'john_doe',
    email: 'john@example.com',
    password: 'MyP@ssw0rd!',
    confirmPassword: 'MyP@ssw0rd!'
  })
});

const data = await response.json();
// { accessToken, refreshToken, expiresAt, user: { id, username, email, displayName, avatarUrl, status } }
localStorage.setItem('accessToken', data.accessToken);
localStorage.setItem('refreshToken', data.refreshToken);
```

### Вход
```javascript
const response = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'john@example.com',
    password: 'MyP@ssw0rd!'
  })
});
```

### Обновление токена
```javascript
const response = await fetch('http://localhost:5000/api/auth/refresh', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    refreshToken: localStorage.getItem('refreshToken')
  })
});
```

## Работа с комнатами

### Получить список комнат
```javascript
const response = await fetch('http://localhost:5000/api/rooms', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const rooms = await response.json();
```

### Создать комнату
```javascript
const response = await fetch('http://localhost:5000/api/rooms', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    name: 'Моя комната',
    description: 'Смотрим фильмы вместе',
    type: 'public', // public, bylink, private
    maxUsers: 10
  })
});
```

### Войти в комнату
```javascript
const response = await fetch(`http://localhost:5000/api/rooms/${roomId}/join`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ password: 'optional' })
});
```

## SignalR — Чат

```javascript
// Подключение
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/chat', {
    accessTokenFactory: () => localStorage.getItem('accessToken')
  })
  .withAutomaticReconnect()
  .build();

// Обработчики событий
connection.on('NewMessage', (message) => {
  console.log('New message:', message);
  // { id, roomId, userId, username, avatarUrl, content, parentMessageId, createdAt, reactions }
});

connection.on('UserJoined', (data) => {
  console.log('User joined:', data);
});

connection.on('UserLeft', (data) => {
  console.log('User left:', data);
});

connection.on('UserTyping', (data) => {
  console.log('User typing:', data.username);
});

connection.on('ReactionAdded', (data) => {
  console.log('Reaction added:', data);
});

// Запуск
await connection.start();

// Войти в комнату
await connection.invoke('JoinRoom', roomId);

// Отправить сообщение
await connection.invoke('SendMessage', roomId, 'Привет всем!', null);

// Ответить на сообщение
await connection.invoke('SendMessage', roomId, 'Ответ', parentMessageId);

// Добавить реакцию
await connection.invoke('AddReaction', messageId, '👍');

// Показать что печатаем
await connection.invoke('Typing', roomId);

// Покинуть комнату
await connection.invoke('LeaveRoom', roomId);
```

## SignalR — Видео синхронизация

```javascript
const videoConnection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/video', {
    accessTokenFactory: () => localStorage.getItem('accessToken')
  })
  .withAutomaticReconnect()
  .build();

videoConnection.on('VideoPlayed', (data) => {
  console.log('Play at:', data.currentTime);
  videoPlayer.currentTime = data.currentTime;
  videoPlayer.play();
});

videoConnection.on('VideoPaused', (data) => {
  console.log('Pause at:', data.currentTime);
  videoPlayer.currentTime = data.currentTime;
  videoPlayer.pause();
});

videoConnection.on('VideoSeeked', (data) => {
  videoPlayer.currentTime = data.currentTime;
});

videoConnection.on('VideoChanged', (data) => {
  console.log('New video:', data.videoId);
  loadVideo(data.videoId);
});

videoConnection.on('SyncState', (state) => {
  console.log('Current sync state:', state);
  // { currentVideoId, isPlaying, currentTime, lastSyncAt }
});

await videoConnection.start();
await videoConnection.invoke('JoinVideoRoom', roomId);

// Управление воспроизведением
await videoConnection.invoke('PlayVideo', roomId, videoPlayer.currentTime);
await videoConnection.invoke('PauseVideo', roomId, videoPlayer.currentTime);
await videoConnection.invoke('SeekVideo', roomId, videoPlayer.currentTime);
await videoConnection.invoke('ChangeVideo', roomId, videoId);

// Запросить текущее состояние (при входе)
await videoConnection.invoke('RequestSync', roomId);
```

## История сообщений

```javascript
const response = await fetch(`http://localhost:5000/api/rooms/${roomId}/messages?page=1&pageSize=50`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
const messages = await response.json();
```

## Профиль пользователя

```javascript
// Получить свой профиль
const response = await fetch('http://localhost:5000/api/users/me', {
  headers: { 'Authorization': `Bearer ${token}` }
});

// Обновить профиль
const response = await fetch('http://localhost:5000/api/users/me', {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    displayName: 'Новое имя',
    bio: 'О себе...'
  })
});

// Загрузить аватар
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch('http://localhost:5000/api/users/me/avatar', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: formData
});
```

## Полный пример интеграции

```javascript
class WatchTogetherAPI {
  constructor(baseUrl = 'http://localhost:5000') {
    this.baseUrl = baseUrl;
    this.token = localStorage.getItem('accessToken');
  }

  setToken(token) {
    this.token = token;
    localStorage.setItem('accessToken', token);
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      ...options.headers
    };
    
    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    const response = await fetch(url, {
      ...options,
      headers
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Request failed');
    }

    return response.json();
  }

  // Auth
  register(data) { return this.request('/api/auth/register', { method: 'POST', body: JSON.stringify(data) }); }
  login(data) { return this.request('/api/auth/login', { method: 'POST', body: JSON.stringify(data) }); }
  
  // Rooms
  getRooms() { return this.request('/api/rooms'); }
  createRoom(data) { return this.request('/api/rooms', { method: 'POST', body: JSON.stringify(data) }); }
  joinRoom(roomId, password) { return this.request(`/api/rooms/${roomId}/join`, { method: 'POST', body: JSON.stringify({ password }) }); }
  
  // Messages
  getMessages(roomId) { return this.request(`/api/rooms/${roomId}/messages`); }
}

// Использование
const api = new WatchTogetherAPI();

async function init() {
  // Вход
  const auth = await api.login({ email: 'user@example.com', password: 'password' });
  api.setToken(auth.accessToken);
  
  // Получить комнаты
  const rooms = await api.getRooms();
  console.log('Rooms:', rooms);
  
  // Подключиться к SignalR
  const chat = new signalR.HubConnectionBuilder()
    .withUrl(`${api.baseUrl}/hubs/chat`, {
      accessTokenFactory: () => api.token
    })
    .build();
    
  chat.on('NewMessage', (msg) => console.log('Message:', msg));
  await chat.start();
  await chat.invoke('JoinRoom', rooms[0].id);
}
```
