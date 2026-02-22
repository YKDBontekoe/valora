import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { TrendingUp, AlertCircle, RefreshCw } from 'lucide-react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const SystemHealth = () => {
  const [health, setHealth] = useState<SystemHealthType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchHealth = async () => {
    try {
      const data = await adminService.getSystemHealth();
      setHealth(data);
      setLastUpdated(new Date());
      setError(null);
    } catch {
      setError('Unable to fetch system status');
      // Keep old data if available (stale-while-revalidate)
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHealth();
    const interval = setInterval(fetchHealth, 30000); // 30s poll
    return () => clearInterval(interval);
  }, []);

  if (loading && !health) {
      return (
        <div className="mt-12 p-8 bg-white rounded-3xl border border-brand-100 shadow-premium animate-pulse h-48" />
      );
  }

  const isDegraded = health?.status === 'degraded' || error;

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      transition={{ delay: 0.4 }}
      className={`mt-12 p-8 bg-white rounded-3xl border ${isDegraded ? 'border-orange-200' : 'border-brand-100'} shadow-premium relative overflow-hidden transition-colors duration-500`}
    >
      <div className="absolute top-0 right-0 p-8 opacity-5">
          <TrendingUp size={160} />
      </div>

      <div className="flex justify-between items-start mb-6">
          <h2 className="text-xl font-black text-brand-900">System Health</h2>
          {lastUpdated && (
              <div className="flex items-center gap-2 text-[10px] font-bold text-brand-400 uppercase tracking-widest">
                  {error && <AlertCircle size={12} className="text-orange-500" />}
                  <span>Updated {lastUpdated.toLocaleTimeString()}</span>
              </div>
          )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Database Status */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Database Status</span>
              <div className="flex items-center gap-2">
                  <div className={`w-2 h-2 rounded-full ${health?.database === 'connected' ? 'bg-success-500' : 'bg-error-500'} animate-pulse`} />
                  <span className={`font-bold ${health?.database === 'connected' ? 'text-brand-700' : 'text-error-600'}`}>
                      {health?.database === 'connected' ? 'Healthy & Connected' : 'Disconnected'}
                  </span>
              </div>
          </div>

          {/* API Latency */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">API Latency (P95)</span>
              <div className="flex items-center gap-2">
                  <span className="font-bold text-brand-700">
                      {health ? `${health.apiLatency.p95}ms` : '--'}
                  </span>
                  {health && (
                      <span className={`text-xs font-bold px-2 py-0.5 rounded-md ${
                          health.apiLatency.p95 < 200 ? 'text-success-600 bg-success-50' :
                          health.apiLatency.p95 < 500 ? 'text-orange-600 bg-orange-50' : 'text-error-600 bg-error-50'
                      }`}>
                          {health.apiLatency.p95 < 200 ? 'Optimal' : health.apiLatency.p95 < 500 ? 'Fair' : 'High'}
                      </span>
                  )}
              </div>
          </div>

          {/* Active Jobs */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Active Jobs</span>
              <div className="flex items-center gap-2">
                  <span className="font-bold text-brand-700">{health?.jobs.active ?? 0}</span>
                  {health && health.jobs.active > 0 ? (
                       <span className="text-xs text-primary-600 font-bold bg-primary-50 px-2 py-0.5 rounded-md animate-pulse">Processing</span>
                  ) : (
                       <span className="text-xs text-brand-400 font-bold italic">No background tasks</span>
                  )}
              </div>
          </div>
      </div>

      {error && (
          <div className="absolute bottom-0 left-0 w-full bg-orange-50/90 backdrop-blur-sm p-2 text-center text-xs font-bold text-orange-600 border-t border-orange-100 flex items-center justify-center gap-2">
              <RefreshCw size={12} className="animate-spin" />
              Connection unstable. Using cached data.
          </div>
      )}
    </motion.div>
  );
};

export default SystemHealth;
