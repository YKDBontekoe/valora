import { motion, AnimatePresence } from 'framer-motion';
import { TrendingUp, Server, Database, Activity, RefreshCw } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';
import Skeleton from '../Skeleton';

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const SystemHealth = () => {
  const [health, setHealth] = useState<SystemHealthType | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  const fetchHealth = async () => {
    try {
      const data = await adminService.getHealth();
      setHealth(data);
      setLastUpdated(new Date());
    } catch (error) {
      console.error('Failed to fetch system health:', error);
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
      case 'Healthy': return 'bg-success-500';
      case 'Degraded': return 'bg-warning-500';
      case 'Unhealthy': return 'bg-error-500';
      default: return 'bg-brand-400';
    }
  };

  const getLatencyStatus = (latency: number) => {
    if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50' };
    if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50' };
    return { label: 'High', color: 'text-error-600 bg-error-50' };
  };

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      transition={{ delay: 0.4 }}
      className="mt-12 p-8 bg-white rounded-[2rem] border border-brand-100 shadow-premium relative overflow-hidden group"
    >
      <div className="absolute top-0 right-0 p-8 text-brand-50 transition-transform duration-500 group-hover:scale-110 group-hover:rotate-3">
          <TrendingUp size={160} />
      </div>

      <div className="relative z-10">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl font-black text-brand-900 tracking-tight flex items-center gap-3">
              <Server className="text-primary-600" />
              System Infrastructure
            </h2>
            <p className="text-brand-400 text-sm font-bold mt-1">
              Real-time cluster performance and health monitoring.
            </p>
          </div>
          <motion.button
            whileHover={{ rotate: 180 }}
            whileTap={{ scale: 0.9 }}
            onClick={() => { setLoading(true); fetchHealth(); }}
            className="p-3 bg-brand-50 text-brand-500 rounded-xl hover:bg-primary-50 hover:text-primary-600 transition-colors"
          >
            <RefreshCw size={20} className={loading ? 'animate-spin' : ''} />
          </motion.button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Database Status */}
          <div className="p-6 bg-brand-50/50 rounded-2xl border border-brand-100/50 hover:border-primary-100 transition-colors">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-white rounded-lg shadow-sm">
                <Database size={18} className="text-primary-600" />
              </div>
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Database Engine</span>
            </div>
            <AnimatePresence mode="wait">
              {loading && !health ? (
                <Skeleton variant="text" width="80%" height={24} />
              ) : (
                <motion.div
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  className="flex items-center gap-3"
                >
                  <div className={`w-3 h-3 rounded-full ${getStatusColor(health?.status || 'Unhealthy')} shadow-lg shadow-current/20 ${health?.status === 'Healthy' ? 'animate-pulse' : ''}`} />
                  <span className="font-black text-brand-900">
                    {health?.database ? 'Healthy & Connected' : 'Connection Failed'}
                  </span>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* API Latency */}
          <div className="p-6 bg-brand-50/50 rounded-2xl border border-brand-100/50 hover:border-primary-100 transition-colors">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-white rounded-lg shadow-sm">
                <Activity size={18} className="text-primary-600" />
              </div>
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">API Response</span>
            </div>
            <AnimatePresence mode="wait">
              {loading && !health ? (
                <Skeleton variant="text" width="60%" height={24} />
              ) : (
                <motion.div
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  className="flex items-center gap-3"
                >
                  <span className="font-black text-brand-900 text-xl">{health?.apiLatency}ms</span>
                  {health && (
                    <span className={`text-[10px] font-black px-2.5 py-1 rounded-full uppercase tracking-wider ${getLatencyStatus(health.apiLatency).color}`}>
                      {getLatencyStatus(health.apiLatency).label}
                    </span>
                  )}
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Active Jobs */}
          <div className="p-6 bg-brand-50/50 rounded-2xl border border-brand-100/50 hover:border-primary-100 transition-colors">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-white rounded-lg shadow-sm">
                <TrendingUp size={18} className="text-primary-600" />
              </div>
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Parallel Tasks</span>
            </div>
            <AnimatePresence mode="wait">
              {loading && !health ? (
                <Skeleton variant="text" width="40%" height={24} />
              ) : (
                <motion.div
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  className="flex items-center gap-3"
                >
                  <span className="font-black text-brand-900 text-xl">{health?.activeJobs}</span>
                  <span className="text-xs text-brand-400 font-bold italic">
                    {health?.activeJobs === 0 ? 'Quiet workload' : 'Processing backlog'}
                  </span>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>

        <div className="mt-8 flex items-center justify-between border-t border-brand-100 pt-6">
          <div className="flex items-center gap-4">
            <div className="flex -space-x-2">
              {[1, 2, 3].map(i => (
                <div key={i} className="w-6 h-6 rounded-full bg-success-50 border-2 border-white flex items-center justify-center">
                  <div className="w-1.5 h-1.5 rounded-full bg-success-500" />
                </div>
              ))}
            </div>
            <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">All Nodes Operational</span>
          </div>
          <span className="text-[10px] font-bold text-brand-300 uppercase tracking-[0.2em]">
            Last Sync: {lastUpdated.toLocaleTimeString()}
          </span>
        </div>
      </div>
    </motion.div>
  );
};

export default SystemHealth;
