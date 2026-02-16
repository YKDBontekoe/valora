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
            className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden relative z-50 border border-brand-100"
          >
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <div className={`p-2 rounded-full ${isDestructive ? 'bg-red-50 text-red-600' : 'bg-brand-50 text-brand-600'}`}>
                  <AlertTriangle className="h-6 w-6" />
                </div>
                <button
                  onClick={onClose}
                  className="text-brand-400 hover:text-brand-600 transition-colors p-1 rounded-lg hover:bg-brand-50"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              <h3 className="text-xl font-bold text-brand-900 mb-2">{title}</h3>
              <p className="text-brand-500 leading-relaxed mb-6">
                {message}
              </p>

              <div className="flex justify-end gap-3">
                <button
                  onClick={onClose}
                  className="px-4 py-2 rounded-xl text-sm font-semibold text-brand-600 hover:bg-brand-50 border border-transparent hover:border-brand-200 transition-all cursor-pointer"
                >
                  {cancelLabel}
                </button>
                <button
                  onClick={() => {
                    onConfirm();
                    onClose();
                  }}
                  className={`px-4 py-2 rounded-xl text-sm font-semibold text-white shadow-sm transition-all cursor-pointer ${
                    isDestructive
                      ? 'bg-red-600 hover:bg-red-700 shadow-red-200'
                      : 'bg-primary-600 hover:bg-primary-700 shadow-primary-200'
                  }`}
                >
                  {confirmLabel}
                </button>
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default ConfirmationDialog;
