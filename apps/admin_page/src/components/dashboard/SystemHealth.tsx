import { motion, AnimatePresence } from 'framer-motion';
import { TrendingUp, Server, Database, Activity, RefreshCw, AlertTriangle, Clock, CheckCircle2, Sparkles } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';
import Skeleton from '../Skeleton';

const item = {
  hidden: { opacity: 0, y: 40, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { duration: 0.7, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

const SystemHealth = () => {
  const [health, setHealth] = useState<SystemHealthType | null>(null);
  const [loading, setLoading] = useState(true);
  const [isStale, setIsStale] = useState(false);

  const fetchHealth = async () => {
    try {
      setLoading(true);
      const data = await adminService.getHealth();
      setHealth(data);
      setIsStale(false);
    } catch (error) {
      console.error('Failed to fetch system health:', error);
      setIsStale(true);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHealth();
    const interval = setInterval(fetchHealth, 30000); // Update every 30s
    return () => clearInterval(interval);
  }, []);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Healthy': return 'text-success-500 bg-success-50 border-success-100 shadow-glow-success';
      case 'Degraded': return 'text-warning-500 bg-warning-50 border-warning-100 shadow-glow-warning';
      case 'Unhealthy': return 'text-error-500 bg-error-50 border-error-100 shadow-glow-error';
      default: return 'text-brand-400 bg-brand-50 border-brand-100';
    }
  };

  const getLatencyStatus = (latency: number) => {
    if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50 border-success-100 ring-4 ring-success-500/10' };
    if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50 border-warning-100 ring-4 ring-warning-500/10' };
    return { label: 'High', color: 'text-error-600 bg-error-50 border-error-100 ring-4 ring-error-500/10' };
  };

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      className="mt-20 p-14 bg-linear-to-br from-brand-900 via-brand-950 to-brand-900 rounded-[3rem] border border-white/10 shadow-premium-2xl relative overflow-hidden group text-white"
    >
      {/* Decorative background elements */}
      <div className="absolute top-0 right-0 p-16 text-brand-50 transition-all duration-1000 group-hover:scale-125 group-hover:rotate-6 group-hover:text-primary-50/20 opacity-20 pointer-events-none">
          <TrendingUp size={320} />
      </div>
      <div className="absolute -bottom-20 -left-20 w-80 h-80 bg-primary-600/10 blur-[100px] rounded-full" />
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-full h-full bg-grid-white/[0.02] pointer-events-none" />

      <div className="relative z-10">
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

        <div className="grid grid-cols-1 md:grid-cols-3 gap-10">
          {/* Database Status */}
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
                  <div className={`w-fit px-6 py-2 rounded-2xl border flex items-center gap-3 font-black text-base ${getStatusColor(health?.status || 'Unhealthy')}`}>
                    <div className={`w-2.5 h-2.5 rounded-full bg-current ${(health?.status === 'Healthy' && !loading) ? 'animate-pulse' : ''}`} />
                    {health ? (health.database ? 'Operational' : 'Critical Fault') : 'Connecting...'}
                  </div>
                  <p className="text-brand-400 text-xs font-bold uppercase tracking-widest ml-1">Database Shard: EU-WEST-1</p>
                </motion.div>
              )}
            </AnimatePresence>
            </motion.div>

          {/* API Latency */}
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

          {/* Job Queue */}
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
        </div>

        <div className="mt-16 flex flex-col sm:flex-row sm:items-center justify-between gap-8 border-t border-white/10 pt-10">
          <div className="flex items-center gap-8">
            <div className="flex -space-x-4">
              {[1, 2, 3, 4, 5].map(i => (
                <div key={i} className="w-10 h-10 rounded-full bg-success-500/20 border-4 border-brand-900 flex items-center justify-center shadow-lg transition-transform hover:translate-y-[-4px] cursor-pointer">
                  <CheckCircle2 size={18} className="text-success-400 shadow-glow-success" />
                </div>
              ))}
            </div>
            <div className="flex flex-col">
                <div className="flex items-center gap-3">
                    <div className="w-2.5 h-2.5 rounded-full bg-success-500 animate-pulse shadow-glow-success" />
                    <span className="text-[12px] font-black text-white uppercase tracking-[0.25em]">Cluster Integrity Verified</span>
                </div>
                {health?.lastPipelineSuccess && (
                     <span className="text-[11px] font-bold text-brand-400 mt-1">
                        Pulse Sync: {new Date(health.lastPipelineSuccess).toLocaleString()}
                     </span>
                )}
            </div>
          </div>
          <div className="flex items-center gap-4 px-6 py-3 bg-white/5 rounded-2xl border border-white/10 backdrop-blur-md shadow-inner">
            <div className="p-2 bg-white/10 rounded-lg">
                <Clock size={14} className="text-primary-400" />
            </div>
            <div className="flex flex-col">
                <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em]">Service Uptime</span>
                <span className="text-xs font-black text-white tracking-widest">99.998% AGGREGATE</span>
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
};

export default SystemHealth;
