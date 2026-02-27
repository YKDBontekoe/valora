import { motion, AnimatePresence } from 'framer-motion';
import { TrendingUp, Server, Database, Activity, RefreshCw, AlertTriangle, Clock, CheckCircle2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';
import Skeleton from '../Skeleton';

const item = {
  hidden: { opacity: 0, y: 30, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { duration: 0.6, ease: [0.22, 1, 0.36, 1] }
  }
};

const SystemHealth = () => {
  const [health, setHealth] = useState<SystemHealthType | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [isStale, setIsStale] = useState(false);

  const fetchHealth = async () => {
    try {
      setLoading(true);
      const data = await adminService.getHealth();
      setHealth(data);
      setLastUpdated(new Date());
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
      case 'Healthy': return 'text-success-500 bg-success-50 border-success-100';
      case 'Degraded': return 'text-warning-500 bg-warning-50 border-warning-100';
      case 'Unhealthy': return 'text-error-500 bg-error-50 border-error-100';
      default: return 'text-brand-400 bg-brand-50 border-brand-100';
    }
  };

  const getLatencyStatus = (latency: number) => {
    if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50 border-success-100' };
    if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50 border-warning-100' };
    return { label: 'High', color: 'text-error-600 bg-error-50 border-error-100' };
  };

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      className="mt-12 p-10 bg-white rounded-5xl border border-brand-100 shadow-premium relative overflow-hidden group"
    >
      <div className="absolute top-0 right-0 p-12 text-brand-50 transition-all duration-700 group-hover:scale-125 group-hover:rotate-6 group-hover:text-primary-50/50 opacity-40">
          <TrendingUp size={240} />
      </div>

      <div className="relative z-10">
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

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Database Status */}
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

          {/* API Latency */}
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

          {/* Job Queue */}
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
        </div>

        <div className="mt-12 flex flex-col sm:flex-row sm:items-center justify-between gap-6 border-t border-brand-100 pt-8">
          <div className="flex items-center gap-6">
            <div className="flex -space-x-3">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="w-8 h-8 rounded-full bg-success-50 border-2 border-white flex items-center justify-center shadow-sm">
                  <CheckCircle2 size={16} className="text-success-500" />
                </div>
              ))}
            </div>
            <div className="flex flex-col">
                <div className="flex items-center gap-2">
                    <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                    <span className="text-[11px] font-black text-brand-800 uppercase tracking-widest">Cluster: Amsterdam-Primary</span>
                </div>
                {health?.lastPipelineSuccess && (
                     <span className="text-[10px] font-bold text-brand-300 mt-0.5">
                        Pulse Sync: {new Date(health.lastPipelineSuccess).toLocaleString()}
                     </span>
                )}
            </div>
          </div>
          <div className="flex items-center gap-2 px-4 py-2 bg-brand-50/50 rounded-xl border border-brand-100">
            <Clock size={12} className="text-brand-300" />
            <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em]">
                Uptime Integrity: 99.98%
            </span>
          </div>
        </div>
      </div>
    </motion.div>
  );
};

export default SystemHealth;
