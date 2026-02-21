import { AlertCircle, RefreshCw } from 'lucide-react';
import Button from './Button';

interface ErrorStateProps {
  title?: string;
  message: string;
  onRetry?: () => void;
}

const ErrorState = ({ title = 'Something went wrong', message, onRetry }: ErrorStateProps) => {
  return (
    <div className="p-8 bg-error-50 border border-error-100 rounded-3xl text-error-700 flex flex-col items-center justify-center text-center shadow-sm">
      <AlertCircle className="w-12 h-12 text-error-500 mb-4" />
      <h3 className="text-lg font-black text-brand-900 mb-2">{title}</h3>
      <p className="text-brand-600 font-medium mb-6 max-w-md">{message}</p>
      {onRetry && (
        <Button onClick={onRetry} variant="outline" className="border-error-200 text-error-700 hover:bg-error-100">
          <RefreshCw className="w-4 h-4 mr-2" />
          Try Again
        </Button>
      )}
    </div>
  );
};

export default ErrorState;
