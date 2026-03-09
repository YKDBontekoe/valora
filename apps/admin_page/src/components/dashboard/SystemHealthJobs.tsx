import { motion, AnimatePresence } from 'framer-motion';
import { Clock, Activity, AlertTriangle } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface SystemHealthJobsProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
}

export const SystemHealthJobs: React.FC<SystemHealthJobsProps> = ({ health, loading, isStale }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay: 0.2 }}
      className="p-10 bg-white/[0.03] rounded-[2.5rem] border border-white/5 hover:border-white/20 hover:bg-white/[0.08] transition-all duration-500 group/card shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 w-24 h-24 bg-success-500/5 blur-[40px] rounded-full group-hover/card:bg-success-500/10 transition-colors" />
      <div className="flex items-center gap-5 mb-8">
        <div className="p-4 bg-white/10 rounded-2xl shadow-sm border border-white/10 group-hover/card:scale-110 group-hover/card:rotate-6 transition-transform">
          <Clock size={26} className="text-success-400" />
        </div>
        <span className="text-[12px] font-black text-brand-400 uppercase tracking-[0.3em]">Job Concurrency</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <div className="grid grid-cols-3 gap-4">
              <Skeleton variant="rectangular" width="100%" height={40} className="rounded-xl" />
              <Skeleton variant="rectangular" width="100%" height={40} className="rounded-xl" />
              <Skeleton variant="rectangular" width="100%" height={40} className="rounded-xl" />
          </div>
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex flex-col gap-6"
          >
            <div className="flex items-center justify-between">
              <div className="flex flex-col group/stat" title="Active">
                  <span className="text-[10px] font-black text-primary-400 uppercase tracking-[0.2em] mb-2">Live</span>
                  <div className="flex items-center gap-2">
                      <Activity size={16} className="text-primary-500" />
                      <span className="font-black text-white text-2xl leading-none">{health ? health.activeJobs : '-'}</span>
                  </div>
              </div>
              <div className="w-px h-10 bg-white/10" />
              <div className="flex flex-col group/stat" title="Queued">
                  <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em] mb-2">Wait</span>
                  <div className="flex items-center gap-2">
                      <Clock size={16} className="text-brand-300" />
                      <span className="font-bold text-brand-100 text-2xl leading-none">{health ? health.queuedJobs : '-'}</span>
                  </div>
              </div>
              <div className="w-px h-10 bg-white/10" />
              <div className="flex flex-col group/stat" title="Failed">
                  <span className="text-[10px] font-black text-error-400 uppercase tracking-[0.2em] mb-2">Fault</span>
                  <div className="flex items-center gap-2">
                       <AlertTriangle size={16} className="text-error-500" />
                      <span className="font-black text-error-100 text-2xl leading-none">{health ? health.failedJobs : '-'}</span>
                  </div>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};
