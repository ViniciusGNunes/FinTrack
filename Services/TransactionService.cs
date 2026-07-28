using Microsoft.EntityFrameworkCore;

public class TransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    // BROWSE / GET ALL
    public async Task<IEnumerable<TransactionReadDto>> GetAllAsync(int? userId = null)
    {
        var query = _context.Transactions.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserID == userId.Value);
        }

        return await query.Select(t => new TransactionReadDto
        {
            TransactionID = t.TransactionID,
            Name = t.Name,
            TotalAmount = t.TotalAmount,
            Category = t.Category,
            PaymentMethod = t.PaymentMethod,
            IsInstallment = t.IsInstallment,
            TotalInstallments = t.TotalInstallments,
            IsRecurrent = t.IsRecurrent,
            RecurrenceInterval = t.RecurrenceInterval,
            UserID = t.UserID
        }).ToListAsync();
    }

    // READ / GET BY ID
    public async Task<TransactionReadDto?> GetByIdAsync(int id)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        return transaction == null ? null : MapToReadDto(transaction);
    }

    // ADD / CREATE
    public async Task<TransactionReadDto> CreateAsync(TransactionCreateDto dto)
    {
        var entity = new Transaction
        {
            Name = dto.Name,
            TotalAmount = dto.TotalAmount,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Uncathegorized" : dto.Category,
            PaymentMethod = dto.PaymentMethod,
            IsInstallment = dto.IsInstallment,
            TotalInstallments = dto.TotalInstallments,
            IsRecurrent = dto.IsRecurrent,
            RecurrenceInterval = dto.RecurrenceInterval,
            UserID = dto.UserID
        };

        _context.Transactions.Add(entity);
        await _context.SaveChangesAsync();

        return MapToReadDto(entity);
    }

    // EDIT / UPDATE
    public async Task<bool> UpdateAsync(int id, TransactionUpdateDto dto)
    {
        var entity = await _context.Transactions.FindAsync(id);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.TotalAmount = dto.TotalAmount;
        entity.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Uncathegorized" : dto.Category;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.IsInstallment = dto.IsInstallment;
        entity.TotalInstallments = dto.TotalInstallments;
        entity.IsRecurrent = dto.IsRecurrent;
        entity.RecurrenceInterval = dto.RecurrenceInterval;

        await _context.SaveChangesAsync();
        return true;
    }

    // DELETE
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Transactions.FindAsync(id);
        if (entity == null) return false;

        _context.Transactions.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    // Mapping Helper
    private static TransactionReadDto MapToReadDto(Transaction t)
    {
        return new TransactionReadDto
        {
            TransactionID = t.TransactionID,
            Name = t.Name,
            TotalAmount = t.TotalAmount,
            Category = t.Category,
            PaymentMethod = t.PaymentMethod,
            IsInstallment = t.IsInstallment,
            TotalInstallments = t.TotalInstallments,
            IsRecurrent = t.IsRecurrent,
            RecurrenceInterval = t.RecurrenceInterval,
            UserID = t.UserID
        };
    }
}