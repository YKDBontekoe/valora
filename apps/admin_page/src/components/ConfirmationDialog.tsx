import { motion, AnimatePresence } from 'framer-motion';
import { AlertCircle, X } from 'lucide-react';
import Button from './Button';

interface ConfirmationDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
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
  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="absolute inset-0 bg-brand-900/40 backdrop-blur-sm"
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            className="relative w-full max-w-md bg-white rounded-[2rem] shadow-premium-xl overflow-hidden border border-brand-100"
          >
            <div className="p-8">
              <div className="flex items-center justify-between mb-6">
                <div className={`p-3 rounded-2xl ${isDestructive ? 'bg-error-50 text-error-600' : 'bg-primary-50 text-primary-600'}`}>
                  <AlertCircle size={24} />
                </div>
                <button
                  onClick={onClose}
                  className="p-2 text-brand-400 hover:text-brand-900 transition-colors rounded-xl hover:bg-brand-50"
                >
                  <X size={20} />
                </button>
              </div>

              <h3 className="text-2xl font-black text-brand-900 tracking-tight mb-3">
                {title}
              </h3>
              <p className="text-brand-500 font-medium leading-relaxed">
                {message}
              </p>

              <div className="flex gap-4 mt-10">
                <Button
                  variant="outline"
                  onClick={onClose}
                  className="flex-1"
                >
                  {cancelLabel}
                </Button>
                <Button
                  variant={isDestructive ? 'danger' : 'secondary'}
                  onClick={onConfirm}
                  className="flex-1"
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
