import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Database, RefreshCw, AlertCircle } from 'lucide-react';
import { adminService } from '../services/api';
import type { DatasetStatus } from '../types';
import Button from '../components/Button';
import Skeleton from '../components/Skeleton';

interface DatasetStatusModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const containerVariants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.05
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 20, scale: 0.95 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { type: 'spring', stiffness: 400, damping: 25 }
  }
} as const;

const DatasetStatusModal: React.FC<DatasetStatusModalProps> = ({ isOpen, onClose }) => {
  const [data, setData] = useState<DatasetStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await adminService.getDatasetStatus();
      setData(result);
    } catch {
      setError('Failed to load dataset status.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchData();
    }
  }, [isOpen]);

  const isStale = (dateStr: string | null) => {
    if (!dateStr) return true;
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = diff / (1000 * 3600 * 24);
    return days > 30;
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 bg-brand-900/40 backdrop-blur-2xl"
          onClick={onClose}
        />
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 40 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 40 }}
          transition={{ type: "spring", damping: 25, stiffness: 300 }}
          className="relative bg-white rounded-[2.5rem] shadow-premium-xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden border border-white/20 z-10"
        >
          <div className="flex items-center justify-between px-10 py-8 border-b border-brand-100 bg-white z-10">
            <div className="flex items-center gap-5">
              <div className="p-3 bg-brand-50 rounded-2xl text-primary-600 shadow-sm border border-brand-100/50">
                <Database size={24} />
              </div>
              <div>
                <h2 className="text-2xl font-black text-brand-900 tracking-tightest">Dataset Status</h2>
                <p className="text-sm font-bold text-brand-400">Overview of ingested municipalities</p>
              </div>
            </div>
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="sm" onClick={fetchData} disabled={loading} className="w-12 h-12 rounded-xl">
                    <RefreshCw size={20} className={loading ? 'animate-spin' : ''} />
                </Button>
                <button
                  onClick={onClose}
                  className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-xl transition-all"
                >
                  <X size={24} />
                </button>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-10 bg-brand-50/30 custom-scrollbar">
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {[...Array(6)].map((_, i) => <Skeleton key={i} height={120} className="rounded-3xl" />)}
                </div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-20 text-error-500 gap-6">
                    <div className="p-6 bg-error-50 rounded-[2rem] border border-error-100 shadow-sm">
                        <AlertCircle size={48} className="opacity-50" />
                    </div>
                    <p className="font-black text-xl">{error}</p>
                    <Button variant="outline" onClick={fetchData} className="px-10">Retry Sync</Button>
                </div>
            ) : data.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-20 text-brand-300 gap-4">
                    <Database size={64} className="opacity-10" />
                    <span className="font-black text-xl uppercase tracking-widest">No Records Found</span>
                </div>
            ) : (
                <motion.div
                    variants={containerVariants}
                    initial="hidden"
                    animate="show"
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"
                >
                    {data.map((item) => (
                        <motion.div
                            key={item.city}
                            variants={itemVariants}
                            whileHover={{ y: -5, transition: { type: 'spring', stiffness: 400, damping: 20 } }}
                            className="bg-white p-6 rounded-3xl border border-brand-100 shadow-premium hover:shadow-premium-lg transition-all flex flex-col gap-4 group cursor-default relative overflow-hidden"
                        >
                            <div className="absolute top-0 left-0 w-full h-1.5 bg-linear-to-r from-transparent via-primary-500/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />

                            <div className="flex justify-between items-start relative z-10">
                                <h3 className="font-black text-lg text-brand-900 tracking-tight group-hover:text-primary-700 transition-colors">{item.city}</h3>
                                {isStale(item.lastUpdated) ? (
                                    <span className="px-2.5 py-1 rounded-lg bg-warning-50 text-warning-700 text-[9px] font-black uppercase tracking-widest border border-warning-100">Stale</span>
                                ) : (
                                    <span className="px-2.5 py-1 rounded-lg bg-success-50 text-success-700 text-[9px] font-black uppercase tracking-widest border border-success-100">Fresh</span>
                                )}
                            </div>

                            <div className="space-y-3 relative z-10">
                                <div className="flex justify-between items-center text-[10px] font-black uppercase tracking-widest">
                                    <span className="text-brand-300">Neighborhoods</span>
                                    <span className="text-brand-900 font-mono bg-brand-50 px-2 py-0.5 rounded-md">{item.neighborhoodCount}</span>
                                </div>
                                <div className="flex justify-between items-center text-[10px] font-black uppercase tracking-widest">
                                    <span className="text-brand-300">Last Sync</span>
                                    <span className="text-brand-900 font-mono">
                                        {item.lastUpdated ? new Date(item.lastUpdated).toLocaleDateString() : 'Never'}
                                    </span>
                                </div>
                            </div>

                            <div className="pt-2 relative z-10">
                                <div className="h-1.5 w-full bg-brand-50 rounded-full overflow-hidden">
                                    <motion.div
                                        initial={{ width: 0 }}
                                        animate={{ width: '100%' }}
                                        transition={{ duration: 1, ease: "circOut" }}
                                        className={`h-full rounded-full ${isStale(item.lastUpdated) ? 'bg-warning-400' : 'bg-success-400'}`}
                                    />
                                </div>
                            </div>
                        </motion.div>
                    ))}
                </motion.div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default DatasetStatusModal;
