import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { LayoutDashboard, Users, LogOut, Activity, Settings } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useState } from 'react';

const Layout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [roles] = useState<string[]>(() => { try { return JSON.parse(localStorage.getItem('admin_roles') || '[]'); } catch { return []; } });

  const handleLogout = () => {
    localStorage.removeItem('admin_token');
    localStorage.removeItem('admin_refresh_token');
    localStorage.removeItem('admin_email');
    localStorage.removeItem('admin_userId');
    localStorage.removeItem('admin_roles');
    navigate('/login');
  };

  const navGroups = [
    {
      title: 'Monitoring',
      items: [
        { name: 'Dashboard', path: '/', icon: LayoutDashboard },
      ]
    },
    {
      title: 'Operations',
      items: [
        { name: 'Batch Jobs', path: '/jobs', icon: Activity },
        { name: 'AI Models', path: '/ai-models', icon: Settings },
      ]
    },
    {
      title: 'Access',
      requiredRoles: ['Admin'], // Basic check, adjust as needed
      items: [
        { name: 'Users', path: '/users', icon: Users },
      ]
    }
  ];

  const hasAccess = (requiredRoles?: string[]) => {
    if (!requiredRoles || requiredRoles.length === 0) return true;
    return roles.some(role => requiredRoles.includes(role));
  };

  return (
    <div className="flex h-screen bg-brand-50 w-full overflow-hidden">
      {/* Sidebar */}
      <aside className="w-64 bg-white/80 backdrop-blur-md border-r border-brand-200 flex flex-col shadow-premium z-10 relative">
        <div className="p-8">
          <div className="flex items-center space-x-3">
            <motion.div
              whileHover={{ rotate: 10, scale: 1.05 }}
              className="w-10 h-10 bg-primary-600 rounded-xl flex items-center justify-center shadow-lg shadow-primary-200/50"
            >
              <span className="text-white font-black text-xl">V</span>
            </motion.div>
            <h1 className="text-xl font-black text-brand-900 tracking-tight">Valora Admin</h1>
          </div>
        </div>

        <nav className="flex-1 px-4 space-y-6 overflow-y-auto">
          {navGroups.map((group) => (
            hasAccess(group.requiredRoles) && (
              <div key={group.title}>
                <h3 className="px-4 text-[10px] font-black text-brand-400 uppercase tracking-widest mb-2">
                  {group.title}
                </h3>
                <div className="space-y-1">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const isActive = location.pathname === item.path;
                    return (
                      <motion.div
                        key={item.name}
                        whileTap={{ scale: 0.98 }}
                        whileHover={{ x: 4 }}
                        className="relative"
                      >
                        <Link
                          to={item.path}
                          className={`group flex items-center px-4 py-3 text-sm font-bold rounded-xl transition-all duration-300 ${
                            isActive
                              ? 'bg-primary-50 text-primary-700 shadow-premium border border-primary-100/50'
                              : 'text-brand-500 hover:bg-primary-50/40 hover:text-primary-600'
                          }`}
                        >
                          <Icon className={`mr-3 h-5 w-5 transition-colors duration-300 ${isActive ? 'text-primary-600' : 'text-brand-400 group-hover:text-primary-500'}`} />
                          {item.name}

                          {isActive && (
                            <motion.div
                              layoutId="active-pill"
                              className="absolute left-0 w-1 h-5 bg-primary-600 rounded-r-full"
                              transition={{ type: "spring", stiffness: 300, damping: 30 }}
                            />
                          )}
                        </Link>
                      </motion.div>
                    );
                  })}
                </div>
              </div>
            )
          ))}
        </nav>

        <div className="p-4 border-t border-brand-100 mt-auto">
          <motion.button
            whileTap={{ scale: 0.98 }}
            onClick={handleLogout}
            className="flex items-center w-full px-4 py-3.5 text-sm font-bold text-brand-500 rounded-xl hover:bg-error-50 hover:text-error-600 transition-all duration-200 cursor-pointer group"
          >
            <LogOut className="mr-3 h-5 w-5 transition-transform group-hover:-translate-x-1" />
            Logout
          </motion.button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto relative bg-brand-50/30">
        <div className="max-w-7xl mx-auto p-10">
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{
                duration: 0.4,
                ease: [0.22, 1, 0.36, 1]
              } as const}
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
