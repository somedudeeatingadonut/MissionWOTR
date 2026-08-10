import { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { TECH_TREE } from '../../data/techTree';
import { TechCategory } from '../../types/engine';
import { 
  Cpu, 
  CheckCircle2, 
  Lock, 
  Plus, 
  Sparkles, 
  DollarSign
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface TechTreeViewProps {
  onOpenEngineCreator: () => void;
}

export const TechTreeView: React.FC<TechTreeViewProps> = ({ onOpenEngineCreator }) => {
  const { 
    company, 
    unlockedTechIds, 
    activeResearchTechId, 
    startResearch, 
    engines 
  } = useGameStore();

  const [categoryFilter, setCategoryFilter] = useState<string>('all');

  const filteredNodes = TECH_TREE.filter(t => {
    if (categoryFilter === 'all') return true;
    return t.category === categoryFilter;
  });

  const categories: { id: string; label: string }[] = [
    { id: 'all', label: 'All Technologies' },
    { id: 'graphics', label: 'Graphics & Rendering' },
    { id: 'physics', label: 'Physics & Simulation' },
    { id: 'ai', label: 'Artificial Intelligence' },
    { id: 'audio', label: 'Audio & Acoustics' },
    { id: 'networking', label: 'Networking & MMO' },
    { id: 'workflow', label: 'Workflow & Tools' }
  ];

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h2 className="text-2xl font-bold text-white flex items-center space-x-2">
            <Cpu className="w-7 h-7 text-sky-400" />
            <span>Research Tech Tree & Proprietary Engines</span>
          </h2>
          <p className="text-sm text-slate-400 mt-0.5">
            Research cutting-edge technologies and combine them into custom engines to license to rival studios.
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <button
            onClick={onOpenEngineCreator}
            className="flex items-center space-x-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white font-bold text-xs px-5 py-2.5 rounded-xl shadow-lg transition"
          >
            <Plus className="w-4 h-4" />
            <span>Create Custom Engine ({engines.length} Active)</span>
          </button>
        </div>
      </div>

      {/* Category Filter Pills */}
      <div className="flex flex-wrap items-center gap-2">
        {categories.map(cat => {
          const isActive = categoryFilter === cat.id;
          return (
            <button
              key={cat.id}
              onClick={() => { soundEngine.playClick(); setCategoryFilter(cat.id); }}
              className={`px-4 py-2 rounded-xl text-xs font-semibold transition ${
                isActive
                  ? 'bg-sky-600 text-white shadow-md'
                  : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              {cat.label}
            </button>
          );
        })}
      </div>

      {/* Custom Engines Banner list */}
      {engines.length > 0 && (
        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-base text-white flex items-center space-x-2">
              <Cpu className="w-5 h-5 text-sky-400" />
              <span>Studio Proprietary Engines</span>
            </h3>
            <span className="text-xs text-slate-400 font-mono">
              Earns passive royalties from rival studios
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {engines.map(eng => (
              <div
                key={eng.id}
                className="p-4 rounded-xl bg-slate-950 border border-slate-800 flex items-center justify-between"
              >
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-bold text-sm text-white">{eng.name}</span>
                    <span className="px-1.5 py-0.5 rounded bg-sky-500/10 text-sky-400 text-[10px] font-mono">
                      v{eng.version}
                    </span>
                  </div>
                  <div className="text-xs text-slate-400 mt-1">
                    {eng.unlockedFeatureIds.length} Tech Features • Upfront fee: ${eng.licensingPrice.toLocaleString()} • Royalty: {eng.royaltyPercentage}%
                  </div>
                  {eng.activeLicensingStudios.length > 0 && (
                    <div className="text-[11px] text-emerald-400 mt-1">
                      Licensed to: {eng.activeLicensingStudios.join(', ')}
                    </div>
                  )}
                </div>

                <div className="text-right font-mono">
                  <div className="text-[10px] text-slate-400">Royalty Earned</div>
                  <div className="text-sm font-bold text-emerald-400">
                    ${eng.totalRoyaltyEarned.toLocaleString()}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tech Tree Nodes Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredNodes.map(tech => {
          const isUnlocked = unlockedTechIds.includes(tech.id);
          const isResearching = activeResearchTechId === tech.id;
          const prereqsMet = tech.prerequisites.every(pId => unlockedTechIds.includes(pId));
          const canAfford = company.cash >= tech.researchCost;

          let cardBorder = 'border-slate-800 bg-slate-900';
          if (isUnlocked) cardBorder = 'border-emerald-500/40 bg-slate-900/95';
          if (isResearching) cardBorder = 'border-sky-500 bg-slate-900/90 shadow-lg shadow-sky-500/10';
          if (!prereqsMet) cardBorder = 'border-slate-800/60 bg-slate-950/60 opacity-60';

          return (
            <div
              key={tech.id}
              className={`p-5 rounded-2xl border transition flex flex-col justify-between space-y-4 ${cardBorder}`}
            >
              <div>
                <div className="flex items-start justify-between">
                  <div>
                    <span className="text-[10px] uppercase font-bold tracking-wider text-slate-400">
                      {tech.category} • Era: {tech.unlockYear}
                    </span>
                    <h3 className="font-bold text-base text-white mt-0.5">
                      {tech.name}
                    </h3>
                  </div>

                  {isUnlocked ? (
                    <div className="p-1.5 rounded-lg bg-emerald-500/10 text-emerald-400" title="Unlocked">
                      <CheckCircle2 className="w-5 h-5" />
                    </div>
                  ) : isResearching ? (
                    <span className="px-2 py-0.5 rounded bg-sky-500/10 text-sky-400 text-xs font-mono font-bold animate-pulse">
                      Researching...
                    </span>
                  ) : !prereqsMet ? (
                    <div className="p-1.5 rounded-lg bg-slate-800 text-slate-500" title="Prerequisites not met">
                      <Lock className="w-4 h-4" />
                    </div>
                  ) : null}
                </div>

                <p className="text-xs text-slate-300 mt-2 leading-relaxed">
                  {tech.description}
                </p>

                {/* Stat Bonuses */}
                <div className="flex flex-wrap gap-2 mt-3 text-[11px]">
                  {tech.statBonus.qualityBoost && (
                    <span className="px-2 py-0.5 rounded bg-sky-500/10 text-sky-300 font-mono">
                      +{tech.statBonus.qualityBoost}% Quality
                    </span>
                  )}
                  {tech.statBonus.bugReduction && (
                    <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-300 font-mono">
                      -{tech.statBonus.bugReduction}% Bugs
                    </span>
                  )}
                  {tech.statBonus.devSpeedBonus && (
                    <span className="px-2 py-0.5 rounded bg-indigo-500/10 text-indigo-300 font-mono">
                      +{tech.statBonus.devSpeedBonus}% Speed
                    </span>
                  )}
                  {tech.statBonus.hypeBonus && (
                    <span className="px-2 py-0.5 rounded bg-purple-500/10 text-purple-300 font-mono">
                      +{tech.statBonus.hypeBonus} Hype
                    </span>
                  )}
                </div>
              </div>

              {/* Research progress or Action button */}
              <div className="pt-3 border-t border-slate-800/80">
                {isUnlocked ? (
                  <div className="text-xs font-bold text-emerald-400 flex items-center space-x-1.5">
                    <CheckCircle2 className="w-4 h-4" />
                    <span>Technology Available in Engine Creator</span>
                  </div>
                ) : isResearching ? (
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-sky-400 font-semibold">Research in progress</span>
                      <span className="font-mono text-slate-300">
                        {Math.round((tech.progress / tech.workPointsRequired) * 100)}%
                      </span>
                    </div>
                    <div className="w-full bg-slate-950 h-2 rounded-full overflow-hidden border border-slate-800">
                      <div
                        className="h-full bg-sky-500 transition-all duration-300"
                        style={{ width: `${Math.round((tech.progress / tech.workPointsRequired) * 100)}%` }}
                      />
                    </div>
                  </div>
                ) : !prereqsMet ? (
                  <div className="text-xs text-slate-500">
                    Requires: {tech.prerequisites.join(', ')}
                  </div>
                ) : (
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-mono font-bold text-slate-300 flex items-center space-x-1">
                      <DollarSign className="w-3.5 h-3.5 text-emerald-400" />
                      <span>{tech.researchCost.toLocaleString()}</span>
                    </span>

                    <button
                      onClick={() => startResearch(tech.id)}
                      disabled={!canAfford}
                      className={`px-4 py-2 rounded-xl font-bold text-xs shadow-md transition ${
                        canAfford
                          ? 'bg-sky-600 hover:bg-sky-500 text-white'
                          : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
                      }`}
                    >
                      Research Tech
                    </button>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
