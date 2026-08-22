using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class InvestmentService
{
    private readonly AppDbContext _context;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<InvestmentService> _logger;

    public InvestmentService(AppDbContext context, IMarketDataService marketDataService, ILogger<InvestmentService> logger)
    {
        _context = context;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(int userId)
    {
        var investments = await _context.Investments
            .Include(i => i.Transactions)
            .Where(i => i.UserID == userId && !i.IsLiquidated)
            .ToListAsync();

        // Refresh market values before returning
        await RefreshPortfolioValuesAsync(investments);

        var dtos = investments.Select(MapToReadDto).ToList();

        var totalInvested = dtos.Sum(i => i.TotalInvested);
        var totalCurrentValue = dtos.Sum(i => i.CurrentValue);
        var totalProfitLoss = totalCurrentValue - totalInvested;
        var totalProfitLossPct = totalInvested > 0 ? Math.Round((totalProfitLoss / totalInvested) * 100, 2) : 0;

        return new PortfolioSummaryDto
        {
            TotalInvested = totalInvested,
            TotalCurrentValue = totalCurrentValue,
            TotalProfitLossAmount = totalProfitLoss,
            TotalProfitLossPercentage = totalProfitLossPct,
            Investments = dtos
        };
    }

    public async Task<InvestmentReadDto?> GetByIdAsync(int id, int userId)
    {
        var investment = await _context.Investments
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.InvestmentID == id && i.UserID == userId);

        if (investment == null) return null;

        await RefreshSingleInvestmentValueAsync(investment);
        return MapToReadDto(investment);
    }

    public async Task<InvestmentReadDto> CreateAsync(InvestmentCreateDto dto)
    {
        var entity = new Investment
        {
            UserID = dto.UserID,
            Name = dto.Name,
            Ticker = dto.Ticker?.ToUpper().Trim(),
            InvestmentType = dto.InvestmentType,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim(),
            TotalInvested = dto.TotalInvested,
            CurrentValue = dto.TotalInvested, // Initial value equals invested amount
            Quantity = dto.Quantity,
            PurchasePricePerUnit = dto.PurchasePricePerUnit,
            CurrentPricePerUnit = dto.PurchasePricePerUnit,
            RateType = dto.RateType,
            AnnualRate = dto.AnnualRate,
            IsTaxExempt = dto.IsTaxExempt,
            StartDate = dto.StartDate,
            MaturityDate = dto.MaturityDate,
            IsLiquidated = false,
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Create initial transaction
        entity.Transactions.Add(new InvestmentTransaction
        {
            TransactionType = InvestmentTransactionType.Buy,
            Amount = dto.TotalInvested,
            Quantity = dto.Quantity,
            UnitPrice = dto.PurchasePricePerUnit,
            TransactionDate = dto.StartDate,
            Notes = "Initial investment creation",
            CreatedAtUtc = DateTime.UtcNow
        });

        _context.Investments.Add(entity);
        await _context.SaveChangesAsync();

        await RefreshSingleInvestmentValueAsync(entity);
        return MapToReadDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, int userId, InvestmentUpdateDto dto)
    {
        var entity = await _context.Investments.FirstOrDefaultAsync(i => i.InvestmentID == id && i.UserID == userId);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Ticker = dto.Ticker?.ToUpper().Trim();
        entity.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim();
        entity.Quantity = dto.Quantity;
        entity.PurchasePricePerUnit = dto.PurchasePricePerUnit;
        entity.CurrentPricePerUnit = dto.CurrentPricePerUnit;
        entity.RateType = dto.RateType;
        entity.AnnualRate = dto.AnnualRate;
        entity.IsTaxExempt = dto.IsTaxExempt;
        entity.MaturityDate = dto.MaturityDate;
        entity.LastUpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddTransactionAsync(int investmentId, int userId, InvestmentTransactionCreateDto dto)
    {
        var investment = await _context.Investments
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.InvestmentID == investmentId && i.UserID == userId);

        if (investment == null) return false;

        var transaction = new InvestmentTransaction
        {
            InvestmentID = investmentId,
            TransactionType = dto.TransactionType,
            Amount = dto.Amount,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TransactionDate = dto.TransactionDate,
            Notes = dto.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Apply ledger adjustments to holding
        switch (dto.TransactionType)
        {
            case InvestmentTransactionType.Buy:
                investment.TotalInvested += dto.Amount;
                if (dto.Quantity.HasValue && dto.Quantity > 0)
                {
                    var oldQty = investment.Quantity ?? 0;
                    var newQty = oldQty + dto.Quantity.Value;
                    investment.Quantity = newQty;

                    if (dto.UnitPrice.HasValue)
                    {
                        var oldCost = oldQty * (investment.PurchasePricePerUnit ?? 0);
                        var newCost = dto.Quantity.Value * dto.UnitPrice.Value;
                        investment.PurchasePricePerUnit = newQty > 0 ? (oldCost + newCost) / newQty : dto.UnitPrice.Value;
                    }
                }
                investment.CurrentValue += dto.Amount;
                break;

            case InvestmentTransactionType.Sell:
                investment.TotalInvested = Math.Max(0, investment.TotalInvested - dto.Amount);
                if (dto.Quantity.HasValue && investment.Quantity.HasValue)
                {
                    investment.Quantity = Math.Max(0, investment.Quantity.Value - dto.Quantity.Value);
                }
                investment.CurrentValue = Math.Max(0, investment.CurrentValue - dto.Amount);
                break;

            case InvestmentTransactionType.Liquidate:
                investment.IsLiquidated = true;
                investment.CurrentValue = 0;
                break;

            case InvestmentTransactionType.StockSplit:
                if (dto.Quantity.HasValue && dto.Quantity.Value > 0)
                {
                    investment.Quantity = (investment.Quantity ?? 0) * dto.Quantity.Value;
                    if (investment.PurchasePricePerUnit.HasValue && investment.PurchasePricePerUnit.Value > 0)
                    {
                        investment.PurchasePricePerUnit /= dto.Quantity.Value;
                    }
                }
                break;

            case InvestmentTransactionType.Dividend:
                // Recorded in ledger for historical cash returns
                break;
        }

        investment.LastUpdatedUtc = DateTime.UtcNow;
        _context.InvestmentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LiquidateAsync(int id, int userId)
    {
        var investment = await _context.Investments.FirstOrDefaultAsync(i => i.InvestmentID == id && i.UserID == userId);
        if (investment == null) return false;

        return await AddTransactionAsync(id, userId, new InvestmentTransactionCreateDto
        {
            TransactionType = InvestmentTransactionType.Liquidate,
            Amount = investment.CurrentValue,
            TransactionDate = DateTime.UtcNow,
            Notes = "Position liquidated"
        });
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var investment = await _context.Investments.FirstOrDefaultAsync(i => i.InvestmentID == id && i.UserID == userId);
        if (investment == null) return false;

        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InvestmentGrowthPointDto>> GetGrowthHistoryAsync(int id, int userId)
    {
        var investment = await _context.Investments
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.InvestmentID == id && i.UserID == userId);

        if (investment == null) return new();

        var points = new List<InvestmentGrowthPointDto>();
        var startDate = DateOnly.FromDateTime(investment.StartDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if ((investment.InvestmentType == InvestmentType.VariableIncome || investment.InvestmentType == InvestmentType.Crypto) && !string.IsNullOrWhiteSpace(investment.Ticker))
        {
            var quotes = await _marketDataService.GetHistoricalQuotesAsync(investment.Ticker, startDate, today);
            var transactions = investment.Transactions.OrderBy(t => t.TransactionDate).ToList();

            decimal runningInvested = 0;
            decimal runningQty = 0;

            foreach (var quote in quotes)
            {
                var quoteDate = quote.Date.ToDateTime(TimeOnly.MinValue);
                var relevantTx = transactions.Where(t => DateOnly.FromDateTime(t.TransactionDate) <= quote.Date).ToList();

                runningInvested = relevantTx.Where(t => t.TransactionType == InvestmentTransactionType.Buy).Sum(t => t.Amount)
                                - relevantTx.Where(t => t.TransactionType == InvestmentTransactionType.Sell).Sum(t => t.Amount);

                runningQty = (relevantTx.Where(t => t.TransactionType == InvestmentTransactionType.Buy).Sum(t => t.Quantity ?? 0))
                           - (relevantTx.Where(t => t.TransactionType == InvestmentTransactionType.Sell).Sum(t => t.Quantity ?? 0));

                var val = runningQty * quote.ClosePrice;
                var pnl = val - runningInvested;
                var pnlPct = runningInvested > 0 ? (pnl / runningInvested) * 100 : 0;

                points.Add(new InvestmentGrowthPointDto
                {
                    Date = quoteDate,
                    InvestedAmount = runningInvested,
                    CurrentValue = val,
                    ProfitLossAmount = pnl,
                    ProfitLossPercentage = Math.Round(pnlPct, 2)
                });
            }
        }
        else
        {
            // Fixed income compound projection
            var totalDays = Math.Max(1, (DateTime.UtcNow - investment.StartDate).Days);
            var annualRate = (investment.AnnualRate ?? 12.0m) / 100m;
            var dailyRate = (decimal)Math.Pow((double)(1 + annualRate), 1.0 / 252.0) - 1.0m;

            for (int d = 0; d <= totalDays; d += Math.Max(1, totalDays / 30))
            {
                var pointDate = investment.StartDate.AddDays(d);
                var businessDays = (decimal)(d * (252.0 / 365.0));
                var compFactor = (decimal)Math.Pow((double)(1 + dailyRate), (double)businessDays);
                var val = investment.TotalInvested * compFactor;
                var pnl = val - investment.TotalInvested;

                points.Add(new InvestmentGrowthPointDto
                {
                    Date = pointDate,
                    InvestedAmount = investment.TotalInvested,
                    CurrentValue = Math.Round(val, 2),
                    ProfitLossAmount = Math.Round(pnl, 2),
                    ProfitLossPercentage = Math.Round((pnl / investment.TotalInvested) * 100, 2)
                });
            }
        }

        return points;
    }

    private async Task RefreshPortfolioValuesAsync(IEnumerable<Investment> investments)
    {
        foreach (var inv in investments)
        {
            await RefreshSingleInvestmentValueAsync(inv);
        }
        await _context.SaveChangesAsync();
    }

    private async Task RefreshSingleInvestmentValueAsync(Investment investment)
    {
        if (investment.IsLiquidated) return;

        if ((investment.InvestmentType == InvestmentType.VariableIncome || investment.InvestmentType == InvestmentType.Crypto) && !string.IsNullOrWhiteSpace(investment.Ticker))
        {
            var livePrice = await _marketDataService.GetCurrentPriceOrRateAsync(investment.Ticker);
            if (livePrice.HasValue)
            {
                investment.CurrentPricePerUnit = livePrice.Value;
                investment.CurrentValue = (investment.Quantity ?? 0) * livePrice.Value;
                investment.LastUpdatedUtc = DateTime.UtcNow;
            }
        }
        else if (investment.InvestmentType == InvestmentType.FixedIncome)
        {
            // Calculate compound interest from StartDate to now
            var days = (DateTime.UtcNow - investment.StartDate).TotalDays;
            if (days > 0 && investment.TotalInvested > 0)
            {
                var annualRate = (investment.AnnualRate ?? 10.0m) / 100m;
                var dailyRate = (decimal)Math.Pow((double)(1 + annualRate), 1.0 / 252.0) - 1.0m;
                var businessDays = (decimal)(days * (252.0 / 365.0));
                var compoundFactor = (decimal)Math.Pow((double)(1 + dailyRate), (double)businessDays);
                investment.CurrentValue = Math.Round(investment.TotalInvested * compoundFactor, 2);
                investment.LastUpdatedUtc = DateTime.UtcNow;
            }
        }
    }

    private static InvestmentReadDto MapToReadDto(Investment i)
    {
        return new InvestmentReadDto
        {
            InvestmentID = i.InvestmentID,
            UserID = i.UserID,
            Name = i.Name,
            Ticker = i.Ticker,
            InvestmentType = i.InvestmentType,
            Currency = i.Currency,
            TotalInvested = i.TotalInvested,
            CurrentValue = i.CurrentValue,
            ProfitLossAmount = i.ProfitLossAmount,
            ProfitLossPercentage = i.ProfitLossPercentage,
            Quantity = i.Quantity,
            PurchasePricePerUnit = i.PurchasePricePerUnit,
            CurrentPricePerUnit = i.CurrentPricePerUnit,
            RateType = i.RateType,
            AnnualRate = i.AnnualRate,
            IsTaxExempt = i.IsTaxExempt,
            StartDate = i.StartDate,
            MaturityDate = i.MaturityDate,
            IsLiquidated = i.IsLiquidated,
            LastUpdatedUtc = i.LastUpdatedUtc,
            Transactions = i.Transactions.Select(t => new InvestmentTransactionReadDto
            {
                InvestmentTransactionID = t.InvestmentTransactionID,
                InvestmentID = t.InvestmentID,
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                Quantity = t.Quantity,
                UnitPrice = t.UnitPrice,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes
            }).OrderByDescending(t => t.TransactionDate).ToList()
        };
    }
}
