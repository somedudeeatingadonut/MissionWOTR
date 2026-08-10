import { TraitConfig } from '../types/employee';

export const EMPLOYEE_TRAITS: TraitConfig[] = [
  {
    id: 'spaghetti_coder',
    name: 'Spaghetti Coder',
    description: 'Writes code incredibly fast, but leaves behind tangled bugs and tech debt.',
    isPositive: false,
    effectSummary: '+35% Coding Speed, +40% Bug & Tech Debt generation.'
  },
  {
    id: 'perfectionist',
    name: 'Perfectionist',
    description: 'Refuses to compromise on quality. Works slower, but delivers pristine results.',
    isPositive: true,
    effectSummary: '+25% Polish Points generated, -20% Speed, -50% Bug generation.'
  },
  {
    id: 'bug_hunter',
    name: 'Bug Hunter',
    description: 'Has a natural instinct for tracking down crash bugs and memory leaks.',
    isPositive: true,
    effectSummary: '+60% QA Efficiency, reduces team tech debt.'
  },
  {
    id: 'hit_maker',
    name: 'Hit Maker',
    description: 'Has an uncanny pulse on what gamers want. Projects they touch gain Hype.',
    isPositive: true,
    effectSummary: '+20% Game Quality & Hype boost on assigned projects.'
  },
  {
    id: 'crunch_resistant',
    name: 'Crunch Resistant',
    description: 'Can work overtime without losing morale or burning out as quickly.',
    isPositive: true,
    effectSummary: '-60% Stress gain during Overtime and Crunch mode.'
  },
  {
    id: 'diva',
    name: 'Studio Diva',
    description: 'Brilliant talent, but demands high pay and lowers morale if not put on AAA titles.',
    isPositive: false,
    effectSummary: '+30% Primary Skill, +50% Salary expectation, -10 Team Morale.'
  },
  {
    id: 'team_player',
    name: 'Team Player',
    description: 'Boosts the energy and morale of everyone sitting in the same office.',
    isPositive: true,
    effectSummary: '+15% Morale recovery for all employees.'
  },
  {
    id: 'speed_demon',
    name: 'Speed Demon',
    description: 'An absolute machine at completing tasks ahead of schedule.',
    isPositive: true,
    effectSummary: '+40% Work Speed across all assigned tasks.'
  },
  {
    id: 'coffee_addict',
    name: 'Coffee Addict',
    description: 'Needs constant caffeine breaks, but works at 200% speed after visiting the break room.',
    isPositive: true,
    effectSummary: '+20% Speed, energy depletes 15% faster without break room.'
  },
  {
    id: 'lore_master',
    name: 'Lore Master',
    description: 'Writes encyclopedic worldbuilding and character backstories.',
    isPositive: true,
    effectSummary: '+35% Design & Writing quality for RPGs and Action-Adventures.'
  }
];

export function getTraitConfig(id: string): TraitConfig | undefined {
  return EMPLOYEE_TRAITS.find(t => t.id === id);
}
