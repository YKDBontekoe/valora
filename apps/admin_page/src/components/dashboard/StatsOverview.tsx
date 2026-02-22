import { motion } from 'framer-motion';
import { Users, Bell } from 'lucide-react';
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
      staggerChildren: 0.1
    }
  }
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const StatsOverview = ({ stats, loading, error }: StatsOverviewProps) => {
  if (loading) return <LoadingState rows={1} />;
  if (error) return <ErrorState message={error} />;

  const cards = [
    { title: 'Total Users', value: stats?.totalUsers || 0, icon: Users, color: 'text-info-600', bg: 'bg-info-50', gradient: 'from-info-50/50 to-white' },
    { title: 'Notifications', value: stats?.totalNotifications || 0, icon: Bell, color: 'text-primary-600', bg: 'bg-primary-50', gradient: 'from-primary-50/50 to-white' },
  ];

  return (
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
            whileHover={{
              y: -12,
              scale: 1.02,
              transition: { type: 'spring', stiffness: 400, damping: 10 }
            }}
            className={`bg-linear-to-br ${card.gradient} overflow-hidden shadow-premium hover:shadow-premium-xl rounded-3xl transition-all duration-500 border border-brand-100/50 group cursor-default`}
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
      })}
    </motion.div>
  );
};

export default StatsOverview;
