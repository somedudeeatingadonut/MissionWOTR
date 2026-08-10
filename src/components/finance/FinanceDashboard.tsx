import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { 
  DollarSign, 
  TrendingUp, 
  TrendingDown, 
  CreditCard, 
  Building2, 
  PieChart, 
  ArrowUpRight, 
  ArrowDownRight,
  Plus
} from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

export const FinanceDashboard: React.FC = () => {
  const { company, takeBankLoan, repayBankLoan } = useGameStore();

  const [loanPrincipal, setLoanPrincipal] = useState<number>(100000);

  const latestLedger = company.ledgerHistory[0] || {
    month: 1,
    year: 1990,
    revenue: { gameSales: 0, dlcSales: 0, engineLicensing: 0, investorFunding: 0, other: 0 },
    expenses: { salaries: 0, rent: 0, serverCosts: 0, marketing: 0, researchAndDev: 0, loanPayments: 0, other: 0 },
    netProfit: 0,
    cashAfter: company.cash
  };

  const totalRev = Object.values(latestLedger.revenue).reduce((a, b) => a + b, 0);
  const totalExp = Object.values(latestLedger.expenses).reduce((a, b) => a + b, 0);

  const handleTakeLoan = () => {
    soundEngine.playCash();
    takeBankLoan(loanPrincipal, 0.08, 24);
  };

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-950">
      {/* Header */}
      <div className="border-b border-slate-800 pb-5">
        <h2 className="text-2xl font-bold text-white flex items-center space-x-2">
          <DollarSign className="w-7 h-7 text-emerald-400" />
          <span>Financial Ledgers, Bank Loans, & Company Valuation</span>
        </h2>
        <p className="text-sm text-slate-400 mt-0.5">
          Track monthly profit & loss, manage debt service, and grow company shareholder equity.
        </p>
      </div>

      {/* Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
            Bank Account Balance
          </div>
          <div className={`font-mono text-3xl font-extrabold ${company.cash >= 0 ? 'text-emerald-400' : 'text-rose-400'}`}>
            ${company.cash.toLocaleString()}
          </div>
          <p className="text-xs text-slate-400">
            Available working capital
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
            Last Month Net Profit
          </div>
          <div className={`font-mono text-3xl font-extrabold flex items-center space-x-1 ${
            latestLedger.netProfit >= 0 ? 'text-emerald-400' : 'text-rose-400'
          }`}>
            {latestLedger.netProfit >= 0 ? <TrendingUp className="w-6 h-6" /> : <TrendingDown className="w-6 h-6" />}
            <span>${Math.abs(latestLedger.netProfit).toLocaleString()}</span>
          </div>
          <p className="text-xs text-slate-400">
            Rev: ${totalRev.toLocaleString()} | Exp: ${totalExp.toLocaleString()}
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
            Company Share Price
          </div>
          <div className="font-mono text-3xl font-extrabold text-sky-400">
            ${company.sharePrice.toFixed(2)}
          </div>
          <p className="text-xs text-slate-400">
            {company.totalShares.toLocaleString()} Total Shares
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 space-y-2">
          <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
            Enterprise Valuation
          </div>
          <div className="font-mono text-3xl font-extrabold text-purple-400">
            ${company.companyValuation.toLocaleString()}
          </div>
          <p className="text-xs text-slate-400">
            Based on IP assets, reputation, & profit
          </p>
        </div>
      </div>

      {/* P&L Statement Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Revenue Card */}
        <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <h3 className="font-bold text-base text-white flex items-center space-x-2">
              <ArrowUpRight className="w-5 h-5 text-emerald-400" />
              <span>Revenue Breakdown (Last Month)</span>
            </h3>
            <span className="font-mono font-extrabold text-emerald-400">
              +${totalRev.toLocaleString()}
            </span>
          </div>

          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-slate-300">Game Sales</span>
              <span className="font-mono text-white">${latestLedger.revenue.gameSales.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">DLC & Expansion Packs</span>
              <span className="font-mono text-white">${latestLedger.revenue.dlcSales.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">Engine Licensing Royalties</span>
              <span className="font-mono text-white">${latestLedger.revenue.engineLicensing.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">Investor Funding & Grants</span>
              <span className="font-mono text-white">${latestLedger.revenue.investorFunding.toLocaleString()}</span>
            </div>
          </div>
        </div>

        {/* Expenses Card */}
        <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <h3 className="font-bold text-base text-white flex items-center space-x-2">
              <ArrowDownRight className="w-5 h-5 text-rose-400" />
              <span>Expense Breakdown (Last Month)</span>
            </h3>
            <span className="font-mono font-extrabold text-rose-400">
              -${totalExp.toLocaleString()}
            </span>
          </div>

          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-slate-300">Employee Payroll</span>
              <span className="font-mono text-white">${latestLedger.expenses.salaries.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">Office Rent</span>
              <span className="font-mono text-white">${latestLedger.expenses.rent.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">Server & MMO Maintenance</span>
              <span className="font-mono text-white">${latestLedger.expenses.serverCosts.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-300">Loan Debt Payments</span>
              <span className="font-mono text-white">${latestLedger.expenses.loanPayments.toLocaleString()}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Bank Loans Manager */}
      <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="font-bold text-base text-white flex items-center space-x-2">
            <CreditCard className="w-5 h-5 text-sky-400" />
            <span>Bank Lines of Credit & Outstanding Debt</span>
          </h3>
          <span className="text-xs text-slate-400 font-mono">
            Interest Rate: 8.0% APR
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {company.loans.length === 0 ? (
            <div className="md:col-span-2 p-5 rounded-xl bg-slate-950 border border-slate-800 flex items-center justify-between">
              <div>
                <div className="font-bold text-sm text-white">No Outstanding Debt</div>
                <div className="text-xs text-slate-400 mt-0.5">
                  Your studio is debt-free! You can borrow capital for major studio expansions.
                </div>
              </div>
            </div>
          ) : (
            company.loans.map(loan => (
              <div
                key={loan.id}
                className="p-4 rounded-xl bg-slate-950 border border-slate-800 space-y-3 flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-sm text-white">{loan.name}</span>
                    <span className="text-xs font-mono text-amber-400">{loan.remainingMonths} mo left</span>
                  </div>
                  <div className="text-xs text-slate-400 mt-1">
                    Payment: <strong className="text-white">${loan.monthlyPayment.toLocaleString()}/mo</strong> | Total Due: <strong className="text-white">${loan.remainingAmount.toLocaleString()}</strong>
                  </div>
                </div>

                <button
                  onClick={() => repayBankLoan(loan.id)}
                  disabled={company.cash < loan.remainingAmount}
                  className={`w-full py-2 rounded-lg text-xs font-semibold transition ${
                    company.cash >= loan.remainingAmount
                      ? 'bg-emerald-600 hover:bg-emerald-500 text-white'
                      : 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
                  }`}
                >
                  Repay Early (${loan.remainingAmount.toLocaleString()})
                </button>
              </div>
            ))
          )}

          {/* New Loan Card */}
          <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 space-y-3 flex flex-col justify-between">
            <div>
              <h4 className="font-bold text-sm text-white">Secure New Credit Line</h4>
              <p className="text-xs text-slate-400 mt-0.5">
                Borrow cash for 24 months to fund AAA games or campus upgrades.
              </p>
            </div>

            <div className="space-y-2">
              <select
                value={loanPrincipal}
                onChange={(e) => setLoanPrincipal(Number(e.target.value))}
                className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none"
              >
                <option value={50000}>Borrow $50,000</option>
                <option value={100000}>Borrow $100,000</option>
                <option value={250000}>Borrow $250,000</option>
                <option value={500000}>Borrow $500,000</option>
              </select>

              <button
                onClick={handleTakeLoan}
                className="w-full bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs py-2 rounded-lg transition flex items-center justify-center space-x-1"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Borrow ${loanPrincipal.toLocaleString()}</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Historical Ledger Table */}
      {company.ledgerHistory.length > 0 && (
        <div className="p-6 rounded-2xl bg-slate-900 border border-slate-800 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-base text-white flex items-center space-x-2">
              <PieChart className="w-5 h-5 text-sky-400" />
              <span>Historical Monthly Ledger Statement</span>
            </h3>
            <span className="text-xs text-slate-400 font-mono">
              Last {company.ledgerHistory.length} Months
            </span>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-slate-800 text-slate-400 uppercase tracking-wider font-semibold">
                  <th className="py-2 px-3">Date</th>
                  <th className="py-2 px-3">Game Sales</th>
                  <th className="py-2 px-3">DLC Sales</th>
                  <th className="py-2 px-3">Payroll</th>
                  <th className="py-2 px-3">Rent / Server</th>
                  <th className="py-2 px-3">Net Profit</th>
                  <th className="py-2 px-3">Cash After</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-mono">
                {company.ledgerHistory.map((entry, idx) => {
                  const revSales = entry.revenue.gameSales;
                  const revDlc = entry.revenue.dlcSales;
                  const expSal = entry.expenses.salaries;
                  const expOther = entry.expenses.rent + entry.expenses.serverCosts + entry.expenses.loanPayments;

                  return (
                    <tr key={idx} className="hover:bg-slate-800/40">
                      <td className="py-2 px-3 text-slate-300 font-sans font-semibold">
                        {entry.month}/{entry.year}
                      </td>
                      <td className="py-2 px-3 text-emerald-400">${revSales.toLocaleString()}</td>
                      <td className="py-2 px-3 text-emerald-400">${revDlc.toLocaleString()}</td>
                      <td className="py-2 px-3 text-rose-400">-${expSal.toLocaleString()}</td>
                      <td className="py-2 px-3 text-rose-400">-${expOther.toLocaleString()}</td>
                      <td className={`py-2 px-3 font-bold ${entry.netProfit >= 0 ? 'text-emerald-400' : 'text-rose-400'}`}>
                        ${entry.netProfit.toLocaleString()}
                      </td>
                      <td className="py-2 px-3 text-slate-300">${entry.cashAfter.toLocaleString()}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
