import { motion } from 'framer-motion';
import { TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';

import { SystemHealthHeader } from './SystemHealthHeader';
import { SystemHealthDatabase } from './SystemHealthDatabase';
import { SystemHealthLatency } from './SystemHealthLatency';
import { SystemHealthJobs } from './SystemHealthJobs';
import { SystemHealthFooter } from './SystemHealthFooter';

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
        <SystemHealthHeader loading={loading} isStale={isStale} fetchHealth={fetchHealth} />

        <div className="grid grid-cols-1 md:grid-cols-3 gap-10">
          <SystemHealthDatabase health={health} loading={loading} isStale={isStale} getStatusColor={getStatusColor} />
          <SystemHealthLatency health={health} loading={loading} isStale={isStale} />
          <SystemHealthJobs health={health} loading={loading} isStale={isStale} />
        </div>

        <SystemHealthFooter health={health} isStale={isStale} />
      </div>
    </motion.div>
  );
};

export default SystemHealth;
