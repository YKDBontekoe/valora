import { motion, animate } from 'framer-motion';
import { Users, Bell, Sparkles, TrendingUp } from 'lucide-react';
import { useEffect, useRef } from 'react';
import type { Stats } from '../../types';
import LoadingState from '../LoadingState';
import ErrorState from '../ErrorState';

interface StatsOverviewProps {
  stats: Stats | null;
  loading: boolean;
  error: string | null;
}

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.15,
      delayChildren: 0.3
    }
  }
};

const item = {
  hidden: { opacity: 0, y: 40, scale: 0.95 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { type: 'spring' as const, stiffness: 260, damping: 25 }
  }
} as const;

const CountUp = ({ value }: { value: number }) => {
  const nodeRef = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    const node = nodeRef.current;
    if (node) {
      const controls = animate(0, value, {
        duration: 2.5,
        ease: [0.22, 1, 0.36, 1],
        onUpdate(value) {
          node.textContent = Math.round(value).toLocaleString();
        },
      });
      return () => controls.stop();
    }
  }, [value]);

  return <span ref={nodeRef}>0</span>;
};

const StatsOverview = ({ stats, loading, error }: StatsOverviewProps) => {
  if (loading) return <LoadingState rows={3} />;
  if (error) return <ErrorState message={error} />;

  const cards = [
    {
        title: 'Total Users',
        value: stats?.totalUsers || 0,
        icon: Users,
        color: 'text-info-600',
        bg: 'bg-info-50',
        gradient: 'from-info-50/50 via-white/80 to-white',
        accent: 'bg-info-500',
        glow: 'shadow-info-200/40'
    },
    {
        title: 'Notifications',
        value: stats?.totalNotifications || 0,
        icon: Bell,
        color: 'text-primary-600',
        bg: 'bg-primary-50',
        gradient: 'from-primary-50/50 via-white/80 to-white',
        accent: 'bg-primary-500',
        glow: 'shadow-primary-200/40'
    },
    {
        title: 'Active Pipelines',
        value: stats?.activeJobs || 0,
        icon: Sparkles,
        color: 'text-success-600',
        bg: 'bg-success-50',
        gradient: 'from-success-50/50 via-white/80 to-white',
        accent: 'bg-success-500',
        glow: 'shadow-success-200/40'
    },
  ];

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="grid grid-cols-1 gap-12 sm:grid-cols-2 lg:grid-cols-3"
    >
      {cards.map((card) => {
        const Icon = card.icon;
        return (
          <motion.div
            key={card.title}
            variants={item}
            whileHover={{
              y: -15,
              scale: 1.02,
              transition: { type: 'spring', stiffness: 300, damping: 15 }
            }}
            className={`bg-linear-to-br ${card.gradient} overflow-hidden shadow-premium-xl hover:shadow-premium-2xl rounded-[3rem] transition-all duration-500 border border-brand-100/60 group cursor-default relative hover-border-gradient backdrop-blur-sm`}
          >
            {/* Top Accent line */}
            <div className={`absolute top-0 left-0 w-full h-2 ${card.accent} opacity-0 group-hover:opacity-100 transition-all duration-500 translate-y-[-100%] group-hover:translate-y-0`} />

            <div className="p-12 relative z-10">
              <div className="flex flex-col gap-10">
                <div className="flex items-center justify-between">
                    <div className={`w-24 h-24 ${card.bg} rounded-[2rem] flex items-center justify-center transition-all duration-700 group-hover:scale-110 group-hover:rotate-6 group-hover:shadow-premium group-hover:shadow-brand-200/50 relative overflow-hidden border border-brand-100/50`}>
                      <div className={`absolute inset-0 opacity-0 group-hover:opacity-30 bg-white transition-opacity duration-500`} />
                      <Icon className={`h-12 w-12 ${card.color} relative z-10`} />
                    </div>
                    <div className={`flex items-center gap-2 px-4 py-2 bg-brand-50 rounded-full border border-brand-100/50 opacity-0 group-hover:opacity-100 transition-all duration-500 translate-x-4 group-hover:translate-x-0`}>
                        <TrendingUp size={14} className={card.color} />
                        <span className={`text-[10px] font-black uppercase tracking-widest ${card.color}`}>Live</span>
                    </div>
                </div>
                <div className="flex flex-col gap-3">
                    <dt className="text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] ml-1">{card.title}</dt>
                    <dd className="text-7xl font-black text-brand-900 leading-none tracking-tightest flex items-baseline gap-2">
                      <CountUp value={card.value} />
                    </dd>
                </div>
              </div>
            </div>

            {/* Subtly animated background pattern */}
            <div className="absolute -right-12 -bottom-12 opacity-[0.05] text-brand-900 rotate-12 transition-all duration-1000 group-hover:scale-150 group-hover:-rotate-12 group-hover:opacity-[0.1]">
                <Icon size={260} />
            </div>
          </motion.div>
        );
      })}
    </motion.div>
  );
};

export default StatsOverview;
