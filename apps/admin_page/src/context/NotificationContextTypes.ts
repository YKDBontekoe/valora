import { createContext } from 'react';
import type { ToastType } from '../components/Toast';

export interface NotificationContextType {
  showToast: (message: string, type: ToastType) => void;
}

export const NotificationContext = createContext<NotificationContextType | undefined>(undefined);
