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
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-brand-900/40 backdrop-blur-sm">
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="bg-white rounded-2xl shadow-premium-xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden"
        >
          <div className="flex items-center justify-between px-8 py-6 border-b border-brand-100 bg-white z-10">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-brand-50 rounded-lg text-brand-600">
                <Database size={20} />
              </div>
              <div>
                <h2 className="text-xl font-black text-brand-900">Dataset Status</h2>
                <p className="text-sm font-medium text-brand-500">Overview of ingested municipalities</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
                <Button variant="ghost" size="sm" onClick={fetchData} disabled={loading}>
                    <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
                </Button>
                <button
                  onClick={onClose}
                  className="p-2 text-brand-400 hover:text-brand-600 hover:bg-brand-50 rounded-lg transition-colors"
                >
                  <X size={20} />
                </button>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-8 bg-brand-50/30">
            {loading ? (
                <div className="space-y-4">
                    {[...Array(5)].map((_, i) => <Skeleton key={i} height={60} className="rounded-xl" />)}
                </div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-12 text-error-500 gap-4">
                    <AlertCircle size={48} className="opacity-50" />
                    <p className="font-bold">{error}</p>
                    <Button variant="outline" onClick={fetchData}>Retry</Button>
                </div>
            ) : data.length === 0 ? (
                <div className="text-center py-12 text-brand-400 font-bold">No dataset information available.</div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {data.map((item) => (
                        <div key={item.city} className="bg-white p-5 rounded-xl border border-brand-100 shadow-sm hover:shadow-md transition-all flex flex-col gap-3">
                            <div className="flex justify-between items-start">
                                <h3 className="font-bold text-brand-900">{item.city}</h3>
                                {isStale(item.lastUpdated) ? (
                                    <span className="px-2 py-0.5 rounded-full bg-warning-50 text-warning-700 text-[10px] font-black uppercase tracking-wider border border-warning-200">Stale</span>
                                ) : (
                                    <span className="px-2 py-0.5 rounded-full bg-success-50 text-success-700 text-[10px] font-black uppercase tracking-wider border border-success-200">Fresh</span>
                                )}
                            </div>
                            <div className="grid grid-cols-2 gap-2 text-xs">
                                <div className="text-brand-400 font-bold uppercase tracking-wider">Neighborhoods</div>
                                <div className="text-right font-mono font-medium text-brand-700">{item.neighborhoodCount}</div>
                                <div className="text-brand-400 font-bold uppercase tracking-wider">Updated</div>
                                <div className="text-right font-mono font-medium text-brand-700">
                                    {item.lastUpdated ? new Date(item.lastUpdated).toLocaleDateString() : 'Never'}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default DatasetStatusModal;
