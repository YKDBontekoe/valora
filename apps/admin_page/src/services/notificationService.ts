type NotificationType = 'success' | 'error' | 'info' | 'warning';

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

  success(message: string) {
    this.notify(message, 'success');
  }

  error(message: string) {
    this.notify(message, 'error');
  }

  info(message: string) {
    this.notify(message, 'info');
  }

  warning(message: string) {
    this.notify(message, 'warning');
  }
}

export const notificationService = new NotificationService();
