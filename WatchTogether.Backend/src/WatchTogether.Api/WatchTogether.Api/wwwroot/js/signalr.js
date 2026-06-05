// ===== SignalR Connections =====
const SIGNALR_URL = 'http://localhost:5000';

class ChatConnection {
    constructor() {
        this.connection = null;
        this.connected = false;
        this.currentRoomId = null;
        this.messageHandlers = [];
        this.userJoinedHandlers = [];
        this.userLeftHandlers = [];
        this.typingHandlers = [];
    }

    async connect() {
        if (this.connection) return;

        const token = localStorage.getItem('accessToken');
        if (!token) throw new Error('Not authenticated');

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${SIGNALR_URL}/hubs/chat`, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.on('NewMessage', (message) => {
            this.messageHandlers.forEach(h => h(message));
        });

        this.connection.on('UserJoined', (data) => {
            this.userJoinedHandlers.forEach(h => h(data));
        });

        this.connection.on('UserLeft', (data) => {
            this.userLeftHandlers.forEach(h => h(data));
        });

        this.connection.on('UserTyping', (data) => {
            this.typingHandlers.forEach(h => h(data));
        });

        this.connection.on('ReactionAdded', (data) => {
            // Handle reaction
        });

        this.connection.on('ReactionRemoved', (data) => {
            // Handle reaction removal
        });

        this.connection.onreconnecting(() => {
            console.log('Chat reconnecting...');
            this.connected = false;
        });

        this.connection.onreconnected(() => {
            console.log('Chat reconnected');
            this.connected = true;
            if (this.currentRoomId) {
                this.joinRoom(this.currentRoomId);
            }
        });

        this.connection.onclose(() => {
            console.log('Chat connection closed');
            this.connected = false;
            this.connection = null;
        });

        await this.connection.start();
        this.connected = true;
        console.log('Chat connected');
    }

    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            this.connected = false;
            this.currentRoomId = null;
        }
    }

    async joinRoom(roomId) {
        if (!this.connected) await this.connect();
        this.currentRoomId = roomId;
        await this.connection.invoke('JoinRoom', roomId);
    }

    async leaveRoom(roomId) {
        if (!this.connected) return;
        await this.connection.invoke('LeaveRoom', roomId);
        this.currentRoomId = null;
    }

    async sendMessage(content, parentMessageId = null) {
        if (!this.connected || !this.currentRoomId) throw new Error('Not in a room');
        await this.connection.invoke('SendMessage', this.currentRoomId, content, parentMessageId);
    }

    async addReaction(messageId, emoji) {
        if (!this.connected) return;
        await this.connection.invoke('AddReaction', messageId, emoji);
    }

    async removeReaction(messageId, emoji) {
        if (!this.connected) return;
        await this.connection.invoke('RemoveReaction', messageId, emoji);
    }

    async typing() {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('Typing', this.currentRoomId);
    }

    onMessage(handler) {
        this.messageHandlers.push(handler);
        return () => {
            this.messageHandlers = this.messageHandlers.filter(h => h !== handler);
        };
    }

    onUserJoined(handler) {
        this.userJoinedHandlers.push(handler);
        return () => {
            this.userJoinedHandlers = this.userJoinedHandlers.filter(h => h !== handler);
        };
    }

    onUserLeft(handler) {
        this.userLeftHandlers.push(handler);
        return () => {
            this.userLeftHandlers = this.userLeftHandlers.filter(h => h !== handler);
        };
    }

    onTyping(handler) {
        this.typingHandlers.push(handler);
        return () => {
            this.typingHandlers = this.typingHandlers.filter(h => h !== handler);
        };
    }
}

class VideoConnection {
    constructor() {
        this.connection = null;
        this.connected = false;
        this.currentRoomId = null;
        this.playHandlers = [];
        this.pauseHandlers = [];
        this.seekHandlers = [];
        this.changeHandlers = [];
        this.syncHandlers = [];
    }

    async connect() {
        if (this.connection) return;

        const token = localStorage.getItem('accessToken');
        if (!token) throw new Error('Not authenticated');

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${SIGNALR_URL}/hubs/video`, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.on('VideoPlayed', (data) => {
            this.playHandlers.forEach(h => h(data));
        });

        this.connection.on('VideoPaused', (data) => {
            this.pauseHandlers.forEach(h => h(data));
        });

        this.connection.on('VideoSeeked', (data) => {
            this.seekHandlers.forEach(h => h(data));
        });

        this.connection.on('VideoChanged', (data) => {
            this.changeHandlers.forEach(h => h(data));
        });

        this.connection.on('SyncState', (state) => {
            this.syncHandlers.forEach(h => h(state));
        });

        this.connection.onreconnecting(() => {
            console.log('Video reconnecting...');
            this.connected = false;
        });

        this.connection.onreconnected(() => {
            console.log('Video reconnected');
            this.connected = true;
            if (this.currentRoomId) {
                this.joinRoom(this.currentRoomId);
            }
        });

        this.connection.onclose(() => {
            console.log('Video connection closed');
            this.connected = false;
            this.connection = null;
        });

        await this.connection.start();
        this.connected = true;
        console.log('Video connected');
    }

    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            this.connected = false;
            this.currentRoomId = null;
        }
    }

    async joinRoom(roomId) {
        if (!this.connected) await this.connect();
        this.currentRoomId = roomId;
        await this.connection.invoke('JoinVideoRoom', roomId);
        // Request sync state after joining
        await this.requestSync();
    }

    async leaveRoom(roomId) {
        if (!this.connected) return;
        await this.connection.invoke('LeaveVideoRoom', roomId);
        this.currentRoomId = null;
    }

    async play(currentTime) {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('PlayVideo', this.currentRoomId, currentTime);
    }

    async pause(currentTime) {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('PauseVideo', this.currentRoomId, currentTime);
    }

    async seek(currentTime) {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('SeekVideo', this.currentRoomId, currentTime);
    }

    async changeVideo(videoId) {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('ChangeVideo', this.currentRoomId, videoId);
    }

    async requestSync() {
        if (!this.connected || !this.currentRoomId) return;
        await this.connection.invoke('RequestSync', this.currentRoomId);
    }

    onPlay(handler) {
        this.playHandlers.push(handler);
        return () => {
            this.playHandlers = this.playHandlers.filter(h => h !== handler);
        };
    }

    onPause(handler) {
        this.pauseHandlers.push(handler);
        return () => {
            this.pauseHandlers = this.pauseHandlers.filter(h => h !== handler);
        };
    }

    onSeek(handler) {
        this.seekHandlers.push(handler);
        return () => {
            this.seekHandlers = this.seekHandlers.filter(h => h !== handler);
        };
    }

    onChange(handler) {
        this.changeHandlers.push(handler);
        return () => {
            this.changeHandlers = this.changeHandlers.filter(h => h !== handler);
        };
    }

    onSync(handler) {
        this.syncHandlers.push(handler);
        return () => {
            this.syncHandlers = this.syncHandlers.filter(h => h !== handler);
        };
    }
}

// Global instances
const chatConnection = new ChatConnection();
const videoConnection = new VideoConnection();
