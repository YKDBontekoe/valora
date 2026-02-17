import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { Stats } from '../types';
import { Users, List, Bell } from 'lucide-react';
import { motion } from 'framer-motion';

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

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
    </div>
  );

  const cards = [
    { title: 'Total Users', value: stats?.totalUsers || 0, icon: Users, color: 'text-info-600', bg: 'bg-info-50' },
    { title: 'Total Listings', value: stats?.totalListings || 0, icon: List, color: 'text-success-600', bg: 'bg-success-50' },
    { title: 'Notifications', value: stats?.totalNotifications || 0, icon: Bell, color: 'text-primary-600', bg: 'bg-primary-50' },
  ];

  return (
    <div>
      <div className="mb-10">
        <h1 className="text-3xl font-bold text-brand-900">Dashboard</h1>
        <p className="text-brand-500 mt-1">Welcome back, here's what's happening today.</p>
      </div>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3"
      >
        {cards.map((card) => {
          const Icon = card.icon;
          return (
            <motion.div
              key={card.title}
              variants={item}
              whileHover={{ y: -8, scale: 1.02, transition: { duration: 0.2 } }}
              className="bg-white overflow-hidden shadow-premium hover:shadow-premium-lg rounded-2xl transition-all duration-300 border border-brand-100/50 group"
            >
              <div className="p-8">
                <div className="flex items-center">
                  <div className={`flex-shrink-0 ${card.bg} rounded-2xl p-4 transition-all duration-300 group-hover:scale-110 group-hover:shadow-lg group-hover:shadow-brand-200/50`}>
                    <Icon className={`h-8 w-8 ${card.color}`} />
                  </div>
                  <div className="ml-6 w-0 flex-1">
                    <dl>
                      <dt className="text-[10px] font-black text-brand-400 uppercase tracking-[0.15em] mb-1.5">{card.title}</dt>
                      <dd className="text-4xl font-black text-brand-900 leading-none tracking-tight">
                        <motion.span
                          initial={{ opacity: 0 }}
                          animate={{ opacity: 1 }}
                          transition={{ delay: 0.5, duration: 0.5 }}
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
        })}
      </motion.div>
    </div>
  );
};

export default Dashboard;
