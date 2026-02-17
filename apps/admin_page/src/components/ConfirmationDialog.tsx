import { motion, AnimatePresence } from 'framer-motion';
import { AlertTriangle, X } from 'lucide-react';

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
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/20 backdrop-blur-sm"
            onClick={onClose}
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            className="bg-white rounded-3xl shadow-premium-xl w-full max-w-md overflow-hidden relative z-50 border border-brand-100"
          >
            <div className="p-8">
              <div className="flex items-center justify-between mb-6">
                <div className={`p-3 rounded-2xl ${isDestructive ? 'bg-error-50 text-error-600' : 'bg-info-50 text-info-600'}`}>
                  <AlertTriangle className="h-6 w-6" />
                </div>
                <motion.button
                  whileHover={{ rotate: 90 }}
                  whileTap={{ scale: 0.9 }}
                  onClick={onClose}
                  className="text-brand-300 hover:text-brand-600 transition-colors p-2 rounded-xl hover:bg-brand-50"
                >
                  <X className="h-5 w-5" />
                </motion.button>
              </div>

              <h3 className="text-2xl font-black text-brand-900 mb-2 tracking-tight">{title}</h3>
              <p className="text-brand-500 font-medium leading-relaxed mb-8">
                {message}
              </p>

              <div className="flex flex-col sm:flex-row justify-end gap-3">
                <motion.button
                  whileTap={{ scale: 0.98 }}
                  onClick={onClose}
                  className="px-6 py-3 rounded-2xl text-sm font-bold text-brand-500 hover:bg-brand-50 border border-brand-100 hover:border-brand-200 transition-all cursor-pointer"
                >
                  {cancelLabel}
                </motion.button>
                <motion.button
                  whileHover={{ scale: 1.02 }}
                  whileTap={{ scale: 0.98 }}
                  onClick={() => {
                    onConfirm();
                    onClose();
                  }}
                  className={`px-6 py-3 rounded-2xl text-sm font-bold text-white shadow-premium transition-all cursor-pointer ${
                    isDestructive
                      ? 'bg-error-600 hover:bg-error-700 shadow-error-200/50'
                      : 'bg-primary-600 hover:bg-primary-700 shadow-primary-200/50'
                  }`}
                >
                  {confirmLabel}
                </motion.button>
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default ConfirmationDialog;
