import { motion } from 'framer-motion';
import { MapPin, Euro, Star } from 'lucide-react';
import type { Listing } from '../types';

interface ListingRowProps {
  listing: Listing;
}

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0 }
};

const ListingRow = ({ listing }: ListingRowProps) => (
  <motion.tr
    variants={rowVariants}
    exit={{ opacity: 0, scale: 0.98 }}
    layout
    className="hover:bg-brand-50/50 transition-colors group cursor-default"
  >
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <div className="flex items-center">
        <div className="w-10 h-10 rounded-xl bg-brand-50 flex items-center justify-center mr-4 group-hover:bg-primary-50 transition-colors">
          <MapPin className="h-5 w-5 text-brand-400 group-hover:text-primary-600 transition-colors" />
        </div>
        <div className="flex flex-col">
          <span className="font-bold text-brand-900">{listing.address}</span>
          <span className="text-[10px] text-brand-400 uppercase font-black">{listing.city}</span>
        </div>
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-sm text-center">
      {listing.contextCompositeScore != null ? (
        <div className="inline-flex items-center px-2.5 py-1 rounded-lg bg-primary-50 text-primary-700 font-bold border border-primary-100">
          <Star className="h-3.5 w-3.5 mr-1 fill-primary-500" />
          {listing.contextCompositeScore.toFixed(1)}
        </div>
      ) : (
        <span className="text-brand-300">-</span>
      )}
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <div className="flex flex-col">
        <div className="flex items-center text-brand-700 font-bold">
          <Euro className="h-4 w-4 mr-1 text-brand-400" />
          {listing.price != null ? listing.price.toLocaleString() : '-'}
        </div>
        {listing.wozValue != null && (
          <span className="text-[10px] text-brand-400 font-medium">
            WOZ: €{listing.wozValue.toLocaleString()}
          </span>
        )}
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-sm">
      <span className="px-3 py-1.5 rounded-lg bg-brand-100 text-brand-700 text-[10px] font-black uppercase tracking-widest border border-brand-200/50">
        {listing.status || 'Active'}
      </span>
    </td>
  </motion.tr>
);

export default ListingRow;
