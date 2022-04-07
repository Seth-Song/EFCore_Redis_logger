using System.Linq.Expressions;

namespace EFCore_Redis_logger.Models
{
    public class WebPageInfo
    {

        public class TablePageParameter
        {
            public int PageIndex { get; set; }

            public int PageSize { get; set; }

            public string SortKey { get; set; } = string.Empty;

            public bool IsAscending { get; set; }

            public string SearchContent { get; set; }

            public Dictionary<string, List<string>> Filters { get; set; } = new Dictionary<string, List<string>>();
        }

        public class TableQueryParameter<T, TOrder>
        {
            public Pager Pager { get; set; } = new Pager();
            public Expression<Func<T, bool>> Filter { get; set; }
            public Sorter<T, TOrder> Sorter { get; set; } = new Sorter<T, TOrder>();
        }

        public class TableInfo<T>
        {
            public List<T> Items { get; set; }
            public int PageCount { get; set; }
            public int TotalItemsCount { get; set; }
        }

        public class Pager
        {
            public int Index { get; set; }
            public int Size { get; set; }
        }

        public class Sorter<T, TResult>
        {
            public Expression<Func<T, TResult>> SortBy { get; set; }
            public Expression<Func<T, TResult>> ThenSortBy { get; set; }
            public bool IsAscending { get; set; }
        }

        public class DataCountInfo<T>
        {
            public List<T> Items { get; set; }
            public int TotalItemsCount { get; set; }
        }
    }
}
