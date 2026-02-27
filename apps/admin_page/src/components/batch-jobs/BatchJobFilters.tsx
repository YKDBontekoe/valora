import React from 'react';
import { Search, X } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import Button from '../Button';

interface BatchJobFiltersProps {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  statusFilter: string;
  setStatusFilter: (status: string) => void;
  typeFilter: string;
  setTypeFilter: (type: string) => void;
  hasActiveFilters: boolean;
  clearFilters: () => void;
  setPage: (page: number) => void;
}

export const BatchJobFilters: React.FC<BatchJobFiltersProps> = ({
  searchQuery,
  setSearchQuery,
  statusFilter,
  setStatusFilter,
  typeFilter,
  setTypeFilter,
  hasActiveFilters,
  clearFilters,
  setPage,
}) => {
  return (
    <div className="px-10 py-8 border-b border-brand-100 bg-brand-50/30 flex flex-col xl:flex-row xl:items-center justify-between gap-6">
      <div className="flex items-center gap-6 flex-1">
        <h2 className="font-black text-brand-900 uppercase tracking-[0.2em] text-xs whitespace-nowrap">
          Pipeline History
        </h2>
        <div className="relative max-w-sm w-full group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-300 group-focus-within:text-primary-500 transition-colors" />
          <input
            type="text"
            placeholder="Search by target..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-11 pr-4 py-3 bg-white border border-brand-100 rounded-xl text-sm font-black text-brand-900 outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all placeholder:font-bold placeholder:text-brand-200"
          />
        </div>
      </div>
      <div className="flex flex-col sm:flex-row sm:items-center gap-4">
        <div className="flex items-center gap-3">
          <AnimatePresence>
            {hasActiveFilters && (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.9 }}
              >
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearFilters}
                  leftIcon={<X size={16} />}
                  className="text-brand-400 hover:text-brand-600 hover:bg-brand-50 mr-2"
                >
                  Clear
                </Button>
              </motion.div>
            )}
          </AnimatePresence>
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
            className="px-5 py-2.5 bg-white border border-brand-100 rounded-xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 cursor-pointer hover:border-brand-200 transition-colors appearance-none"
          >
            <option value="All">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
          </select>
          <select
            value={typeFilter}
            onChange={(e) => {
              setTypeFilter(e.target.value);
              setPage(1);
            }}
            className="px-5 py-2.5 bg-white border border-brand-100 rounded-xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 cursor-pointer hover:border-brand-200 transition-colors appearance-none"
          >
            <option value="All">All Types</option>
            <option value="CityIngestion">CityIngestion</option>
            <option value="MapGeneration">MapGeneration</option>
            <option value="AllCitiesIngestion">AllCitiesIngestion</option>
          </select>
        </div>
        <div className="flex items-center gap-2 bg-primary-50 px-3 py-1.5 rounded-full border border-primary-100/50 ml-2">
          <span className="w-2 h-2 rounded-full bg-primary-500 animate-pulse" />
          <span className="text-[10px] font-black text-primary-700 uppercase tracking-wider hidden sm:inline">
            Live
          </span>
        </div>
      </div>
    </div>
  );
};
