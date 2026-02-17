import { useContext } from 'react';
import { NotificationContext, type NotificationContextType } from '../context/NotificationContextTypes';

export const useNotification = (): NotificationContextType => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
};
