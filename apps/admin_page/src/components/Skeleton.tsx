import { motion } from 'framer-motion';

interface SkeletonProps {
  className?: string;
  width?: string | number;
  height?: string | number;
  variant?: 'rectangular' | 'circular' | 'text';
}

const Skeleton = ({
  className = '',
  width,
  height,
  variant = 'rectangular'
}: SkeletonProps) => {
  const borderRadius = variant === 'circular' ? '9999px' : variant === 'text' ? '4px' : '12px';

  return (
    <motion.div
      initial={{ opacity: 0.5 }}
      animate={{ opacity: [0.5, 0.8, 0.5] }}
      transition={{
        duration: 2,
        repeat: Infinity,
        ease: "easeInOut"
      }}
      className={`relative overflow-hidden bg-brand-100 ${className}`}
      style={{
        width: width ?? '100%',
        height: height ?? (variant === 'text' ? '1em' : '100%'),
        borderRadius
      }}
    >
      <motion.div
        className="absolute inset-0"
        animate={{
          x: ['-100%', '100%']
        }}
        transition={{
          duration: 1.5,
          repeat: Infinity,
          ease: "linear"
        }}
        style={{
          background: 'linear-gradient(90deg, transparent 0%, rgba(255, 255, 255, 0.4) 50%, transparent 100%)',
        }}
      />
    </motion.div>
  );
};

export default Skeleton;
