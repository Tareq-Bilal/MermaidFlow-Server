using MermaidFlow.Domain.Mermaid;

namespace MermaidFlow.Application.Common.Interfaces;

public interface IThemeRepository
{
    Task<List<Theme>> GetAllAsync();
    Task<List<Theme>> GetActiveAsync();
    Task<Theme?> GetByIdAsync(Guid id);
    Task<Theme?> GetByNameAsync(string name);
    Task AddAsync(Theme theme);
    void Remove(Theme theme);
    Task<bool> ExistsByNameAsync(string name);
}
