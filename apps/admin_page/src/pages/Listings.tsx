import { motion, AnimatePresence } from 'framer-motion';
import { ChevronLeft, ChevronRight, List } from 'lucide-react';
import { useListings } from '../hooks/useListings';
import ListingRow from '../components/ListingRow';
import Skeleton from '../components/Skeleton';
import Button from '../components/Button';

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
    <div className="space-y-8">
      <div className="mb-8">
        <h1 className="text-4xl font-black text-brand-900 tracking-tight">Listing Management</h1>
        <p className="text-brand-500 mt-2 font-medium">Browse and monitor property listings across the platform.</p>
      </div>

      <div className="bg-white shadow-premium rounded-3xl overflow-hidden border border-brand-100 flex flex-col min-h-[600px]">
        <div className="overflow-x-auto flex-grow">
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50/50">
              <tr>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">Property Address</th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">Listing Price</th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">Region / City</th>
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
                   [...Array(8)].map((_, i) => (
                    <tr key={`skeleton-${i}`}>
                      <td className="px-8 py-6">
                        <div className="flex items-center gap-4">
                            <Skeleton variant="rectangular" width={40} height={40} className="rounded-xl" />
                            <Skeleton variant="text" width="50%" height={16} />
                        </div>
                      </td>
                      <td className="px-8 py-6"><Skeleton variant="text" width="30%" height={16} /></td>
                      <td className="px-8 py-6"><Skeleton variant="rectangular" width={100} height={28} className="rounded-lg" /></td>
                    </tr>
                  ))
                ) : listings.length === 0 ? (
                  <motion.tr
                    key="empty"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                  >
                    <td colSpan={3} className="px-8 py-20 text-center">
                        <div className="flex flex-col items-center gap-4">
                            <div className="p-4 bg-brand-50 rounded-full">
                                <List className="h-8 w-8 text-brand-200" />
                            </div>
                            <span className="text-brand-500 font-bold">No listings found.</span>
                        </div>
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
        <div className="bg-brand-50/50 px-8 py-6 border-t border-brand-100 flex items-center justify-between">
            <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">
                Page <span className="text-brand-900">{page}</span> <span className="mx-2 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
            </span>
            <div className="flex gap-3">
                <Button
                    variant="outline"
                    size="sm"
                    onClick={prevPage}
                    disabled={page === 1 || loading}
                    leftIcon={<ChevronLeft size={14} />}
                >
                    Previous
                </Button>
                <Button
                    variant="outline"
                    size="sm"
                    onClick={nextPage}
                    disabled={page === totalPages || loading}
                    rightIcon={<ChevronRight size={14} />}
                >
                    Next
                </Button>
            </div>
        </div>
      </div>
    </div>
  );
};

export default Listings;
