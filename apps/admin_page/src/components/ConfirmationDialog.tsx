import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { AlertCircle, X, ShieldAlert } from 'lucide-react';
import Button from './Button';

interface ConfirmationDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => Promise<void> | void;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isDestructive?: boolean;
}

const ConfirmationDialog = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  isDestructive = false,
}: ConfirmationDialogProps) => {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleConfirm = async () => {
    setIsSubmitting(true);
    try {
      await onConfirm();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <div
          className="fixed inset-0 z-[100] flex items-center justify-center p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="dialog-title"
          aria-describedby="dialog-description"
        >
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => !isSubmitting && onClose()}
            className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 30 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 30 }}
            transition={{ type: "spring", damping: 25, stiffness: 350 }}
            className="relative w-full max-w-md bg-white/90 backdrop-blur-2xl rounded-[3rem] shadow-premium-2xl overflow-hidden border border-white/40"
          >
            {/* Top accent bar */}
            <div className={`absolute top-0 left-0 w-full h-2 ${isDestructive ? 'bg-error-500' : 'bg-primary-500'}`} />

            <div className="p-10">
              <div className="flex items-center justify-between mb-8">
                <div className={`p-4 rounded-2xl ${isDestructive ? 'bg-error-50 text-error-600 shadow-glow-error' : 'bg-primary-50 text-primary-600 shadow-glow-primary'}`}>
                  {isDestructive ? <ShieldAlert size={28} /> : <AlertCircle size={28} />}
                </div>
                <button
                  onClick={() => !isSubmitting && onClose()}
                  className="p-2.5 text-brand-300 hover:text-brand-900 transition-all duration-300 rounded-xl hover:bg-brand-50"
                  disabled={isSubmitting}
                  aria-label="Close dialog"
                >
                  <X size={22} />
                </button>
              </div>

              <h3 id="dialog-title" className="text-3xl font-black text-brand-900 tracking-tight mb-4">
                {title}
              </h3>
              <p id="dialog-description" className="text-brand-500 font-bold leading-relaxed text-lg">
                {message}
              </p>

              <div className="flex gap-5 mt-12">
                <Button
                  variant="outline"
                  onClick={onClose}
                  className="flex-1 font-black"
                  disabled={isSubmitting}
                >
                  {cancelLabel}
                </Button>
                <Button
                  variant={isDestructive ? 'danger' : 'secondary'}
                  onClick={handleConfirm}
                  className="flex-1 font-black shadow-glow"
                  isLoading={isSubmitting}
                  disabled={isSubmitting}
                >
                  {confirmLabel}
                </Button>
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default ConfirmationDialog;
