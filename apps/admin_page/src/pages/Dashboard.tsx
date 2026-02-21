import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { Stats } from '../types';
import { TrendingUp } from 'lucide-react';
import { motion } from 'framer-motion';
import StatsOverview from '../components/dashboard/StatsOverview';
import SystemHealth from '../components/dashboard/SystemHealth';
import QuickActions from '../components/dashboard/QuickActions';

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1
    }
  }
};

const Dashboard = () => {
  const [stats, setStats] = useState<Stats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await adminService.getStats();
      setStats(data);
    } catch {
      setError('Failed to fetch dashboard statistics. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStats();
  }, []);

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
      >
        <StatsOverview stats={stats} loading={loading} error={error} />

        <QuickActions onRefreshStats={fetchStats} />

        <SystemHealth />
      </motion.div>
    </div>
  );
};

export default Dashboard;
