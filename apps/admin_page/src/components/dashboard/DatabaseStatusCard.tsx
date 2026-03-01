import { motion, AnimatePresence } from 'framer-motion';
import { Database } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface DatabaseStatusCardProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
}

const getStatusColor = (status: string) => {
  switch (status) {
    case 'Healthy': return 'text-success-500 bg-success-50 border-success-100';
    case 'Degraded': return 'text-warning-500 bg-warning-50 border-warning-100';
    case 'Unhealthy': return 'text-error-500 bg-error-50 border-error-100';
    default: return 'text-brand-400 bg-brand-50 border-brand-100';
  }
};

const DatabaseStatusCard = ({ health, loading, isStale }: DatabaseStatusCardProps) => {
  return (
    <div className="p-8 bg-brand-50/30 rounded-4xl border border-brand-100/50 hover:border-primary-200 hover:bg-white transition-all duration-500 group/card shadow-sm hover:shadow-premium-lg">
      <div className="flex items-center gap-4 mb-6">
        <div className="p-3 bg-white rounded-2xl shadow-sm border border-brand-100 group-hover/card:scale-110 group-hover/card:rotate-3 transition-transform">
          <Database size={22} className="text-primary-600" />
        </div>
        <span className="text-[11px] font-black text-brand-400 uppercase tracking-[0.2em]">Database</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <Skeleton variant="text" width="80%" height={28} />
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex items-center gap-3"
          >
            <div className={`px-4 py-1.5 rounded-full border flex items-center gap-2 font-black text-sm ${getStatusColor(health?.status || 'Unhealthy')}`}>
              <div className={`w-2 h-2 rounded-full bg-current ${(health?.status === 'Healthy' && !loading) ? 'animate-pulse' : ''}`} />
              {health ? (health.database ? 'Operational' : 'Fault Detected') : 'Unknown'}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default DatabaseStatusCard;
