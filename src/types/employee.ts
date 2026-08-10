export type EmployeeRole =
  | 'programmer'
  | 'designer'
  | 'artist'
  | 'audio_engineer'
  | 'qa_tester'
  | 'marketing'
  | 'producer';

export type TraitId =
  | 'spaghetti_coder'
  | 'perfectionist'
  | 'bug_hunter'
  | 'hit_maker'
  | 'crunch_resistant'
  | 'diva'
  | 'team_player'
  | 'speed_demon'
  | 'coffee_addict'
  | 'lore_master';

export interface TraitConfig {
  id: TraitId;
  name: string;
  description: string;
  isPositive: boolean;
  effectSummary: string;
}

export interface EmployeeSkills {
  code: number;
  design: number;
  art: number;
  audio: number;
  qa: number;
  marketing: number;
  management: number;
}

export interface Employee {
  id: string;
  name: string;
  avatarSeed: string; // seed for SVG generation or consistent icon
  role: EmployeeRole;
  level: number; // 1 to 5 (Junior, Mid, Senior, Lead, Director)
  
  skills: EmployeeSkills;
  
  // Core personal attributes (0 to 100)
  intelligence: number;
  creativity: number;
  workEthic: number;
  stressTolerance: number;
  speed: number;

  // Dynamic simulation state
  morale: number;   // 0 to 100
  stress: number;   // 0 to 100
  energy: number;   // 0 to 100 (depletes during day, restores in break room/night)
  
  // Economics
  monthlySalary: number;
  hiredDate: string;

  // Traits and assignment
  traits: TraitId[];
  assignedProjectId: string | null; // ID of GameProject or CustomEngine or null
  isTraining?: boolean;
  trainingSkill?: keyof EmployeeSkills;
  trainingDaysLeft?: number;
}

export interface JobApplicant extends Employee {
  askingSalary: number;
  signingBonus: number;
}
