import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { LayoutDashboard, Users, LogOut, Activity, Settings, ChevronRight, Sparkles } from 'lucide-react';
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

  const sidebarVariants = {
    hidden: { opacity: 0, x: -20 },
    visible: {
        opacity: 1,
        x: 0,
        transition: {
            staggerChildren: 0.1,
            delayChildren: 0.1
        }
    }
  };

  const groupVariants = {
    hidden: { opacity: 0, y: 10 },
    visible: { opacity: 1, y: 0 }
  };

  const hasAccess = (requiredRoles?: string[]) => {
    if (!requiredRoles || requiredRoles.length === 0) return true;
    return roles.some(role => requiredRoles.includes(role));
  };

  return (
    <div className="flex h-screen bg-brand-50 w-full overflow-hidden font-body">
      {/* Sidebar */}
      <aside className="w-80 glass-premium-accent border-r border-white/40 flex flex-col z-20 relative shadow-premium-2xl">
        <div className="p-10">
          <Link to="/" className="flex items-center space-x-5 group">
            <motion.div
              whileHover={{ rotate: 12, scale: 1.15 }}
              whileTap={{ scale: 0.9 }}
              className="w-14 h-14 bg-linear-to-br from-primary-500 to-primary-700 rounded-[1.25rem] flex items-center justify-center shadow-premium-lg shadow-primary-200/50 transition-all duration-500 group-hover:shadow-glow-primary-lg"
            >
              <span className="text-white font-black text-3xl">V</span>
            </motion.div>
            <div className="flex flex-col">
                <h1 className="text-2xl font-black text-brand-900 tracking-tight leading-none">Valora</h1>
                <div className="flex items-center gap-2 mt-1.5">
                    <Sparkles size={10} className="text-primary-500 animate-pulse" />
                    <span className="text-[10px] font-black text-primary-600 uppercase tracking-[0.25em]">Admin Console</span>
                </div>
            </div>
          </Link>
        </div>

        <motion.nav
            variants={sidebarVariants}
            initial="hidden"
            animate="visible"
            className="flex-1 px-8 space-y-12 overflow-y-auto py-6 custom-scrollbar"
        >
          {navGroups.map((group) => (
            hasAccess(group.requiredRoles) && (
              <motion.div key={group.title} variants={groupVariants} className="space-y-4">
                <h3 className="px-4 text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-4">
                  {group.title}
                </h3>
                <div className="space-y-2">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const isActive = location.pathname === item.path;
                    return (
                      <motion.div
                        key={item.name}
                        whileHover={{ x: 6 }}
                        whileTap={{ scale: 0.98 }}
                        className="relative"
                      >
                        <Link
                          to={item.path}
                          className={`group flex items-center px-5 py-4 text-sm font-black rounded-2xl transition-all duration-500 relative ${
                            isActive
                              ? 'text-primary-700'
                              : 'text-brand-500 hover:text-brand-900'
                          }`}
                        >
                          {isActive && (
                            <motion.div
                              layoutId="nav-glow"
                              className="absolute inset-0 bg-white border border-primary-100 shadow-premium z-0 ring-4 ring-primary-500/10"
                              transition={{ type: "spring", stiffness: 300, damping: 25 }}
                            />
                          )}

                          <div className="relative z-10 flex items-center w-full">
                              <div className={`p-2 rounded-xl transition-all duration-500 mr-4 ${isActive ? 'bg-primary-50 text-primary-600 scale-110 shadow-sm' : 'text-brand-400 group-hover:text-brand-900 group-hover:rotate-6'}`}>
                                <Icon className="h-5 w-5" />
                              </div>
                              <span className="flex-1 tracking-tight">{item.name}</span>
                              {isActive && (
                                <motion.div
                                    initial={{ opacity: 0, x: -10 }}
                                    animate={{ opacity: 1, x: 0 }}
                                >
                                    <ChevronRight size={16} className="text-primary-400" />
                                </motion.div>
                              )}
                          </div>
                        </Link>
                      </motion.div>
                    );
                  })}
                </div>
              </motion.div>
            )
          ))}
        </motion.nav>

        <div className="p-8 border-t border-brand-100 mt-auto bg-brand-50/20 backdrop-blur-md">
          <motion.button
            whileTap={{ scale: 0.98 }}
            onClick={handleLogout}
            className="flex items-center w-full px-5 py-5 text-sm font-black text-brand-400 rounded-2xl hover:bg-error-50 hover:text-error-600 transition-all duration-300 cursor-pointer group shadow-sm hover:shadow-glow-error border border-transparent hover:border-error-100"
          >
            <div className="p-2 rounded-xl transition-transform group-hover:-translate-x-1 group-hover:bg-white mr-4">
                <LogOut className="h-5 w-5" />
            </div>
            Sign Out
          </motion.button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto relative bg-brand-50/30">
        {/* Top Decorative bar */}
        <div className="h-2 w-full bg-linear-to-r from-primary-600 via-primary-400 to-transparent opacity-20 absolute top-0 left-0 z-10" />

        {/* Subtle background decoration */}
        <div className="absolute top-0 right-0 w-[60%] h-[60%] bg-primary-50/20 blur-[150px] -z-10 rounded-full" />
        <div className="absolute bottom-0 left-0 w-[40%] h-[40%] bg-info-50/20 blur-[120px] -z-10 rounded-full" />

        <div className="max-w-7xl mx-auto p-10 md:p-14 lg:p-20">
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={{ opacity: 0, y: 30, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -30, scale: 0.98 }}
              transition={{
                duration: 0.6,
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
