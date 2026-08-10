import React from 'react';
import { useGameStore } from '../../store/useGameStore';
import { Award, Check, X as CloseIcon, ThumbsUp, ThumbsDown, Star } from 'lucide-react';

export const ReviewModal: React.FC = () => {
  const { activeReviewModalProject, closeReviewModal } = useGameStore();

  if (!activeReviewModalProject) return null;

  const { title, reviewScore, criticReviews } = activeReviewModalProject;
  const score = reviewScore ?? activeReviewModalProject.qualityScore;

  let verdictText = 'Mixed Reception';
  let scoreColor = 'from-amber-500 to-yellow-600';

  if (score >= 90) {
    verdictText = 'CRITICAL MASTERPIECE';
    scoreColor = 'from-emerald-500 to-teal-600';
  } else if (score >= 80) {
    verdictText = 'GREAT HIT';
    scoreColor = 'from-sky-500 to-indigo-600';
  } else if (score >= 70) {
    verdictText = 'SOLID SUCCESS';
    scoreColor = 'from-blue-500 to-sky-600';
  } else if (score < 60) {
    verdictText = 'DISAPPOINTING LAUNCH';
    scoreColor = 'from-rose-600 to-red-700';
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/85 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-2xl w-full shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Dramatic Header Banner */}
        <div className={`p-8 bg-gradient-to-r ${scoreColor} text-white text-center relative overflow-hidden`}>
          <div className="absolute top-2 left-4 opacity-20">
            <Award className="w-32 h-32" />
          </div>

          <span className="text-xs font-mono tracking-widest uppercase font-bold bg-black/20 px-3 py-1 rounded-full">
            {verdictText}
          </span>
          <h2 className="text-3xl font-extrabold mt-3 tracking-tight">
            {title}
          </h2>
          <p className="text-sm opacity-90 mt-1">
            Global Critic Consensus & Magazine Review Roundup
          </p>

          {/* Massive Score Circle */}
          <div className="mt-6 mx-auto w-28 h-28 rounded-3xl bg-slate-950/90 border-4 border-white/20 flex flex-col items-center justify-center shadow-2xl">
            <span className="font-mono text-4xl font-black text-white leading-none">
              {score}
            </span>
            <span className="text-[10px] uppercase font-bold text-slate-400 mt-1">
              Out of 100
            </span>
          </div>
        </div>

        {/* Magazine Critics Breakdown */}
        <div className="p-6 space-y-4 max-h-[50vh] overflow-y-auto">
          <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400">
            Magazine & Web Outlet Reviews
          </h4>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {criticReviews.map((rev, idx) => (
              <div
                key={idx}
                className="p-4 rounded-xl bg-slate-950 border border-slate-800 space-y-2"
              >
                <div className="flex items-center justify-between border-b border-slate-800/80 pb-2">
                  <span className="font-bold text-sm text-sky-400">{rev.outlet}</span>
                  <span className="font-mono text-base font-extrabold text-white">
                    {rev.score}/100
                  </span>
                </div>

                <p className="text-xs text-slate-300 italic leading-snug">
                  &ldquo;{rev.summary}&rdquo;
                </p>

                {/* Pros / Cons list */}
                <div className="space-y-1 pt-1">
                  {rev.pros.map((p, pIdx) => (
                    <div key={`pro-${pIdx}`} className="flex items-center space-x-1.5 text-[11px] text-emerald-400">
                      <ThumbsUp className="w-3 h-3 shrink-0" />
                      <span className="truncate">{p}</span>
                    </div>
                  ))}
                  {rev.cons.map((c, cIdx) => (
                    <div key={`con-${cIdx}`} className="flex items-center space-x-1.5 text-[11px] text-rose-400">
                      <ThumbsDown className="w-3 h-3 shrink-0" />
                      <span className="truncate">{c}</span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Footer button */}
        <div className="p-4 bg-slate-950 border-t border-slate-800 flex items-center justify-between">
          <div className="text-xs text-slate-400 flex items-center space-x-1">
            <Star className="w-4 h-4 text-amber-400" />
            <span>Sales begin on the first weekly tick!</span>
          </div>

          <button
            onClick={closeReviewModal}
            className="px-6 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs shadow-md transition"
          >
            Continue to Sales Chart
          </button>
        </div>
      </div>
    </div>
  );
};
