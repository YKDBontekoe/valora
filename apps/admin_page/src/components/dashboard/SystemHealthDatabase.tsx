import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Database } from 'lucide-react';
import Skeleton from '../Skeleton';
import type { SystemHealth as SystemHealthType } from '../../types';

interface SystemHealthDatabaseProps {
  health: SystemHealthType | null;
  loading: boolean;
  isStale: boolean;
  getStatusColor: (status: string) => string;
}

export const SystemHealthDatabase: React.FC<SystemHealthDatabaseProps> = ({ health, loading, isStale, getStatusColor }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      className="p-10 bg-white/[0.03] rounded-[2.5rem] border border-white/5 hover:border-white/20 hover:bg-white/[0.08] transition-all duration-500 group/card shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 w-24 h-24 bg-primary-500/5 blur-[40px] rounded-full group-hover/card:bg-primary-500/10 transition-colors" />
      <div className="flex items-center gap-5 mb-8">
        <div className="p-4 bg-white/10 rounded-2xl shadow-sm border border-white/10 group-hover/card:scale-110 group-hover/card:rotate-6 transition-transform">
          <Database size={26} className="text-primary-400" />
        </div>
        <span className="text-[12px] font-black text-brand-400 uppercase tracking-[0.3em]">Persistent Layer</span>
      </div>
      <AnimatePresence mode="wait">
        {loading && !health && !isStale ? (
          <div className="space-y-4">
              <Skeleton variant="text" width="80%" height={32} />
              <Skeleton variant="text" width="50%" height={16} />
          </div>
        ) : (
          <motion.div
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex flex-col gap-4"
          >
            <div className={`w-fit px-6 py-2 rounded-2xl border flex items-center gap-3 font-black text-base ${getStatusColor(health?.database ? 'Healthy' : 'Unhealthy')}`}>
              <div className={`w-2.5 h-2.5 rounded-full bg-current ${(health?.database && !loading) ? 'animate-pulse' : ''}`} />
              {health ? (health.database ? 'Operational' : 'Critical Fault') : 'Connecting...'}
            </div>
            <p className="text-brand-400 text-xs font-bold uppercase tracking-widest ml-1">Database Shard: EU-WEST-1</p>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};
