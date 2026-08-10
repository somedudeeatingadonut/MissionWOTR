export type OfficeTierId =
  | 'garage'
  | 'indie_loft'
  | 'midtown_studio'
  | 'campus'
  | 'skyscraper_hq';

export interface OfficeTier {
  id: OfficeTierId;
  name: string;
  description: string;
  maxEmployees: number;
  monthlyRent: number;
  upgradeCost: number;
  amenities: {
    energyRegenRate: number; // bonus energy regen
    moraleBonus: number; // static morale boost
    bugReductionBonus: number; // QA lab efficiency
    serverCapacityBonus: number; // built-in data center
  };
  imageGradient: string;
}

export interface BankLoan {
  id: string;
  name: string;
  principal: number;
  remainingAmount: number;
  interestRate: number; // e.g. 0.08 = 8%
  monthlyPayment: number;
  remainingMonths: number;
}

export interface FinanceLedgerEntry {
  month: number;
  year: number;
  revenue: {
    gameSales: number;
    dlcSales: number;
    engineLicensing: number;
    investorFunding: number;
    other: number;
  };
  expenses: {
    salaries: number;
    rent: number;
    serverCosts: number;
    marketing: number;
    researchAndDev: number;
    loanPayments: number;
    other: number;
  };
  netProfit: number;
  cashAfter: number;
}

export interface Company {
  name: string;
  ceoName: string;
  ceoSpecialty: 'programmer' | 'designer' | 'marketer' | 'producer';
  reputation: number; // 0 to 100+
  fanBase: number;    // number of loyal fans
  cash: number;       // bank account balance in USD
  officeTierId: OfficeTierId;
  loans: BankLoan[];
  ledgerHistory: FinanceLedgerEntry[];
  
  // Stock / Valuation
  companyValuation: number;
  sharePrice: number;
  totalShares: number;
  playerOwnedShares: number; // Percentage or absolute
  
  // Stats
  totalGamesReleased: number;
  gameOfTheYearAwards: number;
}
