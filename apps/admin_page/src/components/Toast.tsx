import { useEffect, useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { toastManager, type ToastMessage } from '../services/toast';
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-react';

const Toast = () => {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const timers = useRef<Record<string, number>>({});

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
    if (timers.current[id]) {
      window.clearTimeout(timers.current[id]);
      delete timers.current[id];
    }
  };

  useEffect(() => {
    const handleToast = (event: Event) => {
      const customEvent = event as CustomEvent<ToastMessage>;
      const newToast = customEvent.detail;
      setToasts((prev) => [...prev, newToast]);

      const timer = window.setTimeout(() => {
        removeToast(newToast.id);
      }, 5000);

      timers.current[newToast.id] = timer;
    };

    toastManager.addEventListener('toast', handleToast);
    return () => {
      toastManager.removeEventListener('toast', handleToast);
      // Clear all timers on unmount
      Object.values(timers.current).forEach(window.clearTimeout);
    };
  }, []);

  const getIcon = (type: string) => {
    switch (type) {
      case 'success': return <CheckCircle2 className="text-success-500" size={20} />;
      case 'error': return <AlertCircle className="text-error-500" size={20} />;
      default: return <Info className="text-primary-500" size={20} />;
    }
  };

  const getBgColor = (type: string) => {
    switch (type) {
      case 'success': return 'border-success-100 bg-success-50/50';
      case 'error': return 'border-error-100 bg-error-50/50';
      default: return 'border-primary-100 bg-primary-50/50';
    }
  };

  return (
    <div className="fixed top-6 right-6 z-50 flex flex-col gap-3 pointer-events-none">
      <AnimatePresence mode="popLayout">
        {toasts.map((toast) => (
          <motion.div
            key={toast.id}
            initial={{ opacity: 0, x: 20, scale: 0.95 }}
            animate={{ opacity: 1, x: 0, scale: 1 }}
            exit={{ opacity: 0, x: 10, scale: 0.95 }}
            layout
            className={`pointer-events-auto flex items-center gap-4 px-5 py-4 rounded-[1.25rem] border shadow-premium backdrop-blur-xl min-w-[320px] max-w-md ${getBgColor(toast.type)}`}
          >
            <div className="flex-shrink-0">
              {getIcon(toast.type)}
            </div>
            <div className="flex-1">
              <p className="text-sm font-bold text-brand-900 leading-tight">
                {toast.message}
              </p>
            </div>
            <button
              onClick={() => removeToast(toast.id)}
              className="flex-shrink-0 text-brand-400 hover:text-brand-600 transition-colors p-1"
            >
              <X size={16} />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
};

export default Toast;
