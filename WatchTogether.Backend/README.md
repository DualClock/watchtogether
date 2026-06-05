# WatchTogether Backend

Полноценный бекенд на ASP.NET Core 8 для совместного просмотра фильмов.

## Стек технологий

- **ASP.NET Core 8** — веб-фреймворк
- **Entity Framework Core + SQL Server** — ORM и БД
- **SignalR** — real-time коммуникации (чат, синхронизация видео)
- **JWT** — аутентификация
- **Redis** — кэш и сессии
- **BCrypt** — хеширование паролей
- **FluentValidation** — валидация
- **Swagger/OpenAPI** — документация API

## Запуск

### Вариант 1: Локально с SQL Server (рекомендуется)

**Требования:**
- SQL Server (LocalDB, Express или полная версия)
- .NET 8 SDK
- Redis (опционально, для кэша)

**Строка подключения** (уже настроена в `appsettings.json`):
```
Server=localhost;Database=WatchTogether;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

**Команды:**
```bash
cd WatchTogether.Backend

# Применить миграции (создать базу данных)
dotnet ef database update --startup-project src/WatchTogether.Api/WatchTogether.Api --project src/WatchTogether.Infrastructure/WatchTogether.Infrastructure

# Запуск
dotnet run --project src/WatchTogether.Api/WatchTogether.Api
```

API будет доступно на `http://localhost:5000`
Swagger на `http://localhost:5000/swagger`

### Вариант 2: Docker Compose

```bash
cd WatchTogether.Backend
docker-compose up -d
```

Это запустит:
- API на `http://localhost:5000`
- SQL Server на порту `1433`
- Redis на порту `6379`

## API Endpoints

### Auth
- `POST /api/auth/register` — Регистрация
- `POST /api/auth/login` — Вход
- `POST /api/auth/refresh` — Обновление токена
- `POST /api/auth/logout` — Выход

### Users
- `GET /api/users/me` — Мой профиль
- `PUT /api/users/me` — Редактировать профиль
- `POST /api/users/me/avatar` — Загрузить аватар
- `DELETE /api/users/me` — Удалить аккаунт
- `GET /api/users/{username}` — Профиль пользователя

### Rooms
- `GET /api/rooms` — Список публичных комнат
- `GET /api/rooms/search?query=...` — Поиск комнат
- `GET /api/rooms/{id}` — Детали комнаты
- `POST /api/rooms` — Создать комнату
- `POST /api/rooms/{id}/join` — Войти в комнату
- `POST /api/rooms/{id}/leave` — Покинуть комнату
- `DELETE /api/rooms/{id}` — Закрыть комнату
- `POST /api/rooms/{id}/kick/{userId}` — Кикнуть пользователя

### Messages
- `GET /api/rooms/{roomId}/messages` — История сообщений
- `POST /api/rooms/{roomId}/messages` — Отправить сообщение
- `PUT /api/rooms/{roomId}/messages/{id}` — Редактировать сообщение
- `DELETE /api/rooms/{roomId}/messages/{id}` — Удалить сообщение

### SignalR Hubs
- `/hubs/chat` — Чат в реальном времени
- `/hubs/video` — Синхронизация видео

## WebSocket Events

### Chat Hub
- `JoinRoom(roomId)` — Войти в комнату
- `LeaveRoom(roomId)` — Покинуть комнату
- `SendMessage(roomId, content, parentMessageId?)` — Отправить сообщение
- `AddReaction(messageId, emoji)` — Добавить реакцию
- `RemoveReaction(messageId, emoji)` — Убрать реакцию
- `Typing(roomId)` — Печатает...

### Video Hub
- `JoinVideoRoom(roomId)` — Подключиться к видео
- `PlayVideo(roomId, currentTime)` — Воспроизвести
- `PauseVideo(roomId, currentTime)` — Пауза
- `SeekVideo(roomId, currentTime)` — Перемотка
- `ChangeVideo(roomId, videoId)` — Сменить видео

## Переменные окружения

| Переменная | Описание | По умолчанию |
|-----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | localhost |
| `ConnectionStrings__Redis` | Redis connection string | localhost:6379 |
| `JwtSettings__SecretKey` | JWT секрет (мин. 32 символа) | — |
| `JwtSettings__Issuer` | JWT issuer | WatchTogether |
| `JwtSettings__Audience` | JWT audience | WatchTogetherClient |
| `JwtSettings__ExpiryMinutes` | Время жизни access token | 15 |
