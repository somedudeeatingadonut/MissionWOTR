import { GameProject } from '../types/game';
import { getAvailablePlatforms } from '../data/platforms';

export interface WeeklySalesResult {
  unitsSoldThisWeek: number;
  revenueThisWeek: number;
  newHype: number;
  activePlayersChange: number;
}

export function simulateWeeklySales(
  project: GameProject,
  currentYear: number,
  fanBase: number
): WeeklySalesResult {
  const reviewScore = project.reviewScore || project.qualityScore || 70;

  // Platform reach multiplier
  const availablePlatforms = getAvailablePlatforms(currentYear);
  const matchedPlatforms = availablePlatforms.filter(p => project.platformIds.includes(p.id));
  const totalMarketShare = matchedPlatforms.reduce((sum, p) => sum + p.marketShare, 0); // e.g. 80.0

  // Weeks since release
  const weekNumber = project.weeklySalesHistory.length + 1;

  // Base demand curve: strong week 1, decay over time
  let baseDemand = 0;
  if (weekNumber === 1) {
    baseDemand = 15000 + fanBase * 1.5 + project.hype * 800;
  } else if (weekNumber === 2) {
    baseDemand = 8000 + fanBase * 0.8 + project.hype * 300;
  } else if (weekNumber <= 6) {
    baseDemand = Math.max(1000, 4000 / (weekNumber - 1));
  } else {
    // Long tail
    baseDemand = Math.max(200, 1500 / Math.sqrt(weekNumber));
  }

  // Review multiplier (e.g. 90 score = 1.8x, 50 score = 0.5x)
  const reviewMultiplier = Math.pow(reviewScore / 65, 1.8);

  // Market reach multiplier
  const marketMultiplier = Math.max(0.3, totalMarketShare / 50);

  // Price sensitivity
  const priceMultiplier = Math.max(0.4, 1.5 - project.currentPrice / 50);

  let unitsSoldThisWeek = Math.round(
    baseDemand * reviewMultiplier * marketMultiplier * priceMultiplier
  );

  // Random weekly variance
  const variance = 0.85 + Math.random() * 0.3; // 0.85 to 1.15
  unitsSoldThisWeek = Math.round(unitsSoldThisWeek * variance);

  const revenueThisWeek = Math.round(unitsSoldThisWeek * project.currentPrice);

  // Hype decays after release
  const newHype = Math.max(0, project.hype - 8);

  // Live service / MMORPG player changes
  let activePlayersChange = 0;
  if (project.isLiveService) {
    if (weekNumber === 1) {
      activePlayersChange = Math.min(project.serverCapacity, unitsSoldThisWeek * 3);
    } else {
      // Churn vs retention
      const retentionRate = Math.min(0.98, 0.75 + (reviewScore / 100) * 0.23);
      const newPlayers = Math.round(unitsSoldThisWeek * 0.8);
      const lostPlayers = Math.round(project.activePlayers * (1 - retentionRate));
      activePlayersChange = newPlayers - lostPlayers;
    }
  }

  return {
    unitsSoldThisWeek,
    revenueThisWeek,
    newHype,
    activePlayersChange
  };
}
