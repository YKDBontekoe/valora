import { useEffect, useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { toastManager, type ToastMessage } from '../services/toast';
import { CheckCircle2, AlertCircle, X, Sparkles } from 'lucide-react';

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
      case 'success': return <CheckCircle2 className="text-success-600" size={24} />;
      case 'error': return <AlertCircle className="text-error-600" size={24} />;
      default: return <Sparkles className="text-primary-600" size={24} />;
    }
  };

  const getBgColor = (type: string) => {
    switch (type) {
      case 'success': return 'border-success-200 bg-white/95 shadow-glow-success ring-4 ring-success-500/5';
      case 'error': return 'border-error-200 bg-white/95 shadow-glow-error ring-4 ring-error-500/5';
      default: return 'border-primary-200 bg-white/95 shadow-glow-primary ring-4 ring-primary-500/5';
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
    <div className="fixed top-12 right-12 z-[100] flex flex-col gap-6 pointer-events-none">
      <AnimatePresence mode="popLayout">
        {toasts.map((toast) => (
          <motion.div
            key={toast.id}
            initial={{ opacity: 0, x: 60, scale: 0.8, y: 10 }}
            animate={{ opacity: 1, x: 0, scale: 1, y: 0 }}
            exit={{ opacity: 0, x: 30, scale: 0.9, transition: { duration: 0.25, ease: "easeInOut" } }}
            layout
            className={`pointer-events-auto flex items-center gap-6 px-8 py-7 rounded-[2.5rem] border backdrop-blur-3xl min-w-[380px] max-w-lg relative overflow-hidden ${getBgColor(toast.type)}`}
          >
            {/* Background Glow Effect */}
            <div className={`absolute top-0 right-0 w-[40%] h-[100%] opacity-10 bg-linear-to-bl from-current to-transparent pointer-events-none`} style={{ color: `var(--color-${toast.type === 'info' ? 'primary' : toast.type}-500)` }} />

            {/* Progress Bar */}
            <motion.div
                initial={{ width: '100%' }}
                animate={{ width: 0 }}
                transition={{ duration: 5, ease: "linear" }}
                className={`absolute bottom-0 left-0 h-1.5 opacity-40 ${getProgressColor(toast.type)} shadow-[0_0_10px_rgba(0,0,0,0.1)]`}
            />

            <div className={`flex-shrink-0 w-14 h-14 rounded-2xl shadow-premium border flex items-center justify-center transition-transform duration-500 group-hover:scale-110 rotate-3 ${toast.type === 'success' ? 'bg-success-50 border-success-100' : toast.type === 'error' ? 'bg-error-50 border-error-100' : 'bg-primary-50 border-primary-100'}`}>
              {getIcon(toast.type)}
            </div>
            <div className="flex-1">
              <p className="text-lg font-black text-brand-900 leading-tight tracking-tight">
                {toast.message}
              </p>
              <div className="flex items-center gap-2 mt-1.5 opacity-40">
                <div className="w-1.5 h-1.5 rounded-full bg-current" style={{ color: `var(--color-${toast.type === 'info' ? 'primary' : toast.type}-500)` }} />
                <span className="text-[10px] font-black uppercase tracking-[0.2em]">{toast.type === 'info' ? 'System Bulletin' : 'Real-time Signal'}</span>
              </div>
            </div>
            <button
              onClick={() => removeToast(toast.id)}
              className="flex-shrink-0 w-10 h-10 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300 p-1 group"
              aria-label="Dismiss notification"
            >
              <X size={20} className="group-hover:rotate-90 transition-transform duration-300" />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
};

export default Toast;
