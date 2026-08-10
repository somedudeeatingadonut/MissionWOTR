export type TechCategory =
  | 'graphics'
  | 'physics'
  | 'audio'
  | 'ai'
  | 'networking'
  | 'workflow';

export interface TechNode {
  id: string;
  name: string;
  category: TechCategory;
  description: string;
  unlockYear: number;
  researchCost: number; // in $
  workPointsRequired: number; // research effort
  progress: number;
  unlocked: boolean;
  prerequisites: string[]; // IDs of required nodes
  statBonus: {
    qualityBoost: number; // +X% quality to games using this feature
    bugReduction?: number;
    devSpeedBonus?: number;
    hypeBonus?: number;
  };
}

export interface CustomEngine {
  id: string;
  name: string;
  version: string;
  unlockedFeatureIds: string[];
  platformIds: string[];
  developmentCost: number;
  isLicensedToRivals: boolean;
  licensingPrice: number; // Upfront fee charged to rivals
  royaltyPercentage: number; // % royalty charged on rival game sales
  totalRoyaltyEarned: number;
  activeLicensingStudios: string[]; // Names of rival studios licensing this engine
  createdAt: string;
}
