import { motion, AnimatePresence } from 'framer-motion';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useListings } from '../hooks/useListings';
import ListingRow from '../components/ListingRow';

const tbodyVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
    }
  }
};

const Listings = () => {
  const {
    listings,
    loading,
    page,
    totalPages,
    nextPage,
    prevPage
  } = useListings();

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-brand-900">Listing Management</h1>
        <p className="text-brand-500 mt-1">Browse and monitor platform listings.</p>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 flex flex-col min-h-[600px]">
        <div className="overflow-x-auto flex-grow">
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Address</th>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Price</th>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">City</th>
              </tr>
            </thead>
            <motion.tbody
              initial="hidden"
              animate="visible"
              variants={tbodyVariants}
              className="bg-white divide-y divide-brand-100"
            >
              <AnimatePresence mode="popLayout">
                {loading ? (
                  <motion.tr
                    key="loading"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                  >
                    <td colSpan={3} className="px-8 py-12 text-center">
                      <div className="flex flex-col items-center">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mb-4"></div>
                        <span className="text-brand-500 font-medium">Loading listings...</span>
                      </div>
                    </td>
                  </motion.tr>
                ) : listings.length === 0 ? (
                  <motion.tr
                    key="empty"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                  >
                    <td colSpan={3} className="px-8 py-12 text-center text-brand-500 font-medium">
                      No listings found.
                    </td>
                  </motion.tr>
                ) : (
                  listings.map((listing) => (
                    <ListingRow key={listing.id} listing={listing} />
                  ))
                )}
              </AnimatePresence>
            </motion.tbody>
          </table>
        </div>

        {/* Pagination Controls */}
        <div className="bg-brand-50 px-8 py-6 border-t border-brand-100 flex items-center justify-between">
            <span className="text-sm font-bold text-brand-400 uppercase tracking-widest">
                Page <span className="text-brand-900">{page}</span> <span className="mx-1 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
            </span>
            <div className="flex space-x-3">
                <motion.button
                    whileTap={{ scale: 0.95 }}
                    onClick={prevPage}
                    disabled={page === 1 || loading}
                    className="p-2.5 rounded-xl bg-white border border-brand-200 shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all text-brand-600 cursor-pointer"
                >
                    <ChevronLeft className="h-5 w-5" />
                </motion.button>
                <motion.button
                    whileTap={{ scale: 0.95 }}
                    onClick={nextPage}
                    disabled={page === totalPages || loading}
                    className="p-2.5 rounded-xl bg-white border border-brand-200 shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all text-brand-600 cursor-pointer"
                >
                    <ChevronRight className="h-5 w-5" />
                </motion.button>
            </div>
        </div>
      </div>
    </div>
  );
};

export default Listings;
