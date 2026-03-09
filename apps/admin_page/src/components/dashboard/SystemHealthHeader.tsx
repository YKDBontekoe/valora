import { motion } from 'framer-motion';
import { Server, Sparkles, AlertTriangle, RefreshCw } from 'lucide-react';

interface SystemHealthHeaderProps {
  loading: boolean;
  isStale: boolean;
  fetchHealth: () => void;
}

export const SystemHealthHeader: React.FC<SystemHealthHeaderProps> = ({ loading, isStale, fetchHealth }) => {
  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-12 mb-16">
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-5">
            <div className="w-14 h-14 bg-white/10 rounded-2xl flex items-center justify-center border border-white/20 shadow-premium backdrop-blur-md transition-all duration-700 group-hover:rotate-12 group-hover:scale-110">
                <Server className="text-white" size={28} />
            </div>
            <div className="flex flex-col">
                <h2 className="text-4xl font-black text-white tracking-tight">System Infrastructure</h2>
                <div className="flex items-center gap-2 mt-1.5">
                    <Sparkles size={12} className="text-primary-400" />
                    <span className="text-[10px] font-black uppercase tracking-[0.25em] text-primary-400">Node Cluster: Primary-Alpha</span>
                </div>
            </div>
        </div>
        <div className="flex items-center gap-4 ml-1">
          <p className="text-brand-300 font-bold text-lg leading-relaxed max-w-lg">
            High-performance cluster monitoring with real-time throughput analysis and node integrity checks.
          </p>
          {isStale && (
            <motion.span
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                className="flex items-center gap-2 text-[10px] font-black text-error-600 bg-error-50 px-4 py-1.5 rounded-full uppercase tracking-widest border border-error-100 animate-pulse shadow-glow-error"
            >
              <AlertTriangle size={14} />
              Out of Sync
            </motion.span>
          )}
        </div>
      </div>
      <motion.button
        whileHover={{ rotate: 180, scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        onClick={fetchHealth}
        disabled={loading}
        className={`flex items-center gap-4 px-8 py-4 bg-white/10 backdrop-blur-xl text-white font-black text-sm uppercase tracking-widest rounded-[1.25rem] hover:bg-white hover:text-brand-900 transition-all border border-white/10 hover:border-white shadow-premium group/btn ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
      >
        <RefreshCw size={20} className={`transition-colors ${loading ? 'animate-spin' : ''}`} />
        Sync Nodes
      </motion.button>
    </div>
  );
};
