import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence, type Variants } from 'framer-motion';
import { X, Database, RefreshCw, AlertCircle, Sparkles, MapPin, Calendar, Layers } from 'lucide-react';
import { adminService } from '../services/api';
import type { DatasetStatus } from '../types';
import Button from '../components/Button';
import Skeleton from '../components/Skeleton';

interface DatasetStatusModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.03,
      delayChildren: 0.1
    }
  }
};

const cardVariants: Variants = {
  hidden: { opacity: 0, y: 20, scale: 0.95 },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { type: 'spring', stiffness: 260, damping: 25 }
  }
};

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

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 40 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 40 }}
            transition={{ type: "spring", stiffness: 300, damping: 30 }}
            className="relative w-full max-w-5xl bg-white rounded-5xl shadow-premium-2xl overflow-hidden border border-white/20 flex flex-col max-h-[90vh]"
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-title"
          >
          {/* Header */}
          <div className="flex items-center justify-between px-10 py-10 border-b border-brand-100 bg-white/50 backdrop-blur-md sticky top-0 z-20">
            <div className="flex items-center gap-6">
              <div className="w-16 h-16 bg-primary-50 rounded-2xl flex items-center justify-center border border-primary-100 shadow-sm transition-all duration-700 hover:rotate-6 hover:scale-110">
                <Database size={32} className="text-primary-600" />
              </div>
              <div>
                <h2 id="modal-title" className="text-3xl font-black text-brand-900 tracking-tightest uppercase">Dataset Catalog</h2>
                <div className="flex items-center gap-2 mt-1">
                    <Sparkles size={12} className="text-primary-400" />
                    <span className="text-[10px] font-black uppercase tracking-ultra-wide text-brand-400">Inventory of ingested clusters</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-4">
                <motion.button
                  whileHover={{ rotate: 180, scale: 1.1 }}
                  whileTap={{ scale: 0.9 }}
                  onClick={fetchData}
                  disabled={loading}
                  className="p-3 bg-brand-50 rounded-xl text-brand-400 hover:text-primary-600 hover:bg-primary-50 transition-all border border-brand-100/50"
                  aria-label="Refresh data"
                >
                  <RefreshCw size={20} className={loading ? 'animate-spin' : ''} />
                </motion.button>
                <button
                  onClick={onClose}
                  className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300"
                  aria-label="Close modal"
                >
                  <X size={32} />
                </button>
            </div>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-10 bg-brand-50/20 custom-scrollbar">
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {[...Array(6)].map((_, i) => (
                        <div key={i} className="bg-white p-8 rounded-3xl border border-brand-100 shadow-sm space-y-4">
                            <Skeleton height={24} width="60%" className="rounded-lg" />
                            <div className="space-y-2">
                                <Skeleton height={16} width="80%" className="rounded-lg" />
                                <Skeleton height={16} width="40%" className="rounded-lg" />
                            </div>
                        </div>
                    ))}
                </div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-32 text-center gap-8">
                    <div className="p-10 bg-error-50 rounded-4xl text-error-200 border border-error-100 shadow-inner">
                        <AlertCircle size={80} />
                    </div>
                    <div className="flex flex-col gap-2">
                        <span className="font-black text-2xl text-brand-900 uppercase tracking-ultra-wide">Query Failure</span>
                        <p className="text-brand-400 font-bold max-w-xs">{error}</p>
                    </div>
                    <Button onClick={fetchData} variant="secondary" className="px-10 shadow-glow">Retry Sync</Button>
                </div>
            ) : data.length === 0 ? (
                <div className="text-center py-40 flex flex-col items-center gap-8">
                    <div className="p-10 bg-brand-50 rounded-4xl text-brand-100 border border-brand-100 shadow-inner">
                        <Layers size={80} />
                    </div>
                    <div>
                        <span className="text-brand-900 font-black text-2xl uppercase tracking-ultra-wide">Repository Empty</span>
                        <p className="text-brand-300 font-bold mt-2">No municipalities have been provisioned yet.</p>
                    </div>
                </div>
            ) : (
                <motion.div
                    variants={containerVariants}
                    initial="hidden"
                    animate="visible"
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8"
                >
                    {data.map((item) => (
                        <motion.div
                            key={item.city}
                            variants={cardVariants}
                            whileHover={{ y: -8, scale: 1.02 }}
                            className="bg-white p-8 rounded-[2rem] border border-brand-100 shadow-premium hover:shadow-premium-lg transition-all duration-500 group relative overflow-hidden"
                        >
                            <div className="absolute top-0 right-0 p-4 opacity-5 text-brand-900 group-hover:scale-125 group-hover:rotate-12 transition-transform duration-1000">
                                <MapPin size={120} />
                            </div>

                            <div className="flex justify-between items-start mb-8 relative z-10">
                                <div className="p-3 bg-brand-50 rounded-xl group-hover:bg-primary-50 transition-colors">
                                    <MapPin size={24} className="text-brand-300 group-hover:text-primary-500 transition-colors" />
                                </div>
                                {isStale(item.lastUpdated) ? (
                                    <span className="px-4 py-1.5 rounded-full bg-warning-50 text-warning-700 text-[10px] font-black uppercase tracking-ultra-wide border border-warning-100 shadow-sm flex items-center gap-2">
                                        <div className="w-1.5 h-1.5 rounded-full bg-warning-500 animate-pulse" />
                                        Stale
                                    </span>
                                ) : (
                                    <span className="px-4 py-1.5 rounded-full bg-success-50 text-success-700 text-[10px] font-black uppercase tracking-ultra-wide border border-success-100 shadow-sm flex items-center gap-2">
                                        <div className="w-1.5 h-1.5 rounded-full bg-success-500 animate-pulse" />
                                        Fresh
                                    </span>
                                )}
                            </div>

                            <h3 className="text-2xl font-black text-brand-900 mb-8 tracking-tight group-hover:text-primary-700 transition-colors relative z-10">{item.city}</h3>

                            <div className="space-y-4 relative z-10">
                                <div className="flex items-center justify-between p-4 bg-brand-50/50 rounded-2xl border border-brand-100 shadow-inner group-hover:bg-white transition-colors">
                                    <div className="flex items-center gap-3 text-brand-400">
                                        <Layers size={16} />
                                        <span className="text-[10px] font-black uppercase tracking-ultra-wide">Neighborhoods</span>
                                    </div>
                                    <span className="text-lg font-black text-brand-900 font-mono tracking-tighter">{item.neighborhoodCount}</span>
                                </div>
                                <div className="flex items-center justify-between p-4 bg-brand-50/50 rounded-2xl border border-brand-100 shadow-inner group-hover:bg-white transition-colors">
                                    <div className="flex items-center gap-3 text-brand-400">
                                        <Calendar size={16} />
                                        <span className="text-[10px] font-black uppercase tracking-ultra-wide">Last Sync</span>
                                    </div>
                                    <span className="text-xs font-black text-brand-700">
                                        {item.lastUpdated ? new Date(item.lastUpdated).toLocaleDateString() : 'NEVER'}
                                    </span>
                                </div>
                            </div>
                        </motion.div>
                    ))}
                </motion.div>
            )}
          </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default DatasetStatusModal;
