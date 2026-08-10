import { Company, FinanceLedgerEntry } from '../types/company';
import { Employee } from '../types/employee';
import { GameProject } from '../types/game';
import { getOfficeTierById } from '../data/offices';

export interface MonthlyFinanceResult {
  ledgerEntry: FinanceLedgerEntry;
  updatedCompany: Company;
  newValuation: number;
}

export function processMonthlyFinances(
  company: Company,
  employees: Employee[],
  projects: GameProject[],
  month: number,
  year: number
): MonthlyFinanceResult {
  const officeTier = getOfficeTierById(company.officeTierId);

  // Calculate expenses
  const salaries = employees.reduce((sum, e) => sum + e.monthlySalary, 0);
  const rent = officeTier.monthlyRent;
  
  const serverCosts = projects
    .filter(p => p.isLiveService && p.phase === 'live_service')
    .reduce((sum, p) => sum + p.serverMonthlyCost, 0);

  // Loan payments
  let loanPayments = 0;
  const updatedLoans = company.loans.map(loan => {
    if (loan.remainingMonths > 0 && loan.remainingAmount > 0) {
      loanPayments += loan.monthlyPayment;
      return {
        ...loan,
        remainingAmount: Math.max(0, loan.remainingAmount - loan.monthlyPayment),
        remainingMonths: Math.max(0, loan.remainingMonths - 1)
      };
    }
    return loan;
  }).filter(loan => loan.remainingMonths > 0 && loan.remainingAmount > 0);

  const totalExpenses = salaries + rent + serverCosts + loanPayments;

  // Calculate revenue from live games & licensing
  let gameSalesRevenue = 0;
  let dlcSalesRevenue = 0;

  projects.forEach(p => {
    if (p.phase === 'released' || p.phase === 'live_service') {
      // Add monthly live service sub / mtx revenue
      if (p.isLiveService) {
        const arpu = 8.50; // average revenue per user per month
        gameSalesRevenue += Math.round(p.activePlayers * arpu);
      }
    }
  });

  const totalRevenue = gameSalesRevenue + dlcSalesRevenue;
  const netProfit = totalRevenue - totalExpenses;
  const cashAfter = company.cash + netProfit;

  // Valuation formula
  const annualProfitEstimate = Math.max(0, netProfit * 12);
  const assetsValue = Math.max(50000, cashAfter) + employees.length * 25000;
  const reputationValue = company.reputation * 10000 + company.fanBase * 5;
  const newValuation = Math.round(assetsValue + annualProfitEstimate * 3 + reputationValue);

  const sharePrice = Math.max(0.50, Math.round((newValuation / company.totalShares) * 100) / 100);

  const ledgerEntry: FinanceLedgerEntry = {
    month,
    year,
    revenue: {
      gameSales: gameSalesRevenue,
      dlcSales: dlcSalesRevenue,
      engineLicensing: 0,
      investorFunding: 0,
      other: 0
    },
    expenses: {
      salaries,
      rent,
      serverCosts,
      marketing: 0,
      researchAndDev: 0,
      loanPayments,
      other: 0
    },
    netProfit,
    cashAfter
  };

  const updatedCompany: Company = {
    ...company,
    cash: cashAfter,
    loans: updatedLoans,
    companyValuation: newValuation,
    sharePrice,
    ledgerHistory: [ledgerEntry, ...company.ledgerHistory].slice(0, 24) // keep last 2 years
  };

  return {
    ledgerEntry,
    updatedCompany,
    newValuation
  };
}
