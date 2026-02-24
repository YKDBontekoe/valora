import { AlertCircle, RefreshCw } from 'lucide-react';
import Button from './Button';
import { motion } from 'framer-motion';

interface ErrorStateProps {
  title?: string;
  message: string;
  onRetry?: () => void;
}

const ErrorState = ({ title = 'Something went wrong', message, onRetry }: ErrorStateProps) => {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }}
      className="p-10 bg-error-50/50 backdrop-blur-md border border-error-100 rounded-[2.5rem] text-error-700 flex flex-col items-center justify-center text-center shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 left-0 w-full h-1 bg-error-200 opacity-30" />

      <div className="w-20 h-20 bg-white rounded-3xl shadow-sm flex items-center justify-center mb-6 text-error-500 border border-error-100/50">
        <AlertCircle className="w-10 h-10" />
      </div>

      <h3 className="text-2xl font-black text-brand-900 mb-3 tracking-tight">{title}</h3>
      <p className="text-brand-500 font-bold mb-10 max-w-md leading-relaxed">{message}</p>

      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outline"
          className="px-8 py-3.5 border-error-200 text-error-700 hover:bg-error-50 bg-white shadow-sm"
          leftIcon={<RefreshCw className="w-4 h-4" />}
        >
          Retry Connection
        </Button>
      )}
    </motion.div>
  );
};

export default ErrorState;
