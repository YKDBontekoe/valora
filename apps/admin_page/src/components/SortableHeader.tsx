import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react';

interface SortableHeaderProps {
  label: string;
  field: string;
  currentSortBy?: string;
  currentSortOrder?: 'asc' | 'desc' | string; // Relaxed type
  onSort: (field: string) => void;
  align?: 'left' | 'right' | 'center';
}

const SortableHeader = ({
  label,
  field,
  currentSortBy,
  currentSortOrder,
  onSort,
  align = 'left',
}: SortableHeaderProps) => {
  const isActive = currentSortBy === field;

  return (
    <th
      scope="col"
      className={`px-8 py-4 text-xs font-bold text-brand-500 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/50 transition-colors select-none ${
        align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left'
      }`}
      onClick={() => onSort(field)}
    >
      <div className={`flex items-center gap-2 ${
        align === 'right' ? 'justify-end' : align === 'center' ? 'justify-center' : 'justify-start'
      }`}>
        {label}
        <span className={`transition-colors ${isActive ? 'text-primary-600' : 'text-brand-300 group-hover:text-brand-400'}`}>
          {isActive ? (
            currentSortOrder === 'desc' ? (
              <ArrowDown className="h-4 w-4" />
            ) : (
              <ArrowUp className="h-4 w-4" />
            )
          ) : (
            <ChevronsUpDown className="h-4 w-4" />
          )}
        </span>
      </div>
    </th>
  );
};

export default SortableHeader;
