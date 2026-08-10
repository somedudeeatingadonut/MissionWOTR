import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { GAME_GENRES } from '../../data/genres';
import { GAME_THEMES } from '../../data/themes';
import { getAvailablePlatforms } from '../../data/platforms';
import { GameGenreId, GameThemeId, Audience, GameScale } from '../../types/game';
import { 
  Gamepad2, 
  X, 
  ChevronRight, 
  ChevronLeft, 
  Sparkles, 
  Monitor, 
  Cpu, 
  CheckCircle2, 
  Info 
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface NewGameWizardProps {
  onClose: () => void;
}

export const NewGameWizard: React.FC<NewGameWizardProps> = ({ onClose }) => {
  const { date, engines, createNewGame } = useGameStore();

  const [step, setStep] = useState<number>(1);
  const [title, setTitle] = useState<string>('');
  const [genreId, setGenreId] = useState<GameGenreId>('rpg');
  const [themeId, setThemeId] = useState<GameThemeId>('fantasy');
  const [audience, setAudience] = useState<Audience>('teen');
  const [scale, setScale] = useState<GameScale>('aa');
  
  const availablePlatforms = getAvailablePlatforms(date.year);
  const [selectedPlatformIds, setSelectedPlatformIds] = useState<string[]>([
    availablePlatforms[0]?.id || 'pc_win'
  ]);
  const [targetEngineId, setTargetEngineId] = useState<string>(
    engines[0]?.id || 'default_engine'
  );

  const selectedGenreConfig = GAME_GENRES.find(g => g.id === genreId) || GAME_GENRES[0];

  const togglePlatform = (id: string) => {
    soundEngine.playClick();
    if (selectedPlatformIds.includes(id)) {
      if (selectedPlatformIds.length > 1) {
        setSelectedPlatformIds(selectedPlatformIds.filter(p => p !== id));
      }
    } else {
      setSelectedPlatformIds([...selectedPlatformIds, id]);
    }
  };

  const handleFinish = () => {
    if (!title.trim()) {
      return;
    }
    createNewGame({
      title: title.trim(),
      genreId,
      themeId,
      audience,
      scale,
      platformIds: selectedPlatformIds,
      targetEngineId,
      crunchMode: 'normal',
      featureCreepCount: 0,
      basePrice: scale === 'indie' ? 19.99 : scale === 'aa' ? 39.99 : 59.99,
      currentPrice: scale === 'indie' ? 19.99 : scale === 'aa' ? 39.99 : 59.99,
      isLiveService: genreId === 'mmorpg',
      serverCapacity: genreId === 'mmorpg' ? 100000 : 0,
      serverMonthlyCost: genreId === 'mmorpg' ? 15000 : 0
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-3xl w-full shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white">
              <Gamepad2 className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white leading-none">
                New Game Project Wizard
              </h3>
              <p className="text-xs text-slate-400 mt-1">
                Step {step} of 4: {step === 1 ? 'Concept & Genre' : step === 2 ? 'Audience & Scale' : step === 3 ? 'Platforms & Engine' : 'Review & Start'}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-slate-500 hover:text-white p-1 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Wizard Content Body */}
        <div className="p-6 overflow-y-auto flex-1 space-y-6">
          {/* STEP 1: Title, Genre, & Theme */}
          {step === 1 && (
            <div className="space-y-6">
              {/* Game Title Input */}
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                  Game Title
                </label>
                <input
                  type="text"
                  placeholder="e.g. Wrath of the Righteous, Cyberpunk Odyssey..."
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl px-4 py-3 text-base text-white font-semibold focus:outline-none focus:border-sky-500 transition"
                  autoFocus
                />
              </div>

              {/* Genre Selection Grid */}
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                  Select Game Genre
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                  {GAME_GENRES.map((g) => {
                    const isSelected = genreId === g.id;
                    return (
                      <button
                        key={g.id}
                        type="button"
                        onClick={() => { soundEngine.playClick(); setGenreId(g.id); }}
                        className={`p-3.5 rounded-xl border text-left transition ${
                          isSelected
                            ? 'bg-sky-500/20 border-sky-500 text-white shadow-md'
                            : 'bg-slate-950 border-slate-800 text-slate-300 hover:border-slate-700'
                        }`}
                      >
                        <div className="font-bold text-xs">{g.name}</div>
                        <div className="text-[10px] text-slate-400 mt-1 line-clamp-2">
                          {g.description}
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Theme Selection Grid */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                    Select Theme & Setting
                  </label>
                  <span className="text-[11px] text-sky-400 flex items-center space-x-1">
                    <Sparkles className="w-3.5 h-3.5" />
                    <span>Highlighted themes have high synergy with {selectedGenreConfig.name}</span>
                  </span>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
                  {GAME_THEMES.map((t) => {
                    const isSelected = themeId === t.id;
                    const isSynergy = selectedGenreConfig.synergyThemes.includes(t.id);
                    return (
                      <button
                        key={t.id}
                        type="button"
                        onClick={() => { soundEngine.playClick(); setThemeId(t.id); }}
                        className={`p-3 rounded-xl border text-left transition relative ${
                          isSelected
                            ? 'bg-indigo-500/20 border-indigo-500 text-white shadow-md'
                            : isSynergy
                            ? 'bg-slate-900 border-sky-500/50 text-sky-300'
                            : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700'
                        }`}
                      >
                        <div className="font-semibold text-xs flex items-center justify-between">
                          <span>{t.name}</span>
                          {isSynergy && <Sparkles className="w-3.5 h-3.5 text-sky-400 shrink-0" />}
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            </div>
          )}

          {/* STEP 2: Audience & Scale */}
          {step === 2 && (
            <div className="space-y-6">
              {/* Audience */}
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                  Target Audience
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                  {(['everyone', 'teen', 'mature', 'hardcore'] as Audience[]).map((aud) => (
                    <button
                      key={aud}
                      type="button"
                      onClick={() => { soundEngine.playClick(); setAudience(aud); }}
                      className={`p-4 rounded-xl border text-center transition ${
                        audience === aud
                          ? 'bg-sky-500/20 border-sky-500 text-white font-bold shadow-md'
                          : 'bg-slate-950 border-slate-800 text-slate-300 hover:border-slate-700'
                      }`}
                    >
                      <div className="capitalize font-bold text-sm">{aud}</div>
                    </button>
                  ))}
                </div>
              </div>

              {/* Game Scale */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                  Development Scope & Scale
                </label>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {[
                    {
                      id: 'indie' as GameScale,
                      title: 'Indie Release',
                      desc: 'Short development cycle, humble budget ($10k-$50k), $19.99 price.',
                      req: '800 pts'
                    },
                    {
                      id: 'aa' as GameScale,
                      title: 'AA Studio Title',
                      desc: 'Balanced scope and budget ($200k-$1M), $39.99 price.',
                      req: '2,600 pts'
                    },
                    {
                      id: 'aaa' as GameScale,
                      title: 'AAA Blockbuster',
                      desc: 'Massive production values ($3M-$10M), demanding schedule, $59.99 price.',
                      req: '5,500 pts'
                    },
                    {
                      id: 'aaaa' as GameScale,
                      title: 'AAAA Global Phenomenon',
                      desc: 'The ultimate industry spectacle. Needs a large team and massive budget.',
                      req: '12,000 pts'
                    }
                  ].map((item) => (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => { soundEngine.playClick(); setScale(item.id); }}
                      className={`p-4 rounded-2xl border text-left transition flex items-start justify-between ${
                        scale === item.id
                          ? 'bg-gradient-to-r from-sky-500/20 to-indigo-500/20 border-sky-500 text-white shadow-md'
                          : 'bg-slate-950 border-slate-800 text-slate-300 hover:border-slate-700'
                      }`}
                    >
                      <div>
                        <div className="font-bold text-sm text-white">{item.title}</div>
                        <div className="text-xs text-slate-400 mt-1">{item.desc}</div>
                      </div>
                      <span className="px-2 py-0.5 rounded bg-slate-900 text-sky-400 text-xs font-mono font-bold shrink-0 ml-2">
                        {item.req}
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* STEP 3: Platforms & Game Engine */}
          {step === 3 && (
            <div className="space-y-6">
              {/* Platforms */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                    Target Platforms ({date.year} Market)
                  </label>
                  <span className="text-xs text-slate-400">
                    Select 1 or more hardware platforms
                  </span>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {availablePlatforms.map((plat) => {
                    const isChecked = selectedPlatformIds.includes(plat.id);
                    return (
                      <div
                        key={plat.id}
                        onClick={() => togglePlatform(plat.id)}
                        className={`p-4 rounded-xl border transition cursor-pointer flex items-center justify-between ${
                          isChecked
                            ? 'bg-sky-500/20 border-sky-500 text-white'
                            : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700'
                        }`}
                      >
                        <div className="flex items-center space-x-3">
                          <Monitor className={`w-5 h-5 ${isChecked ? 'text-sky-400' : 'text-slate-500'}`} />
                          <div>
                            <div className="font-bold text-xs text-white">{plat.name}</div>
                            <div className="text-[10px] text-slate-400">
                              {plat.manufacturer} • Market Share: {plat.marketShare}%
                            </div>
                          </div>
                        </div>
                        <div
                          className={`w-5 h-5 rounded-md border flex items-center justify-center transition ${
                            isChecked ? 'bg-sky-500 border-sky-500 text-white' : 'border-slate-700 bg-slate-900'
                          }`}
                        >
                          {isChecked && <CheckCircle2 className="w-3.5 h-3.5" />}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Game Engine Selector */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                  Select Game Engine
                </label>

                <div className="space-y-2">
                  <button
                    type="button"
                    onClick={() => { soundEngine.playClick(); setTargetEngineId('default_engine'); }}
                    className={`w-full p-4 rounded-xl border text-left transition flex items-center justify-between ${
                      targetEngineId === 'default_engine'
                        ? 'bg-indigo-500/20 border-indigo-500 text-white'
                        : 'bg-slate-950 border-slate-800 text-slate-300 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex items-center space-x-3">
                      <Cpu className="w-5 h-5 text-indigo-400" />
                      <div>
                        <div className="font-bold text-sm text-white">Default Studio Framework</div>
                        <div className="text-xs text-slate-400">
                          Basic 2D/3D capabilities without royalty costs.
                        </div>
                      </div>
                    </div>
                  </button>

                  {engines.map((eng) => (
                    <button
                      key={eng.id}
                      type="button"
                      onClick={() => { soundEngine.playClick(); setTargetEngineId(eng.id); }}
                      className={`w-full p-4 rounded-xl border text-left transition flex items-center justify-between ${
                        targetEngineId === eng.id
                          ? 'bg-sky-500/20 border-sky-500 text-white'
                          : 'bg-slate-950 border-slate-800 text-slate-300 hover:border-slate-700'
                      }`}
                    >
                      <div className="flex items-center space-x-3">
                        <Cpu className="w-5 h-5 text-sky-400" />
                        <div>
                          <div className="font-bold text-sm text-white">
                            {eng.name} (v{eng.version})
                          </div>
                          <div className="text-xs text-slate-400">
                            Custom Proprietary Engine • {eng.unlockedFeatureIds.length} Tech Features Unlocked
                          </div>
                        </div>
                      </div>
                      <span className="px-2 py-0.5 rounded bg-sky-500/10 text-sky-400 text-xs font-mono">
                        Proprietary
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* STEP 4: Summary & Start */}
          {step === 4 && (
            <div className="space-y-6">
              <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900 to-slate-950 border border-slate-800 space-y-4">
                <div className="flex items-center space-x-3 border-b border-slate-800 pb-4">
                  <div className="p-3 rounded-2xl bg-sky-500/10 text-sky-400">
                    <Gamepad2 className="w-8 h-8" />
                  </div>
                  <div>
                    <h3 className="text-2xl font-extrabold text-white">
                      {title || 'Untitled Game Project'}
                    </h3>
                    <p className="text-xs text-slate-400">
                      {selectedGenreConfig.name} • {GAME_THEMES.find(t => t.id === themeId)?.name}
                    </p>
                  </div>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">
                  <div className="p-3 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-[11px] text-slate-400">Target Scale</div>
                    <div className="font-bold text-sm text-white uppercase mt-0.5">
                      {scale}
                    </div>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-[11px] text-slate-400">Target Audience</div>
                    <div className="font-bold text-sm text-white capitalize mt-0.5">
                      {audience}
                    </div>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-[11px] text-slate-400">Platforms Selected</div>
                    <div className="font-bold text-sm text-white mt-0.5">
                      {selectedPlatformIds.length} Platform(s)
                    </div>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-[11px] text-slate-400">Estimated Price</div>
                    <div className="font-bold text-sm text-emerald-400 mt-0.5">
                      ${scale === 'indie' ? 19.99 : scale === 'aa' ? 39.99 : 59.99}
                    </div>
                  </div>
                </div>

                <div className="p-4 rounded-xl bg-sky-500/10 border border-sky-500/20 flex items-start space-x-3 text-xs text-sky-300">
                  <Info className="w-5 h-5 shrink-0 mt-0.5" />
                  <p>
                    Once production begins, assign developers from the <strong>Studio Floor</strong> or <strong>Staff</strong> tabs. Keep tech debt low and squash bugs before release to maximize critic review scores!
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Wizard Footer Controls */}
        <div className="p-4 border-t border-slate-800 bg-slate-950 flex items-center justify-between">
          <button
            type="button"
            onClick={() => { soundEngine.playClick(); setStep(Math.max(1, step - 1)); }}
            disabled={step === 1}
            className={`flex items-center space-x-1.5 px-4 py-2 rounded-xl text-xs font-semibold border transition ${
              step === 1
                ? 'text-slate-600 border-slate-800 cursor-not-allowed'
                : 'text-slate-300 border-slate-700 hover:bg-slate-800'
            }`}
          >
            <ChevronLeft className="w-4 h-4" />
            <span>Previous</span>
          </button>

          {step < 4 ? (
            <button
              type="button"
              onClick={() => {
                if (step === 1 && !title.trim()) return;
                soundEngine.playClick();
                setStep(step + 1);
              }}
              disabled={step === 1 && !title.trim()}
              className={`flex items-center space-x-1.5 px-6 py-2.5 rounded-xl text-xs font-bold transition shadow-md ${
                step === 1 && !title.trim()
                  ? 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  : 'bg-sky-600 hover:bg-sky-500 text-white'
              }`}
            >
              <span>Next Step</span>
              <ChevronRight className="w-4 h-4" />
            </button>
          ) : (
            <button
              type="button"
              onClick={handleFinish}
              className="flex items-center space-x-2 px-8 py-2.5 rounded-xl bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white font-extrabold text-sm shadow-xl transition transform active:scale-95"
            >
              <Sparkles className="w-4 h-4" />
              <span>Start Game Production!</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
