import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { TrendingUp, AlertCircle, RefreshCw, CheckCircle, Database, Activity, Clock } from 'lucide-react';
import { adminService } from '../../services/api';
import type { HealthStatus } from '../../types';
import Skeleton from '../Skeleton';

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const SystemHealth = () => {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchHealth = async () => {
    try {
      const data = await adminService.getSystemHealth();
      setHealth(data);
      setError(null);
    } catch {
      setError('Unable to fetch system health status.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHealth();
    const interval = setInterval(fetchHealth, 30000); // 30s poll
    return () => clearInterval(interval);
  }, []);

  if (error && !health) {
    return (
        <motion.div
        variants={item}
        initial="hidden"
        animate="show"
        transition={{ delay: 0.4 }}
        className="mt-12 p-8 bg-error-50 rounded-3xl border border-error-100 shadow-sm relative overflow-hidden"
      >
          <div className="flex items-center gap-4 text-error-700">
              <AlertCircle size={24} />
              <div className="flex flex-col">
                  <span className="font-bold">System Health Monitor Unavailable</span>
                  <span className="text-xs opacity-80">{error}</span>
              </div>
              <button onClick={() => { setLoading(true); fetchHealth(); }} className="ml-auto p-2 hover:bg-error-100 rounded-full transition-colors">
                  <RefreshCw size={16} />
              </button>
          </div>
      </motion.div>
    );
  }

  const isLoading = loading && !health;

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      transition={{ delay: 0.4 }}
      className="mt-12 p-8 bg-white rounded-3xl border border-brand-100 shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 p-8 opacity-5">
          <TrendingUp size={160} />
      </div>

      <div className="flex items-center justify-between mb-8 relative z-10">
        <h2 className="text-xl font-black text-brand-900">System Health</h2>
        {health && (
            <div className="flex items-center gap-2 text-[10px] font-bold text-brand-400 uppercase tracking-wider bg-brand-50 px-3 py-1.5 rounded-full border border-brand-100">
                <Clock size={12} />
                Updated: {new Date(health.timestamp).toLocaleTimeString()}
            </div>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-8 relative z-10">
          {/* Database Status */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-2">
                  <Database size={12} /> Database
              </span>
              <div className="flex items-center gap-2">
                  {isLoading ? (
                      <Skeleton variant="text" width={100} />
                  ) : (
                      <>
                        <div className={`w-2 h-2 rounded-full animate-pulse ${health?.databaseStatus === 'Connected' ? 'bg-success-500' : 'bg-error-500'}`} />
                        <span className={`font-bold ${health?.databaseStatus === 'Connected' ? 'text-brand-700' : 'text-error-600'}`}>
                            {health?.databaseStatus === 'Connected' ? 'Connected' : 'Disconnected'}
                        </span>
                      </>
                  )}
              </div>
          </div>

          {/* API Latency */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-2">
                  <Activity size={12} /> API Latency
              </span>
              <div className="flex items-center gap-2">
                  {isLoading ? (
                       <Skeleton variant="text" width={80} />
                  ) : (
                      <>
                        <span className="font-bold text-brand-700">{health?.apiLatencyMs}ms</span>
                        <span className={`text-xs font-bold px-2 py-0.5 rounded-md ${
                            (health?.apiLatencyMs || 0) < 100 ? 'text-success-600 bg-success-50' :
                            (health?.apiLatencyMs || 0) < 500 ? 'text-warning-600 bg-warning-50' : 'text-error-600 bg-error-50'
                        }`}>
                            {(health?.apiLatencyMs || 0) < 100 ? 'Optimal' : (health?.apiLatencyMs || 0) < 500 ? 'Fair' : 'High'}
                        </span>
                      </>
                  )}
              </div>
          </div>

          {/* Active Jobs */}
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-2">
                  <RefreshCw size={12} /> Active Jobs
              </span>
              <div className="flex items-center gap-2">
                  {isLoading ? (
                      <Skeleton variant="text" width={60} />
                  ) : (
                      <>
                        <span className="font-bold text-brand-700">{health?.activeJobs}</span>
                        {health && health.queuedJobs > 0 && (
                             <span className="text-xs text-brand-400 font-bold">({health.queuedJobs} queued)</span>
                        )}
                        {health && health.failedJobs > 0 && (
                             <span className="text-xs text-error-500 font-bold bg-error-50 px-1.5 py-0.5 rounded ml-1">!{health.failedJobs}</span>
                        )}
                      </>
                  )}
              </div>
          </div>

           {/* Last Success */}
           <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-2">
                  <CheckCircle size={12} /> Last Pipeline
              </span>
              <div className="flex items-center gap-2">
                  {isLoading ? (
                      <Skeleton variant="text" width={120} />
                  ) : (
                      <span className="font-bold text-brand-700 text-sm">
                          {health?.lastPipelineSuccess ? new Date(health.lastPipelineSuccess).toLocaleString() : 'No recent success'}
                      </span>
                  )}
              </div>
          </div>
      </div>
    </motion.div>
  );
};

export default SystemHealth;
