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
  const borderRadius = variant === 'circular' ? '9999px' : variant === 'text' ? '6px' : '16px';

  return (
    <div
      className={`relative overflow-hidden bg-brand-100/80 border border-brand-200/20 ${className}`}
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
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut"
        }}
        style={{
          background: 'linear-gradient(90deg, transparent 0%, rgba(255, 255, 255, 0.6) 50%, transparent 100%)',
        }}
      />
    </div>
  );
};

export default Skeleton;
