using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PciDss.Entities;

namespace PciDss.DataAccess;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? filter = null);
    IQueryable<T> Query();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db;
    public Repository(AppDbContext db) => Db = db;

    public Task<T?> GetByIdAsync(int id) => Db.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    public Task<List<T>> ListAsync(Expression<Func<T, bool>>? filter = null)
        => (filter is null ? Db.Set<T>() : Db.Set<T>().Where(filter)).ToListAsync();
    public IQueryable<T> Query() => Db.Set<T>().AsQueryable();
    public async Task AddAsync(T entity) => await Db.Set<T>().AddAsync(entity);
    public void Update(T entity) => Db.Set<T>().Update(entity);
    public void Remove(T entity) => Db.Set<T>().Remove(entity);
    public Task<int> SaveChangesAsync() => Db.SaveChangesAsync();
}
