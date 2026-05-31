import { motion } from 'framer-motion';
import { TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';

import { item } from './system-health/utils';
import { SystemHealthHeader } from './system-health/SystemHealthHeader';
import { DatabaseStatus } from './system-health/DatabaseStatus';
import { ApiLatency } from './system-health/ApiLatency';
import { JobQueueStatus } from './system-health/JobQueueStatus';
import { SystemHealthFooter } from './system-health/SystemHealthFooter';

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
        <SystemHealthHeader isStale={isStale} loading={loading} fetchHealth={fetchHealth} />

        <div className="grid grid-cols-1 md:grid-cols-3 gap-10">
          <DatabaseStatus health={health} loading={loading} isStale={isStale} />
          <ApiLatency health={health} loading={loading} isStale={isStale} />
          <JobQueueStatus health={health} loading={loading} isStale={isStale} />
        </div>

        <SystemHealthFooter health={health} />
      </div>
    </motion.div>
  );
};

export default SystemHealth;
