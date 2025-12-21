using System.Linq.Expressions;

namespace SinavPortaliFinal.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        List<T> GetAll(string? p = null); // İlişkili tablo getirme özelliğiyle
        List<T> GetListByFilter(Expression<Func<T, bool>> filter, string? p = null); // Filtreleme
        T GetById(int id);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }
}