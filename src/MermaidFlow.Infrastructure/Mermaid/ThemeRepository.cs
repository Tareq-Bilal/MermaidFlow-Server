using MermaidFlow.Application.Common.Interfaces;
using MermaidFlow.Domain.Mermaid;
using MermaidFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MermaidFlow.Infrastructure.Mermaid;

public class ThemeRepository : IThemeRepository
{
    private readonly MermaidFlowDbContext _dbContext;

    public ThemeRepository(MermaidFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Theme>> GetAllAsync()
    {
        return await _dbContext.Themes.ToListAsync();
    }

    public async Task<List<Theme>> GetActiveAsync()
    {
        return await _dbContext.Themes.Where(t => t.IsActive).ToListAsync();
    }

    public async Task<Theme?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Themes.FindAsync(id);
    }

    public async Task<Theme?> GetByNameAsync(string name)
    {
        return await _dbContext.Themes.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task AddAsync(Theme theme)
    {
        await _dbContext.Themes.AddAsync(theme);
    }

    public void Remove(Theme theme)
    {
        _dbContext.Themes.Remove(theme);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _dbContext.Themes.AnyAsync(t => t.Name == name);
    }
}
