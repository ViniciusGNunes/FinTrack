using Microsoft.EntityFrameworkCore;

public class ReceivableService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReceivableService> _logger;

    public ReceivableService(AppDbContext context, ILogger<ReceivableService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReceivableSummaryDto> GetSummaryAsync(int userId)
    {
        var receivables = await _context.Receivables
            .Include(r => r.Items)
            .Where(r => r.UserID == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        var dtos = receivables.Select(MapToReadDto).ToList();

        var totalShared = dtos.Sum(r => r.TotalAmount);
        var totalOwed = dtos.Sum(r => r.TotalOwedByOthers);
        var totalCollected = dtos.Sum(r => r.TotalCollected);
        var totalPending = Math.Max(0, totalOwed - totalCollected);
        var overallCollectionPct = totalOwed > 0 ? Math.Round((totalCollected / totalOwed) * 100, 2) : 100m;

        // Group by debtor / person name across all shared expenditures
        var allItems = dtos.SelectMany(r => r.Items).ToList();
        var debtorsGrouped = allItems
            .GroupBy(i => i.PersonName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var owed = g.Sum(x => x.AmountOwed);
                var paid = g.Where(x => x.IsPaid).Sum(x => x.AmountPaid > 0 ? x.AmountPaid : x.AmountOwed);
                var pending = Math.Max(0, owed - paid);
                var lastPayment = g.Where(x => x.IsPaid && x.PaidDate.HasValue).Max(x => x.PaidDate);

                return new DebtorSummaryDto
                {
                    PersonName = g.Key,
                    TotalOwed = owed,
                    TotalPaid = paid,
                    TotalPending = pending,
                    ActiveSharedBillsCount = g.Count(x => !x.IsPaid),
                    SettledSharedBillsCount = g.Count(x => x.IsPaid),
                    LastPaymentDate = lastPayment
                };
            })
            .OrderByDescending(d => d.TotalPending)
            .ThenByDescending(d => d.TotalOwed)
            .ToList();

        return new ReceivableSummaryDto
        {
            TotalPendingReceivables = totalPending,
            TotalCollectedReceivables = totalCollected,
            TotalSharedExpenditures = totalShared,
            OverallCollectionPercentage = overallCollectionPct,
            ActiveBillsCount = dtos.Count(r => !r.IsSettled),
            SettledBillsCount = dtos.Count(r => r.IsSettled),
            Receivables = dtos,
            Debtors = debtorsGrouped
        };
    }

    public async Task<ReceivableReadDto?> GetByIdAsync(int id, int userId)
    {
        var receivable = await _context.Receivables
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ReceivableID == id && r.UserID == userId);

        if (receivable == null) return null;
        return MapToReadDto(receivable);
    }

    public async Task<ReceivableReadDto> CreateAsync(ReceivableCreateDto dto)
    {
        var myShare = dto.MyShareAmount ?? 0;

        var entity = new Receivable
        {
            UserID = dto.UserID,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            TotalAmount = dto.TotalAmount,
            MyShareAmount = myShare,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim(),
            DueDate = dto.DueDate,
            IsSettled = false,
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        if (dto.Items != null && dto.Items.Any())
        {
            foreach (var itemDto in dto.Items)
            {
                entity.Items.Add(new ReceivableItem
                {
                    PersonName = itemDto.PersonName.Trim(),
                    AmountOwed = itemDto.AmountOwed,
                    AmountPaid = itemDto.AmountPaid ?? (itemDto.IsPaid ? itemDto.AmountOwed : 0),
                    IsPaid = itemDto.IsPaid,
                    PaidDate = itemDto.IsPaid ? (itemDto.PaidDate ?? DateTime.UtcNow) : null,
                    Notes = itemDto.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            // Check if all items are already marked as paid
            entity.IsSettled = entity.Items.All(i => i.IsPaid);
        }

        _context.Receivables.Add(entity);
        await _context.SaveChangesAsync();

        return MapToReadDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, int userId, ReceivableUpdateDto dto)
    {
        var receivable = await _context.Receivables
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ReceivableID == id && r.UserID == userId);

        if (receivable == null) return false;

        receivable.Title = dto.Title.Trim();
        receivable.Description = dto.Description?.Trim();
        receivable.TotalAmount = dto.TotalAmount;
        receivable.MyShareAmount = dto.MyShareAmount ?? 0;
        receivable.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim();
        receivable.DueDate = dto.DueDate;
        receivable.LastUpdatedUtc = DateTime.UtcNow;

        if (dto.Items != null)
        {
            // Sync items: Remove items not in dto
            var incomingIds = dto.Items.Where(i => i.ReceivableItemID.HasValue).Select(i => i.ReceivableItemID!.Value).ToHashSet();
            var toRemove = receivable.Items.Where(i => !incomingIds.Contains(i.ReceivableItemID)).ToList();
            foreach (var item in toRemove)
            {
                _context.ReceivableItems.Remove(item);
            }

            // Update existing or add new
            foreach (var itemDto in dto.Items)
            {
                if (itemDto.ReceivableItemID.HasValue)
                {
                    var existing = receivable.Items.FirstOrDefault(i => i.ReceivableItemID == itemDto.ReceivableItemID.Value);
                    if (existing != null)
                    {
                        existing.PersonName = itemDto.PersonName.Trim();
                        existing.AmountOwed = itemDto.AmountOwed;
                        existing.AmountPaid = itemDto.AmountPaid ?? (itemDto.IsPaid ? itemDto.AmountOwed : 0);
                        existing.IsPaid = itemDto.IsPaid;
                        existing.PaidDate = itemDto.IsPaid ? (itemDto.PaidDate ?? existing.PaidDate ?? DateTime.UtcNow) : null;
                        existing.Notes = itemDto.Notes?.Trim();
                    }
                }
                else
                {
                    receivable.Items.Add(new ReceivableItem
                    {
                        PersonName = itemDto.PersonName.Trim(),
                        AmountOwed = itemDto.AmountOwed,
                        AmountPaid = itemDto.AmountPaid ?? (itemDto.IsPaid ? itemDto.AmountOwed : 0),
                        IsPaid = itemDto.IsPaid,
                        PaidDate = itemDto.IsPaid ? (itemDto.PaidDate ?? DateTime.UtcNow) : null,
                        Notes = itemDto.Notes?.Trim(),
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        receivable.IsSettled = receivable.Items.Any() && receivable.Items.All(i => i.IsPaid);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleItemPaidStatusAsync(int itemId, int userId, bool? isPaid = null, decimal? amountPaid = null, string? notes = null)
    {
        var item = await _context.ReceivableItems
            .Include(i => i.Receivable)
            .FirstOrDefaultAsync(i => i.ReceivableItemID == itemId && i.Receivable!.UserID == userId);

        if (item == null) return false;

        var newPaidStatus = isPaid ?? !item.IsPaid;
        item.IsPaid = newPaidStatus;
        item.PaidDate = newPaidStatus ? DateTime.UtcNow : null;
        if (newPaidStatus)
        {
            item.AmountPaid = amountPaid ?? item.AmountOwed;
        }
        else
        {
            item.AmountPaid = 0;
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            item.Notes = notes.Trim();
        }

        if (item.Receivable != null)
        {
            item.Receivable.LastUpdatedUtc = DateTime.UtcNow;
            // Re-check if all items in the receivable are now settled
            var allItems = await _context.ReceivableItems
                .Where(x => x.ReceivableID == item.ReceivableID)
                .ToListAsync();

            item.Receivable.IsSettled = allItems.All(x => x.ReceivableItemID == item.ReceivableItemID ? newPaidStatus : x.IsPaid);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var receivable = await _context.Receivables
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ReceivableID == id && r.UserID == userId);

        if (receivable == null) return false;

        _context.Receivables.Remove(receivable);
        await _context.SaveChangesAsync();
        return true;
    }

    private static ReceivableReadDto MapToReadDto(Receivable r)
    {
        return new ReceivableReadDto
        {
            ReceivableID = r.ReceivableID,
            UserID = r.UserID,
            Title = r.Title,
            Description = r.Description,
            TotalAmount = r.TotalAmount,
            MyShareAmount = r.MyShareAmount,
            TotalOwedByOthers = r.TotalOwedByOthers,
            TotalCollected = r.TotalCollected,
            TotalPending = r.TotalPending,
            ProgressPercentage = r.ProgressPercentage,
            Currency = r.Currency,
            DueDate = r.DueDate,
            IsSettled = r.IsSettled,
            CreatedAtUtc = r.CreatedAtUtc,
            LastUpdatedUtc = r.LastUpdatedUtc,
            Items = r.Items
                .OrderBy(i => i.ReceivableItemID)
                .Select(i => new ReceivableItemReadDto
                {
                    ReceivableItemID = i.ReceivableItemID,
                    ReceivableID = i.ReceivableID,
                    PersonName = i.PersonName,
                    AmountOwed = i.AmountOwed,
                    AmountPaid = i.AmountPaid,
                    IsPaid = i.IsPaid,
                    PaidDate = i.PaidDate,
                    Notes = i.Notes,
                    CreatedAtUtc = i.CreatedAtUtc
                }).ToList()
        };
    }
}
