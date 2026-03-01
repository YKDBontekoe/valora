import { motion } from 'framer-motion';
import { Users, Bell, Sparkles } from 'lucide-react';
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
      staggerChildren: 0.15
    }
  }
};

const item = {
  hidden: { opacity: 0, y: 30, scale: 0.95 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { type: 'spring' as const, stiffness: 260, damping: 20 }
  }
} as const;

const StatsOverview = ({ stats, loading, error }: StatsOverviewProps) => {
  if (loading) return <LoadingState rows={1} />;
  if (error) return <ErrorState message={error} />;

  const cards = [
    {
        title: 'Total Users',
        value: stats?.totalUsers || 0,
        icon: Users,
        color: 'text-info-600',
        bg: 'bg-info-50',
        gradient: 'from-info-50/40 via-white to-white',
        accent: 'bg-info-500'
    },
    {
        title: 'Notifications',
        value: stats?.totalNotifications || 0,
        icon: Bell,
        color: 'text-primary-600',
        bg: 'bg-primary-50',
        gradient: 'from-primary-50/40 via-white to-white',
        accent: 'bg-primary-500'
    },
    {
        title: 'Active Pipelines',
        value: stats?.activeJobs || 0,
        icon: Sparkles,
        color: 'text-success-600',
        bg: 'bg-success-50',
        gradient: 'from-success-50/40 via-white to-white',
        accent: 'bg-success-500'
    },
  ];

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-3"
    >
      {cards.map((card) => {
        const Icon = card.icon;
        return (
          <motion.div
            key={card.title}
            variants={item}
            whileHover={{
              y: -10,
              scale: 1.02,
              transition: { type: 'spring', stiffness: 260, damping: 20 }
            }}
            className={`bg-linear-to-br ${card.gradient} overflow-hidden shadow-premium hover:shadow-premium-xl rounded-5xl transition-all duration-500 border border-brand-100/50 group cursor-default relative hover-border-gradient`}
          >
            {/* Top Accent line */}
            <div className={`absolute top-0 left-0 w-full h-1.5 ${card.accent} opacity-0 group-hover:opacity-100 transition-all duration-500 translate-y-[-100%] group-hover:translate-y-0`} />

            <div className="p-10 relative z-10">
              <div className="flex flex-col gap-8">
                <div className={`w-20 h-20 ${card.bg} rounded-3xl flex items-center justify-center transition-all duration-500 group-hover:scale-110 group-hover:rotate-6 group-hover:shadow-xl group-hover:shadow-brand-200/50 relative overflow-hidden`}>
                  <div className={`absolute inset-0 opacity-0 group-hover:opacity-20 bg-white transition-opacity duration-500`} />
                  <Icon className={`h-10 w-10 ${card.color} relative z-10`} />
                </div>
                <div className="flex flex-col gap-2">
                    <dt className="text-xs font-black text-brand-300 uppercase tracking-[0.25em]">{card.title}</dt>
                    <dd className="text-6xl font-black text-brand-900 leading-none tracking-tightest flex items-baseline gap-2">
                      <motion.span
                        initial={{ opacity: 0, x: -10 }}
                        animate={{ opacity: 1, x: 0 }}
                        transition={{ delay: 0.4, duration: 0.8, ease: "easeOut" }}
                      >
                        {card.value.toLocaleString()}
                      </motion.span>
                    </dd>
                </div>
              </div>
            </div>

            {/* Subtly animated background pattern */}
            <div className="absolute -right-8 -bottom-8 opacity-[0.04] text-brand-900 rotate-12 transition-all duration-1000 group-hover:scale-150 group-hover:-rotate-12 group-hover:opacity-[0.08]">
                <Icon size={180} />
            </div>
          </motion.div>
        );
      })}
    </motion.div>
  );
};

export default StatsOverview;
