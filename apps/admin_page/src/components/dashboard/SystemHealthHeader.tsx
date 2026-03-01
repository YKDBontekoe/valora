import { motion } from 'framer-motion';
import { Server, AlertTriangle, RefreshCw } from 'lucide-react';

interface SystemHealthHeaderProps {
  loading: boolean;
  isStale: boolean;
  fetchHealth: () => void;
}

const SystemHealthHeader = ({ loading, isStale, fetchHealth }: SystemHealthHeaderProps) => {
  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
      <div>
        <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 bg-primary-50 rounded-xl flex items-center justify-center border border-primary-100/50 shadow-sm">
                <Server className="text-primary-600" size={20} />
            </div>
            <h2 className="text-3xl font-black text-brand-900 tracking-tight">System Infrastructure</h2>
        </div>
        <div className="flex items-center gap-3">
          <p className="text-brand-400 font-bold">
            Real-time cluster performance and health monitoring.
          </p>
          {isStale && (
            <span className="flex items-center gap-1.5 text-[10px] font-black text-error-600 bg-error-50 px-3 py-1 rounded-full uppercase tracking-widest border border-error-100 animate-pulse">
              <AlertTriangle size={12} />
              Offline Data
            </span>
          )}
        </div>
      </div>
      <motion.button
        whileHover={{ rotate: 180, scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        onClick={fetchHealth}
        disabled={loading}
        className={`flex items-center gap-3 px-6 py-3 bg-brand-50 text-brand-600 font-black text-xs uppercase tracking-widest rounded-2xl hover:bg-primary-50 hover:text-primary-600 transition-all border border-brand-100 hover:border-primary-100 shadow-sm ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
      >
        <RefreshCw size={18} className={loading ? 'animate-spin' : ''} />
        Refresh Nodes
      </motion.button>
    </div>
  );
};

export default SystemHealthHeader;
