import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { getOfficeTierById, OFFICE_TIERS } from '../../data/offices';
import { Employee, EmployeeRole } from '../../types/employee';
import { 
  Building2, 
  Coffee, 
  Bug, 
  Server, 
  UserPlus, 
  Heart, 
  Zap, 
  Code2, 
  PenTool, 
  Palette, 
  Music, 
  ShieldCheck, 
  Megaphone, 
  Award,
  ChevronRight,
  TrendingUp,
  Sparkles
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface StudioFloorViewProps {
  onOpenHire: () => void;
  onOpenNewGame: () => void;
  onNavigateTab: (tab: 'studio' | 'projects' | 'staff' | 'tech' | 'market' | 'finance') => void;
}

const ROLE_ICONS: Record<EmployeeRole, React.ReactNode> = {
  programmer: <Code2 className="w-3.5 h-3.5 text-sky-400" />,
  designer: <PenTool className="w-3.5 h-3.5 text-emerald-400" />,
  artist: <Palette className="w-3.5 h-3.5 text-pink-400" />,
  audio_engineer: <Music className="w-3.5 h-3.5 text-amber-400" />,
  qa_tester: <Bug className="w-3.5 h-3.5 text-rose-400" />,
  marketing: <Megaphone className="w-3.5 h-3.5 text-purple-400" />,
  producer: <Award className="w-3.5 h-3.5 text-yellow-400" />
};

const ROLE_LABELS: Record<EmployeeRole, string> = {
  programmer: 'Programmer',
  designer: 'Designer',
  artist: '3D/2D Artist',
  audio_engineer: 'Audio Engineer',
  qa_tester: 'QA Tester',
  marketing: 'Marketing',
  producer: 'Producer / Lead'
};

export const StudioFloorView: React.FC<StudioFloorViewProps> = ({
  onOpenHire,
  onOpenNewGame,
  onNavigateTab
}) => {
  const { 
    company, 
    employees, 
    projects, 
    upgradeOffice, 
    selectEmployee, 
    selectedEmployeeId,
    trainEmployee 
  } = useGameStore();

  const [activeDrawerEmp, setActiveDrawerEmp] = useState<Employee | null>(
    employees.find(e => e.id === selectedEmployeeId) || null
  );

  const officeTier = getOfficeTierById(company.officeTierId);
  const nextTierIndex = OFFICE_TIERS.findIndex(o => o.id === officeTier.id) + 1;
  const nextTier = OFFICE_TIERS[nextTierIndex];

  const handleSelectEmployee = (emp: Employee) => {
    soundEngine.playClick();
    setActiveDrawerEmp(emp);
    selectEmployee(emp.id);
  };

  const handleUpgradeOffice = () => {
    if (!nextTier) return;
    upgradeOffice(nextTier.id);
  };

  // Generate desk array based on maxEmployees
  const maxCapacity = officeTier.maxEmployees;
  const deskSlots = Array.from({ length: Math.min(30, Math.max(6, maxCapacity)) }, (_, i) => {
    return employees[i] || null;
  });

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Studio Banner */}
      <div className={`p-6 rounded-2xl bg-gradient-to-r ${officeTier.imageGradient} border border-slate-800 shadow-xl flex flex-col md:flex-row md:items-center justify-between gap-4`}>
        <div>
          <div className="flex items-center space-x-2">
            <Building2 className="w-6 h-6 text-sky-400" />
            <h2 className="text-2xl font-extrabold text-white">
              {officeTier.name}
            </h2>
          </div>
          <p className="text-sm text-slate-300 mt-1 max-w-xl">
            {officeTier.description}
          </p>
          <div className="flex flex-wrap items-center gap-4 mt-3 text-xs text-slate-300">
            <span className="flex items-center space-x-1.5 bg-slate-900/80 px-2.5 py-1 rounded-lg border border-slate-800">
              <Zap className="w-3.5 h-3.5 text-amber-400" />
              <span>Energy Regen: <strong>{officeTier.amenities.energyRegenRate}x</strong></span>
            </span>
            <span className="flex items-center space-x-1.5 bg-slate-900/80 px-2.5 py-1 rounded-lg border border-slate-800">
              <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
              <span>Bug Reduction: <strong>-{officeTier.amenities.bugReductionBonus}%</strong></span>
            </span>
            <span className="flex items-center space-x-1.5 bg-slate-900/80 px-2.5 py-1 rounded-lg border border-slate-800">
              <Server className="w-3.5 h-3.5 text-purple-400" />
              <span>Server Racks: <strong>{officeTier.amenities.serverCapacityBonus.toLocaleString()} players</strong></span>
            </span>
          </div>
        </div>

        {/* Upgrade Office Button */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 shrink-0">
          {nextTier ? (
            <button
              onClick={handleUpgradeOffice}
              disabled={company.cash < nextTier.upgradeCost}
              className={`flex items-center justify-center space-x-2 px-5 py-3 rounded-xl font-bold text-sm shadow-lg transition transform active:scale-95 ${
                company.cash >= nextTier.upgradeCost
                  ? 'bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white'
                  : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
              }`}
            >
              <TrendingUp className="w-4 h-4" />
              <span>Upgrade to {nextTier.name} (${nextTier.upgradeCost.toLocaleString()})</span>
            </button>
          ) : (
            <div className="px-4 py-2.5 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs font-semibold flex items-center space-x-2">
              <Sparkles className="w-4 h-4 text-amber-400" />
              <span>Max Studio Tier Reached!</span>
            </div>
          )}

          <button
            onClick={onOpenHire}
            className="flex items-center justify-center space-x-2 px-4 py-3 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-sm font-semibold shadow-md transition"
          >
            <UserPlus className="w-4 h-4" />
            <span>Hire Staff ({employees.length}/{officeTier.maxEmployees})</span>
          </button>
        </div>
      </div>

      {/* Special Rooms / Facilities */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Break Room */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-lg bg-amber-500/10 text-amber-400">
              <Coffee className="w-5 h-5" />
            </div>
            <div>
              <h4 className="font-bold text-sm text-white">Employee Break Room</h4>
              <p className="text-xs text-slate-400">
                Restores {Math.round(officeTier.amenities.energyRegenRate * 45)} energy overnight
              </p>
            </div>
          </div>
          <span className="text-xs font-semibold text-emerald-400 px-2 py-1 rounded bg-emerald-500/10">
            Active
          </span>
        </div>

        {/* QA Lab */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-lg bg-rose-500/10 text-rose-400">
              <Bug className="w-5 h-5" />
            </div>
            <div>
              <h4 className="font-bold text-sm text-white">Dedicated QA Lab</h4>
              <p className="text-xs text-slate-400">
                Reduces bug generation by {officeTier.amenities.bugReductionBonus}%
              </p>
            </div>
          </div>
          <span className="text-xs font-semibold text-emerald-400 px-2 py-1 rounded bg-emerald-500/10">
            Active
          </span>
        </div>

        {/* Server Room */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-lg bg-purple-500/10 text-purple-400">
              <Server className="w-5 h-5" />
            </div>
            <div>
              <h4 className="font-bold text-sm text-white">Studio Server Racks</h4>
              <p className="text-xs text-slate-400">
                Supports {officeTier.amenities.serverCapacityBonus.toLocaleString()} MMO players
              </p>
            </div>
          </div>
          <span className="text-xs font-semibold text-emerald-400 px-2 py-1 rounded bg-emerald-500/10">
            Active
          </span>
        </div>
      </div>

      {/* Interactive Desk Floor Grid */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="font-bold text-base text-white flex items-center space-x-2">
            <span>Workstation Floor Grid</span>
            <span className="text-xs font-normal text-slate-400">
              (Click any developer to inspect stats or train them)
            </span>
          </h3>

          <div className="flex items-center space-x-2">
            <button
              onClick={() => onNavigateTab('projects')}
              className="text-xs text-sky-400 hover:text-sky-300 font-semibold flex items-center"
            >
              <span>Manage Projects</span>
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {deskSlots.map((emp, index) => {
            if (!emp) {
              return (
                <div
                  key={`empty-${index}`}
                  onClick={onOpenHire}
                  className="p-4 rounded-2xl bg-slate-900/40 border-2 border-dashed border-slate-800 hover:border-slate-700 hover:bg-slate-900/70 transition flex flex-col items-center justify-center text-center space-y-2 cursor-pointer h-40"
                >
                  <div className="w-10 h-10 rounded-full bg-slate-800/80 flex items-center justify-center text-slate-500">
                    <UserPlus className="w-5 h-5" />
                  </div>
                  <div>
                    <div className="text-xs font-semibold text-slate-400">
                      Empty Workstation #{index + 1}
                    </div>
                    <div className="text-[10px] text-slate-500 mt-0.5">
                      Click to Hire Developer
                    </div>
                  </div>
                </div>
              );
            }

            const assignedProj = projects.find(p => p.id === emp.assignedProjectId);
            const isSelected = activeDrawerEmp?.id === emp.id;

            return (
              <div
                key={emp.id}
                onClick={() => handleSelectEmployee(emp)}
                className={`p-4 rounded-2xl bg-slate-900 border transition cursor-pointer flex flex-col justify-between h-40 relative group ${
                  isSelected
                    ? 'border-sky-500 bg-slate-800/80 shadow-lg shadow-sky-500/10'
                    : 'border-slate-800 hover:border-slate-700 hover:bg-slate-900/90'
                }`}
              >
                {/* Top: Avatar & Name */}
                <div className="flex items-start justify-between">
                  <div className="flex items-center space-x-2.5">
                    <div className="w-10 h-10 rounded-xl bg-slate-800 border border-slate-700 flex items-center justify-center font-bold text-sm text-sky-400 relative">
                      {emp.name.charAt(0)}
                      <div className="absolute -bottom-1 -right-1 p-0.5 rounded bg-slate-900 border border-slate-700">
                        {ROLE_ICONS[emp.role]}
                      </div>
                    </div>
                    <div className="min-w-0">
                      <div className="font-bold text-xs text-white truncate max-w-[100px]">
                        {emp.name}
                      </div>
                      <div className="text-[10px] text-slate-400 truncate">
                        {ROLE_LABELS[emp.role]}
                      </div>
                    </div>
                  </div>

                  {/* Level Badge */}
                  <span className="px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 text-[10px] font-mono border border-slate-700">
                    Lvl {emp.level}
                  </span>
                </div>

                {/* Center: Current Project & Animated Work Bubble */}
                <div className="my-2 p-2 rounded-lg bg-slate-950/80 border border-slate-800/80 flex items-center justify-between">
                  <div className="min-w-0">
                    <div className="text-[10px] text-slate-400">Assigned To:</div>
                    <div className="text-xs font-semibold text-slate-200 truncate">
                      {assignedProj ? assignedProj.title : 'Idle / Research'}
                    </div>
                  </div>
                  {assignedProj && (
                    <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" title="Active Working" />
                  )}
                </div>

                {/* Bottom: Energy & Morale Bars */}
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between text-[10px]">
                    <span className="flex items-center space-x-1 text-slate-400">
                      <Zap className="w-3 h-3 text-amber-400" />
                      <span>Energy</span>
                    </span>
                    <span className="font-mono text-slate-300">{emp.energy}%</span>
                  </div>
                  <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
                    <div
                      className={`h-full transition-all duration-300 ${
                        emp.energy > 50 ? 'bg-amber-400' : emp.energy > 25 ? 'bg-amber-600' : 'bg-rose-500'
                      }`}
                      style={{ width: `${emp.energy}%` }}
                    />
                  </div>

                  <div className="flex items-center justify-between text-[10px]">
                    <span className="flex items-center space-x-1 text-slate-400">
                      <Heart className="w-3 h-3 text-rose-400" />
                      <span>Morale</span>
                    </span>
                    <span className="font-mono text-slate-300">{emp.morale}%</span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Employee Inspection Drawer */}
      {activeDrawerEmp && (
        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 shadow-2xl flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
          <div className="flex items-start space-x-4">
            <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-2xl font-bold text-sky-400">
              {activeDrawerEmp.name.charAt(0)}
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h4 className="text-lg font-bold text-white">{activeDrawerEmp.name}</h4>
                <span className="px-2 py-0.5 rounded bg-slate-800 text-sky-400 text-xs font-semibold border border-slate-700">
                  {ROLE_LABELS[activeDrawerEmp.role]} • Lvl {activeDrawerEmp.level}
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                Salary: <strong className="text-slate-200">${activeDrawerEmp.monthlySalary}/mo</strong> | Hired: {activeDrawerEmp.hiredDate}
              </p>

              {/* Skills breakdown */}
              <div className="flex flex-wrap items-center gap-3 mt-3">
                <span className="text-xs bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800 font-mono">
                  Code: <strong className="text-sky-400">{activeDrawerEmp.skills.code}</strong>
                </span>
                <span className="text-xs bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800 font-mono">
                  Design: <strong className="text-emerald-400">{activeDrawerEmp.skills.design}</strong>
                </span>
                <span className="text-xs bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800 font-mono">
                  Art: <strong className="text-pink-400">{activeDrawerEmp.skills.art}</strong>
                </span>
                <span className="text-xs bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800 font-mono">
                  Audio: <strong className="text-amber-400">{activeDrawerEmp.skills.audio}</strong>
                </span>
                <span className="text-xs bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800 font-mono">
                  QA: <strong className="text-rose-400">{activeDrawerEmp.skills.qa}</strong>
                </span>
              </div>
            </div>
          </div>

          {/* Quick Training Actions */}
          <div className="flex flex-wrap items-center gap-2">
            <button
              onClick={() => trainEmployee(activeDrawerEmp.id, 'code', 2000)}
              className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
              title="Spend $2,000 to train Coding skill"
            >
              Train Code ($2k)
            </button>
            <button
              onClick={() => trainEmployee(activeDrawerEmp.id, 'design', 2000)}
              className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
              title="Spend $2,000 to train Game Design skill"
            >
              Train Design ($2k)
            </button>
            <button
              onClick={() => trainEmployee(activeDrawerEmp.id, 'art', 2000)}
              className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
              title="Spend $2,000 to train Art skill"
            >
              Train Art ($2k)
            </button>
            <button
              onClick={() => trainEmployee(activeDrawerEmp.id, 'qa', 2000)}
              className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
              title="Spend $2,000 to train QA skill"
            >
              Train QA ($2k)
            </button>
            <button
              onClick={() => setActiveDrawerEmp(null)}
              className="px-3 py-2 rounded-lg bg-rose-600/20 hover:bg-rose-600/30 text-rose-300 text-xs font-semibold border border-rose-500/30 transition"
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
