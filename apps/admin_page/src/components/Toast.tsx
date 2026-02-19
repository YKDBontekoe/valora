import { useEffect, useState } from 'react';
import { toastManager, ToastMessage } from '../services/toast';

const Toast = () => {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  useEffect(() => {
    const handleToast = (event: Event) => {
      const customEvent = event as CustomEvent<ToastMessage>;
      const newToast = customEvent.detail;
      setToasts((prev) => [...prev, newToast]);

      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== newToast.id));
      }, 5000);
    };

    toastManager.addEventListener('toast', handleToast);
    return () => {
      toastManager.removeEventListener('toast', handleToast);
    };
  }, []);

  if (toasts.length === 0) return null;

  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`pointer-events-auto px-4 py-3 rounded shadow-lg text-white font-medium min-w-[300px] transition-all duration-300 ${
            toast.type === 'error'
              ? 'bg-red-600'
              : toast.type === 'success'
              ? 'bg-green-600'
              : 'bg-blue-600'
          }`}
        >
          {toast.message}
        </div>
      ))}
    </div>
  );
};

export default Toast;
