import React from 'react';
import Skeleton from '../../Skeleton';

export const BatchJobTableLoading: React.FC = () => {
  return (
    <>
      {[...Array(5)].map((_, i) => (
        <tr key={i}>
          <td className="px-12 py-10"><Skeleton variant="text" width="60%" height={24} /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="40%" height={20} /></td>
          <td className="px-12 py-10"><Skeleton variant="rectangular" width={100} height={32} className="rounded-2xl" /></td>
          <td className="px-12 py-10"><Skeleton variant="rectangular" width="100%" height={12} className="rounded-full" /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="70%" height={20} /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="80%" height={16} /></td>
          <td className="px-12 py-10"></td>
        </tr>
      ))}
    </>
  );
};
