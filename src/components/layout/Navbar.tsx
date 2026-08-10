import React from 'react';
import { useGameStore } from '../../store/useGameStore';
import { 
  Play, 
  Pause, 
  FastForward, 
  DollarSign, 
  Users, 
  Star, 
  Building2, 
  Volume2, 
  VolumeX, 
  Save, 
  PlusCircle, 
  Compass 
} from 'lucide-react';
import { getOfficeTierById } from '../../data/offices';
import { soundEngine } from '../../engine/soundEngine';

interface NavbarProps {
  onOpenNewGame: () => void;
  onOpenScenarios: () => void;
  onOpenSaveLoad: () => void;
}

const MONTH_NAMES = [
  '', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
];

export const Navbar: React.FC<NavbarProps> = ({
  onOpenNewGame,
  onOpenScenarios,
  onOpenSaveLoad
}) => {
  const { date, speed, setSpeed, company } = useGameStore();
  const [soundEnabled, setSoundEnabled] = React.useState(true);

  const officeTier = getOfficeTierById(company.officeTierId);

  const toggleSound = () => {
    soundEngine.enabled = !soundEnabled;
    setSoundEnabled(!soundEnabled);
  };

  return (
    <header className="bg-slate-900 border-b border-slate-800 text-slate-200 px-4 py-2.5 sticky top-0 z-40 flex items-center justify-between shadow-lg">
      {/* Left: Logo & Company Name */}
      <div className="flex items-center space-x-3">
        <div className="w-9 h-9 rounded-lg bg-gradient-to-br from-sky-500 to-indigo-600 flex items-center justify-center font-bold text-white shadow-md shadow-sky-500/20">
          DT
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <h1 className="font-bold text-base leading-none text-white tracking-tight">
              {company.name}
            </h1>
            <span className="text-[10px] uppercase font-semibold tracking-wider px-1.5 py-0.5 rounded bg-slate-800 text-sky-400 border border-slate-700">
              {officeTier.name}
            </span>
          </div>
          <p className="text-xs text-slate-400 mt-0.5">
            CEO: <span className="text-slate-300 font-medium">{company.ceoName}</span>
          </p>
        </div>
      </div>

      {/* Center: Top Stats & Finance Ticker */}
      <div className="hidden lg:flex items-center space-x-6 bg-slate-950/60 px-5 py-1.5 rounded-full border border-slate-800/80">
        {/* Cash */}
        <div className="flex items-center space-x-1.5" title="Studio Bank Balance">
          <div className="p-1 rounded bg-emerald-500/10 text-emerald-400">
            <DollarSign className="w-4 h-4" />
          </div>
          <div>
            <div className="text-xs text-slate-400 leading-none">Bank Cash</div>
            <div className={`font-mono text-sm font-bold ${company.cash >= 0 ? 'text-emerald-400' : 'text-rose-400'}`}>
              ${company.cash.toLocaleString()}
            </div>
          </div>
        </div>

        {/* Fans */}
        <div className="flex items-center space-x-1.5" title="Loyal Fan Base">
          <div className="p-1 rounded bg-sky-500/10 text-sky-400">
            <Users className="w-4 h-4" />
          </div>
          <div>
            <div className="text-xs text-slate-400 leading-none">Fan Base</div>
            <div className="font-mono text-sm font-bold text-sky-300">
              {company.fanBase.toLocaleString()}
            </div>
          </div>
        </div>

        {/* Reputation */}
        <div className="flex items-center space-x-1.5" title="Studio Prestige & Reputation">
          <div className="p-1 rounded bg-amber-500/10 text-amber-400">
            <Star className="w-4 h-4" />
          </div>
          <div>
            <div className="text-xs text-slate-400 leading-none">Reputation</div>
            <div className="font-mono text-sm font-bold text-amber-300">
              {company.reputation} / 100
            </div>
          </div>
        </div>

        {/* Valuation */}
        <div className="flex items-center space-x-1.5" title="Company Valuation">
          <div className="p-1 rounded bg-purple-500/10 text-purple-400">
            <Building2 className="w-4 h-4" />
          </div>
          <div>
            <div className="text-xs text-slate-400 leading-none">Valuation</div>
            <div className="font-mono text-sm font-bold text-purple-300">
              ${company.companyValuation.toLocaleString()}
            </div>
          </div>
        </div>
      </div>

      {/* Right: Date, Speed Controls, & Action Buttons */}
      <div className="flex items-center space-x-4">
        {/* Date Display */}
        <div className="text-right bg-slate-950/80 px-3 py-1 rounded border border-slate-800">
          <div className="font-mono font-bold text-sm text-sky-300">
            {MONTH_NAMES[date.month]} {date.day}, {date.year}
          </div>
          <div className="text-[10px] text-slate-400 uppercase tracking-wider">
            Year {date.year - 1989}
          </div>
        </div>

        {/* Speed Controls */}
        <div className="flex items-center bg-slate-950 p-1 rounded-lg border border-slate-800 space-x-1">
          <button
            onClick={() => setSpeed(0)}
            className={`p-1.5 rounded transition ${speed === 0 ? 'bg-rose-600 text-white shadow' : 'text-slate-400 hover:text-white'}`}
            title="Pause Simulation"
          >
            <Pause className="w-4 h-4" />
          </button>
          <button
            onClick={() => setSpeed(1)}
            className={`p-1.5 rounded transition ${speed === 1 ? 'bg-sky-600 text-white shadow' : 'text-slate-400 hover:text-white'}`}
            title="Normal Speed (1x)"
          >
            <Play className="w-4 h-4" />
          </button>
          <button
            onClick={() => setSpeed(2)}
            className={`p-1.5 rounded transition ${speed === 2 ? 'bg-sky-600 text-white shadow' : 'text-slate-400 hover:text-white'}`}
            title="Fast Speed (2x)"
          >
            <FastForward className="w-4 h-4" />
          </button>
          <button
            onClick={() => setSpeed(5)}
            className={`p-1.5 rounded transition font-mono text-xs font-bold px-2 py-1 ${speed === 5 ? 'bg-indigo-600 text-white shadow' : 'text-slate-400 hover:text-white'}`}
            title="Ultra Speed (5x)"
          >
            5x
          </button>
        </div>

        {/* Quick Action Buttons */}
        <div className="flex items-center space-x-2">
          <button
            onClick={onOpenNewGame}
            className="flex items-center space-x-1.5 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white font-semibold text-xs px-3 py-2 rounded-lg shadow-md transition transform active:scale-95"
            title="Develop a New Game"
          >
            <PlusCircle className="w-4 h-4" />
            <span className="hidden md:inline">New Game</span>
          </button>

          <button
            onClick={onOpenScenarios}
            className="flex items-center space-x-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 text-xs px-2.5 py-2 rounded-lg transition"
            title="Switch Scenario Presets (1990, 2002, 2024)"
          >
            <Compass className="w-4 h-4 text-sky-400" />
            <span className="hidden sm:inline">Scenarios</span>
          </button>

          <button
            onClick={onOpenSaveLoad}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 transition"
            title="Save, Load, or Export Game"
          >
            <Save className="w-4 h-4" />
          </button>

          <button
            onClick={toggleSound}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 transition"
            title={soundEnabled ? 'Mute Sound Effects' : 'Enable Sound Effects'}
          >
            {soundEnabled ? <Volume2 className="w-4 h-4 text-emerald-400" /> : <VolumeX className="w-4 h-4 text-slate-500" />}
          </button>
        </div>
      </div>
    </header>
  );
};
