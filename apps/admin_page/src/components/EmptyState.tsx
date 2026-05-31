import type { ElementType } from 'react';
import { PackageOpen, Sparkles } from 'lucide-react';
import Button from './Button';
import { motion } from 'framer-motion';

interface EmptyStateProps {
  title: string;
  description: string;
  icon?: ElementType;
  action?: {
    label: string;
    onClick: () => void;
  };
}

const EmptyState = ({ title, description, icon: Icon = PackageOpen, action }: EmptyStateProps) => {
  return (
    <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 40 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 260, damping: 25 }}
        className="flex flex-col items-center justify-center p-20 text-center bg-white border border-brand-100 rounded-[3rem] shadow-premium-xl relative overflow-hidden group"
    >
      {/* Decorative dynamic background elements */}
      <div className="absolute top-[-10%] right-[-10%] w-[30%] h-[30%] rounded-full bg-primary-50/40 blur-[80px] group-hover:bg-primary-100/40 transition-colors duration-1000" />
      <div className="absolute bottom-[-10%] left-[-10%] w-[30%] h-[30%] rounded-full bg-info-50/40 blur-[80px] group-hover:bg-info-100/40 transition-colors duration-1000" />

      <div className="relative z-10 flex flex-col items-center">
          <div className="relative mb-10">
            <motion.div
              animate={{ rotate: [0, 5, -5, 0] }}
              transition={{ duration: 6, repeat: Infinity, ease: "easeInOut" }}
              className="w-28 h-28 bg-brand-50 rounded-[2.5rem] flex items-center justify-center transition-all duration-700 group-hover:scale-110 group-hover:rotate-6 shadow-inner border border-brand-100"
            >
              <Icon className="w-12 h-12 text-brand-300" />
            </motion.div>
            <motion.div
              className="absolute -top-4 -right-4 p-3 bg-white rounded-2xl shadow-premium border border-brand-100 text-warning-400"
              animate={{ scale: [1, 1.1, 1] }}
              transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
            >
              <Sparkles size={20} />
            </motion.div>
          </div>

          <h3 className="text-3xl font-black text-brand-900 mb-4 uppercase tracking-ultra-wide leading-none">
            {title}
          </h3>
          <p className="text-brand-400 font-bold max-w-sm mb-12 leading-relaxed text-lg">
            {description}
          </p>

          {action && (
            <Button
              onClick={action.onClick}
              variant="secondary"
              className="px-12 py-5 shadow-glow"
              size="lg"
            >
              {action.label}
            </Button>
          )}
      </div>
    </motion.div>
  );
};

export default EmptyState;
