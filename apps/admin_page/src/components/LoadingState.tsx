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
      staggerChildren: 0.1
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.4,
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
      className="space-y-4"
    >
      {Array.from({ length: rows }).map((_, i) => (
        <motion.div
          key={i}
          variants={itemVariants}
          className="bg-white p-6 rounded-2xl border border-brand-100 shadow-premium flex items-center gap-4"
        >
          <Skeleton variant="circular" width={40} height={40} />
          <div className="space-y-2 flex-1">
            <Skeleton variant="text" width="30%" height={16} />
            <Skeleton variant="text" width="60%" height={12} />
          </div>
        </motion.div>
      ))}
    </motion.div>
  );
};

export default LoadingState;
