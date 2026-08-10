import React from 'react';
import { 
  Building, 
  Gamepad2, 
  Users, 
  Cpu, 
  TrendingUp, 
  DollarSign,
  Briefcase
} from 'lucide-react';

export type TabId = 'studio' | 'projects' | 'staff' | 'tech' | 'market' | 'finance';

interface SidebarProps {
  activeTab: TabId;
  onSelectTab: (tab: TabId) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ activeTab, onSelectTab }) => {
  const tabs = [
    {
      id: 'studio' as TabId,
      label: 'Studio Floor',
      icon: Building,
      description: 'Office view & desk assignments'
    },
    {
      id: 'projects' as TabId,
      label: 'Projects & Pipeline',
      icon: Gamepad2,
      description: 'Active games & live ops'
    },
    {
      id: 'staff' as TabId,
      label: 'Staff & Teams',
      icon: Users,
      description: 'Employee cards & recruiting'
    },
    {
      id: 'tech' as TabId,
      label: 'Tech & Engines',
      icon: Cpu,
      description: 'R&D tech tree & custom engines'
    },
    {
      id: 'market' as TabId,
      label: 'Market & Rivals',
      icon: TrendingUp,
      description: 'Trends, platforms, & rival studios'
    },
    {
      id: 'finance' as TabId,
      label: 'Finance & Office',
      icon: DollarSign,
      description: 'P&L ledger, loans, & upgrades'
    }
  ];

  return (
    <aside className="w-64 bg-slate-900 border-r border-slate-800 flex flex-col shrink-0 select-none">
      <div className="p-3 border-b border-slate-800/60">
        <div className="flex items-center space-x-2 text-xs font-semibold text-slate-400 uppercase tracking-wider">
          <Briefcase className="w-3.5 h-3.5 text-sky-400" />
          <span>Management Console</span>
        </div>
      </div>

      <nav className="p-2 space-y-1 flex-1 overflow-y-auto">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => onSelectTab(tab.id)}
              className={`w-full flex items-center space-x-3 px-3 py-3 rounded-xl transition text-left group ${
                isActive
                  ? 'bg-gradient-to-r from-sky-600/20 to-indigo-600/10 text-white border-l-4 border-sky-500 font-semibold shadow-sm'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <div
                className={`p-2 rounded-lg transition ${
                  isActive
                    ? 'bg-sky-500 text-white shadow-md shadow-sky-500/20'
                    : 'bg-slate-800 text-slate-400 group-hover:bg-slate-700 group-hover:text-slate-200'
                }`}
              >
                <Icon className="w-4 h-4" />
              </div>
              <div>
                <div className="text-sm leading-tight">{tab.label}</div>
                <div className="text-[11px] text-slate-500 font-normal mt-0.5">
                  {tab.description}
                </div>
              </div>
            </button>
          );
        })}
      </nav>

      {/* Bottom Info Footer */}
      <div className="p-4 border-t border-slate-800/80 bg-slate-950/40 text-xs text-slate-400">
        <div className="flex items-center justify-between">
          <span className="font-semibold text-slate-300">Arena Agent Mode</span>
          <span className="px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-400 text-[10px] font-mono">
            v0.1.0
          </span>
        </div>
        <div className="text-[11px] text-slate-500 mt-1">
          Complex Tycoon Simulation Engine
        </div>
      </div>
    </aside>
  );
};
