import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Settings2, Cpu, Sparkles, Edit2 } from 'lucide-react';
import Skeleton from '../Skeleton';
import Button from '../Button';
import type { AiModelConfig } from '../../services/api';

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1
    }
  }
} as const;

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

interface AiModelsTableProps {
  configs: AiModelConfig[];
  loading: boolean;
  onEdit: (config: AiModelConfig) => void;
}

const AiModelsTable: React.FC<AiModelsTableProps> = ({ configs, loading, onEdit }) => {
  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="bg-white rounded-[2.5rem] border border-brand-100 shadow-premium overflow-hidden"
    >
      <div className="px-10 py-8 border-b border-brand-100 bg-brand-50/30 flex items-center justify-between">
        <h2 className="font-black text-brand-900 uppercase tracking-[0.2em] text-xs flex items-center gap-3">
          <Settings2 size={18} className="text-primary-600" />
          Routing Configurations
        </h2>
        <div className="flex items-center gap-3 bg-success-50 px-4 py-2 rounded-full border border-success-100/50">
          <span className="w-2.5 h-2.5 rounded-full bg-success-500 animate-pulse" />
          <span className="text-[10px] font-black text-success-700 uppercase tracking-widest">Active Policy: Dynamic</span>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-brand-100">
          <thead>
            <tr className="bg-brand-50/10">
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Intent Key</th>
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Primary Model</th>
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Fallback Stack</th>
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Status</th>
              <th className="px-10 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest">Action</th>
            </tr>
          </thead>
          <motion.tbody
            initial="hidden"
            animate="visible"
            variants={container}
            className="divide-y divide-brand-100"
          >
            {loading ? (
              [...Array(3)].map((_, i) => (
                <tr key={i}>
                  <td className="px-10 py-7"><Skeleton variant="text" width="40%" height={20} /></td>
                  <td className="px-10 py-7"><Skeleton variant="text" width="60%" height={20} /></td>
                  <td className="px-10 py-7"><Skeleton variant="text" width="50%" height={20} /></td>
                  <td className="px-10 py-7"><Skeleton variant="rectangular" width={80} height={28} className="rounded-full" /></td>
                  <td className="px-10 py-7"></td>
                </tr>
              ))
            ) : configs.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-10 py-32 text-center">
                  <div className="flex flex-col items-center gap-6 text-brand-200">
                    <Cpu size={64} className="opacity-10 mb-2" />
                    <span className="font-black text-xl uppercase tracking-widest">No custom policies defined</span>
                  </div>
                </td>
              </tr>
            ) : (
              <AnimatePresence mode="popLayout">
                {configs.map((config) => (
                  <motion.tr
                    key={config.id || (config as any)._clientId || config.intent}
                    variants={rowVariants}
                    whileHover={{ scale: 1.002, backgroundColor: 'var(--color-brand-50)' }}
                    className="group cursor-pointer relative"
                    onClick={() => onEdit(config)}
                  >
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex items-center gap-4">
                        <div className="p-2.5 bg-primary-50 rounded-xl text-primary-600 shadow-sm border border-primary-100/50">
                          <Sparkles size={16} />
                        </div>
                        <span className="text-sm font-black text-brand-900 group-hover:text-primary-700 transition-colors">{config.intent}</span>
                      </div>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap">
                      <span className="text-[11px] font-black text-brand-600 font-mono bg-white border border-brand-100 px-3 py-1.5 rounded-lg shadow-sm">
                        {config.primaryModel}
                      </span>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex flex-wrap gap-2">
                        {config.fallbackModels.length > 0 ? (
                          config.fallbackModels.map((m, idx) => (
                            <span key={idx} className="text-[10px] font-black text-brand-400 border border-brand-100 px-2 py-1 rounded-md bg-brand-50/50">
                              {m}
                            </span>
                          ))
                        ) : (
                          <span className="text-[10px] text-brand-200 font-black uppercase tracking-widest italic">Default Stack</span>
                        )}
                      </div>
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
                      <Button
                        onClick={(e) => { e.stopPropagation(); onEdit(config); }}
                        variant="ghost"
                        size="sm"
                        leftIcon={<Edit2 size={16} />}
                        className="text-brand-300 hover:text-primary-600 hover:bg-primary-50"
                      >
                        Modify
                      </Button>
                    </td>
                  </motion.tr>
                ))}
              </AnimatePresence>
            )}
          </motion.tbody>
        </table>
      </div>
    </motion.div>
  );
};

export default AiModelsTable;
