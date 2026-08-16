import React from 'react';
import { ArrowUp, ArrowDown } from 'lucide-react';

interface BatchJobTableHeaderProps {
  sortBy: string | undefined;
  toggleSort: (field: string) => void;
}

export const BatchJobTableHeader: React.FC<BatchJobTableHeaderProps> = ({ sortBy, toggleSort }) => {
  const getAriaSort = (field: string) => {
    if (sortBy === `${field}_asc`) return 'ascending';
    if (sortBy === `${field}_desc`) return 'descending';
    return 'none';
  };

  const handleKeyDown = (e: React.KeyboardEvent, field: string) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleSort(field);
    }
  };

  return (
    <thead className="bg-brand-50/50 backdrop-blur-md">
      <tr>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
          onClick={() => toggleSort('type')}
          onKeyDown={(e) => handleKeyDown(e, 'type')}
          tabIndex={0}
          role="columnheader"
          aria-sort={getAriaSort('type')}
          aria-label="Sort by Definition"
        >
          <div className="flex items-center gap-3">
            Definition
            <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </div>
        </th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
          onClick={() => toggleSort('target')}
          onKeyDown={(e) => handleKeyDown(e, 'target')}
          tabIndex={0}
          role="columnheader"
          aria-sort={getAriaSort('target')}
          aria-label="Sort by Target"
        >
          <div className="flex items-center gap-3">
            Target
            <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </div>
        </th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
          onClick={() => toggleSort('status')}
          onKeyDown={(e) => handleKeyDown(e, 'status')}
          tabIndex={0}
          role="columnheader"
          aria-sort={getAriaSort('status')}
          aria-label="Sort by Status"
        >
          <div className="flex items-center gap-3">
            Status
            <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </div>
        </th>
        <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Progress</th>
        <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Context</th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
          onClick={() => toggleSort('createdAt')}
          onKeyDown={(e) => handleKeyDown(e, 'createdAt')}
          tabIndex={0}
          role="columnheader"
          aria-sort={getAriaSort('createdAt')}
          aria-label="Sort by Timestamp"
        >
          <div className="flex items-center gap-3">
            Timestamp
            <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_desc' || (!sortBy) ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </div>
        </th>
        <th className="px-12 py-8 text-right text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Action</th>
      </tr>
    </thead>
  );
};
