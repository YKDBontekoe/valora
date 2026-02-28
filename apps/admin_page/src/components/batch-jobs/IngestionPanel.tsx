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
        className="bg-white p-10 rounded-5xl border border-brand-100 shadow-premium relative overflow-hidden group"
    >
      <div className="absolute top-0 right-0 p-10 text-brand-50 transition-all duration-1000 group-hover:scale-125 group-hover:rotate-12 group-hover:text-primary-50/50 opacity-50">
        <Database size={240} />
      </div>
      <div className="relative z-10">
        <div className="flex items-center gap-4 mb-10">
            <div className="w-12 h-12 bg-primary-50 rounded-2xl flex items-center justify-center border border-primary-100/50 shadow-sm group-hover:scale-110 group-hover:rotate-3 transition-transform duration-500">
                <Sparkles className="text-primary-500" size={24} />
            </div>
            <h2 className="text-3xl font-black text-brand-900 tracking-tight">
              Ingestion Pipeline
            </h2>
        </div>

        <form onSubmit={handleStartJob} className="flex flex-col sm:flex-row gap-6">
          <div className="flex-1 relative group/input">
            <input
              type="text"
              placeholder="Target Municipality (e.g. Rotterdam)"
              value={targetCity}
              onChange={(e) => setTargetCity(e.target.value)}
              className="w-full px-8 py-5 bg-brand-50/50 rounded-2xl border border-brand-100 focus:ring-8 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 placeholder:text-brand-300 placeholder:font-bold shadow-sm"
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

        <div className="flex flex-col sm:flex-row items-center gap-8 mt-12 pt-10 border-t border-brand-100/50">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsDatasetModalOpen(true)}
            leftIcon={<Layers size={18} />}
            className="text-brand-500 hover:bg-brand-50 font-black px-6"
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
            className="w-full sm:w-auto border-brand-200 text-brand-500 hover:bg-brand-50 font-black px-8"
          >
            Provision All Cities
          </Button>
        </div>

        <div className="flex items-center gap-3 mt-8 p-4 bg-warning-50/50 rounded-2xl border border-warning-100/50 w-fit">
            <AlertCircle size={16} className="text-warning-600" />
            <p className="text-[10px] text-warning-700 font-black uppercase tracking-[0.2em]">
              Warning: Ingestion jobs are resource-intensive. Avoid overlapping same-city clusters.
            </p>
        </div>
      </div>
    </motion.div>
  );
};
