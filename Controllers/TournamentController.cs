using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.Mvc;
using Tounaent_Fixtures.Models;

namespace Tounaent_Fixtures.Controllers
{
    public class TournamentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TournamentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<List<SelectListItem>> GetDistrictsAsync()
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
            var model = new TournamentViewModel
            {
                DistrictOptions = await GetDistrictsAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Register(TournamentViewModel model)
        {
            var district = await _context.TblDistricts
                .Where(d => d.DistictId == model.DistictId).FirstOrDefaultAsync();

            byte[] logo1bytes = null;
            if (model.Logo1 != null && model.Logo1.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    model.Logo1.InputStream.CopyTo(memoryStream);
                    logo1bytes = memoryStream.ToArray();
                }
            }
            byte[] logo2bytes = null;
            if (model.Logo2 != null && model.Logo2.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    model.Logo2.InputStream.CopyTo(memoryStream);
                    logo2bytes = memoryStream.ToArray();
                }
            }
            var tournament = new TblTournament
            {
                TournamentName = model.TournamentName,
                OrganizedBy = model.OrganizedBy,
                Venue = model.Venue,
                FromDt = model.From_dt,
                ToDt = model.To_dt,
                AddedDt = DateTime.Now,
                AddedBy = User.Identity?.Name ?? "admin",
                IsActive = model.IsActive,
                DistictId = model.DistictId,
                DistictName = district.DistictName,
                Logo1 = logo1bytes,
                Logo2 = logo2bytes
            };

            // 1. Save tournament to get generated TournamentId
            _context.TblTournament.Add(tournament);
            await _context.SaveChangesAsync();

            // 2. Generate token and update URL.
            // Request.Scheme / Request.Host (ASP.NET Core HttpRequest) -> Request.Url.Scheme /
            // Request.Url.Authority (classic System.Web HttpRequest).
            var token = UrlEncryptionHelper.Encrypt(tournament.TournamentId.ToString());
            tournament.URL = $"{Request.Url.Scheme}://{Request.Url.Authority}/PlayerRegistration/Register?token={token}";

            // 3. Save updated URL. EF6 tracks the entity automatically since it was already
            // Added above - no explicit Update() call needed (that was an EF Core convenience).
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tournament successfully registered!";

            return View(new TournamentViewModel());
        }
    }
}
