import { memo } from 'react';
import { motion } from 'framer-motion';
import { MapPin, Euro } from 'lucide-react';
import type { Listing } from '../types';

interface ListingRowProps {
  listing: Listing;
}

const ListingRow = memo(({ listing }: ListingRowProps) => (
  <motion.tr
    initial={{ opacity: 0, y: 4 }}
    animate={{ opacity: 1, y: 0 }}
    exit={{ opacity: 0, y: -4 }}
    layout
    className="hover:bg-brand-50/50 transition-colors group"
  >
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <div className="flex items-center">
        <div className="w-10 h-10 rounded-xl bg-brand-50 flex items-center justify-center mr-4 group-hover:bg-primary-50 transition-colors">
          <MapPin className="h-5 w-5 text-brand-400 group-hover:text-primary-600 transition-colors" />
        </div>
        <span className="font-bold text-brand-900">{listing.address}</span>
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <div className="flex items-center text-brand-700 font-bold">
        <Euro className="h-4 w-4 mr-1 text-brand-400" />
        {listing.price != null ? listing.price.toLocaleString() : '-'}
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <span className="px-3 py-1.5 rounded-lg bg-brand-100 text-brand-700 text-[10px] font-black uppercase tracking-widest border border-brand-200/50">
        {listing.city}
      </span>
    </td>
  </motion.tr>
));

ListingRow.displayName = 'ListingRow';

export default ListingRow;
