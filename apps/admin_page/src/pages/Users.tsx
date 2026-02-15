import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';
import { Trash2, ChevronLeft, ChevronRight } from 'lucide-react';

const Users = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const currentUserId = localStorage.getItem('admin_userId');

  useEffect(() => {
    fetchUsers(page);
  }, [page]);

  const fetchUsers = async (pageNumber: number) => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(pageNumber);
      setUsers(data.items);
      setTotalPages(data.totalPages);
    } catch {
      console.error('Failed to fetch users');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (user: User) => {
    if (user.id === currentUserId) {
      alert('You cannot delete your own account.');
      return;
    }

    if (!window.confirm(`Are you sure you want to delete user ${user.email}?`)) return;

    try {
      await adminService.deleteUser(user.id);
      setUsers(users.filter(u => u.id !== user.id));
    } catch {
      alert('Failed to delete user. It might be protected or you might have lost permissions.');
    }
  };

  if (loading && users.length === 0) return <div>Loading users...</div>;

  return (
    <div>
      <h1 className="text-2xl font-semibold text-gray-900 mb-6">User Management</h1>
      <div className="bg-white shadow overflow-hidden sm:rounded-lg">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Roles</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {users.map((user) => (
              <tr key={user.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{user.email}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {user.roles.map(role => (
                    <span key={role} className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800 mr-1">
                      {role}
                    </span>
                  ))}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <button
                    onClick={() => handleDelete(user)}
                    disabled={user.id === currentUserId}
                    className={`text-red-600 hover:text-red-900 ${user.id === currentUserId ? 'opacity-30 cursor-not-allowed' : 'cursor-pointer'}`}
                    title={user.id === currentUserId ? 'You cannot delete yourself' : 'Delete user'}
                  >
                    <Trash2 className="h-5 w-5" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="mt-4 flex items-center justify-between">
        <div className="text-sm text-gray-700">
          Page {page} of {totalPages}
        </div>
        <div className="flex space-x-2">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="p-2 border rounded-md disabled:opacity-30 cursor-pointer"
          >
            <ChevronLeft className="h-5 w-5" />
          </button>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="p-2 border rounded-md disabled:opacity-30 cursor-pointer"
          >
            <ChevronRight className="h-5 w-5" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default Users;
