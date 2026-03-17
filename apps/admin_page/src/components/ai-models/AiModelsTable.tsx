import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Settings2, Cpu, ArrowUp, ArrowDown, Search, X } from 'lucide-react';
import Skeleton from '../Skeleton';
import AiModelRow from './AiModelRow';
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


interface AiModelsTableProps {
  configs: AiModelConfig[];
  loading: boolean;
  onEdit: (config: AiModelConfig) => void;
  onDelete: (config: AiModelConfig) => void;
  tableSortBy?: string;
  toggleSort?: (field: string) => void;
  hasActiveFilters?: boolean;
}

const AiModelsTable: React.FC<AiModelsTableProps> = ({ configs, loading, onEdit, onDelete, tableSortBy, toggleSort, hasActiveFilters }) => {
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
          Configurations
        </h2>
        <div className="flex items-center gap-3 bg-success-50 px-4 py-2 rounded-full border border-success-100/50">
          <span className="w-2.5 h-2.5 rounded-full bg-success-500 animate-pulse" />
          <span className="text-[10px] font-black text-success-700 uppercase tracking-widest">Active State</span>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-brand-100">
          <thead>
            <tr className="bg-brand-50/10">
              <th
                role="columnheader"
                className="px-10 py-5 text-left cursor-pointer group hover:bg-brand-50 transition-colors w-[25%]"
              >
                <button
                  type="button"
                  onClick={() => toggleSort && toggleSort('feature')}
                  className="flex items-center gap-2 w-full h-full text-[10px] font-black text-brand-400 uppercase tracking-widest outline-none focus-visible:ring-2 focus-visible:ring-primary-500 rounded-md"
                >
                  Feature
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${tableSortBy === 'feature_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${tableSortBy === 'feature_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </button>
              </th>
              <th
                role="columnheader"
                className="px-10 py-5 text-left cursor-pointer group hover:bg-brand-50 transition-colors w-[40%]"
              >
                <button
                  type="button"
                  onClick={() => toggleSort && toggleSort('modelId')}
                  className="flex items-center gap-2 w-full h-full text-[10px] font-black text-brand-400 uppercase tracking-widest outline-none focus-visible:ring-2 focus-visible:ring-primary-500 rounded-md"
                >
                  Model
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${tableSortBy === 'modelId_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${tableSortBy === 'modelId_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </button>
              </th>
              <th
                role="columnheader"
                className="px-10 py-5 text-left cursor-pointer group hover:bg-brand-50 transition-colors w-[20%]"
              >
                <button
                  type="button"
                  onClick={() => toggleSort && toggleSort('isEnabled')}
                  className="flex items-center gap-2 w-full h-full text-[10px] font-black text-brand-400 uppercase tracking-widest outline-none focus-visible:ring-2 focus-visible:ring-primary-500 rounded-md"
                >
                  Status
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${tableSortBy === 'isEnabled_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${tableSortBy === 'isEnabled_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </button>
              </th>
              <th className="px-10 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest w-[15%]">Action</th>
            </tr>
          </thead>
          <motion.tbody
            initial="hidden"
            animate="show"
            variants={container}
            className="divide-y divide-brand-100"
          >
            {loading ? (
              [...Array(3)].map((_, i) => (
                <tr key={i}>
                  <td className="px-10 py-7"><Skeleton variant="text" width="40%" height={20} /></td>
                  <td className="px-10 py-7"><Skeleton variant="text" width="60%" height={20} /></td>
                  <td className="px-10 py-7"><Skeleton variant="rectangular" width={80} height={28} className="rounded-full" /></td>
                  <td className="px-10 py-7"></td>
                </tr>
              ))
            ) : configs.length === 0 ? (
              <motion.tr
                  key="empty"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
              >
                  <td colSpan={4} className="px-10 py-32 text-center">
                      <div className="flex flex-col items-center gap-8">
                          {hasActiveFilters ? (
                              <>
                                <div className="p-10 bg-brand-50 rounded-4xl text-brand-100 border border-brand-100 shadow-inner relative">
                                    <Search className="h-20 w-20" />
                                    <div className="absolute -bottom-2 -right-2 p-3 bg-white rounded-2xl shadow-premium text-brand-300 border border-brand-100">
                                        <X size={24} />
                                    </div>
                                </div>
                                <div className="flex flex-col gap-2">
                                    <span className="text-brand-900 font-black text-2xl uppercase tracking-widest">
                                        No matching results
                                    </span>
                                    <p className="text-brand-300 font-bold max-w-sm mx-auto">
                                        We couldn't find any configuration matching your filters. Try adjusting your search query.
                                    </p>
                                </div>
                              </>
                          ) : (
                              <>
                                <Cpu size={64} className="text-brand-200 opacity-20 mb-2" />
                                <span className="font-black text-brand-200 text-xl uppercase tracking-widest">No configurations defined</span>
                              </>
                          )}
                      </div>
                  </td>
              </motion.tr>
            ) : (
              <AnimatePresence mode="popLayout">
                {configs.map((config) => (
                  <AiModelRow
                    key={config.id || (config as AiModelConfig & { _clientId?: string })._clientId || config.feature}
                    config={config}
                    onEdit={onEdit}
                    onDelete={onDelete}
                    onClick={() => onEdit(config)}
                  />
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
