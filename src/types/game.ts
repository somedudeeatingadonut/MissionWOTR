export type GameGenreId =
  | 'rpg'
  | 'fps'
  | 'rts'
  | 'action_adv'
  | 'sim'
  | 'city_builder'
  | 'mmorpg'
  | 'horror'
  | 'puzzle'
  | 'fighting';

export type GameThemeId =
  | 'scifi'
  | 'cyberpunk'
  | 'fantasy'
  | 'medieval'
  | 'post_apoc'
  | 'zombie'
  | 'cozy'
  | 'space'
  | 'noir'
  | 'mythology'
  | 'martial_arts'
  | 'mecha'
  | 'ww2'
  | 'crime'
  | 'pirates'
  | 'supernatural';

export type Audience = 'everyone' | 'teen' | 'mature' | 'hardcore';

export type GameScale = 'indie' | 'aa' | 'aaa' | 'aaaa';

export type PlatformCategory = 'pc' | 'console' | 'handheld' | 'vr' | 'mobile';

export interface Platform {
  id: string;
  name: string;
  manufacturer: string;
  startYear: number;
  endYear: number;
  marketShare: number; // 0.0 to 100.0 percentage
  devKitCost: number;
  techTier: number; // 1 to 5
  category: PlatformCategory;
  icon: string;
}

export interface GenreConfig {
  id: GameGenreId;
  name: string;
  description: string;
  baseCostMultiplier: number;
  workDistribution: {
    code: number; // percentage (sums to 100)
    design: number;
    art: number;
    audio: number;
    qa: number;
  };
  preferredAudience: Audience[];
  synergyThemes: GameThemeId[];
}

export interface ThemeConfig {
  id: GameThemeId;
  name: string;
  description: string;
  preferredGenres: GameGenreId[];
}

export type GamePhase =
  | 'concept'
  | 'pre_production'
  | 'production'
  | 'alpha_testing'
  | 'beta_polish'
  | 'marketing_hype'
  | 'released'
  | 'live_service';

export type CrunchMode = 'normal' | 'mild_overtime' | 'heavy_crunch';

export interface CriticReview {
  outlet: string;
  score: number; // 0 - 100
  summary: string;
  pros: string[];
  cons: string[];
}

export interface GameDLC {
  id: string;
  title: string;
  price: number;
  releaseDate: string; // YYYY-MM-DD
  unitsSold: number;
  revenue: number;
}

export interface GameProject {
  id: string;
  title: string;
  genreId: GameGenreId;
  themeId: GameThemeId;
  audience: Audience;
  scale: GameScale;
  platformIds: string[];
  targetEngineId: string; // Custom engine ID or 'default_engine'
  phase: GamePhase;
  
  // Work progress (current vs required)
  progress: {
    code: number;
    design: number;
    art: number;
    audio: number;
    qa: number;
  };
  totalRequirements: {
    code: number;
    design: number;
    art: number;
    audio: number;
    qa: number;
  };

  // Depth metrics
  bugs: number;
  techDebt: number; // High tech debt slows coding and introduces bugs
  polishPoints: number;
  hype: number; // 0 to 100+
  crunchMode: CrunchMode;
  featureCreepCount: number;

  // Outcome & reviews
  qualityScore: number; // Computed score 0 - 100
  reviewScore: number | null; // Final critic average (0 - 100)
  criticReviews: CriticReview[];

  // Economics
  developmentCost: number;
  revenue: number;
  unitsSold: number;
  weeklySalesHistory: number[];
  currentPrice: number;
  basePrice: number;

  // Live service / MMORPG features
  isLiveService: boolean;
  serverCapacity: number; // max concurrent players supported
  activePlayers: number;
  serverMonthlyCost: number;
  dlcList: GameDLC[];
  patchLevel: number;

  // Dates
  createdAtYear: number;
  createdAtMonth: number;
  createdAtDay: number;
  releasedAtDate?: string; // formatted YYYY-MM-DD
}
