import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface SystemHealthLatencyProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
}

export const SystemHealthLatency: React.FC<SystemHealthLatencyProps> = ({ health, loading, isStale }) => {
  const getLatencyStatus = (latency: number) => {
    if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50 border-success-100 ring-4 ring-success-500/10' };
    if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50 border-warning-100 ring-4 ring-warning-500/10' };
    return { label: 'High', color: 'text-error-600 bg-error-50 border-error-100 ring-4 ring-error-500/10' };
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay: 0.1 }}
      className="p-10 bg-white/[0.03] rounded-[2.5rem] border border-white/5 hover:border-white/20 hover:bg-white/[0.08] transition-all duration-500 group/card shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 w-24 h-24 bg-info-500/5 blur-[40px] rounded-full group-hover/card:bg-info-500/10 transition-colors" />
      <div className="flex items-center gap-5 mb-8">
        <div className="p-4 bg-white/10 rounded-2xl shadow-sm border border-white/10 group-hover/card:scale-110 group-hover/card:rotate-6 transition-transform">
          <Activity size={26} className="text-info-400" />
        </div>
        <span className="text-[12px] font-black text-brand-400 uppercase tracking-[0.3em]">Traffic Signal</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <div className="space-y-4">
              <Skeleton variant="text" width="60%" height={48} />
              <Skeleton variant="rectangular" width={100} height={28} className="rounded-full" />
          </div>
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex flex-col gap-5"
          >
            <div className="flex items-baseline gap-2">
              <span className="font-black text-white text-5xl leading-none tracking-tighter">
                  {health ? health.apiLatencyP95 : '--'}
              </span>
              <span className="text-xl text-brand-400 font-black uppercase tracking-widest">ms</span>
            </div>
            {health && (
              <span className={`w-fit text-[11px] font-black px-4 py-1.5 rounded-full uppercase tracking-wider border shadow-sm ${getLatencyStatus(health.apiLatencyP95).color}`}>
                Network: {getLatencyStatus(health.apiLatencyP95).label}
              </span>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};
