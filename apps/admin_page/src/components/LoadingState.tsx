import { motion } from 'framer-motion';
import Skeleton from './Skeleton';

interface LoadingStateProps {
  rows?: number;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 15, scale: 0.98 },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      duration: 0.6,
      ease: [0.22, 1, 0.36, 1] as const
    }
  }
};

const LoadingState = ({ rows = 3 }: LoadingStateProps) => {
  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"
    >
      {Array.from({ length: rows }).map((_, i) => (
        <motion.div
          key={i}
          variants={itemVariants}
          className="bg-white p-8 rounded-[2rem] border border-brand-100 shadow-premium flex flex-col gap-6 relative overflow-hidden group"
        >
          {/* Animated shimmer for the background of the card */}
          <div className="absolute inset-0 bg-linear-to-r from-transparent via-brand-50/30 to-transparent -translate-x-full animate-shimmer" />

          <div className="flex items-center gap-5 relative z-10">
            <Skeleton variant="circular" width={64} height={64} className="rounded-2xl" />
            <div className="space-y-3 flex-1">
              <Skeleton variant="text" width="40%" height={20} />
              <Skeleton variant="text" width="70%" height={14} />
            </div>
          </div>
          <div className="pt-4 border-t border-brand-50 relative z-10">
            <Skeleton variant="rectangular" width="100%" height={48} className="rounded-xl" />
          </div>
        </motion.div>
      ))}
    </motion.div>
  );
};

export default LoadingState;
