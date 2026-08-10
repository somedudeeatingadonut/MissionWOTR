import React from 'react';
import { useGameStore } from '../../store/useGameStore';
import { AlertTriangle, Sparkles, HelpCircle, ArrowRight } from 'lucide-react';

export const EventModal: React.FC = () => {
  const { activeEvent, resolveEventChoice } = useGameStore();

  if (!activeEvent) return null;

  let Icon = AlertTriangle;
  let bannerColor = 'from-amber-600/30 to-slate-900 border-amber-500/40 text-amber-300';

  if (activeEvent.category === 'opportunity') {
    Icon = Sparkles;
    bannerColor = 'from-emerald-600/30 to-slate-900 border-emerald-500/40 text-emerald-300';
  } else if (activeEvent.category === 'industry') {
    Icon = HelpCircle;
    bannerColor = 'from-sky-600/30 to-slate-900 border-sky-500/40 text-sky-300';
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-lg w-full shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Banner */}
        <div className={`p-6 bg-gradient-to-r ${bannerColor} border-b flex items-center space-x-3`}>
          <Icon className="w-6 h-6 shrink-0" />
          <div>
            <span className="text-[10px] uppercase font-bold tracking-wider opacity-80">
              Studio Dilemma ({activeEvent.category})
            </span>
            <h3 className="text-lg font-bold text-white leading-tight">
              {activeEvent.title}
            </h3>
          </div>
        </div>

        {/* Content */}
        <div className="p-6 space-y-4">
          <p className="text-sm text-slate-300 leading-relaxed">
            {activeEvent.description}
          </p>

          <div className="space-y-3 pt-2">
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Choose an action:
            </h4>
            {activeEvent.choices.map((choice, idx) => (
              <button
                key={idx}
                onClick={() => resolveEventChoice(idx)}
                className="w-full text-left p-4 rounded-xl bg-slate-950 hover:bg-slate-800/80 border border-slate-800 hover:border-sky-500/50 transition group flex items-start justify-between"
              >
                <div className="space-y-1">
                  <div className="font-bold text-sm text-white group-hover:text-sky-300 transition">
                    {choice.label}
                  </div>
                  <div className="text-xs text-slate-400">
                    {choice.description}
                  </div>
                </div>
                <ArrowRight className="w-5 h-5 text-slate-600 group-hover:text-sky-400 shrink-0 ml-3 mt-0.5 transition transform group-hover:translate-x-1" />
              </button>
            ))}
          </div>
        </div>

        {/* Footer note */}
        <div className="px-6 py-3 bg-slate-950/60 border-t border-slate-800/80 text-[11px] text-slate-500 text-center">
          Decisions have lasting effects on cash, reputation, morale, and game hype.
        </div>
      </div>
    </div>
  );
};
