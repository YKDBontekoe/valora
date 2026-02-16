import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { LayoutDashboard, Users, List, LogOut } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';

const Layout = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    localStorage.removeItem('admin_token');
    localStorage.removeItem('admin_refresh_token');
    localStorage.removeItem('admin_email');
    localStorage.removeItem('admin_userId');
    navigate('/login');
  };

  const navItems = [
    { name: 'Dashboard', path: '/', icon: LayoutDashboard },
    { name: 'Users', path: '/users', icon: Users },
    { name: 'Listings', path: '/listings', icon: List },
  ];

  return (
    <div className="flex h-screen bg-brand-50 w-full overflow-hidden">
      {/* Sidebar */}
      <aside className="w-64 bg-white border-r border-brand-200 flex flex-col shadow-premium z-10">
        <div className="p-8">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center shadow-lg shadow-primary-200">
              <span className="text-white font-bold text-lg">V</span>
            </div>
            <h1 className="text-xl font-bold text-brand-900 tracking-tight">Valora Admin</h1>
          </div>
        </div>
        <nav className="flex-1 px-4 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <motion.div
                key={item.name}
                whileHover={{ x: 4 }}
                whileTap={{ scale: 0.98 }}
              >
                <Link
                  to={item.path}
                  className={`group flex items-center px-4 py-3 text-sm font-semibold rounded-xl transition-all duration-200 ${
                    isActive
                      ? 'bg-primary-50 text-primary-700 shadow-premium border border-primary-100/50'
                      : 'text-brand-500 hover:bg-brand-50 hover:text-brand-900'
                  }`}
                >
                  <Icon className={`mr-3 h-5 w-5 transition-colors ${isActive ? 'text-primary-600' : 'text-brand-400 group-hover:text-brand-600'}`} />
                  {item.name}
                </Link>
              </motion.div>
            );
          })}
        </nav>
        <div className="p-4 border-t border-brand-100">
          <motion.button
            whileTap={{ scale: 0.98 }}
            onClick={handleLogout}
            className="flex items-center w-full px-4 py-3 text-sm font-semibold text-brand-500 rounded-xl hover:bg-error-50 hover:text-error-600 transition-all duration-200 cursor-pointer group"
          >
            <LogOut className="mr-3 h-5 w-5 transition-transform group-hover:-translate-x-1" />
            Logout
          </motion.button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto relative">
        <div className="max-w-7xl mx-auto p-10">
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{ duration: 0.2, ease: "easeOut" }}
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </div>
      </main>
    </div>
  );
};

export default Layout;
