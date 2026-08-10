import { useGameStore } from '../../store/useGameStore';
import { getAvailablePlatforms } from '../../data/platforms';
import { RIVAL_STUDIOS } from '../../data/rivalStudios';
import { GAME_GENRES } from '../../data/genres';
import { 
  TrendingUp, 
  Monitor, 
  Users, 
  Star, 
  Award, 
  Flame, 
  DollarSign, 
  Briefcase 
} from 'lucide-react';

export const MarketTrendsView: React.FC = () => {
  const { date, company } = useGameStore();

  const currentPlatforms = getAvailablePlatforms(date.year);

  // Compute player market share estimate based on fanBase and totalGamesReleased
  const playerMarketShare = Math.min(
    25.0,
    Math.round((company.fanBase / 100000 + company.totalGamesReleased * 1.5) * 10) / 10
  );

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Header */}
      <div className="border-b border-slate-800 pb-5">
        <h2 className="text-2xl font-bold text-white flex items-center space-x-2">
          <TrendingUp className="w-7 h-7 text-sky-400" />
          <span>Game Industry Market, Platforms, & Rival Studios</span>
        </h2>
        <p className="text-sm text-slate-400 mt-0.5">
          Analyze platform hardware shifts, genre trends, and competitor game releases across the gaming industry.
        </p>
      </div>

      {/* Top Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider flex items-center justify-between">
            <span>Your Studio Market Share</span>
            <Briefcase className="w-4 h-4 text-sky-400" />
          </div>
          <div className="font-mono text-3xl font-extrabold text-sky-400">
            {playerMarketShare}%
          </div>
          <p className="text-xs text-slate-400">
            Ranked among {RIVAL_STUDIOS.length + 1} global game publishers
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider flex items-center justify-between">
            <span>Active Hardware Platforms</span>
            <Monitor className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="font-mono text-3xl font-extrabold text-emerald-400">
            {currentPlatforms.length}
          </div>
          <p className="text-xs text-slate-400">
            Platforms supported in the {date.year} console era
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider flex items-center justify-between">
            <span>Hot Trending Genre</span>
            <Flame className="w-4 h-4 text-amber-400" />
          </div>
          <div className="font-bold text-xl text-amber-400">
            {GAME_GENRES[date.year % GAME_GENRES.length].name}
          </div>
          <p className="text-xs text-slate-400">
            +40% consumer demand & weekly sales tail boost
          </p>
        </div>
      </div>

      {/* Hardware Platforms Market Share */}
      <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="font-bold text-base text-white flex items-center space-x-2">
            <Monitor className="w-5 h-5 text-emerald-400" />
            <span>{date.year} Hardware Platforms & Installed User Base</span>
          </h3>
          <span className="text-xs text-slate-400">
            DevKit cost & market penetration
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {currentPlatforms.map((p) => (
            <div
              key={p.id}
              className="p-4 rounded-xl bg-slate-950 border border-slate-800 space-y-2"
            >
              <div className="flex items-center justify-between">
                <span className="font-bold text-sm text-white">{p.name}</span>
                <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 text-xs font-mono font-bold">
                  {p.marketShare}% Share
                </span>
              </div>

              <div className="text-xs text-slate-400">
                {p.manufacturer} • Tech Tier: {p.techTier} • DevKit: ${p.devKitCost.toLocaleString()}
              </div>

              {/* Share bar */}
              <div className="w-full bg-slate-900 h-1.5 rounded-full overflow-hidden">
                <div className="h-full bg-emerald-500" style={{ width: `${p.marketShare}%` }} />
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Simulated Rival Studios Leaderboard */}
      <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="font-bold text-base text-white flex items-center space-x-2">
            <Users className="w-5 h-5 text-sky-400" />
            <span>Simulated Rival Game Studios Leaderboard</span>
          </h3>
          <span className="text-xs text-slate-400">
            Competing for Game of the Year awards
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="border-b border-slate-800 text-slate-400 uppercase tracking-wider font-semibold">
                <th className="py-3 px-4">Studio Name</th>
                <th className="py-3 px-4">Specialty</th>
                <th className="py-3 px-4">Market Share</th>
                <th className="py-3 px-4">Cash Reserve</th>
                <th className="py-3 px-4">Reputation</th>
                <th className="py-3 px-4">Latest Release</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {/* Player company row at top */}
              <tr className="bg-sky-500/10 text-white font-semibold">
                <td className="py-3 px-4 flex items-center space-x-2">
                  <Award className="w-4 h-4 text-sky-400" />
                  <span>{company.name} (You)</span>
                </td>
                <td className="py-3 px-4 capitalize">{company.ceoSpecialty}</td>
                <td className="py-3 px-4 font-mono text-sky-400">{playerMarketShare}%</td>
                <td className="py-3 px-4 font-mono">${company.cash.toLocaleString()}</td>
                <td className="py-3 px-4 font-mono text-amber-300">
                  {company.reputation}/100
                </td>
                <td className="py-3 px-4 text-slate-300">
                  {company.totalGamesReleased} Games Released
                </td>
              </tr>

              {RIVAL_STUDIOS.map((rival) => {
                const specGenre = GAME_GENRES.find(g => g.id === rival.specialtyGenre);
                return (
                  <tr key={rival.id} className="hover:bg-slate-800/40 transition">
                    <td className="py-3 px-4 font-bold text-slate-200">{rival.name}</td>
                    <td className="py-3 px-4 text-slate-400">{specGenre?.name || rival.specialtyGenre}</td>
                    <td className="py-3 px-4 font-mono text-slate-300">{rival.marketShare}%</td>
                    <td className="py-3 px-4 font-mono text-slate-400">
                      ${(rival.cash / 1000000).toFixed(1)}M
                    </td>
                    <td className="py-3 px-4 font-mono text-amber-400 flex items-center space-x-1">
                      <Star className="w-3.5 h-3.5 fill-amber-400" />
                      <span>{rival.reputation}</span>
                    </td>
                    <td className="py-3 px-4">
                      {rival.recentRelease ? (
                        <div>
                          <span className="font-semibold text-white">{rival.recentRelease.title}</span>
                          <span className="text-[10px] text-slate-400 ml-1.5 font-mono">
                            ({rival.recentRelease.score}/100)
                          </span>
                        </div>
                      ) : (
                        <span className="text-slate-500">None</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
