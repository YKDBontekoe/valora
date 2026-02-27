import React from 'react';
import { Database, Sparkles, Play, Layers, Globe } from 'lucide-react';
import Button from '../Button';

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
    <div className="bg-white p-10 rounded-[2.5rem] border border-brand-100 shadow-premium relative overflow-hidden group">
      <div className="absolute top-0 right-0 p-10 text-brand-50 transition-transform duration-1000 group-hover:scale-125 group-hover:rotate-12 opacity-50">
        <Database size={200} />
      </div>
      <div className="relative z-10">
        <h2 className="text-2xl font-black text-brand-900 mb-8 flex items-center gap-3">
          <Sparkles className="text-primary-500" size={24} />
          Start Ingestion Pipeline
        </h2>
        <form onSubmit={handleStartJob} className="flex flex-col sm:flex-row gap-5">
          <div className="flex-1 relative group/input">
            <input
              type="text"
              placeholder="Target City (e.g. Rotterdam)"
              value={targetCity}
              onChange={(e) => setTargetCity(e.target.value)}
              className="w-full px-6 py-5 bg-brand-50/50 rounded-2xl border border-brand-100 focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 placeholder:text-brand-300 placeholder:font-bold"
              disabled={isStarting}
            />
          </div>
          <Button
            type="submit"
            variant="secondary"
            disabled={isStarting || !targetCity}
            isLoading={isStarting}
            leftIcon={!isStarting && <Play size={20} fill="currentColor" />}
            className="px-10"
          >
            Execute Pipeline
          </Button>
        </form>

        <div className="flex flex-col sm:flex-row items-center gap-6 mt-10 pt-8 border-t border-brand-100/50">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsDatasetModalOpen(true)}
            leftIcon={<Layers size={18} />}
            className="text-brand-500 hover:bg-brand-50 font-black"
          >
            View Dataset Status
          </Button>
          <div className="hidden sm:block flex-1" />
          <Button
            variant="outline"
            size="sm"
            onClick={handleIngestAll}
            disabled={isStarting}
            leftIcon={<Globe size={18} />}
            className="w-full sm:w-auto border-brand-200 text-brand-500 hover:bg-brand-50 font-black"
          >
            Ingest All Netherlands
          </Button>
        </div>

        <p className="text-[10px] text-brand-300 mt-6 font-black uppercase tracking-[0.2em]">
          Warning: Ingestion jobs are resource-intensive. Avoid overlapping same-city jobs.
        </p>
      </div>
    </div>
  );
};
