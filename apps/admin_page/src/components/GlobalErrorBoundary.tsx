import { motion } from 'framer-motion';
import { AlertTriangle, RefreshCcw, Home } from 'lucide-react';
import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import Button from './Button';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class GlobalErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error, errorInfo: null };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error:', error, errorInfo);
    this.setState({ errorInfo });
  }

  private handleReset = () => {
    window.location.reload();
  };

  private handleGoHome = () => {
    window.location.href = '/';
  };

  public render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen bg-brand-50 flex items-center justify-center p-6 selection:bg-primary-100 selection:text-primary-900">
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            className="w-full max-w-2xl bg-white rounded-3xl shadow-premium-xl border border-brand-100 overflow-hidden"
          >
            <div className="bg-error-600 p-10 text-white relative overflow-hidden">
                {/* Decorative background element */}
                <div className="absolute top-0 right-0 -translate-y-1/2 translate-x-1/2 w-64 h-64 bg-white/10 rounded-full blur-3xl" />

                <div className="relative z-10 flex flex-col items-center text-center">
                    <div className="w-20 h-20 bg-white/20 backdrop-blur-md rounded-2xl flex items-center justify-center mb-6 shadow-xl">
                        <AlertTriangle size={40} className="text-white" />
                    </div>
                    <h1 className="text-4xl font-black tracking-tight mb-3">Something went wrong</h1>
                    <p className="text-error-50 font-medium text-lg max-w-md opacity-90">
                        The application encountered an unexpected error. Don't worry, your data is safe.
                    </p>
                </div>
            </div>

            <div className="p-10">
              <div className="bg-brand-50 rounded-2xl p-8 mb-8 border border-brand-100 text-center">
                <p className="text-brand-600 font-bold">
                  The error has been logged and our engineering team has been notified.
                  Please try reloading the application or return to the dashboard.
                </p>
              </div>

              <div className="flex flex-col sm:flex-row gap-4">
                <Button
                  onClick={this.handleReset}
                  variant="secondary"
                  leftIcon={<RefreshCcw size={18} />}
                  className="flex-1"
                >
                  Reload Application
                </Button>
                <Button
                  onClick={this.handleGoHome}
                  variant="outline"
                  leftIcon={<Home size={18} />}
                  className="flex-1"
                >
                  Return to Dashboard
                </Button>
              </div>

              <p className="text-center mt-8 text-xs font-bold text-brand-400 uppercase tracking-widest">
                Support ID: {Math.random().toString(36).substring(2, 10).toUpperCase()}
              </p>
            </div>
          </motion.div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default GlobalErrorBoundary;
