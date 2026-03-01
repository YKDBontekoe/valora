import { motion, AnimatePresence } from 'framer-motion';
import { Clock, Activity, AlertTriangle } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface JobQueueCardProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
}

const JobQueueCard = ({ health, loading, isStale }: JobQueueCardProps) => {
  return (
    <div className="p-8 bg-brand-50/30 rounded-4xl border border-brand-100/50 hover:border-primary-200 hover:bg-white transition-all duration-500 group/card shadow-sm hover:shadow-premium-lg">
      <div className="flex items-center gap-4 mb-6">
        <div className="p-3 bg-white rounded-2xl shadow-sm border border-brand-100 group-hover/card:scale-110 group-hover/card:rotate-3 transition-transform">
          <Clock size={22} className="text-primary-600" />
        </div>
        <span className="text-[11px] font-black text-brand-400 uppercase tracking-[0.2em]">Pipelines</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <Skeleton variant="text" width="40%" height={28} />
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex flex-col gap-2"
          >
            <div className="flex items-center gap-5">
              <div className="flex flex-col" title="Active">
                  <span className="text-[10px] font-black text-primary-500 uppercase tracking-widest mb-1">Live</span>
                  <div className="flex items-center gap-1.5">
                      <Activity size={14} className="text-primary-500" />
                      <span className="font-black text-brand-900 text-xl leading-none">{health ? health.activeJobs : '-'}</span>
                  </div>
              </div>
              <div className="w-px h-8 bg-brand-200/50" />
              <div className="flex flex-col" title="Queued">
                  <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest mb-1">Wait</span>
                  <div className="flex items-center gap-1.5">
                      <Clock size={14} className="text-brand-400" />
                      <span className="font-bold text-brand-600 text-xl leading-none">{health ? health.queuedJobs : '-'}</span>
                  </div>
              </div>
              <div className="w-px h-8 bg-brand-200/50" />
              <div className="flex flex-col" title="Failed">
                  <span className="text-[10px] font-black text-error-500 uppercase tracking-widest mb-1">Fault</span>
                  <div className="flex items-center gap-1.5">
                       <AlertTriangle size={14} className="text-error-500" />
                      <span className="font-black text-error-600 text-xl leading-none">{health ? health.failedJobs : '-'}</span>
                  </div>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default JobQueueCard;
