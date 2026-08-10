import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { TECH_TREE } from '../../data/techTree';
import { getAvailablePlatforms } from '../../data/platforms';
import { 
  Cpu, 
  X, 
  CheckCircle2, 
  Monitor, 
  DollarSign, 
  Sparkles 
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface EngineCreatorModalProps {
  onClose: () => void;
}

export const EngineCreatorModal: React.FC<EngineCreatorModalProps> = ({ onClose }) => {
  const { date, company, unlockedTechIds, createCustomEngine } = useGameStore();

  const [name, setName] = useState<string>('NextGen Framework 1.0');
  const availablePlatforms = getAvailablePlatforms(date.year);
  const [selectedPlatformIds, setSelectedPlatformIds] = useState<string[]>([
    availablePlatforms[0]?.id || 'pc_win'
  ]);
  const [selectedFeatureIds, setSelectedFeatureIds] = useState<string[]>(
    unlockedTechIds.slice(0, 5)
  );
  const [licensingPrice, setLicensingPrice] = useState<number>(50000);
  const [royaltyPercentage, setRoyaltyPercentage] = useState<number>(8);

  const unlockedNodes = TECH_TREE.filter(t => unlockedTechIds.includes(t.id));

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

  const toggleFeature = (id: string) => {
    soundEngine.playClick();
    if (selectedFeatureIds.includes(id)) {
      if (selectedFeatureIds.length > 1) {
        setSelectedFeatureIds(selectedFeatureIds.filter(f => f !== id));
      }
    } else {
      setSelectedFeatureIds([...selectedFeatureIds, id]);
    }
  };

  const estimatedCost = 45000 + selectedFeatureIds.length * 15000;

  const handleBuild = () => {
    if (!name.trim()) return;
    createCustomEngine(
      name.trim(),
      selectedPlatformIds,
      selectedFeatureIds,
      licensingPrice,
      royaltyPercentage
    );
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-3xl w-full shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white">
              <Cpu className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white leading-none">
                Proprietary Game Engine Creator
              </h3>
              <p className="text-xs text-slate-400 mt-1">
                Bundle unlocked technologies into a custom engine for your studio and third-party licensing.
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

        {/* Form Content */}
        <div className="p-6 overflow-y-auto flex-1 space-y-6">
          {/* Engine Name Input */}
          <div className="space-y-2">
            <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
              Engine Name & Version
            </label>
            <input
              type="text"
              placeholder="e.g. Vortex 3D Engine 2.0"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full bg-slate-950 border border-slate-700 rounded-xl px-4 py-3 text-base text-white font-semibold focus:outline-none focus:border-sky-500 transition"
            />
          </div>

          {/* Select Hardware Platforms */}
          <div className="space-y-3">
            <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
              Supported Platforms
            </label>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
              {availablePlatforms.map((plat) => {
                const isChecked = selectedPlatformIds.includes(plat.id);
                return (
                  <div
                    key={plat.id}
                    onClick={() => togglePlatform(plat.id)}
                    className={`p-3 rounded-xl border transition cursor-pointer flex items-center justify-between ${
                      isChecked
                        ? 'bg-sky-500/20 border-sky-500 text-white'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex items-center space-x-2">
                      <Monitor className="w-4 h-4 shrink-0" />
                      <span className="font-bold text-xs truncate">{plat.name}</span>
                    </div>
                    {isChecked && <CheckCircle2 className="w-4 h-4 text-sky-400 shrink-0" />}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Select Unlocked Tech Features */}
          <div className="space-y-3">
            <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
              Include Unlocked Technologies ({selectedFeatureIds.length} Selected)
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {unlockedNodes.map((tech) => {
                const isChecked = selectedFeatureIds.includes(tech.id);
                return (
                  <div
                    key={tech.id}
                    onClick={() => toggleFeature(tech.id)}
                    className={`p-3.5 rounded-xl border transition cursor-pointer flex items-start justify-between ${
                      isChecked
                        ? 'bg-indigo-500/20 border-indigo-500 text-white'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    <div>
                      <div className="font-bold text-xs text-white">{tech.name}</div>
                      <div className="text-[10px] text-slate-400 mt-0.5">{tech.description}</div>
                    </div>
                    {isChecked && <CheckCircle2 className="w-4 h-4 text-indigo-400 shrink-0 ml-2" />}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Licensing Fees & Royalties */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-slate-950 p-4 rounded-xl border border-slate-800">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-300">
                Upfront Licensing Price ($)
              </label>
              <input
                type="number"
                step="5000"
                min="0"
                value={licensingPrice}
                onChange={(e) => setLicensingPrice(Number(e.target.value))}
                className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-sm font-mono text-white"
              />
              <span className="text-[10px] text-slate-500 block">
                Charged to rival studios when they license your engine.
              </span>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-300">
                Royalty Share (%)
              </label>
              <input
                type="number"
                step="1"
                min="0"
                max="30"
                value={royaltyPercentage}
                onChange={(e) => setRoyaltyPercentage(Number(e.target.value))}
                className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-sm font-mono text-white"
              />
              <span className="text-[10px] text-slate-500 block">
                Percentage of rival game sales paid to your studio.
              </span>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 bg-slate-950 border-t border-slate-800 flex items-center justify-between">
          <div className="text-xs font-mono">
            <span className="text-slate-400">Development Cost: </span>
            <strong className={`text-base font-extrabold ${company.cash >= estimatedCost ? 'text-emerald-400' : 'text-rose-400'}`}>
              ${estimatedCost.toLocaleString()}
            </strong>
          </div>

          <button
            type="button"
            onClick={handleBuild}
            disabled={company.cash < estimatedCost || !name.trim()}
            className={`px-6 py-2.5 rounded-xl font-bold text-xs shadow-lg flex items-center space-x-2 transition transform active:scale-95 ${
              company.cash >= estimatedCost && name.trim()
                ? 'bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white'
                : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
            }`}
          >
            <Sparkles className="w-4 h-4" />
            <span>Create Engine (${estimatedCost.toLocaleString()})</span>
          </button>
        </div>
      </div>
    </div>
  );
};
