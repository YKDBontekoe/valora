import { motion, AnimatePresence } from 'framer-motion';
import { Activity } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface ApiLatencyCardProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
}

const getLatencyStatus = (latency: number) => {
  if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50 border-success-100' };
  if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50 border-warning-100' };
  return { label: 'High', color: 'text-error-600 bg-error-50 border-error-100' };
};

const ApiLatencyCard = ({ health, loading, isStale }: ApiLatencyCardProps) => {
  return (
    <div className="p-8 bg-brand-50/30 rounded-4xl border border-brand-100/50 hover:border-primary-200 hover:bg-white transition-all duration-500 group/card shadow-sm hover:shadow-premium-lg">
      <div className="flex items-center gap-4 mb-6">
        <div className="p-3 bg-white rounded-2xl shadow-sm border border-brand-100 group-hover/card:scale-110 group-hover/card:rotate-3 transition-transform">
          <Activity size={22} className="text-primary-600" />
        </div>
        <span className="text-[11px] font-black text-brand-400 uppercase tracking-[0.2em]">Response</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <Skeleton variant="text" width="60%" height={28} />
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex items-end gap-3"
          >
            <span className="font-black text-brand-900 text-3xl leading-none">
              {health ? health.apiLatencyP95 : '--'}<span className="text-sm text-brand-300 ml-1">ms</span>
            </span>
            {health && (
              <span className={`text-[10px] font-black px-3 py-1 rounded-full uppercase tracking-wider border ${getLatencyStatus(health.apiLatencyP95).color}`}>
                {getLatencyStatus(health.apiLatencyP95).label}
              </span>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default ApiLatencyCard;
