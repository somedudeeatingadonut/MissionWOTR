import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { getGenreById } from '../../data/genres';
import { getThemeById } from '../../data/themes';
import { GameProject } from '../../types/game';
import { 
  Gamepad2, 
  Bug, 
  AlertTriangle, 
  Sparkles, 
  TrendingUp, 
  DollarSign, 
  Users, 
  Plus, 
  Flame, 
  Zap,
  CheckCircle,
  PackagePlus,
  BarChart3
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface ProjectListViewProps {
  onOpenNewGame: () => void;
}

export const ProjectListView: React.FC<ProjectListViewProps> = ({ onOpenNewGame }) => {
  const { 
    projects, 
    releaseGame, 
    toggleCrunchMode, 
    addFeatureCreep, 
    addDLC,
    selectProject
  } = useGameStore();

  const [filter, setFilter] = useState<'active' | 'released'>('active');
  const [dlcTitle, setDlcTitle] = useState('');
  const [selectedDlcProject, setSelectedDlcProject] = useState<string | null>(null);

  const activeProjects = projects.filter(
    p => p.phase === 'concept' || p.phase === 'pre_production' || p.phase === 'production' || p.phase === 'alpha_testing' || p.phase === 'beta_polish'
  );

  const releasedProjects = projects.filter(
    p => p.phase === 'released' || p.phase === 'live_service'
  );

  const handleRelease = (proj: GameProject) => {
    soundEngine.playAward();
    releaseGame(proj.id);
  };

  const handleCreateDlc = (projId: string) => {
    if (!dlcTitle.trim()) return;
    addDLC(projId, dlcTitle.trim(), 19.99);
    setDlcTitle('');
    setSelectedDlcProject(null);
  };

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h2 className="text-2xl font-bold text-white flex items-center space-x-2">
            <Gamepad2 className="w-7 h-7 text-sky-400" />
            <span>Game Projects & Live Pipeline</span>
          </h2>
          <p className="text-sm text-slate-400 mt-0.5">
            Manage active development pipelines, crunch policies, and post-launch catalog monetizations.
          </p>
        </div>

        <div className="flex items-center space-x-3">
          {/* Tab switches */}
          <div className="flex items-center bg-slate-900 p-1 rounded-xl border border-slate-800">
            <button
              onClick={() => setFilter('active')}
              className={`px-4 py-1.5 rounded-lg text-xs font-semibold transition ${
                filter === 'active'
                  ? 'bg-sky-600 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              In Production ({activeProjects.length})
            </button>
            <button
              onClick={() => setFilter('released')}
              className={`px-4 py-1.5 rounded-lg text-xs font-semibold transition ${
                filter === 'released'
                  ? 'bg-sky-600 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Released & Live Ops ({releasedProjects.length})
            </button>
          </div>

          <button
            onClick={onOpenNewGame}
            className="flex items-center space-x-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white font-bold text-xs px-4 py-2.5 rounded-xl shadow-lg transition"
          >
            <Plus className="w-4 h-4" />
            <span>New Game</span>
          </button>
        </div>
      </div>

      {/* ACTIVE DEVELOPMENT TAB */}
      {filter === 'active' && (
        <div className="space-y-6">
          {activeProjects.length === 0 ? (
            <div className="p-12 rounded-2xl bg-slate-900/50 border border-slate-800 text-center space-y-3">
              <Gamepad2 className="w-12 h-12 text-slate-600 mx-auto" />
              <h3 className="text-lg font-bold text-white">No Active Games in Development</h3>
              <p className="text-sm text-slate-400 max-w-md mx-auto">
                Your studio developers are idle or researching! Click &ldquo;New Game&rdquo; to start pitch design and production.
              </p>
              <button
                onClick={onOpenNewGame}
                className="mt-2 px-5 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs inline-flex items-center space-x-2 shadow-md transition"
              >
                <Plus className="w-4 h-4" />
                <span>Start New Game Project</span>
              </button>
            </div>
          ) : (
            activeProjects.map((proj) => {
              const genre = getGenreById(proj.genreId);
              const theme = getThemeById(proj.themeId);

              // Completion percentages
              const codePct = Math.min(100, Math.round((proj.progress.code / proj.totalRequirements.code) * 100));
              const designPct = Math.min(100, Math.round((proj.progress.design / proj.totalRequirements.design) * 100));
              const artPct = Math.min(100, Math.round((proj.progress.art / proj.totalRequirements.art) * 100));
              const audioPct = Math.min(100, Math.round((proj.progress.audio / proj.totalRequirements.audio) * 100));
              const qaPct = Math.min(100, Math.round((proj.progress.qa / proj.totalRequirements.qa) * 100));

              const overallPct = Math.round(
                (codePct + designPct + artPct + audioPct + qaPct) / 5
              );
              const isReadyForRelease = overallPct >= 70;

              return (
                <div
                  key={proj.id}
                  onClick={() => selectProject(proj.id)}
                  className="p-6 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 shadow-xl space-y-6 transition"
                >
                  {/* Card Header */}
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                      <div className="flex items-center space-x-2.5">
                        <h3 className="text-xl font-bold text-white">{proj.title}</h3>
                        <span className="px-2 py-0.5 rounded-lg bg-sky-500/10 text-sky-400 border border-sky-500/30 text-xs font-semibold">
                          {genre.name}
                        </span>
                        <span className="px-2 py-0.5 rounded-lg bg-indigo-500/10 text-indigo-300 border border-indigo-500/30 text-xs font-semibold">
                          {theme.name}
                        </span>
                        <span className="px-2 py-0.5 rounded-lg bg-slate-800 text-slate-300 border border-slate-700 text-xs uppercase font-mono">
                          {proj.scale}
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 mt-1">
                        Audience: <span className="capitalize text-slate-300">{proj.audience}</span> | Phase: <span className="capitalize text-sky-400 font-semibold">{proj.phase.replace('_', ' ')}</span>
                      </p>
                    </div>

                    {/* Metrics Badge Group */}
                    <div className="flex flex-wrap items-center gap-3">
                      {/* Bugs */}
                      <div
                        className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-xl border ${
                          proj.bugs > 15
                            ? 'bg-rose-500/10 border-rose-500/40 text-rose-400'
                            : 'bg-slate-950 border-slate-800 text-slate-300'
                        }`}
                        title="Known unresolved technical bugs"
                      >
                        <Bug className="w-4 h-4" />
                        <span className="text-xs font-mono font-bold">{proj.bugs} Bugs</span>
                      </div>

                      {/* Tech Debt */}
                      <div
                        className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-xl border ${
                          proj.techDebt > 30
                            ? 'bg-amber-500/10 border-amber-500/40 text-amber-400'
                            : 'bg-slate-950 border-slate-800 text-slate-300'
                        }`}
                        title="Technical debt slowing down coding speed"
                      >
                        <AlertTriangle className="w-4 h-4" />
                        <span className="text-xs font-mono font-bold">{proj.techDebt} Tech Debt</span>
                      </div>

                      {/* Polish */}
                      <div
                        className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-950 border border-slate-800 text-emerald-400"
                        title="Polish points earned from QA and Perfectionist developers"
                      >
                        <Sparkles className="w-4 h-4" />
                        <span className="text-xs font-mono font-bold">{proj.polishPoints} Polish</span>
                      </div>

                      {/* Hype */}
                      <div
                        className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-950 border border-slate-800 text-purple-400"
                        title="Pre-launch marketing Hype"
                      >
                        <Flame className="w-4 h-4" />
                        <span className="text-xs font-mono font-bold">{proj.hype} Hype</span>
                      </div>

                      {/* Estimated Quality Score */}
                      <div className="px-3 py-1.5 rounded-xl bg-sky-500/10 border border-sky-500/30 text-sky-300 text-xs font-bold">
                        Est. Quality: {proj.qualityScore}/100
                      </div>
                    </div>
                  </div>

                  {/* Detailed Multi-Phase Progress Bars */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
                    {/* Code */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-semibold text-sky-400">Code</span>
                        <span className="font-mono text-slate-400">{codePct}%</span>
                      </div>
                      <div className="w-full bg-slate-950 h-2.5 rounded-full overflow-hidden border border-slate-800">
                        <div
                          className="h-full bg-sky-500 transition-all duration-300"
                          style={{ width: `${codePct}%` }}
                        />
                      </div>
                      <div className="text-[10px] text-slate-500 font-mono">
                        {Math.round(proj.progress.code)} / {proj.totalRequirements.code} pts
                      </div>
                    </div>

                    {/* Design */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-semibold text-emerald-400">Design</span>
                        <span className="font-mono text-slate-400">{designPct}%</span>
                      </div>
                      <div className="w-full bg-slate-950 h-2.5 rounded-full overflow-hidden border border-slate-800">
                        <div
                          className="h-full bg-emerald-500 transition-all duration-300"
                          style={{ width: `${designPct}%` }}
                        />
                      </div>
                      <div className="text-[10px] text-slate-500 font-mono">
                        {Math.round(proj.progress.design)} / {proj.totalRequirements.design} pts
                      </div>
                    </div>

                    {/* Art */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-semibold text-pink-400">Art & Visuals</span>
                        <span className="font-mono text-slate-400">{artPct}%</span>
                      </div>
                      <div className="w-full bg-slate-950 h-2.5 rounded-full overflow-hidden border border-slate-800">
                        <div
                          className="h-full bg-pink-500 transition-all duration-300"
                          style={{ width: `${artPct}%` }}
                        />
                      </div>
                      <div className="text-[10px] text-slate-500 font-mono">
                        {Math.round(proj.progress.art)} / {proj.totalRequirements.art} pts
                      </div>
                    </div>

                    {/* Audio */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-semibold text-amber-400">Audio</span>
                        <span className="font-mono text-slate-400">{audioPct}%</span>
                      </div>
                      <div className="w-full bg-slate-950 h-2.5 rounded-full overflow-hidden border border-slate-800">
                        <div
                          className="h-full bg-amber-500 transition-all duration-300"
                          style={{ width: `${audioPct}%` }}
                        />
                      </div>
                      <div className="text-[10px] text-slate-500 font-mono">
                        {Math.round(proj.progress.audio)} / {proj.totalRequirements.audio} pts
                      </div>
                    </div>

                    {/* QA & Polish */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-semibold text-rose-400">QA & Bugfix</span>
                        <span className="font-mono text-slate-400">{qaPct}%</span>
                      </div>
                      <div className="w-full bg-slate-950 h-2.5 rounded-full overflow-hidden border border-slate-800">
                        <div
                          className="h-full bg-rose-500 transition-all duration-300"
                          style={{ width: `${qaPct}%` }}
                        />
                      </div>
                      <div className="text-[10px] text-slate-500 font-mono">
                        {Math.round(proj.progress.qa)} / {proj.totalRequirements.qa} pts
                      </div>
                    </div>
                  </div>

                  {/* Actions Bar: Crunch, Feature Creep, and Release */}
                  <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4 pt-2 border-t border-slate-800/80">
                    {/* Crunch Policy Controls */}
                    <div className="flex items-center space-x-2">
                      <span className="text-xs text-slate-400">Crunch Mode:</span>
                      <div className="flex items-center bg-slate-950 p-1 rounded-xl border border-slate-800">
                        <button
                          onClick={(e) => { e.stopPropagation(); toggleCrunchMode(proj.id, 'normal'); }}
                          className={`px-3 py-1 rounded-lg text-xs transition ${
                            proj.crunchMode === 'normal'
                              ? 'bg-sky-600 text-white font-semibold'
                              : 'text-slate-400 hover:text-white'
                          }`}
                        >
                          Normal (1x)
                        </button>
                        <button
                          onClick={(e) => { e.stopPropagation(); toggleCrunchMode(proj.id, 'mild_overtime'); }}
                          className={`px-3 py-1 rounded-lg text-xs transition ${
                            proj.crunchMode === 'mild_overtime'
                              ? 'bg-amber-600 text-white font-semibold'
                              : 'text-slate-400 hover:text-white'
                          }`}
                        >
                          Overtime (1.25x)
                        </button>
                        <button
                          onClick={(e) => { e.stopPropagation(); toggleCrunchMode(proj.id, 'heavy_crunch'); }}
                          className={`px-3 py-1 rounded-lg text-xs transition ${
                            proj.crunchMode === 'heavy_crunch'
                              ? 'bg-rose-600 text-white font-semibold'
                              : 'text-slate-400 hover:text-white'
                          }`}
                        >
                          Heavy Crunch (1.6x)
                        </button>
                      </div>
                    </div>

                    {/* Right Actions */}
                    <div className="flex items-center space-x-3">
                      <button
                        onClick={(e) => { e.stopPropagation(); addFeatureCreep(proj.id); }}
                        className="px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition flex items-center space-x-1.5"
                        title="Add scope to boost potential hype (+15% required work)"
                      >
                        <Plus className="w-3.5 h-3.5 text-sky-400" />
                        <span>Add Feature Creep</span>
                      </button>

                      <button
                        onClick={(e) => { e.stopPropagation(); handleRelease(proj); }}
                        disabled={!isReadyForRelease}
                        className={`px-5 py-2.5 rounded-xl font-bold text-xs shadow-lg flex items-center space-x-2 transition transform active:scale-95 ${
                          isReadyForRelease
                            ? 'bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white animate-pulse'
                            : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
                        }`}
                      >
                        <CheckCircle className="w-4 h-4" />
                        <span>Release Game ({overallPct}%)</span>
                      </button>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}

      {/* RELEASED & LIVE OPERATIONS TAB */}
      {filter === 'released' && (
        <div className="space-y-6">
          {releasedProjects.length === 0 ? (
            <div className="p-12 rounded-2xl bg-slate-900/50 border border-slate-800 text-center space-y-3">
              <CheckCircle className="w-12 h-12 text-slate-600 mx-auto" />
              <h3 className="text-lg font-bold text-white">No Released Games Yet</h3>
              <p className="text-sm text-slate-400 max-w-md mx-auto">
                Finish developing a game and click &ldquo;Release Game&rdquo; to start generating weekly sales and reviews!
              </p>
            </div>
          ) : (
            releasedProjects.map((proj) => {
              const genre = getGenreById(proj.genreId);

              // Recent sales bars
              const salesHistory = proj.weeklySalesHistory.slice(-8);
              const maxWeekly = Math.max(1, ...salesHistory);

              return (
                <div
                  key={proj.id}
                  className="p-6 rounded-2xl bg-slate-900 border border-slate-800 shadow-xl space-y-6"
                >
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                      <div className="flex items-center space-x-2.5">
                        <h3 className="text-xl font-bold text-white">{proj.title}</h3>
                        <span className="px-2 py-0.5 rounded-lg bg-sky-500/10 text-sky-400 text-xs font-semibold">
                          {genre.name}
                        </span>
                        {proj.isLiveService && (
                          <span className="px-2 py-0.5 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 text-xs font-semibold">
                            Live MMORPG World
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-slate-400 mt-1">
                        Released: <span className="text-slate-300">{proj.releasedAtDate || `${proj.createdAtYear}-10-01`}</span> | Base Price: <strong className="text-white">${proj.currentPrice}</strong>
                      </p>
                    </div>

                    {/* Review Score Badge */}
                    <div className="flex items-center space-x-4">
                      <div className="text-right">
                        <div className="text-xs text-slate-400">Critic Average</div>
                        <div className="font-mono text-2xl font-extrabold text-white">
                          {proj.reviewScore ?? proj.qualityScore}/100
                        </div>
                      </div>
                      <div
                        className={`w-14 h-14 rounded-2xl flex items-center justify-center font-extrabold text-xl shadow-lg ${
                          (proj.reviewScore ?? 70) >= 90
                            ? 'bg-gradient-to-br from-emerald-500 to-teal-600 text-white'
                            : (proj.reviewScore ?? 70) >= 80
                            ? 'bg-gradient-to-br from-sky-500 to-indigo-600 text-white'
                            : (proj.reviewScore ?? 70) >= 70
                            ? 'bg-gradient-to-br from-amber-500 to-yellow-600 text-white'
                            : 'bg-gradient-to-br from-rose-600 to-red-700 text-white'
                        }`}
                      >
                        {proj.reviewScore ?? proj.qualityScore}
                      </div>
                    </div>
                  </div>

                  {/* Financial & Player Stats Grid */}
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                      <div className="text-xs text-slate-400 flex items-center space-x-1.5">
                        <TrendingUp className="w-3.5 h-3.5 text-sky-400" />
                        <span>Total Units Sold</span>
                      </div>
                      <div className="font-mono text-lg font-bold text-white mt-1">
                        {proj.unitsSold.toLocaleString()}
                      </div>
                    </div>

                    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                      <div className="text-xs text-slate-400 flex items-center space-x-1.5">
                        <DollarSign className="w-3.5 h-3.5 text-emerald-400" />
                        <span>Total Revenue</span>
                      </div>
                      <div className="font-mono text-lg font-bold text-emerald-400 mt-1">
                        ${proj.revenue.toLocaleString()}
                      </div>
                    </div>

                    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                      <div className="text-xs text-slate-400 flex items-center space-x-1.5">
                        <Users className="w-3.5 h-3.5 text-indigo-400" />
                        <span>Active Concurrents</span>
                      </div>
                      <div className="font-mono text-lg font-bold text-indigo-300 mt-1">
                        {proj.isLiveService
                          ? `${proj.activePlayers.toLocaleString()} players`
                          : `${Math.round(proj.unitsSold * 0.08).toLocaleString()} players`}
                      </div>
                    </div>

                    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                      <div className="text-xs text-slate-400 flex items-center space-x-1.5">
                        <PackagePlus className="w-3.5 h-3.5 text-purple-400" />
                        <span>DLC Expansions</span>
                      </div>
                      <div className="font-mono text-lg font-bold text-purple-300 mt-1">
                        {proj.dlcList.length} Released
                      </div>
                    </div>
                  </div>

                  {/* Weekly Sales Mini Chart & DLC section */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-2">
                    <div className="md:col-span-2 space-y-2">
                      <div className="flex items-center justify-between text-xs text-slate-400">
                        <span className="flex items-center space-x-1 font-semibold text-slate-300">
                          <BarChart3 className="w-4 h-4 text-sky-400" />
                          <span>Weekly Units Sold (Last 8 Weeks)</span>
                        </span>
                        <span>Max week: {maxWeekly.toLocaleString()} units</span>
                      </div>

                      {/* SVG/CSS Bar Chart */}
                      <div className="h-24 bg-slate-950 rounded-xl p-3 border border-slate-800 flex items-end justify-between space-x-2">
                        {salesHistory.length === 0 ? (
                          <div className="w-full text-center text-xs text-slate-500 my-auto">
                            Sales will begin on the first weekly tick after release!
                          </div>
                        ) : (
                          salesHistory.map((val, idx) => {
                            const heightPct = Math.max(12, Math.round((val / maxWeekly) * 100));
                            return (
                              <div key={idx} className="flex-1 flex flex-col items-center group relative">
                                <div
                                  className="w-full bg-gradient-to-t from-sky-600 to-indigo-500 rounded-t-md hover:from-sky-400 hover:to-indigo-400 transition"
                                  style={{ height: `${heightPct}%` }}
                                  title={`Week ${idx + 1}: ${val.toLocaleString()} units`}
                                />
                              </div>
                            );
                          })
                        )}
                      </div>
                    </div>

                    {/* Add DLC Box */}
                    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 flex flex-col justify-between space-y-3">
                      <div>
                        <h4 className="font-bold text-sm text-white">Release Expansion DLC</h4>
                        <p className="text-xs text-slate-400 mt-0.5">
                          Launch a new DLC package to re-ignite player interest and generate revenue.
                        </p>
                      </div>

                      {selectedDlcProject === proj.id ? (
                        <div className="space-y-2">
                          <input
                            type="text"
                            placeholder="DLC Title (e.g. Dark Realms)"
                            value={dlcTitle}
                            onChange={(e) => setDlcTitle(e.target.value)}
                            className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none focus:border-sky-500"
                          />
                          <div className="flex space-x-2">
                            <button
                              onClick={() => handleCreateDlc(proj.id)}
                              className="flex-1 bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs py-1.5 rounded-lg transition"
                            >
                              Launch DLC ($19.99)
                            </button>
                            <button
                              onClick={() => setSelectedDlcProject(null)}
                              className="px-3 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs rounded-lg"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : (
                        <button
                          onClick={() => setSelectedDlcProject(proj.id)}
                          className="w-full py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition flex items-center justify-center space-x-2"
                        >
                          <PackagePlus className="w-4 h-4 text-purple-400" />
                          <span>Create DLC Package</span>
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
};
