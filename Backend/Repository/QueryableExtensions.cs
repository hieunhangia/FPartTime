using System.Linq.Dynamic.Core;
using Repository.Constants;

namespace Repository;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IOrderedQueryable<T> DynamicOrderBy(string sortColumn, SortDirection sortDirection,
            params object[] parameters)
        {
            var sortDirectionString = sortDirection == SortDirection.Ascending ? "ASC" : "DESC";
            var orderString = $"{sortColumn} {sortDirectionString}";
            try
            {
                return query.OrderBy(orderString, parameters);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Invalid sort column", e);
            }
        }

        public IQueryable<T> ApplyPaging(int pageIndex, int pageSize) =>
            query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }
}