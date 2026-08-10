import { GameDate, ToastNotification, DynamicEvent } from '../types/simulation';
import { Company } from '../types/company';
import { Employee } from '../types/employee';
import { GameProject } from '../types/game';
import { CustomEngine } from '../types/engine';
import { calculateDailyDevProgress, computeProjectQualityScore } from './devPipeline';
import { simulateWeeklySales } from './salesEngine';
import { processMonthlyFinances } from './financeEngine';
import { getOfficeTierById } from '../data/offices';
import { DYNAMIC_EVENTS } from '../data/randomEvents';
import { soundEngine } from './soundEngine';
import { getTechNodeById } from '../data/techTree';

export interface SimulationState {
  date: GameDate;
  company: Company;
  employees: Employee[];
  projects: GameProject[];
  engines: CustomEngine[];
  unlockedTechIds: string[];
  activeResearchTechId: string | null;
  activeEvent: DynamicEvent | null;
  toasts: ToastNotification[];
}

export function tickSimulationDay(state: SimulationState): SimulationState {
  let { year, month, day } = state.date;
  let company = { ...state.company };
  let employees = state.employees.map(e => ({ ...e }));
  let projects = state.projects.map(p => ({ ...p }));
  let engines = state.engines.map(e => ({ ...e }));
  let unlockedTechIds = [...state.unlockedTechIds];
  let activeResearchTechId = state.activeResearchTechId;
  const toasts: ToastNotification[] = [];

  // Advance date
  day += 1;
  let isNewWeek = day % 7 === 0;
  let isNewMonth = false;

  if (day > 30) {
    day = 1;
    month += 1;
    isNewMonth = true;
    if (month > 12) {
      month = 1;
      year += 1;
    }
  }

  const officeTier = getOfficeTierById(company.officeTierId);

  // 1. Process project development for active projects
  projects = projects.map(proj => {
    if (proj.phase === 'production' || proj.phase === 'alpha_testing' || proj.phase === 'beta_polish') {
      const assignedEmp = employees.filter(e => e.assignedProjectId === proj.id);
      if (assignedEmp.length > 0) {
        const result = calculateDailyDevProgress(proj, assignedEmp, officeTier);

        // Update progress
        proj.progress.code = Math.min(proj.totalRequirements.code * 1.5, proj.progress.code + result.codeDone);
        proj.progress.design = Math.min(proj.totalRequirements.design * 1.5, proj.progress.design + result.designDone);
        proj.progress.art = Math.min(proj.totalRequirements.art * 1.5, proj.progress.art + result.artDone);
        proj.progress.audio = Math.min(proj.totalRequirements.audio * 1.5, proj.progress.audio + result.audioDone);
        proj.progress.qa = Math.min(proj.totalRequirements.qa * 1.5, proj.progress.qa + result.qaDone);

        proj.bugs = Math.max(0, proj.bugs + result.bugsAdded);
        proj.techDebt = Math.max(0, proj.techDebt + result.techDebtAdded);
        proj.polishPoints += result.polishAdded;

        proj.qualityScore = computeProjectQualityScore(proj);

        // Update employee states
        employees = employees.map(emp => {
          const update = result.employeeUpdates.find(u => u.id === emp.id);
          if (update) {
            return {
              ...emp,
              energy: Math.max(10, Math.min(100, emp.energy + update.energyChange)),
              stress: Math.max(0, Math.min(100, emp.stress + update.stressChange)),
              morale: Math.max(10, Math.min(100, emp.morale + update.moraleChange))
            };
          }
          return emp;
        });
      }
    }
    return proj;
  });

  // 2. Overnight energy & morale restoration for employees
  employees = employees.map(emp => {
    const regen = officeTier.amenities.energyRegenRate;
    const nightRest = 45 * regen;
    const newEnergy = Math.min(100, emp.energy + nightRest);
    const newStress = Math.max(0, emp.stress - 2);
    const moraleBoost = officeTier.amenities.moraleBonus * 0.05;
    const newMorale = Math.min(100, emp.morale + moraleBoost);

    return {
      ...emp,
      energy: Math.round(newEnergy),
      stress: Math.round(newStress),
      morale: Math.round(newMorale)
    };
  });

  // 3. Process research progress if an active technology is being researched
  if (activeResearchTechId) {
    const tech = getTechNodeById(activeResearchTechId);
    if (tech && !unlockedTechIds.includes(tech.id)) {
      // Free research speed from programmers not assigned to a project, or baseline studio speed
      const unassignedProgrammers = employees.filter(e => !e.assignedProjectId && (e.role === 'programmer' || e.role === 'producer'));
      const dailyResearchPoints = Math.max(15, unassignedProgrammers.reduce((sum, e) => sum + e.skills.code / 4, 0));
      tech.progress = (tech.progress || 0) + dailyResearchPoints;
      if (tech.progress >= tech.workPointsRequired) {
        tech.progress = tech.workPointsRequired;
        tech.unlocked = true;
        unlockedTechIds.push(tech.id);
        activeResearchTechId = null;
        soundEngine.playAward();
        toasts.push({
          id: `tech-${Date.now()}`,
          title: 'Technology Research Complete!',
          message: `Unlocked: ${tech.name}`,
          type: 'success',
          timestamp: `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
        });
      }
    }
  }

  // 4. Weekly sales & live service updates
  if (isNewWeek) {
    let totalWeeklyRevenue = 0;
    projects = projects.map(proj => {
      if (proj.phase === 'released' || proj.phase === 'live_service') {
        const salesRes = simulateWeeklySales(proj, year, company.fanBase);
        proj.unitsSold += salesRes.unitsSoldThisWeek;
        proj.revenue += salesRes.revenueThisWeek;
        proj.weeklySalesHistory = [...proj.weeklySalesHistory, salesRes.unitsSoldThisWeek];
        proj.hype = salesRes.newHype;
        if (proj.isLiveService) {
          proj.activePlayers = Math.max(0, proj.activePlayers + salesRes.activePlayersChange);
        }

        totalWeeklyRevenue += salesRes.revenueThisWeek;
      }
      return proj;
    });

    if (totalWeeklyRevenue > 0) {
      company.cash += totalWeeklyRevenue;
      soundEngine.playCash();
    }
  }

  // 5. Monthly financial processing
  if (isNewMonth) {
    const finRes = processMonthlyFinances(company, employees, projects, month, year);
    company = finRes.updatedCompany;

    toasts.push({
      id: `fin-${year}-${month}`,
      title: 'Monthly Financial Report',
      message: `Net Profit: $${finRes.ledgerEntry.netProfit.toLocaleString()} | Cash: $${company.cash.toLocaleString()}`,
      type: finRes.ledgerEntry.netProfit >= 0 ? 'success' : 'warning',
      timestamp: `${year}-${String(month).padStart(2, '0')}-01`
    });
  }

  // 6. Random dilemma trigger chance (approx once every 3 months)
  let activeEvent = state.activeEvent;
  if (!activeEvent && Math.random() < 0.012) {
    const candidateEvents = DYNAMIC_EVENTS;
    if (candidateEvents.length > 0) {
      const randEvent = candidateEvents[Math.floor(Math.random() * candidateEvents.length)];
      activeEvent = randEvent;
      toasts.push({
        id: `ev-${Date.now()}`,
        title: 'Studio Dilemma Occurred!',
        message: randEvent.title,
        type: 'warning',
        timestamp: `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
      });
    }
  }

  return {
    ...state,
    date: { year, month, day },
    company,
    employees,
    projects,
    engines,
    unlockedTechIds,
    activeResearchTechId,
    activeEvent,
    toasts
  };
}
