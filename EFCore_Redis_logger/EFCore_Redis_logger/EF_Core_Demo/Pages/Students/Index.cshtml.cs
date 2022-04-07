#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EF_Core_Demo.Data;
using EF_Core_Demo.Models;
using Microsoft.Extensions.Configuration;

namespace EF_Core_Demo.Pages.Students
{
    public class IndexModel : PageModel
    {
        private readonly EF_Core_Demo.Data.SchoolContext _context;

        private readonly IConfiguration Configuration;

        public IndexModel(SchoolContext context, IConfiguration configuration)
        {
            _context = context;
            Configuration = configuration;
        }

        public string NameSort { get; set; }
        public string DateSort { get; set; }
        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }

        //public IList<Student> Students { get; set; }

        //public async Task OnGetAsync(string sortOrder, string searchString)
        //{
        //    CurrentFilter = searchString;
        //    // using System;
        //    NameSort = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        //    DateSort = sortOrder == "Date" ? "date_desc" : "Date";

        //    IQueryable<Student> studentsIQ = from s in _context.Students
        //                                     select s;

        //    if (!String.IsNullOrEmpty(searchString))
        //    {
        //        studentsIQ = studentsIQ.Where(s => s.LastName.Contains(searchString)
        //                               || s.FirstMidName.Contains(searchString));
        //    }
        //    switch (sortOrder)
        //    {
        //        case "name_desc":
        //            studentsIQ = studentsIQ.OrderByDescending(s => s.LastName);
        //            break;
        //        case "Date":
        //            studentsIQ = studentsIQ.OrderBy(s => s.EnrollmentDate);
        //            break;
        //        case "date_desc":
        //            studentsIQ = studentsIQ.OrderByDescending(s => s.EnrollmentDate);
        //            break;
        //        default:
        //            studentsIQ = studentsIQ.OrderBy(s => s.LastName);
        //            break;
        //    }

        //    Students = await studentsIQ.AsNoTracking().ToListAsync();
        //}

        public PaginatedList<Student> Students { get; set; }

        public async Task OnGetAsync(string sortOrder,
            string currentFilter, string searchString, int? pageIndex)
        {
            CurrentSort = sortOrder;
            NameSort = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            DateSort = sortOrder == "Date" ? "date_desc" : "Date";
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            CurrentFilter = searchString;

            IQueryable<Student> studentsIQ = from s in _context.Students
                                             select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                studentsIQ = studentsIQ.Where(s => s.LastName.Contains(searchString)
                                       || s.FirstMidName.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "name_desc":
                    studentsIQ = studentsIQ.OrderByDescending(s => s.LastName);
                    break;
                case "Date":
                    studentsIQ = studentsIQ.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    studentsIQ = studentsIQ.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    studentsIQ = studentsIQ.OrderBy(s => s.LastName);
                    break;
            }

            var pageSize = Configuration.GetValue("PageSize", 4);
            Students = await PaginatedList<Student>.CreateAsync(
                studentsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
        }
    }
}
