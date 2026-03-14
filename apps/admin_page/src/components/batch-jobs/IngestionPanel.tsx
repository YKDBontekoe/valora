import React from 'react';
import { Database, Sparkles, Play, Layers, Globe, AlertCircle } from 'lucide-react';
import Button from '../Button';
import { motion } from 'framer-motion';

interface IngestionPanelProps {
  targetCity: string;
  setTargetCity: (city: string) => void;
  handleStartJob: (e: React.FormEvent) => void;
  isStarting: boolean;
  setIsDatasetModalOpen: (isOpen: boolean) => void;
  handleIngestAll: () => void;
}

export const IngestionPanel: React.FC<IngestionPanelProps> = ({
  targetCity,
  setTargetCity,
  handleStartJob,
  isStarting,
  setIsDatasetModalOpen,
  handleIngestAll,
}) => {
  return (
    <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="bg-white p-12 rounded-5xl border border-brand-100 shadow-premium-xl relative overflow-hidden group bg-dot-slate-200"
    >
      <div className="absolute top-0 right-0 p-12 text-brand-50 transition-all duration-1000 group-hover:scale-125 group-hover:rotate-12 group-hover:text-primary-50/50 opacity-50 pointer-events-none">
        <Database size={240} />
      </div>

      {/* Decorative gradient overlay */}
      <div className="absolute inset-0 bg-linear-to-br from-white via-white/80 to-transparent pointer-events-none" />

      <div className="relative z-10">
        <div className="flex items-center gap-6 mb-12">
            <div className="w-16 h-16 bg-primary-50 rounded-2xl flex items-center justify-center border border-primary-100/50 shadow-sm group-hover:scale-110 group-hover:rotate-6 transition-transform duration-700">
                <Sparkles className="text-primary-500" size={32} />
            </div>
            <div>
                <h2 className="text-3xl font-black text-brand-900 tracking-tight uppercase">
                Ingestion Pipeline
                </h2>
                <div className="flex items-center gap-2 mt-1">
                    <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                    <span className="text-[10px] font-black uppercase tracking-ultra-wide text-brand-400">Ready for cluster synchronization</span>
                </div>
            </div>
        </div>

        <form onSubmit={handleStartJob} className="flex flex-col sm:flex-row gap-6 max-w-4xl">
          <div className="flex-1 relative group/input">
            <input
              type="text"
              placeholder="Target Municipality (e.g. Rotterdam)"
              value={targetCity}
              onChange={(e) => setTargetCity(e.target.value)}
              className="w-full px-8 py-5 bg-white rounded-2xl border border-brand-100 focus:ring-8 focus:ring-primary-500/10 focus:border-primary-500 outline-none transition-all font-black text-brand-900 placeholder:text-brand-200 placeholder:font-bold shadow-premium hover:shadow-premium-lg"
              disabled={isStarting}
            />
          </div>
          <Button
            type="submit"
            variant="secondary"
            disabled={isStarting || !targetCity}
            isLoading={isStarting}
            leftIcon={!isStarting && <Play size={20} fill="currentColor" />}
            className="px-12 py-5 shadow-glow"
          >
            Execute Sync
          </Button>
        </form>

        <div className="flex flex-col sm:flex-row items-center gap-8 mt-14 pt-12 border-t border-brand-100/50">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsDatasetModalOpen(true)}
            leftIcon={<Layers size={18} />}
            className="text-brand-500 hover:bg-brand-50 font-black px-8 py-3.5 rounded-xl transition-all duration-300 hover:scale-105"
          >
            Dataset Catalog
          </Button>
          <div className="hidden sm:block flex-1" />
          <Button
            variant="outline"
            size="sm"
            onClick={handleIngestAll}
            disabled={isStarting}
            leftIcon={<Globe size={18} />}
            className="w-full sm:w-auto border-brand-200 text-brand-500 hover:bg-brand-50 font-black px-10 py-3.5 rounded-xl transition-all duration-300 hover:scale-105 shadow-sm hover:shadow-md"
          >
            Provision All Cities
          </Button>
        </div>

        <div className="flex items-center gap-4 mt-10 p-5 bg-warning-50/50 backdrop-blur-sm rounded-2xl border border-warning-100/50 w-fit shadow-sm">
            <div className="p-2 bg-white rounded-lg shadow-sm">
                <AlertCircle size={18} className="text-warning-600" />
            </div>
            <p className="text-[10px] text-warning-700 font-black uppercase tracking-ultra-wide max-w-md leading-relaxed">
              Caution: Ingestion jobs are resource-intensive cluster operations. Avoid concurrent overlapping on same-city shards.
            </p>
        </div>
      </div>
    </motion.div>
  );
};
