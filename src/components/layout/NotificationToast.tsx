import React from 'react';
import { useGameStore } from '../../store/useGameStore';
import { X, CheckCircle2, AlertTriangle, Info, Award } from 'lucide-react';

export const NotificationToast: React.FC = () => {
  const { toasts, dismissToast } = useGameStore();

  if (toasts.length === 0) return null;

  return (
    <div className="fixed bottom-4 right-4 z-50 flex flex-col space-y-2 max-w-sm w-full pointer-events-none">
      {toasts.slice(0, 4).map((t) => {
        let borderColor = 'border-sky-500/40 bg-slate-900/95 text-sky-300';
        let Icon = Info;
        if (t.type === 'success') {
          borderColor = 'border-emerald-500/40 bg-slate-900/95 text-emerald-400';
          Icon = CheckCircle2;
        } else if (t.type === 'warning' || t.type === 'error') {
          borderColor = 'border-amber-500/40 bg-slate-900/95 text-amber-400';
          Icon = AlertTriangle;
        } else if (t.type === 'award') {
          borderColor = 'border-purple-500/40 bg-slate-900/95 text-purple-300';
          Icon = Award;
        }

        return (
          <div
            key={t.id}
            className={`pointer-events-auto flex items-start space-x-3 p-3.5 rounded-xl border shadow-xl backdrop-blur-md transition-all duration-300 ${borderColor}`}
          >
            <Icon className="w-5 h-5 shrink-0 mt-0.5" />
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between">
                <h4 className="font-semibold text-xs text-white leading-tight">
                  {t.title}
                </h4>
                <span className="text-[10px] text-slate-500 font-mono ml-2">
                  {t.timestamp}
                </span>
              </div>
              <p className="text-xs text-slate-300 mt-1 leading-snug">
                {t.message}
              </p>
            </div>
            <button
              onClick={() => dismissToast(t.id)}
              className="text-slate-500 hover:text-white p-1 rounded transition"
              title="Dismiss notification"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          </div>
        );
      })}
    </div>
  );
};
