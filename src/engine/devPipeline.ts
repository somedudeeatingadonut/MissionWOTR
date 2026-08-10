import { Employee } from '../types/employee';
import { GameProject } from '../types/game';
import { OfficeTier } from '../types/company';
import { getGenreById } from '../data/genres';

export interface DailyDevResult {
  codeDone: number;
  designDone: number;
  artDone: number;
  audioDone: number;
  qaDone: number;
  bugsAdded: number;
  techDebtAdded: number;
  polishAdded: number;
  employeeUpdates: {
    id: string;
    energyChange: number;
    stressChange: number;
    moraleChange: number;
  }[];
}

export function calculateDailyDevProgress(
  project: GameProject,
  assignedEmployees: Employee[],
  officeTier: OfficeTier
): DailyDevResult {
  let totalCode = 0;
  let totalDesign = 0;
  let totalArt = 0;
  let totalAudio = 0;
  let totalQA = 0;
  let bugsAdded = 0;
  let techDebtAdded = 0;
  let polishAdded = 0;

  const employeeUpdates: DailyDevResult['employeeUpdates'] = [];

  const genre = getGenreById(project.genreId);
  const crunchMode = project.crunchMode;

  // Crunch mode multipliers
  let speedMult = 1.0;
  let stressMult = 1.0;
  let bugChanceMult = 1.0;
  let energyDrain = 10; // normal daily energy drain

  if (crunchMode === 'mild_overtime') {
    speedMult = 1.25;
    stressMult = 1.4;
    bugChanceMult = 1.25;
    energyDrain = 15;
  } else if (crunchMode === 'heavy_crunch') {
    speedMult = 1.60;
    stressMult = 2.2;
    bugChanceMult = 1.6;
    energyDrain = 25;
  }

  // Office amenities
  const energyRegen = officeTier.amenities.energyRegenRate;
  const netEnergyDrain = Math.max(2, energyDrain / energyRegen);
  const officeBugReduction = officeTier.amenities.bugReductionBonus / 100; // e.g. 0.15

  // Tech debt penalty on coding speed: e.g. 50 tech debt = 25% penalty
  const techDebtPenalty = Math.max(0.4, 1 - project.techDebt * 0.005);

  assignedEmployees.forEach(emp => {
    // Check if employee is exhausted
    const effectiveness = emp.energy > 15 ? 1.0 : 0.4;
    const moraleMult = 0.5 + (emp.morale / 100) * 0.7; // 0.5 to 1.2

    let empSpeed = (emp.speed / 100) * speedMult * effectiveness * moraleMult;

    // Traits
    let isSpaghetti = emp.traits.includes('spaghetti_coder');
    let isPerfectionist = emp.traits.includes('perfectionist');
    let isSpeedDemon = emp.traits.includes('speed_demon');
    let isBugHunter = emp.traits.includes('bug_hunter');
    let isCrunchResistant = emp.traits.includes('crunch_resistant');

    if (isSpeedDemon) empSpeed *= 1.35;
    if (isSpaghetti) empSpeed *= 1.35;
    if (isPerfectionist) empSpeed *= 0.8;

    // Primary role contribution
    if (emp.role === 'programmer') {
      const codeOutput = (emp.skills.code / 4) * empSpeed * techDebtPenalty;
      totalCode += codeOutput;

      // Bug generation
      let bugRate = (1 - emp.skills.code / 120) * bugChanceMult;
      if (isSpaghetti) bugRate *= 1.4;
      if (isPerfectionist) bugRate *= 0.5;
      bugRate *= (1 - officeBugReduction);

      if (Math.random() < bugRate) {
        bugsAdded += Math.ceil(Math.random() * 2);
        if (Math.random() < 0.3) {
          techDebtAdded += 1;
        }
      }
    } else if (emp.role === 'designer' || emp.role === 'producer') {
      const designOutput = (emp.skills.design / 4) * empSpeed;
      totalDesign += designOutput;
      if (isPerfectionist) {
        polishAdded += 1.5;
      }
    } else if (emp.role === 'artist') {
      const artOutput = (emp.skills.art / 4) * empSpeed;
      totalArt += artOutput;
      if (isPerfectionist) {
        polishAdded += 2;
      }
    } else if (emp.role === 'audio_engineer') {
      const audioOutput = (emp.skills.audio / 4) * empSpeed;
      totalAudio += audioOutput;
    } else if (emp.role === 'qa_tester') {
      let qaOutput = (emp.skills.qa / 4) * empSpeed;
      if (isBugHunter) qaOutput *= 1.5;
      totalQA += qaOutput;

      // QA testers remove existing bugs & tech debt!
      const bugsFixed = Math.floor(qaOutput / 8);
      if (project.bugs > 0) {
        bugsAdded -= Math.min(project.bugs, bugsFixed);
      }
      if (project.techDebt > 0 && Math.random() < 0.4) {
        techDebtAdded -= 1;
      }
      polishAdded += 1;
    } else if (emp.role === 'marketing') {
      // Marketers generate hype while assigned
      polishAdded += 0.5;
    }

    // Calculate employee state updates
    let energyChange = -netEnergyDrain;
    let stressChange = 0.5 * stressMult;
    let moraleChange = 0;

    if (isCrunchResistant && stressChange > 0.5) {
      stressChange *= 0.4;
    }
    if (crunchMode === 'heavy_crunch') {
      moraleChange = -0.8;
    } else if (emp.stress > 80) {
      moraleChange = -0.5;
    }

    employeeUpdates.push({
      id: emp.id,
      energyChange,
      stressChange,
      moraleChange
    });
  });

  return {
    codeDone: Math.round(totalCode),
    designDone: Math.round(totalDesign),
    artDone: Math.round(totalArt),
    audioDone: Math.round(totalAudio),
    qaDone: Math.round(totalQA),
    bugsAdded,
    techDebtAdded,
    polishAdded: Math.round(polishAdded * 10) / 10,
    employeeUpdates
  };
}

export function computeProjectQualityScore(project: GameProject): number {
  // Base progress ratio
  const req = project.totalRequirements;
  const prog = project.progress;

  const totalReq = req.code + req.design + req.art + req.audio + req.qa;
  const totalProg = prog.code + prog.design + prog.art + prog.audio + prog.qa;

  const completionRatio = Math.min(1.0, totalProg / Math.max(1, totalReq));

  // Base score out of 70
  let score = 55 * completionRatio;

  // Polish bonus (up to +25)
  const polishBonus = Math.min(25, (project.polishPoints / 50) * 10);
  score += polishBonus;

  // Synergy bonus (we check theme synergy)
  const genre = getGenreById(project.genreId);
  if (genre.synergyThemes.includes(project.themeId)) {
    score += 10;
  }

  // Feature creep penalty or bonus
  if (project.featureCreepCount > 0) {
    score += project.featureCreepCount * 3;
  }

  // Penalty for bugs and tech debt
  const bugPenalty = project.bugs * 0.45;
  const techDebtPenalty = project.techDebt * 0.25;

  score = Math.max(10, Math.min(100, score - bugPenalty - techDebtPenalty));
  return Math.round(score);
}
