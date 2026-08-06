using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Web.Mvc;
using Tounaent_Fixtures.Models;
using System;

namespace Tounaent_Fixtures.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ILogger<HomeController> was injected but never actually used anywhere in this
        // controller, so it's dropped rather than wired up through Autofac for nothing.
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> TournamentManagement()
        {
            var tournaments = await _context.TblTournament.ToListAsync();
            return View(tournaments);
        }

        public async Task<ActionResult> DistrictManagement()
        {
            var districts = await _context.TblDistricts.ToListAsync();
            return View(districts);
        }

        public async Task<ActionResult> PlayerManagement(int? tournamentId, int? districtid)
        {
            ViewBag.Tournaments = await _context.TblTournament
                .Select(t => new SelectListItem
                {
                    Value = t.TournamentId.ToString(),
                    Text = t.TournamentName
                })
                .ToListAsync();
            ViewBag.Districts = await _context.TblDistricts
                .Select(t => new SelectListItem
                {
                    Value = t.DistictId.ToString(),
                    Text = t.DistictName
                })
                .ToListAsync();
            ViewBag.SelectedTournament = tournamentId;
            ViewBag.SelectedDistrict = districtid;

            var playersQuery = _context.TblTournamentUserRegs.Where(p => p.IsActive == true).AsQueryable();

            if (tournamentId.HasValue)
                playersQuery = playersQuery.Where(p => p.TrId == tournamentId.Value);

            if (districtid.HasValue)
                playersQuery = playersQuery.Where(p => p.DistrictId == districtid.Value);

            var players = await playersQuery
                .Select(p => new PlayerExportViewModel
                {
                    TrUserId = p.TrUserId,
                    TrId = p.TrId,
                    Name = p.Name,
                    FatherName = p.FatherName,
                    Gender = p.Gender,
                    MobileNo = p.MobileNo,
                    Email = p.Email,
                    Dob = p.Dob,
                    CategoryName = p.CategoryName,
                    WeighCatName = p.WeighCatName,
                    District = p.District,
                    ClubName = p.ClubName,
                    Address = p.Address,
                    Remarks = p.Remarks
                })
                .ToListAsync();

            return View(players);
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> GenderManagement()
        {
            var genders = await _context.Gender.ToListAsync();
            return View(genders);
        }

        public async Task<ActionResult> EditGender(int id)
        {
            var gender = await _context.Gender.FindAsync(id);
            if (gender == null)
            {
                return HttpNotFound();
            }
            return View(gender);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateGender(int genderId, string genderName)
        {
            var gender = await _context.Gender.FindAsync(genderId);
            if (gender == null)
            {
                return HttpNotFound();
            }

            gender.GenderName = genderName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Gender.Any(e => e.GenderId == genderId))
                {
                    return HttpNotFound();
                }
                else
                {
                    throw;
                }
            }

            return new HttpStatusCodeResult(200);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditGender(int id, Gender gender)
        {
            if (id != gender.GenderId)
            {
                return new HttpStatusCodeResult(400);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // context.Update(gender) (EF Core convenience) -> EF6 equivalent
                    _context.Entry(gender).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Gender.Any(e => e.GenderId == id))
                    {
                        return HttpNotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GenderManagement));
            }
            return View(gender);
        }

        public async Task<ActionResult> ExportTournamentsToExcel()
        {
            var tournaments = await _context.TblTournament.ToListAsync();

            var columns = new Dictionary<string, Func<TblTournament, object>>
            {
                { "Tournament Name", t => t.TournamentName },
                { "Organizer", t => t.OrganizedBy },
                { "Venue", t => t.Venue },
                { "From Date", t => t.FromDt?.ToString("dd-MM-yyyy") },
                { "To Date", t => t.ToDt?.ToString("dd-MM-yyyy") },
                { "URL", t => t.URL }
            };

            byte[] excelBytes = ExcelExportHelper.ExportToExcel(tournaments, columns, "Tournaments");

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Tournaments.xlsx");
        }

        public async Task<ActionResult> ExportDistrictsToExcel()
        {
            var districts = await _context.TblDistricts.ToListAsync();

            var columns = new Dictionary<string, Func<TblDistrict, object>>
            {
                { "District Name", d => d.DistictName },
                { "Is Active", d => d.IsActive ? "Yes" : "No" }
            };

            byte[] excelBytes = ExcelExportHelper.ExportToExcel(districts, columns, "Districts");

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Districts.xlsx");
        }

        public async Task<ActionResult> ExportGenderToExcel()
        {
            var gender = await _context.Gender.ToListAsync();

            var columns = new Dictionary<string, Func<Gender, object>>
            {
                { "Gender Name", d => d.GenderName },
                { "Gender Id", d => d.GenderId },
                { "Is Active", d => d.IsActive }
            };

            byte[] excelBytes = ExcelExportHelper.ExportToExcel(gender, columns, "Gender");

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Gender.xlsx");
        }

        public async Task<ActionResult> ExportPlayerToExcel(int? tournamentId, int? districtId)
        {
            var playersQuery = _context.TblTournamentUserRegs
                .Where(p => p.IsActive == true)
                .AsQueryable();

            if (tournamentId.HasValue)
                playersQuery = playersQuery.Where(p => p.TrId == tournamentId.Value);

            if (districtId.HasValue)
                playersQuery = playersQuery.Where(p => p.DistrictId == districtId.Value);

            var players = await playersQuery
                    .Select(p => new PlayerExportViewModel
                    {
                        TrUserId = p.TrUserId,
                        TrId = p.TrId,
                        Name = p.Name,
                        FatherName = p.FatherName,
                        Gender = p.Gender,
                        MobileNo = p.MobileNo,
                        Email = p.Email,
                        Dob = p.Dob,
                        CategoryName = p.CategoryName,
                        WeighCatName = p.WeighCatName,
                        District = p.District,
                        ClubName = p.ClubName,
                        Remarks = p.Remarks
                    })
                    .ToListAsync();
            var columns = new Dictionary<string, Func<PlayerExportViewModel, object>>
            {
                { "User ID", d => d.TrUserId },
                { "Tournament Id", d => d.TrId },
                { "Name", d => d.Name },
                { "Father Name", d => d.FatherName },
                { "Gender", d => d.Gender },
                { "Mobile Number", d => d.MobileNo },
                { "Email", d => d.Email },
                { "DOB", d => d.Dob },
                { "Category Name", d => d.CategoryName },
                { "Weight Category Name", d => d.WeighCatName },
                { "District", d => d.District },
                { "Club Name", d => d.ClubName },
                { "Remarks", d => d.Remarks }
            };

            byte[] excelBytes = ExcelExportHelper.ExportToExcel(players, columns, "Players");

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Players.xlsx");
        }

        public ActionResult Privacy()
        {
            return View();
        }

        [OutputCache(Duration = 0, NoStore = true, VaryByParam = "none")]
        public ActionResult Error()
        {
            // HttpContext.TraceIdentifier (ASP.NET Core) has no classic equivalent -
            // Activity.Current?.Id covers distributed-trace scenarios; a fresh Guid is the
            // fallback either way, so behavior is effectively unchanged.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? Guid.NewGuid().ToString() });
        }
    }
}
