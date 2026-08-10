import { create } from 'zustand';
import { GameDate, ToastNotification, DynamicEvent, GameSpeed } from '../types/simulation';
import { Company } from '../types/company';
import { Employee, JobApplicant, EmployeeSkills } from '../types/employee';
import { GameProject, CriticReview } from '../types/game';
import { CustomEngine } from '../types/engine';
import { getScenarioPreset, ScenarioPreset } from '../data/presets';
import { getOfficeTierById } from '../data/offices';
import { getTechNodeById } from '../data/techTree';
import { tickSimulationDay } from '../engine/simulationEngine';
import { generateCriticReviews } from '../engine/reviewEngine';
import { soundEngine } from '../engine/soundEngine';

export interface GameStoreState {
  // Time & Speed
  date: GameDate;
  speed: GameSpeed;
  
  // Entities
  company: Company;
  employees: Employee[];
  projects: GameProject[];
  engines: CustomEngine[];
  unlockedTechIds: string[];
  activeResearchTechId: string | null;

  // Active Modals & Dialogs
  activeEvent: DynamicEvent | null;
  activeReviewModalProject: GameProject | null;
  selectedProjectId: string | null;
  selectedEmployeeId: string | null;

  // Notifications
  toasts: ToastNotification[];

  // Actions
  setSpeed: (speed: GameSpeed) => void;
  tickDay: () => void;
  loadScenario: (presetId: string) => void;
  
  // Projects
  createNewGame: (projectData: Omit<GameProject, 'id' | 'progress' | 'bugs' | 'techDebt' | 'polishPoints' | 'hype' | 'qualityScore' | 'reviewScore' | 'criticReviews' | 'developmentCost' | 'revenue' | 'unitsSold' | 'weeklySalesHistory' | 'activePlayers' | 'dlcList' | 'patchLevel' | 'createdAtYear' | 'createdAtMonth' | 'createdAtDay'>) => void;
  releaseGame: (projectId: string) => void;
  closeReviewModal: () => void;
  toggleCrunchMode: (projectId: string, mode: GameProject['crunchMode']) => void;
  addFeatureCreep: (projectId: string) => void;
  addDLC: (projectId: string, title: string, price: number) => void;

  // Employees
  hireEmployee: (applicant: JobApplicant) => void;
  fireEmployee: (empId: string) => void;
  assignEmployee: (empId: string, projectId: string | null) => void;
  trainEmployee: (empId: string, skillKey: keyof EmployeeSkills, cost: number) => void;

  // Engine & Tech
  startResearch: (techId: string) => void;
  createCustomEngine: (name: string, platformIds: string[], unlockedFeatureIds: string[], licensingPrice: number, royaltyPercentage: number) => void;

  // Finance & Office
  upgradeOffice: (tierId: Company['officeTierId']) => void;
  takeBankLoan: (principal: number, interestRate: number, months: number) => void;
  repayBankLoan: (loanId: string) => void;

  // Events & UI
  resolveEventChoice: (choiceIndex: number) => void;
  dismissToast: (toastId: string) => void;
  selectProject: (projectId: string | null) => void;
  selectEmployee: (empId: string | null) => void;

  // Persistence
  saveGameToLocal: () => void;
  loadGameFromLocal: () => boolean;
  exportGameJson: () => string;
  importGameJson: (json: string) => boolean;
}

const initialPreset = getScenarioPreset('garage_1990');

