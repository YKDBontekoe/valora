import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { LayoutDashboard, Users, LogOut, Activity, Settings, ChevronRight } from 'lucide-react';
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
      requiredRoles: ['Admin'],
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
      <aside className="w-72 glass-premium border-r border-brand-100 flex flex-col z-20 relative">
        <div className="p-10">
          <Link to="/" className="flex items-center space-x-4 group">
            <motion.div
              whileHover={{ rotate: 12, scale: 1.1 }}
              whileTap={{ scale: 0.9 }}
              className="w-12 h-12 bg-linear-to-br from-primary-500 to-primary-700 rounded-2xl flex items-center justify-center shadow-premium-lg shadow-primary-200/50 transition-all duration-500 group-hover:shadow-primary-400/30"
            >
              <span className="text-white font-black text-2xl">V</span>
            </motion.div>
            <div className="flex flex-col">
                <h1 className="text-xl font-black text-brand-900 tracking-tight leading-none">Valora</h1>
                <span className="text-[10px] font-black text-primary-600 uppercase tracking-[0.2em] mt-1">Admin Console</span>
            </div>
          </Link>
        </div>

        <nav className="flex-1 px-6 space-y-10 overflow-y-auto py-4">
          {navGroups.map((group) => (
            hasAccess(group.requiredRoles) && (
              <div key={group.title} className="space-y-3">
                <h3 className="px-4 text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-4">
                  {group.title}
                </h3>
                <div className="space-y-1.5">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const isActive = location.pathname === item.path;
                    return (
                      <motion.div
                        key={item.name}
                        whileHover={{ x: 4 }}
                        whileTap={{ scale: 0.98 }}
                        className="relative"
                      >
                        <Link
                          to={item.path}
                          className={`group flex items-center px-4 py-3.5 text-sm font-black rounded-2xl transition-all duration-500 relative ${
                            isActive
                              ? 'text-primary-700'
                              : 'text-brand-500 hover:text-brand-900'
                          }`}
                        >
                          {isActive && (
                            <motion.div
                              layoutId="nav-glow"
                              className="absolute inset-0 bg-primary-50 border border-primary-100/50 rounded-2xl shadow-premium z-0"
                              transition={{ type: "spring", stiffness: 260, damping: 20 }}
                            />
                          )}

                          <div className="relative z-10 flex items-center w-full">
                              <Icon className={`mr-3 h-5 w-5 transition-all duration-500 ${isActive ? 'text-primary-600 scale-110' : 'text-brand-400 group-hover:text-brand-900 group-hover:rotate-6'}`} />
                              <span className="flex-1">{item.name}</span>
                              {isActive && (
                                <motion.div
                                    initial={{ opacity: 0, x: -10 }}
                                    animate={{ opacity: 1, x: 0 }}
                                >
                                    <ChevronRight size={14} className="text-primary-400" />
                                </motion.div>
                              )}
                          </div>
                        </Link>
                      </motion.div>
                    );
                  })}
                </div>
              </div>
            )
          ))}
        </nav>

        <div className="p-6 border-t border-brand-100 mt-auto bg-brand-50/30">
          <motion.button
            whileTap={{ scale: 0.98 }}
            onClick={handleLogout}
            className="flex items-center w-full px-4 py-4 text-sm font-black text-brand-400 rounded-2xl hover:bg-error-50 hover:text-error-600 transition-all duration-300 cursor-pointer group shadow-sm hover:shadow-error-100/50 border border-transparent hover:border-error-100"
          >
            <LogOut className="mr-3 h-5 w-5 transition-transform group-hover:-translate-x-1" />
            Sign Out
          </motion.button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto relative bg-brand-50/30">
        {/* Top Decorative bar */}
        <div className="h-1.5 w-full bg-linear-to-r from-primary-600 via-primary-400 to-transparent opacity-10 absolute top-0 left-0 z-10" />

        <div className="max-w-7xl mx-auto p-8 md:p-12 lg:p-16">
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={{ opacity: 0, y: 20, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -20, scale: 0.98 }}
              transition={{
                duration: 0.5,
                ease: [0.22, 1, 0.36, 1] as const
              }}
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
