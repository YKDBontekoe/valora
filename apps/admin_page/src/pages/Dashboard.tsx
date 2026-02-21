import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { Stats, SystemStatus } from '../types';
import { Users, Bell, TrendingUp, AlertCircle, Activity, Database, Server } from 'lucide-react';
import { motion } from 'framer-motion';
import Skeleton from '../components/Skeleton';
import Button from '../components/Button';

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1
    }
  }
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const Dashboard = () => {
  const [stats, setStats] = useState<Stats | null>(null);
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const results = await Promise.allSettled([
        adminService.getStats(),
        adminService.getSystemStatus()
      ]);

      if (results[0].status === 'fulfilled') {
        setStats(results[0].value);
      } else {
        console.error('Failed to fetch stats:', results[0].reason);
        setError('Failed to fetch dashboard statistics.');
      }

      if (results[1].status === 'fulfilled') {
        setSystemStatus(results[1].value);
      } else {
        console.error('Failed to fetch system status:', results[1].reason);
        // We don't set global error here to allow stats to show even if system status fails
      }

    } catch {
      setError('An unexpected error occurred.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const cards = [
    {
      title: 'Total Users',
      value: stats?.totalUsers || 0,
      icon: Users,
      color: 'text-info-600',
      bg: 'bg-info-50',
      gradient: 'from-info-50/50 to-white'
    },
    {
      title: 'Notifications',
      value: stats?.totalNotifications || 0,
      icon: Bell,
      color: 'text-primary-600',
      bg: 'bg-primary-50',
      gradient: 'from-primary-50/50 to-white'
    },
  ];

  return (
    <div>
      <div className="mb-12">
        <h1 className="text-4xl font-black text-brand-900 tracking-tight">Dashboard</h1>
        <div className="flex items-center gap-2 text-brand-500 mt-2 font-medium">
          <TrendingUp className="h-4 w-4 text-success-500" />
          <span>Welcome back, here's what's happening today.</span>
        </div>
      </div>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3"
      >
        {loading ? (
          [1, 2].map((i) => (
            <div key={i} className="bg-white p-8 rounded-2xl border border-brand-100 shadow-premium">
              <div className="flex items-center">
                <Skeleton variant="rectangular" width={64} height={64} className="rounded-2xl" />
                <div className="ml-6 space-y-2 flex-1">
                  <Skeleton variant="text" width="40%" height={12} />
                  <Skeleton variant="text" width="80%" height={32} />
                </div>
              </div>
            </div>
          ))
        ) : error ? (
            <div className="col-span-full p-8 bg-error-50 border border-error-100 rounded-3xl text-error-700 font-bold flex flex-col items-center justify-center shadow-sm text-center">
                 <AlertCircle className="w-12 h-12 text-error-400 mb-4" />
                 <h3 className="text-lg text-brand-900 font-black mb-2">Unable to Load Dashboard</h3>
                 <p className="text-brand-500 font-medium mb-6">{error}</p>
                 <Button onClick={fetchData} variant="outline" className="border-error-200 text-error-700 hover:bg-error-100">
                    Retry Connection
                 </Button>
            </div>
        ) : (
          cards.map((card) => {
            const Icon = card.icon;
            return (
              <motion.div
                key={card.title}
                variants={item}
                whileHover={{ y: -8, scale: 1.02, transition: { duration: 0.2 } }}
                className={`bg-linear-to-br ${card.gradient} overflow-hidden shadow-premium hover:shadow-premium-lg rounded-2xl transition-all duration-300 border border-brand-100/50 group cursor-default`}
              >
                <div className="p-8">
                  <div className="flex items-center">
                    <div className={`flex-shrink-0 ${card.bg} rounded-2xl p-4 transition-all duration-300 group-hover:scale-110 group-hover:shadow-lg group-hover:shadow-brand-200/50`}>
                      <Icon className={`h-8 w-8 ${card.color}`} />
                    </div>
                    <div className="ml-6 w-0 flex-1">
                      <dl>
                        <dt className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em] mb-2">{card.title}</dt>
                        <dd className="text-4xl font-black text-brand-900 leading-none tracking-tight flex items-baseline gap-2">
                          <motion.span
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ delay: 0.2, duration: 0.5 }}
                          >
                            {card.value.toLocaleString()}
                          </motion.span>
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </motion.div>
            );
          })
        )}
      </motion.div>

      {/* System Status Section */}
      <motion.div
        variants={item}
        initial="hidden"
        animate="show"
        transition={{ delay: 0.4 }}
        className="mt-12 p-8 bg-white rounded-3xl border border-brand-100 shadow-premium relative overflow-hidden"
      >
        <div className="absolute top-0 right-0 p-8 opacity-5">
            <Activity size={160} />
        </div>
        <h2 className="text-xl font-black text-brand-900 mb-6 flex items-center gap-2">
          <Server className="w-5 h-5 text-brand-400" />
          System Overview
        </h2>

        {loading ? (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                {[1, 2, 3].map((i) => (
                    <div key={i} className="space-y-2">
                        <Skeleton variant="text" width="40%" height={10} />
                        <div className="flex items-center gap-2">
                            <Skeleton variant="circular" width={8} height={8} />
                            <Skeleton variant="text" width="60%" height={20} />
                        </div>
                    </div>
                ))}
            </div>
        ) : systemStatus ? (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8 relative z-10">
                <div className="space-y-2">
                    <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-1">
                        <Database className="w-3 h-3" />
                        Database Status
                    </span>
                    <div className="flex items-center gap-2">
                        <div className={`w-2 h-2 rounded-full animate-pulse ${systemStatus.dbConnectivity === 'Connected' ? 'bg-success-500' : 'bg-error-500'}`} />
                        <span className={`font-bold ${systemStatus.dbConnectivity === 'Connected' ? 'text-brand-700' : 'text-error-700'}`}>
                            {systemStatus.dbConnectivity}
                        </span>
                    </div>
                </div>
                <div className="space-y-2">
                    <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-1">
                        <Activity className="w-3 h-3" />
                        Response Time
                    </span>
                    <div className="flex items-center gap-2">
                        <span className="font-bold text-brand-700">{systemStatus.dbLatencyMs.toFixed(1)}ms</span>
                        <span className={`text-xs font-bold px-2 py-0.5 rounded-md ${
                            systemStatus.dbLatencyMs < 100 ? 'text-success-600 bg-success-50' :
                            systemStatus.dbLatencyMs < 300 ? 'text-warning-600 bg-warning-50' :
                            'text-error-600 bg-error-50'
                        }`}>
                            {systemStatus.dbLatencyMs < 100 ? 'Optimal' : systemStatus.dbLatencyMs < 300 ? 'Fair' : 'Slow'}
                        </span>
                    </div>
                </div>
                <div className="space-y-2">
                    <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest flex items-center gap-1">
                        <Server className="w-3 h-3" />
                        Worker Health
                    </span>
                    <div className="flex items-center gap-2">
                        <span className="font-bold text-brand-700">{systemStatus.workerHealth}</span>
                        <span className="text-xs text-brand-400 font-bold italic">
                            {systemStatus.queueDepth > 0 ? `${systemStatus.queueDepth} jobs pending` : 'Idle'}
                        </span>
                    </div>
                    {systemStatus.lastIngestionRun && (
                         <p className="text-xs text-brand-300 font-medium">
                            Last ingestion: {new Date(systemStatus.lastIngestionRun).toLocaleString()}
                         </p>
                    )}
                </div>
            </div>
        ) : (
             <div className="flex items-center gap-2 text-brand-400 italic bg-brand-50/50 p-4 rounded-xl border border-brand-100/50">
                <AlertCircle size={16} />
                <span>System status metrics currently unavailable</span>
             </div>
        )}
      </motion.div>
    </div>
  );
};

export default Dashboard;