export const useGameStore = create<GameStoreState>((set, get) => ({
  date: {
    year: initialPreset.startYear,
    month: initialPreset.startMonth,
    day: 1
  },
  speed: 1, // Start at 1x speed

  company: initialPreset.company,
  employees: initialPreset.employees,
  projects: initialPreset.projects,
  engines: initialPreset.engines,
  unlockedTechIds: initialPreset.unlockedTechIds,
  activeResearchTechId: null,

  activeEvent: null,
  activeReviewModalProject: null,
  selectedProjectId: null,
  selectedEmployeeId: null,

  toasts: [
    {
      id: 'welcome',
      title: 'Welcome to DevTycoon Studio!',
      message: `You are leading ${initialPreset.company.name} in ${initialPreset.startYear}. Let's build legendary games!`,
      type: 'info',
      timestamp: `${initialPreset.startYear}-01-01`
    }
  ],

  setSpeed: (speed) => {
    soundEngine.playClick();
    set({ speed });
  },

  tickDay: () => {
    const state = get();
    if (state.speed === 0) return;

    const updated = tickSimulationDay({
      date: state.date,
      company: state.company,
      employees: state.employees,
      projects: state.projects,
      engines: state.engines,
      unlockedTechIds: state.unlockedTechIds,
      activeResearchTechId: state.activeResearchTechId,
      activeEvent: state.activeEvent,
      toasts: state.toasts
    });

    set({
      date: updated.date,
      company: updated.company,
      employees: updated.employees,
      projects: updated.projects,
      engines: updated.engines,
      unlockedTechIds: updated.unlockedTechIds,
      activeResearchTechId: updated.activeResearchTechId,
      activeEvent: updated.activeEvent,
      toasts: updated.toasts.length > 0 ? [...updated.toasts, ...state.toasts].slice(0, 15) : state.toasts
    });
  },

  loadScenario: (presetId: string) => {
    const preset: ScenarioPreset = getScenarioPreset(presetId);
    soundEngine.playClick();
    set({
      date: { year: preset.startYear, month: preset.startMonth, day: 1 },
      speed: 1,
      company: preset.company,
      employees: preset.employees,
      projects: preset.projects,
      engines: preset.engines,
      unlockedTechIds: preset.unlockedTechIds,
      activeResearchTechId: null,
      activeEvent: null,
      activeReviewModalProject: null,
      selectedProjectId: null,
      selectedEmployeeId: null,
      toasts: [
        {
          id: `scenario-${Date.now()}`,
          title: `Loaded Scenario: ${preset.title}`,
          message: preset.description,
          type: 'info',
          timestamp: `${preset.startYear}-${String(preset.startMonth).padStart(2, '0')}-01`
        }
      ]
    });
  },

  createNewGame: (projectData) => {
    const state = get();
    const newId = `proj_${Date.now()}`;
    
    // Scale requirements
    let baseReq = 1000;
    if (projectData.scale === 'indie') baseReq = 800;
    if (projectData.scale === 'aa') baseReq = 2600;
    if (projectData.scale === 'aaa') baseReq = 5500;
    if (projectData.scale === 'aaaa') baseReq = 12000;

    const newProject: GameProject = {
      ...projectData,
      id: newId,
      phase: 'production',
      progress: { code: 0, design: 0, art: 0, audio: 0, qa: 0 },
      totalRequirements: {
        code: Math.round(baseReq * 1.1),
        design: Math.round(baseReq * 1.0),
        art: Math.round(baseReq * 0.9),
        audio: Math.round(baseReq * 0.5),
        qa: Math.round(baseReq * 0.4)
      },
      bugs: 0,
      techDebt: 0,
      polishPoints: 0,
      hype: 25,
      qualityScore: 50,
      reviewScore: null,
      criticReviews: [],
      developmentCost: 0,
      revenue: 0,
      unitsSold: 0,
      weeklySalesHistory: [],
      isLiveService: projectData.genreId === 'mmorpg',
      serverCapacity: projectData.genreId === 'mmorpg' ? 100000 : 0,
      activePlayers: 0,
      serverMonthlyCost: projectData.genreId === 'mmorpg' ? 15000 : 0,
      dlcList: [],
      patchLevel: 0,
      createdAtYear: state.date.year,
      createdAtMonth: state.date.month,
      createdAtDay: state.date.day
    };

    soundEngine.playAward();
    set({
      projects: [...state.projects, newProject],
      selectedProjectId: newId,
      toasts: [
        {
          id: `toast-${Date.now()}`,
          title: 'New Game Production Started!',
          message: `Work has begun on "${newProject.title}"! Assign developers to start progress.`,
          type: 'success',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  releaseGame: (projectId: string) => {
    const state = get();
    const proj = state.projects.find(p => p.id === projectId);
    if (!proj) return;

    const reviewRes = generateCriticReviews(proj);
    const updatedProj: GameProject = {
      ...proj,
      phase: proj.isLiveService ? 'live_service' : 'released',
      reviewScore: reviewRes.averageScore,
      criticReviews: reviewRes.reviews,
      releasedAtDate: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
    };

    soundEngine.playAward();
    set({
      projects: state.projects.map(p => p.id === projectId ? updatedProj : p),
      activeReviewModalProject: updatedProj,
      company: {
        ...state.company,
        reputation: Math.min(100, Math.max(10, state.company.reputation + Math.round((reviewRes.averageScore - 70) / 4))),
        fanBase: Math.max(100, state.company.fanBase + Math.round(reviewRes.averageScore * 120)),
        totalGamesReleased: state.company.totalGamesReleased + 1
      },
      toasts: [
        {
          id: `rel-${Date.now()}`,
          title: `Released: ${updatedProj.title}!`,
          message: `Critic Review Average: ${reviewRes.averageScore}/100!`,
          type: reviewRes.averageScore >= 80 ? 'success' : 'info',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  closeReviewModal: () => {
    soundEngine.playClick();
    set({ activeReviewModalProject: null });
  },

  toggleCrunchMode: (projectId, mode) => {
    soundEngine.playClick();
    set((state) => ({
      projects: state.projects.map(p => p.id === projectId ? { ...p, crunchMode: mode } : p)
    }));
  },

  addFeatureCreep: (projectId) => {
    soundEngine.playClick();
    set((state) => ({
      projects: state.projects.map(p => {
        if (p.id === projectId) {
          return {
            ...p,
            featureCreepCount: p.featureCreepCount + 1,
            totalRequirements: {
              ...p.totalRequirements,
              code: Math.round(p.totalRequirements.code * 1.15),
              design: Math.round(p.totalRequirements.design * 1.15),
              art: Math.round(p.totalRequirements.art * 1.15)
            },
            hype: p.hype + 10
          };
        }
        return p;
      })
    }));
  },

  addDLC: (projectId, title, price) => {
    soundEngine.playCash();
    const newDlc = {
      id: `dlc_${Date.now()}`,
      title,
      price,
      releaseDate: `${get().date.year}-${String(get().date.month).padStart(2, '0')}-${String(get().date.day).padStart(2, '0')}`,
      unitsSold: Math.round(get().company.fanBase * 0.4),
      revenue: Math.round(get().company.fanBase * 0.4 * price)
    };
    set((state) => ({
      company: {
        ...state.company,
        cash: state.company.cash + newDlc.revenue
      },
      projects: state.projects.map(p => p.id === projectId ? {
        ...p,
        dlcList: [...p.dlcList, newDlc],
        revenue: p.revenue + newDlc.revenue
      } : p)
    }));
  },

  hireEmployee: (applicant) => {
    const state = get();
    if (state.company.cash < applicant.signingBonus) {
      soundEngine.playError();
      return;
    }

    const newEmployee: Employee = {
      ...applicant,
      id: `emp_${Date.now()}`,
      monthlySalary: applicant.askingSalary,
      hiredDate: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`,
      assignedProjectId: state.selectedProjectId || (state.projects[0]?.id ?? null)
    };

    soundEngine.playCash();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - applicant.signingBonus
      },
      employees: [...state.employees, newEmployee],
      toasts: [
        {
          id: `hire-${Date.now()}`,
          title: `Hired ${newEmployee.name}!`,
          message: `Role: ${newEmployee.role.toUpperCase()} | Salary: $${newEmployee.monthlySalary}/mo`,
          type: 'success',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  fireEmployee: (empId) => {
    soundEngine.playClick();
    set((state) => ({
      employees: state.employees.filter(e => e.id !== empId),
      selectedEmployeeId: null
    }));
  },

  assignEmployee: (empId, projectId) => {
    soundEngine.playClick();
    set((state) => ({
      employees: state.employees.map(e => e.id === empId ? { ...e, assignedProjectId: projectId } : e)
    }));
  },

  trainEmployee: (empId, skillKey, cost) => {
    const state = get();
    if (state.company.cash < cost) {
      soundEngine.playError();
      return;
    }
    soundEngine.playCash();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - cost
      },
      employees: state.employees.map(e => {
        if (e.id === empId) {
          return {
            ...e,
            skills: {
              ...e.skills,
              [skillKey]: Math.min(100, e.skills[skillKey] + 12)
            },
            morale: Math.min(100, e.morale + 10)
          };
        }
        return e;
      })
    });
  },

  startResearch: (techId) => {
    const tech = getTechNodeById(techId);
    if (!tech) return;
    const state = get();
    if (state.company.cash < tech.researchCost) {
      soundEngine.playError();
      return;
    }
    soundEngine.playClick();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - tech.researchCost
      },
      activeResearchTechId: techId,
      toasts: [
        {
          id: `res-${Date.now()}`,
          title: `Research Started: ${tech.name}`,
          message: `Assigned programmers will now generate research progress.`,
          type: 'info',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  createCustomEngine: (name, platformIds, unlockedFeatureIds, licensingPrice, royaltyPercentage) => {
    const state = get();
    const devCost = 45000 + unlockedFeatureIds.length * 15000;
    if (state.company.cash < devCost) {
      soundEngine.playError();
      return;
    }

    const newEngine: CustomEngine = {
      id: `eng_${Date.now()}`,
      name,
      version: '1.0',
      unlockedFeatureIds,
      platformIds,
      developmentCost: devCost,
      isLicensedToRivals: true,
      licensingPrice,
      royaltyPercentage,
      totalRoyaltyEarned: 0,
      activeLicensingStudios: [],
      createdAt: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
    };

    soundEngine.playAward();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - devCost
      },
      engines: [...state.engines, newEngine],
      toasts: [
        {
          id: `eng-${Date.now()}`,
          title: 'Custom Game Engine Created!',
          message: `Engine "${name}" is now ready for your games and for third-party licensing!`,
          type: 'success',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  upgradeOffice: (tierId) => {
    const tier = getOfficeTierById(tierId);
    const state = get();
    if (state.company.cash < tier.upgradeCost) {
      soundEngine.playError();
      return;
    }
    soundEngine.playAward();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - tier.upgradeCost,
        officeTierId: tierId
      },
      toasts: [
        {
          id: `off-${Date.now()}`,
          title: `Upgraded Office to ${tier.name}!`,
          message: `Max Capacity: ${tier.maxEmployees} Employees | Enhanced energy & morale recovery!`,
          type: 'success',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  takeBankLoan: (principal, interestRate, months) => {
    const state = get();
    const monthlyPayment = Math.round((principal * (1 + interestRate)) / months);
    const newLoan = {
      id: `loan_${Date.now()}`,
      name: `Bank Credit Line ($${(principal / 1000).toFixed(0)}k)`,
      principal,
      remainingAmount: Math.round(principal * (1 + interestRate)),
      interestRate,
      monthlyPayment,
      remainingMonths: months
    };
    soundEngine.playCash();
    set({
      company: {
        ...state.company,
        cash: state.company.cash + principal,
        loans: [...state.company.loans, newLoan]
      },
      toasts: [
        {
          id: `loan-${Date.now()}`,
          title: `Loan Secured: $${principal.toLocaleString()}`,
          message: `Monthly payment: $${monthlyPayment.toLocaleString()}/mo for ${months} months.`,
          type: 'info',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  repayBankLoan: (loanId) => {
    const state = get();
    const loan = state.company.loans.find(l => l.id === loanId);
    if (!loan || state.company.cash < loan.remainingAmount) {
      soundEngine.playError();
      return;
    }
    soundEngine.playCash();
    set({
      company: {
        ...state.company,
        cash: state.company.cash - loan.remainingAmount,
        loans: state.company.loans.filter(l => l.id !== loanId)
      },
      toasts: [
        {
          id: `repay-${Date.now()}`,
          title: `Loan Fully Repaid!`,
          message: `You paid off "${loan.name}" and eliminated monthly debt service!`,
          type: 'success',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  resolveEventChoice: (choiceIndex) => {
    const state = get();
    const ev = state.activeEvent;
    if (!ev || !ev.choices[choiceIndex]) return;

    const choice = ev.choices[choiceIndex];
    let newCash = state.company.cash;
    let newRep = state.company.reputation;
    let newHype = 0;

    if (choice.cost) {
      newCash -= choice.cost;
    }
    if (choice.effect.cashChange) {
      newCash += choice.effect.cashChange;
    }
    if (choice.effect.reputationChange) {
      newRep = Math.max(10, Math.min(100, newRep + choice.effect.reputationChange));
    }
    if (choice.effect.hypeChange) {
      newHype = choice.effect.hypeChange;
    }

    soundEngine.playClick();
    set({
      activeEvent: null,
      company: {
        ...state.company,
        cash: newCash,
        reputation: newRep
      },
      projects: state.projects.map(p => ({
        ...p,
        hype: Math.max(0, p.hype + newHype),
        techDebt: choice.effect.techDebtChange ? Math.max(0, p.techDebt + choice.effect.techDebtChange) : p.techDebt,
        bugs: choice.effect.bugChange ? Math.max(0, p.bugs + choice.effect.bugChange) : p.bugs
      })),
      employees: state.employees.map(e => ({
        ...e,
        morale: choice.effect.moraleChange ? Math.max(10, Math.min(100, e.morale + choice.effect.moraleChange)) : e.morale
      })),
      toasts: [
        {
          id: `evres-${Date.now()}`,
          title: `Decision: ${choice.label}`,
          message: choice.effect.customMessage || 'Your decision has been implemented across the studio.',
          type: 'info',
          timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
        },
        ...state.toasts
      ]
    });
  },

  dismissToast: (toastId) => {
    set((state) => ({
      toasts: state.toasts.filter(t => t.id !== toastId)
    }));
  },

  selectProject: (projectId) => set({ selectedProjectId: projectId }),
  selectEmployee: (empId) => set({ selectedEmployeeId: empId }),

  saveGameToLocal: () => {
    const state = get();
    try {
      const data = {
        date: state.date,
        company: state.company,
        employees: state.employees,
        projects: state.projects,
        engines: state.engines,
        unlockedTechIds: state.unlockedTechIds,
        activeResearchTechId: state.activeResearchTechId
      };
      localStorage.setItem('devtycoon_save_v1', JSON.stringify(data));
      soundEngine.playCash();
      set({
        toasts: [
          {
            id: `save-${Date.now()}`,
            title: 'Game Saved!',
            message: 'Your studio progress has been saved to browser storage.',
            type: 'success',
            timestamp: `${state.date.year}-${String(state.date.month).padStart(2, '0')}-${String(state.date.day).padStart(2, '0')}`
          },
          ...state.toasts
        ]
      });
    } catch (e) {
      console.error('Failed to save game:', e);
    }
  },

  loadGameFromLocal: () => {
    try {
      const saved = localStorage.getItem('devtycoon_save_v1');
      if (!saved) return false;
      const data = JSON.parse(saved);
      soundEngine.playAward();
      set({
        date: data.date,
        company: data.company,
        employees: data.employees,
        projects: data.projects,
        engines: data.engines,
        unlockedTechIds: data.unlockedTechIds,
        activeResearchTechId: data.activeResearchTechId || null,
        activeEvent: null,
        activeReviewModalProject: null,
        selectedProjectId: null,
        selectedEmployeeId: null,
        toasts: [
          {
            id: `load-${Date.now()}`,
            title: 'Saved Game Loaded!',
            message: `Welcome back to ${data.company.name}!`,
            type: 'success',
            timestamp: `${data.date.year}-${String(data.date.month).padStart(2, '0')}-01`
          }
        ]
      });
      return true;
    } catch (e) {
      console.error('Failed to load save:', e);
      return false;
    }
  },

  exportGameJson: () => {
    const state = get();
    return JSON.stringify({
      date: state.date,
      company: state.company,
      employees: state.employees,
      projects: state.projects,
      engines: state.engines,
      unlockedTechIds: state.unlockedTechIds,
      activeResearchTechId: state.activeResearchTechId
    }, null, 2);
  },

  importGameJson: (json) => {
    try {
      const data = JSON.parse(json);
      if (!data.company || !data.date) return false;
      soundEngine.playAward();
      set({
        date: data.date,
        company: data.company,
        employees: data.employees || [],
        projects: data.projects || [],
        engines: data.engines || [],
        unlockedTechIds: data.unlockedTechIds || ['gfx_sprites', 'phys_basic', 'ai_fsm', 'aud_chiptune'],
        activeResearchTechId: data.activeResearchTechId || null,
        toasts: [
          {
            id: `import-${Date.now()}`,
            title: 'Studio Imported Successfully!',
            message: `Loaded studio "${data.company.name}".`,
            type: 'success',
            timestamp: `${data.date.year}-01-01`
          }
        ]
      });
      return true;
    } catch (e) {
      console.error('Import error:', e);
      return false;
    }
  }
}));
