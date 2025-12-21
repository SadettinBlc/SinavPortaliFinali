using Microsoft.EntityFrameworkCore;
using SinavPortaliFinal.Models;
using System.Linq.Expressions;

namespace SinavPortaliFinal.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _table;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _table = _context.Set<T>();
        }

        public List<T> GetAll(string? p = null)
        {
            if (!string.IsNullOrEmpty(p))
            {
                return _table.Include(p).ToList();
            }
            return _table.ToList();
        }

        public List<T> GetListByFilter(Expression<Func<T, bool>> filter, string? p = null)
        {
            var query = _table.AsQueryable();
            if (!string.IsNullOrEmpty(p))
            {
                query = query.Include(p);
            }
            return query.Where(filter).ToList();
        }

        public T GetById(int id)
        {
            return _table.Find(id);
        }

        public void Add(T entity)
        {
            _table.Add(entity);
            _context.SaveChanges();
        }

        public void Update(T entity)
        {
            _table.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var existing = _table.Find(id);
            if (existing != null)
            {
                _table.Remove(existing);
                _context.SaveChanges();
            }
        }
    }
}