import { useState, useEffect, useCallback, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, CheckCircle, AlertCircle, Info } from 'lucide-react';
import { notificationService } from '../services/notification';

interface Notification {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info';
}

const NotificationToast = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const timeouts = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const removeNotification = useCallback((id: string) => {
    setNotifications((prev) => prev.filter((n) => n.id !== id));

    // Clear timeout if it exists
    if (timeouts.current.has(id)) {
      clearTimeout(timeouts.current.get(id));
      timeouts.current.delete(id);
    }
  }, []);

  useEffect(() => {
    const handleNotification = (message: string, type: 'success' | 'error' | 'info') => {
      const id = Math.random().toString(36).substr(2, 9);
      const newNotification: Notification = { id, message, type };

      setNotifications((prev) => [...prev, newNotification]);

      // Auto dismiss after 5 seconds
      const timeoutId = setTimeout(() => {
        removeNotification(id);
      }, 5000);

      timeouts.current.set(id, timeoutId);
    };

    const unsubscribe = notificationService.subscribe(handleNotification);

    // Copy ref to variable for cleanup
    const currentTimeouts = timeouts.current;

    return () => {
      unsubscribe();
      // Clear all pending timeouts on unmount
      currentTimeouts.forEach((id) => clearTimeout(id));
      currentTimeouts.clear();
    };
  }, [removeNotification]);

  const getIcon = (type: string) => {
    switch (type) {
      case 'success':
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case 'error':
        return <AlertCircle className="w-5 h-5 text-red-500" />;
      default:
        return <Info className="w-5 h-5 text-blue-500" />;
    }
  };

  const getBgColor = (type: string) => {
    switch (type) {
      case 'success':
        return 'bg-green-50 border-green-200';
      case 'error':
        return 'bg-red-50 border-red-200';
      default:
        return 'bg-blue-50 border-blue-200';
    }
  };

  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
      <AnimatePresence mode='popLayout'>
        {notifications.map((notification) => (
          <motion.div
            key={notification.id}
            initial={{ opacity: 0, x: 50, scale: 0.95 }}
            animate={{ opacity: 1, x: 0, scale: 1 }}
            exit={{ opacity: 0, x: 50, scale: 0.95 }}
            transition={{ duration: 0.2 }}
            layout
            className={`flex items-start w-80 p-4 rounded-lg shadow-lg border ${getBgColor(notification.type)} backdrop-blur-sm pointer-events-auto`}
          >
            <div className="flex-shrink-0 mt-0.5">
              {getIcon(notification.type)}
            </div>
            <div className="ml-3 flex-1">
              <p className={`text-sm font-medium ${
                notification.type === 'success' ? 'text-green-800' :
                notification.type === 'error' ? 'text-red-800' : 'text-blue-800'
              }`}>
                {notification.message}
              </p>
            </div>
            <button
              onClick={() => removeNotification(notification.id)}
              className="ml-4 flex-shrink-0 rounded-md p-1 hover:bg-black/5 transition-colors cursor-pointer"
            >
              <X className="w-4 h-4 text-gray-500" />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
};

export default NotificationToast;
