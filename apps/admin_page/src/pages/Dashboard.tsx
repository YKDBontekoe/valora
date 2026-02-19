import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { Stats } from '../types';
import { Users, List, Bell, TrendingUp } from 'lucide-react';
import { motion } from 'framer-motion';
import Skeleton from '../components/Skeleton';

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
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const data = await adminService.getStats();
        setStats(data);
      } catch {
        console.error('Failed to fetch stats');
      } finally {
        setLoading(false);
      }
    };
    fetchStats();
  }, []);

  const cards = [
    { title: 'Total Users', value: stats?.totalUsers || 0, icon: Users, color: 'text-info-600', bg: 'bg-info-50', gradient: 'from-info-50/50 to-white' },
    { title: 'Total Listings', value: stats?.totalListings || 0, icon: List, color: 'text-success-600', bg: 'bg-success-50', gradient: 'from-success-50/50 to-white' },
    { title: 'Notifications', value: stats?.totalNotifications || 0, icon: Bell, color: 'text-primary-600', bg: 'bg-primary-50', gradient: 'from-primary-50/50 to-white' },
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
          [1, 2, 3].map((i) => (
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

      {/* Quick Actions / System Status Placeholder */}
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
        <h2 className="text-xl font-black text-brand-900 mb-6">System Overview</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="space-y-2">
                <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Database Status</span>
                <div className="flex items-center gap-2">
                    <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                    <span className="font-bold text-brand-700">Healthy & Connected</span>
                </div>
            </div>
            <div className="space-y-2">
                <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">API Latency</span>
                <div className="flex items-center gap-2">
                    <span className="font-bold text-brand-700">42ms</span>
                    <span className="text-xs text-success-600 font-bold bg-success-50 px-2 py-0.5 rounded-md">Optimal</span>
                </div>
            </div>
            <div className="space-y-2">
                <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Active Jobs</span>
                <div className="flex items-center gap-2">
                    <span className="font-bold text-brand-700">0</span>
                    <span className="text-xs text-brand-400 font-bold italic">No background tasks</span>
                </div>
            </div>
        </div>
      </motion.div>
    </div>
  );
};

export default Dashboard;
