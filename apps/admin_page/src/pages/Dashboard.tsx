import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { Stats } from '../types';
import { Sparkles } from 'lucide-react';
import { motion } from 'framer-motion';
import StatsOverview from '../components/dashboard/StatsOverview';
import SystemHealth from '../components/dashboard/SystemHealth';
import QuickActions from '../components/dashboard/QuickActions';

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2
    }
  }
};

const titleVariants = {
    hidden: { opacity: 0, x: -20 },
    show: {
        opacity: 1,
        x: 0,
        transition: { duration: 0.8, ease: [0.22, 1, 0.36, 1] }
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
    <div className="space-y-16">
      <motion.div
        variants={titleVariants}
        initial="hidden"
        animate="show"
        className="flex flex-col md:flex-row md:items-end justify-between gap-6"
      >
        <div>
          <div className="flex items-center gap-3 mb-4">
              <div className="px-3 py-1 bg-primary-100 text-primary-700 rounded-full text-[10px] font-black uppercase tracking-widest">
                  Enterprise Dashboard
              </div>
          </div>
          <h1 className="text-5xl lg:text-6xl font-black text-brand-900 tracking-tightest">Dashboard</h1>
          <div className="flex items-center gap-2 text-brand-400 mt-4 font-bold text-lg">
            <Sparkles className="h-5 w-5 text-warning-500" />
            <span>Operational overview for today.</span>
          </div>
        </div>

        <div className="flex items-center gap-4 bg-white/50 backdrop-blur-md px-6 py-4 rounded-2xl border border-brand-100 shadow-sm">
            <div className="flex flex-col items-end">
                <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Status</span>
                <span className="text-sm font-black text-success-600">All Systems Go</span>
            </div>
            <div className="w-10 h-10 bg-success-50 rounded-full flex items-center justify-center border border-success-100">
                <div className="w-2.5 h-2.5 bg-success-500 rounded-full animate-pulse" />
            </div>
        </div>
      </motion.div>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="space-y-16"
      >
        <section>
            <StatsOverview stats={stats} loading={loading} error={error} />
        </section>

        <section className="grid grid-cols-1 xl:grid-cols-2 gap-10">
            <div className="h-full">
                <QuickActions onRefreshStats={fetchStats} />
            </div>
            <div className="h-full">
                {/* We could add another component here, but for now we let SystemHealth take full width below */}
                <div className="p-10 bg-linear-to-br from-brand-900 to-brand-800 rounded-[2.5rem] shadow-premium-xl text-white relative overflow-hidden group h-full">
                    <div className="relative z-10 h-full flex flex-col justify-between">
                        <div>
                            <h2 className="text-2xl font-black tracking-tight mb-2">Platform Integrity</h2>
                            <p className="text-brand-300 font-bold">Security and access logs are monitored 24/7.</p>
                        </div>
                        <div className="mt-8">
                            <div className="flex items-center gap-3 text-sm font-black uppercase tracking-[0.2em] text-primary-400">
                                <div className="w-2 h-2 rounded-full bg-primary-500" />
                                Secure Connection
                            </div>
                        </div>
                    </div>
                    <Sparkles className="absolute -right-8 -bottom-8 text-white/5 w-64 h-64 transition-transform duration-700 group-hover:scale-110 group-hover:-rotate-12" />
                </div>
            </div>
        </section>

        <section>
            <SystemHealth />
        </section>
      </motion.div>
    </div>
  );
};

export default Dashboard;
