import { useState, useId } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { AlertCircle, X } from 'lucide-react';
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

const containerVariants = {
  hidden: { opacity: 0, scale: 0.95, y: 20 },
  visible: {
    opacity: 1,
    scale: 1,
    y: 0,
    transition: {
      duration: 0.4,
      ease: [0.22, 1, 0.36, 1] as const,
      staggerChildren: 0.08,
      delayChildren: 0.1
    }
  },
  exit: {
    opacity: 0,
    scale: 0.95,
    y: 20,
    transition: { duration: 0.2 }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 15 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  }
};

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
  const titleId = useId();

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
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => !isSubmitting && onClose()}
            className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
          />
          <motion.div
            variants={containerVariants}
            initial="hidden"
            animate="visible"
            exit="exit"
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            className="relative w-full max-w-md bg-white rounded-[2.5rem] shadow-premium-xl overflow-hidden border border-white/20"
          >
            <div className="p-10">
              <div className="flex items-center justify-between mb-8">
                <motion.div
                    variants={itemVariants}
                    className={`p-4 rounded-2xl shadow-sm ${isDestructive ? 'bg-error-50 text-error-600 border border-error-100' : 'bg-primary-50 text-primary-600 border border-primary-100'}`}
                >
                  <AlertCircle size={28} />
                </motion.div>
                <motion.button
                  variants={itemVariants}
                  onClick={() => !isSubmitting && onClose()}
                  aria-label="Close dialog"
                  className="w-10 h-10 flex items-center justify-center text-brand-300 hover:text-brand-900 transition-all rounded-xl hover:bg-brand-50"
                  disabled={isSubmitting}
                >
                  <X size={24} />
                </motion.button>
              </div>

              <motion.h3 id={titleId} variants={itemVariants} className="text-3xl font-black text-brand-900 tracking-tightest mb-4">
                {title}
              </motion.h3>
              <motion.p variants={itemVariants} className="text-brand-500 font-bold leading-relaxed text-lg">
                {message}
              </motion.p>

              <motion.div variants={itemVariants} className="flex gap-4 mt-12">
                <Button
                  variant="outline"
                  onClick={onClose}
                  className="flex-1 py-4"
                  disabled={isSubmitting}
                >
                  {cancelLabel}
                </Button>
                <Button
                  variant={isDestructive ? 'danger' : 'secondary'}
                  onClick={handleConfirm}
                  className="flex-1 py-4 shadow-lg"
                  isLoading={isSubmitting}
                  disabled={isSubmitting}
                >
                  {confirmLabel}
                </Button>
              </motion.div>
            </div>
            {/* Subtle bottom glow for destructive actions */}
            {isDestructive && (
                <div className="absolute bottom-0 left-0 w-full h-1 bg-linear-to-r from-transparent via-error-500/20 to-transparent" />
            )}
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default ConfirmationDialog;
