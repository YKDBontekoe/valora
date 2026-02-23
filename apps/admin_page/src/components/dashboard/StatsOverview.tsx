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
  hidden: { opacity: 0, y: 30, scale: 0.9 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { type: 'spring' as const, stiffness: 300, damping: 24 }
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
              transition: { type: 'spring', stiffness: 400, damping: 12 }
            }}
            className={`bg-linear-to-br ${card.gradient} overflow-hidden shadow-premium hover:shadow-premium-xl rounded-[2.5rem] transition-all duration-500 border border-brand-100/50 group cursor-default relative`}
          >
            {/* Accent line */}
            <div className={`absolute top-0 left-0 w-full h-1 ${card.accent} opacity-0 group-hover:opacity-100 transition-opacity duration-500`} />

            <div className="p-10">
              <div className="flex flex-col gap-6">
                <div className={`w-16 h-16 ${card.bg} rounded-2xl flex items-center justify-center transition-all duration-500 group-hover:scale-110 group-hover:rotate-3 group-hover:shadow-lg group-hover:shadow-brand-200/50`}>
                  <Icon className={`h-8 w-8 ${card.color}`} />
                </div>
                <div className="flex flex-col gap-1">
                    <dt className="text-[10px] font-black text-brand-300 uppercase tracking-[0.3em]">{card.title}</dt>
                    <dd className="text-5xl font-black text-brand-900 leading-none tracking-tighter flex items-baseline gap-2">
                      <motion.span
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        transition={{ delay: 0.3, duration: 0.8 }}
                      >
                        {card.value.toLocaleString()}
                      </motion.span>
                    </dd>
                </div>
              </div>
            </div>

            {/* Subtle background pattern */}
            <div className="absolute -right-4 -bottom-4 opacity-[0.03] text-brand-900 rotate-12 transition-transform duration-700 group-hover:scale-125 group-hover:-rotate-6">
                <Icon size={120} />
            </div>
          </motion.div>
        );
      })}
    </motion.div>
  );
};

export default StatsOverview;
