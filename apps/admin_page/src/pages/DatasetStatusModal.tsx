import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Database, RefreshCw, AlertCircle, Globe } from 'lucide-react';
import { adminService } from '../services/api';
import type { DatasetStatus } from '../types';
import Button from '../components/Button';
import Skeleton from '../components/Skeleton';

interface DatasetStatusModalProps {
  isOpen: boolean;
  onClose: () => void;
}

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

  const container = {
    hidden: { opacity: 0 },
    show: {
      opacity: 1,
      transition: {
        staggerChildren: 0.05
      }
    }
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 10, scale: 0.95 },
    show: { opacity: 1, y: 0, scale: 1 }
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-brand-900/40 backdrop-blur-md"
        />
        <motion.div
          initial={{ opacity: 0, scale: 0.9, y: 40 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.9, y: 40 }}
          transition={{ type: "spring", damping: 25, stiffness: 300 }}
          className="relative bg-white rounded-[2.5rem] shadow-premium-xl w-full max-w-4xl max-h-[85vh] flex flex-col overflow-hidden border border-white/20"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="px-10 py-8 border-b border-brand-100 flex items-center justify-between bg-brand-50/20">
            <div className="flex items-center gap-5">
              <div className="p-3 bg-white rounded-2xl border border-brand-100 shadow-premium text-primary-600">
                <Database size={24} />
              </div>
              <div>
                <h2 className="text-3xl font-black text-brand-900 tracking-tightest">Dataset Catalog</h2>
                <p className="text-brand-400 font-bold">Synchronized municipalities and geographic nodes.</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={fetchData}
                    disabled={loading}
                    className="text-brand-400 hover:text-primary-600"
                >
                    <RefreshCw size={20} className={loading ? 'animate-spin' : ''} />
                </Button>
                <button
                  onClick={onClose}
                  className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300"
                >
                  <X size={28} />
                </button>
            </div>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-10 bg-brand-50/10 custom-scrollbar">
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {[...Array(9)].map((_, i) => (
                        <div key={i} className="bg-white p-6 rounded-3xl border border-brand-100 shadow-sm space-y-4">
                            <Skeleton variant="text" width="60%" height={24} />
                            <div className="space-y-2">
                                <Skeleton variant="text" width="100%" height={12} />
                                <Skeleton variant="text" width="80%" height={12} />
                            </div>
                        </div>
                    ))}
                </div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-20 text-error-500 gap-6">
                    <div className="p-8 bg-error-50 rounded-4xl border border-error-100 shadow-sm">
                        <AlertCircle size={48} className="opacity-40" />
                    </div>
                    <div className="text-center">
                        <p className="font-black text-xl text-brand-900 mb-2">Sync Interrupted</p>
                        <p className="text-brand-400 font-bold">{error}</p>
                    </div>
                    <Button variant="outline" onClick={fetchData} className="px-8">Retry Asset Query</Button>
                </div>
            ) : data.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-32 text-brand-200">
                    <Database size={64} className="opacity-10 mb-6" />
                    <span className="font-black text-xl uppercase tracking-widest">No assets discovered</span>
                </div>
            ) : (
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"
                >
                    {data.map((item) => (
                        <motion.div
                            key={item.city}
                            variants={itemVariants}
                            whileHover={{ y: -4, scale: 1.02 }}
                            className="bg-white p-6 rounded-[2rem] border border-brand-100 shadow-premium hover:shadow-premium-lg transition-all flex flex-col gap-6 group hover-border-gradient"
                        >
                            <div className="flex justify-between items-start">
                                <div className="flex flex-col">
                                    <h3 className="font-black text-brand-900 text-lg group-hover:text-primary-700 transition-colors">{item.city}</h3>
                                    <div className="flex items-center gap-1.5 mt-1">
                                        <div className={`w-1.5 h-1.5 rounded-full ${isStale(item.lastUpdated) ? 'bg-warning-500' : 'bg-success-500 animate-pulse'}`} />
                                        <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Cluster Status</span>
                                    </div>
                                </div>
                                {isStale(item.lastUpdated) ? (
                                    <span className="px-3 py-1 rounded-xl bg-warning-50 text-warning-700 text-[9px] font-black uppercase tracking-wider border border-warning-100 shadow-sm">Stale</span>
                                ) : (
                                    <span className="px-3 py-1 rounded-xl bg-success-50 text-success-700 text-[9px] font-black uppercase tracking-wider border border-success-100 shadow-sm">Fresh</span>
                                )}
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                <div className="flex flex-col p-3 bg-brand-50 rounded-2xl border border-brand-100/50 group-hover:bg-white transition-colors">
                                    <span className="text-[9px] font-black text-brand-300 uppercase tracking-widest mb-1">Nodes</span>
                                    <span className="font-black text-brand-900 font-mono text-sm">{item.neighborhoodCount}</span>
                                </div>
                                <div className="flex flex-col p-3 bg-brand-50 rounded-2xl border border-brand-100/50 group-hover:bg-white transition-colors">
                                    <span className="text-[9px] font-black text-brand-300 uppercase tracking-widest mb-1">Last Sync</span>
                                    <span className="font-black text-brand-900 font-mono text-sm">
                                        {item.lastUpdated ? new Date(item.lastUpdated).toLocaleDateString() : 'Never'}
                                    </span>
                                </div>
                            </div>

                            <div className="pt-2">
                                <div className="h-1.5 w-full bg-brand-50 rounded-full overflow-hidden border border-brand-100/30">
                                    <motion.div
                                        initial={{ width: 0 }}
                                        animate={{ width: '100%' }}
                                        transition={{ duration: 1, ease: "easeOut" }}
                                        className={`h-full ${isStale(item.lastUpdated) ? 'bg-warning-400' : 'bg-success-500'}`}
                                    />
                                </div>
                            </div>
                        </motion.div>
                    ))}
                </motion.div>
            )}
          </div>

          <div className="px-10 py-8 border-t border-brand-100 bg-brand-50/20 flex justify-between items-center">
              <div className="flex items-center gap-3 text-brand-300">
                  <Globe size={16} />
                  <span className="text-[10px] font-black uppercase tracking-widest">Global Asset Intelligence</span>
              </div>
              <Button variant="ghost" onClick={onClose} className="font-black text-brand-400">
                Close Catalog
              </Button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default DatasetStatusModal;
