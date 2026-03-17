import { forwardRef } from 'react';
import { motion, type HTMLMotionProps } from 'framer-motion';
import { Sparkles, Edit2, Trash2 } from 'lucide-react';
import Button from '../Button';
import type { AiModelConfig } from '../../services/api';

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  show: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

interface AiModelRowProps extends Omit<HTMLMotionProps<"tr">, 'onAnimationStart' | 'onDragStart' | 'onDragEnd' | 'onDrag'> {
  config: AiModelConfig;
  onEdit: (config: AiModelConfig) => void;
  onDelete: (config: AiModelConfig) => void;
}

const AiModelRow = forwardRef<HTMLTableRowElement, AiModelRowProps>(({ config, onEdit, onDelete, onClick, ...props }, ref) => {
  return (
    <motion.tr
      ref={ref}
      variants={rowVariants}
      whileHover={{ x: 10, backgroundColor: 'var(--color-brand-50)' }}
      className="group cursor-pointer relative"
      onClick={onClick}
      {...props}
    >
      <td className="px-10 py-6 whitespace-nowrap">
        <div className="flex items-center gap-4">
          <div className="p-2.5 bg-primary-50 rounded-xl text-primary-600 shadow-sm border border-primary-100/50">
            <Sparkles size={16} />
          </div>
          <span className="text-sm font-black text-brand-900 group-hover:text-primary-700 transition-colors">{config.feature}</span>
        </div>
      </td>
      <td className="px-10 py-6 whitespace-nowrap">
        <span className="text-[11px] font-black text-brand-600 font-mono bg-white border border-brand-100 px-3 py-1.5 rounded-lg shadow-sm">
          {config.modelId}
        </span>
      </td>
      <td className="px-10 py-6 whitespace-nowrap">
        <span className={`px-4 py-1.5 rounded-full text-[10px] font-black uppercase tracking-widest flex items-center gap-2 w-fit border ${
          config.isEnabled
            ? 'bg-success-50 text-success-700 border-success-100 shadow-sm shadow-success-100/50'
            : 'bg-brand-50 text-brand-400 border-brand-100 opacity-60'
        }`}>
          <div className={`w-2 h-2 rounded-full ${config.isEnabled ? 'bg-success-500 animate-pulse' : 'bg-brand-300'}`} />
          {config.isEnabled ? 'Active' : 'Offline'}
        </span>
      </td>
      <td className="px-10 py-6 whitespace-nowrap text-right">
        <div className="flex items-center justify-end gap-3">
          <Button
            onClick={(e) => { e.stopPropagation(); onEdit(config); }}
            variant="ghost"
            size="sm"
            leftIcon={<Edit2 size={16} />}
            className="text-brand-300 hover:text-primary-600 hover:bg-primary-50"
          >
            Modify
          </Button>
          <Button
            aria-label={`Delete ${config.feature} configuration`}
            title="Delete Configuration"
            onClick={(e) => { e.stopPropagation(); onDelete(config); }}
            variant="ghost"
            size="sm"
            className="text-brand-300 hover:text-error-600 hover:bg-error-50 px-2"
          >
            <Trash2 size={16} />
          </Button>
        </div>
      </td>
    </motion.tr>
  );
});

AiModelRow.displayName = 'AiModelRow';

export default AiModelRow;
