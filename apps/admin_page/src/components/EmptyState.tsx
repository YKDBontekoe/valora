import type { ElementType } from 'react';
import { PackageOpen } from 'lucide-react';
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
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="flex flex-col items-center justify-center p-16 text-center bg-white border border-brand-100 rounded-[2.5rem] shadow-premium relative overflow-hidden group"
    >
      <div className="absolute inset-0 bg-linear-to-b from-brand-50/50 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-700" />

      <div className="relative z-10 flex flex-col items-center">
          <div className="w-24 h-24 bg-brand-50 rounded-[2rem] flex items-center justify-center mb-8 transition-transform duration-700 group-hover:scale-110 group-hover:rotate-6 shadow-inner">
            <Icon className="w-10 h-10 text-brand-200" />
          </div>
          <h3 className="text-2xl font-black text-brand-900 mb-3 tracking-tight">{title}</h3>
          <p className="text-brand-400 font-bold max-w-sm mb-10 leading-relaxed">{description}</p>
          {action && (
            <Button onClick={action.onClick} variant="primary" className="px-10 py-4 shadow-premium shadow-primary-200/50">
              {action.label}
            </Button>
          )}
      </div>
    </motion.div>
  );
};

export default EmptyState;
