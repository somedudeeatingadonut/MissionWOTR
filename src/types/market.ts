import { GameGenreId, GameThemeId } from './game';

export interface RivalStudio {
  id: string;
  name: string;
  reputation: number;
  cash: number;
  specialtyGenre: GameGenreId;
  marketShare: number; // 0 to 100
  recentRelease?: {
    title: string;
    genre: GameGenreId;
    score: number;
    sales: number;
    releaseYear: number;
  };
  isLicensingPlayerEngine?: boolean;
}

export interface MarketTrend {
  genreId: GameGenreId;
  themeId: GameThemeId;
  popularityMultiplier: number; // e.g. 1.4 for hot trend, 0.7 for cold
  startYear: number;
  endYear: number;
  reason: string;
}

export interface AwardCategory {
  id: string;
  name: string;
  description: string;
}

export interface AwardNominee {
  gameTitle: string;
  studioName: string;
  isPlayerGame: boolean;
  score: number;
}

export interface AwardEvent {
  year: number;
  categories: {
    categoryId: string;
    categoryName: string;
    nominees: AwardNominee[];
    winner: AwardNominee;
  }[];
}

export interface NewsHeadline {
  id: string;
  year: number;
  month: number;
  day: number;
  title: string;
  category: 'trend' | 'rival' | 'tech' | 'award' | 'general';
  impact?: string;
}
