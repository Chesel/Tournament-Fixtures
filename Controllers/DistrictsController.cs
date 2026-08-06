using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.Mvc;
using Tounaent_Fixtures.Models;

namespace Tounaent_Fixtures.Controllers
{
    public class DistrictsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DistrictsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<List<SelectListItem>> GetStateAsync()
        {
            return await _context.TblDistricts
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.DistictId.ToString(),
                    Text = d.DistictName
                }).ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult> Register()
        {
            var model = new TblDistrict
            {
                StateOptions = await GetStateAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Register(TblDistrict model)
        {
            var districts = new TblDistrict
            {
                DistictName = model.DistictName,
                StateId = model.StateId,
                AddedDt = DateTime.Now,
                AddedBy = User.Identity?.Name ?? "admin",
                IsActive = model.IsActive
            };

            _context.TblDistricts.Add(districts);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "District Added successfully!";

            return View(new TblDistrict());
        }
    }
}
