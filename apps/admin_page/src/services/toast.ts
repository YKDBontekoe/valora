export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: string;
  message: string;
  type: ToastType;
}

class ToastManager extends EventTarget {
  show(message: string, type: ToastType = 'info') {
    this.dispatchEvent(new CustomEvent<ToastMessage>('toast', {
      detail: {
        id: Date.now().toString(),
        message,
        type,
      }
    }));
  }
}

export const toastManager = new ToastManager();

export const showToast = (message: string, type: ToastType = 'info') => {
  toastManager.show(message, type);
};
