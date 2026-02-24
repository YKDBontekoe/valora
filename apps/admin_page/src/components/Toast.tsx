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
    const currentTimers = timers.current;
    const handleToast = (event: Event) => {
      const customEvent = event as CustomEvent<ToastMessage>;
      const newToast = customEvent.detail;
      setToasts((prev) => [...prev, newToast]);

      const timer = window.setTimeout(() => {
        removeToast(newToast.id);
      }, 5000);

      currentTimers[newToast.id] = timer;
    };

    toastManager.addEventListener('toast', handleToast);
    return () => {
      toastManager.removeEventListener('toast', handleToast);
      // Clear all timers on unmount
      Object.values(currentTimers).forEach(window.clearTimeout);
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
      case 'success': return 'border-success-100 bg-success-50/60';
      case 'error': return 'border-error-100 bg-error-50/60';
      default: return 'border-primary-100 bg-primary-50/60';
    }
  };

  const getProgressColor = (type: string) => {
    switch (type) {
      case 'success': return 'bg-success-500';
      case 'error': return 'bg-error-500';
      default: return 'bg-primary-500';
    }
  };

  return (
    <div className="fixed top-8 right-8 z-[100] flex flex-col gap-4 pointer-events-none">
      <AnimatePresence mode="popLayout">
        {toasts.map((toast) => (
          <motion.div
            key={toast.id}
            initial={{ opacity: 0, x: 40, scale: 0.9 }}
            animate={{ opacity: 1, x: 0, scale: 1 }}
            exit={{ opacity: 0, x: 20, scale: 0.9, transition: { duration: 0.2 } }}
            layout
            className={`pointer-events-auto flex items-center gap-5 px-6 py-5 rounded-[1.5rem] border shadow-premium-xl backdrop-blur-2xl min-w-[340px] max-w-md relative overflow-hidden ${getBgColor(toast.type)}`}
          >
            {/* Progress Bar */}
            <motion.div
                initial={{ width: '100%' }}
                animate={{ width: 0 }}
                transition={{ duration: 5, ease: "linear" }}
                className={`absolute bottom-0 left-0 h-1 opacity-40 ${getProgressColor(toast.type)}`}
            />

            <div className="flex-shrink-0 w-10 h-10 bg-white rounded-xl shadow-sm flex items-center justify-center border border-white/50">
              {getIcon(toast.type)}
            </div>
            <div className="flex-1">
              <p className="text-sm font-black text-brand-900 leading-snug">
                {toast.message}
              </p>
            </div>
            <button
              onClick={() => removeToast(toast.id)}
              className="flex-shrink-0 w-8 h-8 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-white/50 rounded-lg transition-all duration-300 p-1"
            >
              <X size={18} />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
};

export default Toast;
