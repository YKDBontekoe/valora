import React from 'react';
import { ArrowUp, ArrowDown } from 'lucide-react';

interface BatchJobTableHeaderCellProps {
  label: string;
  field: string;
  sortBy: string | undefined;
  toggleSort: (field: string) => void;
}

export const BatchJobTableHeaderCell: React.FC<BatchJobTableHeaderCellProps> = ({
  label,
  field,
  sortBy,
  toggleSort,
}) => {
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
    <th
      className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
      onClick={() => toggleSort(field)}
      onKeyDown={(e) => handleKeyDown(e, field)}
      tabIndex={0}
      role="button"
      aria-sort={getAriaSort(field)}
      aria-label={`Sort by ${label}`}
    >
      <div className="flex items-center gap-3">
        {label}
        <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
          <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === `${field}_asc` ? 'text-primary-600' : 'text-brand-200'}`} />
          <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === `${field}_desc` || (field === 'createdAt' && !sortBy) ? 'text-primary-600' : 'text-brand-200'}`} />
        </div>
      </div>
    </th>
  );
};
