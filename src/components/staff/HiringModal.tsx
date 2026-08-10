import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { JobApplicant, EmployeeRole, TraitId } from '../../types/employee';
import { EMPLOYEE_TRAITS } from '../../data/employeeTraits';
import { 
  UserPlus, 
  X, 
  DollarSign, 
  Code2, 
  PenTool, 
  Palette, 
  Music, 
  Bug, 
  Megaphone, 
  Award,
  Sparkles,
  RefreshCw
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface HiringModalProps {
  onClose: () => void;
}

const FIRST_NAMES = [
  'Liam', 'Olivia', 'Noah', 'Emma', 'Oliver', 'Ava', 'Elijah', 'Sophia', 'James', 'Isabella',
  'Benjamin', 'Mia', 'Lucas', 'Evelyn', 'Henry', 'Harper', 'Alexander', 'Camila', 'Sebastian', 'Gianna'
];

const LAST_NAMES = [
  'Smith', 'Johnson', 'Williams', 'Brown', 'Jones', 'Garcia', 'Miller', 'Davis', 'Rodriguez', 'Martinez',
  'Hernandez', 'Lopez', 'Gonzalez', 'Wilson', 'Anderson', 'Thomas', 'Taylor', 'Moore', 'Jackson', 'Martin'
];

const ROLES: EmployeeRole[] = [
  'programmer', 'designer', 'artist', 'audio_engineer', 'qa_tester', 'marketing', 'producer'
];

function generateCandidates(count: number): JobApplicant[] {
  return Array.from({ length: count }, (_, i) => {
    const role = ROLES[Math.floor(Math.random() * ROLES.length)];
    const level = Math.floor(Math.random() * 3) + 1; // 1 to 3
    const firstName = FIRST_NAMES[Math.floor(Math.random() * FIRST_NAMES.length)];
    const lastName = LAST_NAMES[Math.floor(Math.random() * LAST_NAMES.length)];
    const name = `${firstName} ${lastName}`;

    const baseSkill = 40 + level * 15 + Math.floor(Math.random() * 15);
    const skills = {
      code: role === 'programmer' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      design: role === 'designer' || role === 'producer' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      art: role === 'artist' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      audio: role === 'audio_engineer' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      qa: role === 'qa_tester' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      marketing: role === 'marketing' ? baseSkill : Math.floor(Math.random() * 45) + 15,
      management: role === 'producer' ? baseSkill : Math.floor(Math.random() * 45) + 15
    };

    // Pick 1 or 2 traits
    const shuffledTraits = [...EMPLOYEE_TRAITS].sort(() => Math.random() - 0.5);
    const traits: TraitId[] = [
      shuffledTraits[0].id,
      ...(Math.random() < 0.4 ? [shuffledTraits[1].id] : [])
    ];

    const askingSalary = Math.round((level * 1400 + Math.random() * 400) / 50) * 50;
    const signingBonus = Math.round(askingSalary * 1.5);

    return {
      id: `applicant_${i}_${Date.now()}`,
      name,
      avatarSeed: name.toLowerCase().replace(' ', '_'),
      role,
      level,
      skills,
      intelligence: 75 + Math.floor(Math.random() * 20),
      creativity: 75 + Math.floor(Math.random() * 20),
      workEthic: 75 + Math.floor(Math.random() * 20),
      stressTolerance: 75 + Math.floor(Math.random() * 20),
      speed: 75 + Math.floor(Math.random() * 20),
      morale: 95,
      stress: 5,
      energy: 100,
      monthlySalary: askingSalary,
      askingSalary,
      signingBonus,
      hiredDate: 'new',
      traits,
      assignedProjectId: null
    };
  });
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

export const HiringModal: React.FC<HiringModalProps> = ({ onClose }) => {
  const { company, hireEmployee } = useGameStore();
  const [candidates, setCandidates] = useState<JobApplicant[]>(() => generateCandidates(4));

  const handleRefresh = () => {
    soundEngine.playClick();
    setCandidates(generateCandidates(4));
  };

  const handleHire = (applicant: JobApplicant) => {
    hireEmployee(applicant);
    setCandidates(candidates.filter(c => c.id !== applicant.id));
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-4xl w-full shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white">
              <UserPlus className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white leading-none">
                Recruit Candidates & Talent Search
              </h3>
              <p className="text-xs text-slate-400 mt-1">
                Interview candidates, inspect skills and traits, and expand your studio staff.
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={handleRefresh}
              className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
              title="Refresh candidate pool"
            >
              <RefreshCw className="w-3.5 h-3.5 text-sky-400" />
              <span>Refresh Candidates</span>
            </button>
            <button
              onClick={onClose}
              className="text-slate-500 hover:text-white p-1 rounded-lg transition"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Candidates Grid */}
        <div className="p-6 overflow-y-auto flex-1 grid grid-cols-1 md:grid-cols-2 gap-4">
          {candidates.length === 0 ? (
            <div className="col-span-2 text-center py-12 space-y-3">
              <p className="text-sm text-slate-400">All available candidates in this batch were hired!</p>
              <button
                onClick={handleRefresh}
                className="px-5 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs"
              >
                Search More Candidates
              </button>
            </div>
          ) : (
            candidates.map((cand) => {
              const primarySkillVal = cand.skills[cand.role === 'producer' ? 'management' : cand.role === 'audio_engineer' ? 'audio' : cand.role === 'qa_tester' ? 'qa' : cand.role as keyof typeof cand.skills] || cand.skills.code;

              return (
                <div
                  key={cand.id}
                  className="p-5 rounded-2xl bg-slate-950 border border-slate-800 flex flex-col justify-between space-y-4"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex items-center space-x-3">
                      <div className="w-12 h-12 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-center font-bold text-lg text-sky-400 relative">
                        {cand.name.charAt(0)}
                        <div className="absolute -bottom-1 -right-1 p-1 rounded bg-slate-950 border border-slate-700">
                          {ROLE_ICONS[cand.role]}
                        </div>
                      </div>
                      <div>
                        <h4 className="font-bold text-base text-white">{cand.name}</h4>
                        <div className="text-xs text-slate-400 capitalize">
                          {cand.role.replace('_', ' ')} • Level {cand.level}
                        </div>
                      </div>
                    </div>

                    <div className="text-right">
                      <div className="font-mono text-xs font-bold text-emerald-400">
                        ${cand.askingSalary}/mo
                      </div>
                      <div className="text-[10px] text-slate-500">
                        Bonus: ${cand.signingBonus}
                      </div>
                    </div>
                  </div>

                  {/* Skills summary */}
                  <div className="grid grid-cols-2 gap-2 bg-slate-900/60 p-3 rounded-xl border border-slate-800/80 text-xs">
                    <div>
                      <span className="text-slate-400">Primary Skill: </span>
                      <strong className="font-mono text-white">{primarySkillVal}/100</strong>
                    </div>
                    <div>
                      <span className="text-slate-400">Coding: </span>
                      <strong className="font-mono text-sky-400">{cand.skills.code}</strong>
                    </div>
                    <div>
                      <span className="text-slate-400">Design: </span>
                      <strong className="font-mono text-emerald-400">{cand.skills.design}</strong>
                    </div>
                    <div>
                      <span className="text-slate-400">Art / Visual: </span>
                      <strong className="font-mono text-pink-400">{cand.skills.art}</strong>
                    </div>
                  </div>

                  {/* Traits */}
                  {cand.traits.length > 0 && (
                    <div className="flex flex-wrap gap-1.5">
                      {cand.traits.map((tId) => {
                        const t = EMPLOYEE_TRAITS.find(tr => tr.id === tId);
                        return (
                          <span
                            key={tId}
                            className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${
                              t?.isPositive
                                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                                : 'bg-amber-500/10 border-amber-500/30 text-amber-400'
                            }`}
                          >
                            {t?.name || tId}
                          </span>
                        );
                      })}
                    </div>
                  )}

                  {/* Hire button */}
                  <button
                    onClick={() => handleHire(cand)}
                    disabled={company.cash < cand.signingBonus}
                    className={`w-full py-2.5 rounded-xl font-bold text-xs shadow-md transition flex items-center justify-center space-x-2 ${
                      company.cash >= cand.signingBonus
                        ? 'bg-sky-600 hover:bg-sky-500 text-white'
                        : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
                    }`}
                  >
                    <UserPlus className="w-4 h-4" />
                    <span>Hire Candidate (${cand.signingBonus.toLocaleString()} Signing Bonus)</span>
                  </button>
                </div>
              );
            })
          )}
        </div>

        {/* Footer */}
        <div className="p-4 bg-slate-950 border-t border-slate-800 flex items-center justify-between">
          <div className="text-xs text-slate-400 flex items-center space-x-1">
            <DollarSign className="w-4 h-4 text-emerald-400" />
            <span>Studio Bank Cash: <strong className="text-white">${company.cash.toLocaleString()}</strong></span>
          </div>

          <button
            onClick={onClose}
            className="px-5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
          >
            Close Window
          </button>
        </div>
      </div>
    </div>
  );
};
