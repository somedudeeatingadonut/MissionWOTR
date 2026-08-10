import React from 'react';
import { useGameStore } from '../../store/useGameStore';
import { SCENARIO_PRESETS } from '../../data/presets';
import { Compass, X, Calendar, DollarSign, Users, Play } from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface ScenarioModalProps {
  onClose: () => void;
}

export const ScenarioModal: React.FC<ScenarioModalProps> = ({ onClose }) => {
  const { loadScenario } = useGameStore();

  const handleSelectScenario = (id: string) => {
    if (window.confirm('Loading a scenario preset will overwrite your current studio progress. Continue?')) {
      loadScenario(id);
      onClose();
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-3xl w-full shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white">
              <Compass className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white leading-none">
                Select Game Era & Studio Scenario
              </h3>
              <p className="text-xs text-slate-400 mt-1">
                Jump into different historical eras and starting studio conditions.
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

        {/* Preset Cards */}
        <div className="p-6 overflow-y-auto flex-1 space-y-4">
          {SCENARIO_PRESETS.map((scen) => (
            <div
              key={scen.id}
              className="p-5 rounded-2xl bg-slate-950 border border-slate-800 hover:border-sky-500/50 transition flex flex-col md:flex-row md:items-center justify-between gap-4"
            >
              <div className="space-y-2 max-w-lg">
                <div className="flex items-center space-x-2.5">
                  <h4 className="text-lg font-bold text-white">{scen.title}</h4>
                  <span className="px-2 py-0.5 rounded bg-sky-500/10 text-sky-400 text-xs font-semibold border border-sky-500/30">
                    {scen.subtitle}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-300 text-xs font-mono font-bold">
                    {scen.difficulty}
                  </span>
                </div>

                <p className="text-xs text-slate-300 leading-relaxed">
                  {scen.description}
                </p>

                <div className="flex flex-wrap items-center gap-4 text-xs text-slate-400 pt-1">
                  <span className="flex items-center space-x-1">
                    <Calendar className="w-3.5 h-3.5 text-sky-400" />
                    <span>Year {scen.startYear}</span>
                  </span>
                  <span className="flex items-center space-x-1">
                    <DollarSign className="w-3.5 h-3.5 text-emerald-400" />
                    <span>${scen.company.cash.toLocaleString()} Cash</span>
                  </span>
                  <span className="flex items-center space-x-1">
                    <Users className="w-3.5 h-3.5 text-indigo-400" />
                    <span>{scen.employees.length} Developers</span>
                  </span>
                </div>
              </div>

              <button
                onClick={() => { soundEngine.playClick(); handleSelectScenario(scen.id); }}
                className="px-6 py-3 rounded-xl bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs shadow-lg transition shrink-0 flex items-center space-x-2"
              >
                <Play className="w-4 h-4" />
                <span>Load {scen.startYear} Scenario</span>
              </button>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="p-4 bg-slate-950 border-t border-slate-800 flex items-center justify-between">
          <span className="text-xs text-slate-500">
            All scenarios allow full tech tree progression across eras.
          </span>
          <button
            onClick={onClose}
            className="px-5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};
