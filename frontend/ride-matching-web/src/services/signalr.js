import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.statusListeners = [];
    this.isConnected = false;
  }

  startConnection(hubUrl = '/hubs/ride') {
    if (this.connection) {
      return Promise.resolve(this.connection);
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting((error) => {
      console.warn('[SignalR] Reconnecting...', error);
      this.isConnected = false;
      this.notifyStatus('Reconnecting');
    });

    this.connection.onreconnected((connectionId) => {
      console.log('[SignalR] Reconnected with ID:', connectionId);
      this.isConnected = true;
      this.notifyStatus('Connected');
    });

    this.connection.onclose((error) => {
      console.error('[SignalR] Connection closed:', error);
      this.isConnected = false;
      this.notifyStatus('Disconnected');
    });

    return this.connection
      .start()
      .then(() => {
        console.log('[SignalR] Connected successfully to ride hub');
        this.isConnected = true;
        this.notifyStatus('Connected');
        return this.connection;
      })
      .catch((err) => {
        console.error('[SignalR] Error starting connection:', err);
        this.isConnected = false;
        this.notifyStatus('Error');
        // Retry connection after 3 seconds
        setTimeout(() => this.startConnection(hubUrl), 3000);
      });
  }

  on(eventName, callback) {
    if (this.connection) {
      this.connection.on(eventName, callback);
    }
  }

  off(eventName, callback) {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  async invoke(methodName, ...args) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        return await this.connection.invoke(methodName, ...args);
      } catch (err) {
        console.error(`[SignalR] Invoke error for ${methodName}:`, err);
      }
    }
  }

  registerDriver(driverId) {
    return this.invoke('RegisterDriver', driverId);
  }

  registerRider(riderId) {
    return this.invoke('RegisterRider', riderId);
  }

  joinRideGroup(rideId) {
    return this.invoke('JoinRideGroup', rideId);
  }

  leaveRideGroup(rideId) {
    return this.invoke('LeaveRideGroup', rideId);
  }

  onStatusChange(listener) {
    this.statusListeners.push(listener);
    return () => {
      this.statusListeners = this.statusListeners.filter((l) => l !== listener);
    };
  }

  notifyStatus(status) {
    this.statusListeners.forEach((listener) => listener(status));
  }
}

export const signalRService = new SignalRService();
