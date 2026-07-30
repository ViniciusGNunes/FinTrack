using Microsoft.EntityFrameworkCore;

public class CategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    // GET ALL AVAILABLE FOR USER (System Defaults + User Custom)
    public async Task<IEnumerable<CategoryReadDto>> GetForUserAsync(int userId)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.UserID == null || c.UserID == userId)
            .OrderBy(c => c.UserID.HasValue) // System defaults first
            .ThenBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToReadDto);
    }

    // GET BY ID
    public async Task<CategoryReadDto?> GetByIdAsync(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        return category == null ? null : MapToReadDto(category);
    }

    // CREATE
    public async Task<CategoryReadDto> CreateAsync(CategoryCreateDto dto)
    {
        var entity = new Category
        {
            Name = dto.Name,
            Icon = dto.Icon,
            ColorHex = dto.ColorHex,
            UserID = dto.UserID
        };

        _context.Categories.Add(entity);
        await _context.SaveChangesAsync();

        return MapToReadDto(entity);
    }

    // UPDATE
    public async Task<bool> UpdateAsync(int id, CategoryUpdateDto dto, int? userId = null)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        // Protection: Prevent non-admin users from editing global system categories
        if (category.UserID == null && userId.HasValue)
        {
            throw new UnauthorizedAccessException("System default categories cannot be modified.");
        }

        category.Name = dto.Name;
        category.Icon = dto.Icon;
        category.ColorHex = dto.ColorHex;

        await _context.SaveChangesAsync();
        return true;
    }

    // DELETE
    public async Task<bool> DeleteAsync(int id, int? userId = null)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        // Protection: Prevent deleting system defaults
        if (category.UserID == null && userId.HasValue)
        {
            throw new UnauthorizedAccessException("System default categories cannot be deleted.");
        }

        // Safety check: Don't delete a category if active transactions use it
        bool isInUse = await _context.Transactions.AnyAsync(t => t.CategoryID == id);
        if (isInUse)
        {
            throw new InvalidOperationException("Cannot delete category because active transactions are assigned to it.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    // Helper: Seed Default System Categories on initial deployment
    public async Task SeedDefaultCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync(c => c.UserID == null)) return;

        var defaults = new List<Category>
        {
            new() { Name = "Food & Drinks", Icon = "utensils", ColorHex = "#10B981", UserID = null },
            new() { Name = "Shopping", Icon = "shopping-bag", ColorHex = "#EC4899", UserID = null },
            new() { Name = "Housing & Rent", Icon = "home", ColorHex = "#3B82F6", UserID = null },
            new() { Name = "Subscriptions & SaaS", Icon = "cloud", ColorHex = "#6366F1", UserID = null },
            new() { Name = "Transportation", Icon = "car", ColorHex = "#F59E0B", UserID = null },
            new() { Name = "Entertainment", Icon = "film", ColorHex = "#8B5CF6", UserID = null },
            new() { Name = "Uncategorized", Icon = "tag", ColorHex = "#6B7280", UserID = null }
        };

        _context.Categories.AddRange(defaults);
        await _context.SaveChangesAsync();
    }

    // Mapping Helper
    private static CategoryReadDto MapToReadDto(Category c) => new()
    {
        CategoryID = c.CategoryID,
        Name = c.Name,
        Icon = c.Icon,
        ColorHex = c.ColorHex,
        UserID = c.UserID
    };
}