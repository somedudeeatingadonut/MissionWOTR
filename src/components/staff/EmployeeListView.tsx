import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { Employee, EmployeeRole } from '../../types/employee';
import { getOfficeTierById } from '../../data/offices';
import { getTraitConfig } from '../../data/employeeTraits';
import { 
  Users, 
  UserPlus, 
  Zap, 
  Heart, 
  Award, 
  Code2, 
  PenTool, 
  Palette, 
  Music, 
  Bug, 
  Megaphone, 
  Trash2,
  GraduationCap
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface EmployeeListViewProps {
  onOpenHire: () => void;
}

const ROLE_ICONS: Record<EmployeeRole, React.ReactNode> = {
  programmer: <Code2 className="w-4 h-4 text-sky-400" />,
  designer: <PenTool className="w-4 h-4 text-emerald-400" />,
  artist: <Palette className="w-4 h-4 text-pink-400" />,
  audio_engineer: <Music className="w-4 h-4 text-amber-400" />,
  qa_tester: <Bug className="w-4 h-4 text-rose-400" />,
  marketing: <Megaphone className="w-4 h-4 text-purple-400" />,
  producer: <Award className="w-4 h-4 text-yellow-400" />
};

export const EmployeeListView: React.FC<EmployeeListViewProps> = ({ onOpenHire }) => {
  const { 
    company, 
    employees, 
    projects, 
    assignEmployee, 
    fireEmployee,
    trainEmployee 
  } = useGameStore();

  const [roleFilter, setRoleFilter] = useState<string>('all');
  const officeTier = getOfficeTierById(company.officeTierId);

  const filteredEmployees = employees.filter(e => {
    if (roleFilter === 'all') return true;
    return e.role === roleFilter;
  });

  const activeProjects = projects.filter(
    p => p.phase === 'production' || p.phase === 'alpha_testing' || p.phase === 'beta_polish'
  );

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h2 className="text-2xl font-bold text-white flex items-center space-x-2">
            <Users className="w-7 h-7 text-sky-400" />
            <span>Staff & Team Assignments ({employees.length} / {officeTier.maxEmployees})</span>
          </h2>
          <p className="text-sm text-slate-400 mt-0.5">
            Recruit top talent, assign staff to projects, and train developers to level up their skills.
          </p>
        </div>

        <button
          onClick={onOpenHire}
          disabled={employees.length >= officeTier.maxEmployees}
          className={`flex items-center space-x-2 px-5 py-2.5 rounded-xl font-bold text-xs shadow-lg transition ${
            employees.length < officeTier.maxEmployees
              ? 'bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white'
              : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
          }`}
        >
          <UserPlus className="w-4 h-4" />
          <span>Hire New Candidate</span>
        </button>
      </div>

      {/* Role Filter Badges */}
      <div className="flex flex-wrap items-center gap-2">
        <button
          onClick={() => { soundEngine.playClick(); setRoleFilter('all'); }}
          className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
            roleFilter === 'all'
              ? 'bg-sky-600 text-white shadow'
              : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
          }`}
        >
          All Staff ({employees.length})
        </button>
        {(['programmer', 'designer', 'artist', 'audio_engineer', 'qa_tester', 'marketing', 'producer'] as EmployeeRole[]).map((role) => {
          const count = employees.filter(e => e.role === role).length;
          const isActive = roleFilter === role;
          return (
            <button
              key={role}
              onClick={() => { soundEngine.playClick(); setRoleFilter(role); }}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition flex items-center space-x-1.5 ${
                isActive
                  ? 'bg-sky-600 text-white shadow'
                  : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              {ROLE_ICONS[role]}
              <span className="capitalize">{role.replace('_', ' ')} ({count})</span>
            </button>
          );
        })}
      </div>

      {/* Employee Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredEmployees.map((emp) => {
          return (
            <div
              key={emp.id}
              className="p-5 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 shadow-xl flex flex-col justify-between space-y-4"
            >
              {/* Top info */}
              <div className="flex items-start justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-12 h-12 rounded-2xl bg-slate-800 border border-slate-700 flex items-center justify-center font-bold text-lg text-sky-400 relative">
                    {emp.name.charAt(0)}
                    <div className="absolute -bottom-1 -right-1 p-1 rounded bg-slate-950 border border-slate-700">
                      {ROLE_ICONS[emp.role]}
                    </div>
                  </div>
                  <div>
                    <h4 className="font-bold text-base text-white">{emp.name}</h4>
                    <div className="text-xs text-slate-400 capitalize">
                      {emp.role.replace('_', ' ')} • Lvl {emp.level}
                    </div>
                    <div className="text-[11px] text-emerald-400 font-mono mt-0.5">
                      ${emp.monthlySalary}/mo
                    </div>
                  </div>
                </div>

                <button
                  onClick={() => {
                    if (window.confirm(`Are you sure you want to dismiss ${emp.name}?`)) {
                      fireEmployee(emp.id);
                    }
                  }}
                  className="p-1.5 rounded-lg text-slate-600 hover:text-rose-400 hover:bg-rose-500/10 transition"
                  title="Dismiss Employee"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>

              {/* Traits Badges */}
              {emp.traits.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {emp.traits.map((tId) => {
                    const tConfig = getTraitConfig(tId);
                    return (
                      <span
                        key={tId}
                        className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${
                          tConfig?.isPositive
                            ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                            : 'bg-amber-500/10 border-amber-500/30 text-amber-400'
                        }`}
                        title={tConfig?.effectSummary}
                      >
                        {tConfig?.name || tId}
                      </span>
                    );
                  })}
                </div>
              )}

              {/* Skills breakdown bars */}
              <div className="space-y-1.5 pt-1 border-t border-slate-800/80">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400">Coding</span>
                  <span className="font-mono text-sky-400 font-bold">{emp.skills.code}</span>
                </div>
                <div className="w-full bg-slate-950 h-1.5 rounded-full overflow-hidden">
                  <div className="h-full bg-sky-500" style={{ width: `${emp.skills.code}%` }} />
                </div>

                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400">Game Design</span>
                  <span className="font-mono text-emerald-400 font-bold">{emp.skills.design}</span>
                </div>
                <div className="w-full bg-slate-950 h-1.5 rounded-full overflow-hidden">
                  <div className="h-full bg-emerald-500" style={{ width: `${emp.skills.design}%` }} />
                </div>

                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400">Art & Visuals</span>
                  <span className="font-mono text-pink-400 font-bold">{emp.skills.art}</span>
                </div>
                <div className="w-full bg-slate-950 h-1.5 rounded-full overflow-hidden">
                  <div className="h-full bg-pink-500" style={{ width: `${emp.skills.art}%` }} />
                </div>

                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400">Audio / QA</span>
                  <span className="font-mono text-amber-400 font-bold">
                    {emp.skills.audio} / {emp.skills.qa}
                  </span>
                </div>
              </div>

              {/* Energy & Morale gauges */}
              <div className="grid grid-cols-2 gap-2 pt-1">
                <div className="p-2 rounded-xl bg-slate-950 border border-slate-800">
                  <div className="text-[10px] text-slate-400 flex items-center space-x-1">
                    <Zap className="w-3 h-3 text-amber-400" />
                    <span>Energy</span>
                  </div>
                  <div className="font-mono text-sm font-bold text-white mt-0.5">
                    {emp.energy}%
                  </div>
                </div>

                <div className="p-2 rounded-xl bg-slate-950 border border-slate-800">
                  <div className="text-[10px] text-slate-400 flex items-center space-x-1">
                    <Heart className="w-3 h-3 text-rose-400" />
                    <span>Morale</span>
                  </div>
                  <div className="font-mono text-sm font-bold text-white mt-0.5">
                    {emp.morale}%
                  </div>
                </div>
              </div>

              {/* Assignment Selector & Training Button */}
              <div className="pt-2 border-t border-slate-800/80 space-y-2">
                <div>
                  <label className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block mb-1">
                    Assign To Project / Team
                  </label>
                  <select
                    value={emp.assignedProjectId || ''}
                    onChange={(e) => {
                      const val = e.target.value || null;
                      assignEmployee(emp.id, val);
                    }}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none focus:border-sky-500"
                  >
                    <option value="">-- Idle / Research Pool --</option>
                    {activeProjects.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.title} ({p.phase.replace('_', ' ')})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="flex space-x-1.5">
                  <button
                    onClick={() => trainEmployee(emp.id, 'code', 2000)}
                    disabled={company.cash < 2000}
                    className="flex-1 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-[11px] font-semibold border border-slate-700 transition flex items-center justify-center space-x-1"
                    title="Train Coding skill (+$2,000)"
                  >
                    <GraduationCap className="w-3.5 h-3.5 text-sky-400" />
                    <span>Train Code</span>
                  </button>
                  <button
                    onClick={() => trainEmployee(emp.id, 'design', 2000)}
                    disabled={company.cash < 2000}
                    className="flex-1 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-[11px] font-semibold border border-slate-700 transition flex items-center justify-center space-x-1"
                    title="Train Game Design skill (+$2,000)"
                  >
                    <GraduationCap className="w-3.5 h-3.5 text-emerald-400" />
                    <span>Train Design</span>
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
