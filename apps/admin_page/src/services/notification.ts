type NotificationType = 'success' | 'error' | 'info';

type NotificationListener = (message: string, type: NotificationType) => void;

class NotificationService {
  private listeners: NotificationListener[] = [];

  subscribe(listener: NotificationListener) {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  notify(message: string, type: NotificationType = 'info') {
    this.listeners.forEach((listener) => listener(message, type));
  }
}

export const notificationService = new NotificationService();

export const showNotification = (message: string, type: NotificationType = 'info') => {
  notificationService.notify(message, type);
};
