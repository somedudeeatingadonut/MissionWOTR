import { OfficeTier } from '../types/company';

export const OFFICE_TIERS: OfficeTier[] = [
  {
    id: 'garage',
    name: "Founder's Garage",
    description: "Cramped, dusty, but full of passion. Low overhead for humble indie beginnings.",
    maxEmployees: 4,
    monthlyRent: 300,
    upgradeCost: 0,
    amenities: {
      energyRegenRate: 1.0,
      moraleBonus: 0,
      bugReductionBonus: 0,
      serverCapacityBonus: 5000
    },
    imageGradient: 'from-amber-900/40 to-slate-900'
  },
  {
    id: 'indie_loft',
    name: 'Downtown Indie Loft',
    description: 'Exposed brick, beanbag chairs, and an espresso machine. Perfect for small studios.',
    maxEmployees: 10,
    monthlyRent: 2800,
    upgradeCost: 25000,
    amenities: {
      energyRegenRate: 1.25,
      moraleBonus: 5,
      bugReductionBonus: 5,
      serverCapacityBonus: 25000
    },
    imageGradient: 'from-sky-900/40 to-slate-900'
  },
  {
    id: 'midtown_studio',
    name: 'Midtown Game Studio',
    description: 'Professional workstations, dedicated sound booth, and QA lab.',
    maxEmployees: 25,
    monthlyRent: 9500,
    upgradeCost: 120000,
    amenities: {
      energyRegenRate: 1.5,
      moraleBonus: 10,
      bugReductionBonus: 15,
      serverCapacityBonus: 100000
    },
    imageGradient: 'from-emerald-900/40 to-slate-900'
  },
  {
    id: 'campus',
    name: 'High-Tech Studio Campus',
    description: 'Motion capture stage, cafeteria, fitness center, and massive server racks.',
    maxEmployees: 60,
    monthlyRent: 35000,
    upgradeCost: 500000,
    amenities: {
      energyRegenRate: 1.8,
      moraleBonus: 18,
      bugReductionBonus: 25,
      serverCapacityBonus: 500000
    },
    imageGradient: 'from-purple-900/40 to-slate-900'
  },
  {
    id: 'skyscraper_hq',
    name: 'AAA Enterprise Skyscraper HQ',
    description: 'The pinnacle of global gaming dominance. Unrivaled luxury, compute, and prestige.',
    maxEmployees: 150,
    monthlyRent: 120000,
    upgradeCost: 2500000,
    amenities: {
      energyRegenRate: 2.2,
      moraleBonus: 25,
      bugReductionBonus: 35,
      serverCapacityBonus: 2500000
    },
    imageGradient: 'from-rose-900/40 to-slate-900'
  }
];

export function getOfficeTierById(id: string): OfficeTier {
  return OFFICE_TIERS.find(o => o.id === id) || OFFICE_TIERS[0];
}
