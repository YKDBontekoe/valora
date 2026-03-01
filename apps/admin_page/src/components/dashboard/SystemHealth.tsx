import { motion } from 'framer-motion';
import { TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { adminService } from '../../services/api';
import type { SystemHealth as SystemHealthType } from '../../types';
import SystemHealthHeader from './SystemHealthHeader';
import DatabaseStatusCard from './DatabaseStatusCard';
import ApiLatencyCard from './ApiLatencyCard';
import JobQueueCard from './JobQueueCard';
import SystemHealthFooter from './SystemHealthFooter';

const item = {
  hidden: { opacity: 0, y: 30, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { duration: 0.6, ease: [0.22, 1, 0.36, 1] as const }
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

  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      className="mt-12 p-10 bg-white rounded-5xl border border-brand-100 shadow-premium relative overflow-hidden group"
    >
      <div className="absolute top-0 right-0 p-12 text-brand-50 transition-all duration-700 group-hover:scale-125 group-hover:rotate-6 group-hover:text-primary-50/50 opacity-40">
          <TrendingUp size={240} />
      </div>

      <div className="relative z-10">
        <SystemHealthHeader loading={loading} isStale={isStale} fetchHealth={fetchHealth} />

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <DatabaseStatusCard health={health} loading={loading} isStale={isStale} />
          <ApiLatencyCard health={health} loading={loading} isStale={isStale} />
          <JobQueueCard health={health} loading={loading} isStale={isStale} />
        </div>

        <SystemHealthFooter health={health} />
      </div>
    </motion.div>
  );
};

export default SystemHealth;
