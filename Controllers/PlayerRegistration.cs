using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;

using Newtonsoft.Json;
using Tounaent_Fixtures.Models;
using Tounaent_Fixtures;

namespace Tounaent_Fixtures.Controllers
{
    public class PlayerRegistration : Controller
    {
        private readonly AppConfig _config;
        private readonly ApplicationDbContext _context;
        private static readonly object _pdfLock = new object();

        public PlayerRegistration(AppConfig config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<ActionResult> Register(string token)
        {
            // BadRequest(string)/NotFound(string) are Web API-style helpers that don't exist on
            // the classic System.Web.Mvc.Controller base class - HttpStatusCodeResult is the
            // direct MVC5 equivalent and supports a status description the same way.
            if (string.IsNullOrEmpty(token)) return new HttpStatusCodeResult(400, "Missing token");

            int tr_id;
            try
            {
                var decrypted = UrlEncryptionHelper.Decrypt(token);
                tr_id = int.Parse(decrypted);
            }
            catch
            {
                return new HttpStatusCodeResult(400, "Invalid or tampered token.");
            }
            var tournament = await _context.TblTournament
                .Where(t => t.TournamentId == tr_id)
                .FirstOrDefaultAsync();

            if (tournament == null)
            {
                return new HttpStatusCodeResult(404, "Tournament not found.");
            }

            ViewData["TournamentName"] = tournament.TournamentName;
            ViewData["Organization"] = tournament.OrganizedBy;
            ViewData["Venue"] = tournament.Venue;
            ViewData["Logo1"] = tournament.Logo1 != null ? $"data:image/png;base64,{Convert.ToBase64String(tournament.Logo1)}" : null;
            ViewData["Logo2"] = tournament.Logo2 != null ? $"data:image/png;base64,{Convert.ToBase64String(tournament.Logo2)}" : null;
            ViewData["matchtype"] = tournament.MatchType;
            ViewData["TournamentStatus"] = Convert.ToString(tournament.IsActive);

            if (tournament.FromDt == tournament.ToDt)
            {
                ViewData["Date"] = tournament.FromDt?.ToString("dd-MM-yyyy");
            }
            else
            {
                ViewData["Date"] = tournament.FromDt?.ToString("dd-MM-yyyy") + " - " + tournament.ToDt?.ToString("dd-MM-yyyy");
            }

            var model = new PlayerViewModel
            {
                TournamentId = tr_id,
                GenderOptions = await GetGendersAsync(),
                MatchType = tournament.MatchType,

                DistrictName = tournament.DistictName,
                DistictId = (int)tournament.DistictId,
                DistrictOptions = await GetDistrictsAsync(),
                ClubOptions = await GetClubsByDistrictInternal((int)tournament.DistictId)
            };
            if (tournament.MatchType == "State")
            {
                model.DistictId = 0;
            }

            return View(model);
        }

        private async Task<List<SelectListItem>> GetDistrictsAsync()
        {
            return await _context.TblDistricts
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.DistictId.ToString(),
                    Text = d.DistictName
                }).OrderBy(d => d.Text).ToListAsync();
        }

        // Split in two: this private helper is used internally (e.g. from Register() below),
        // and the [HttpGet] action beneath it is what Views/PlayerRegistration/Register.cshtml's
        // AJAX call actually hits. In ASP.NET Core, a public controller method returning a plain
        // List<T> gets auto-serialized to JSON by content negotiation - MVC5 has no such
        // behavior, so calling this directly as an HTTP endpoint would have returned nothing
        // usable to the browser without this split.
        private async Task<List<SelectListItem>> GetClubsByDistrictInternal(int districtId)
        {
            return await _context.TblDistLocalClubs
                .Where(c => c.DistictId == districtId && c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.LocalClubName
                })
                .ToListAsync();
        }

        // This is the actual HTTP endpoint the view's AJAX call hits at
        // /PlayerRegistration/GetClubsByDistrict, so it keeps the original name and now
        // returns Json(...) instead of a raw list.
        [HttpGet]
        public async Task<ActionResult> GetClubsByDistrict(int districtId)
        {
            var clubs = await GetClubsByDistrictInternal(districtId);
            return Json(clubs, JsonRequestBehavior.AllowGet);
        }

        private async Task<List<SelectListItem>> GetGendersAsync()
        {
            return await _context.Gender
                .Select(g => new SelectListItem
                {
                    Value = g.GenderId.ToString(),
                    Text = g.GenderName
                })
                .ToListAsync();
        }

        // Json(object) over HTTP GET is blocked by default in MVC5 (JSON hijacking protection) -
        // ASP.NET Core has no such restriction, so JsonRequestBehavior.AllowGet has to be added
        // explicitly here and in GetWeightCategoriesByCategory below.
        [HttpGet]
        public async Task<ActionResult> GetCategoryByGenderAndAge(int genderId, int age)
        {
            string categoryName;
            if (age < 7) categoryName = "Kids";
            else if (age <= 11) categoryName = "SubJunior";
            else if (age <= 14) categoryName = "Cadet";
            else if (age <= 17) categoryName = "Junior";
            else if (age > 17) categoryName = "Senior";
            else categoryName = "---Select Category---";

            var category = await _context.TblCategory
                .Where(c => c.GenId == genderId && c.CategoryName == categoryName && c.IsActive)
                .FirstOrDefaultAsync();

            if (category != null)
            {
                return Json(new { catId = category.CatId, categoryName = category.CategoryName }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> GetWeightCategoriesByCategory(int catId)
        {
            var weights = await _context.TblWeightCategory
                .Where(w => w.CatId == catId && w.IsActive)
                .Select(w => new SelectListItem
                {
                    Value = w.WeightCatId.ToString(),
                    Text = w.WeightCatName
                })
                .ToListAsync();

            return Json(weights, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Register(PlayerViewModel model, int tr_id)
        {
            string token = UrlEncryptionHelper.Encrypt(model.TournamentId.ToString());

            if (!string.IsNullOrWhiteSpace(model.AdharNumb))
            {
                bool exists = await _context.TblTournamentUserRegs
                    .AnyAsync(p => p.AdharNumb == model.AdharNumb);

                if (exists)
                {
                    ModelState.AddModelError(nameof(model.AdharNumb), "This Aadhaar number is already registered.");
                    ViewBag.AadhaarError = "This Aadhaar number is already registered.";
                    {
                        var tournament1 = await _context.TblTournament
                            .Where(t => t.TournamentId == model.TournamentId)
                            .FirstOrDefaultAsync();
                        ViewData["TournamentName"] = tournament1.TournamentName;
                        ViewData["Organization"] = tournament1.OrganizedBy;
                        ViewData["Venue"] = tournament1.Venue;
                        ViewData["Logo1"] = tournament1.Logo1 != null ? $"data:image/png;base64,{Convert.ToBase64String(tournament1.Logo1)}" : null;
                        ViewData["Logo2"] = tournament1.Logo2 != null ? $"data:image/png;base64,{Convert.ToBase64String(tournament1.Logo2)}" : null;
                        ViewData["TournamentStatus"] = Convert.ToString(tournament1.IsActive);
                        if (tournament1.FromDt == tournament1.ToDt)
                        {
                            ViewData["Date"] = tournament1.FromDt?.ToString("dd-MM-yyyy");
                        }
                        else
                        {
                            ViewData["Date"] = tournament1.FromDt?.ToString("dd-MM-yyyy") + " - " + tournament1.ToDt?.ToString("dd-MM-yyyy");
                        }

                        model = new PlayerViewModel
                        {
                            TournamentId = tr_id,
                            GenderOptions = await GetGendersAsync(),
                            MatchType = tournament1.MatchType,
                            DistrictName = tournament1.DistictName,
                            DistictId = tournament1.DistictId ?? 0,
                            DistrictOptions = await GetDistrictsAsync(),
                            ClubOptions = await GetClubsByDistrictInternal(tournament1.DistictId ?? 0)
                        };

                        return View(model);
                    }
                }
            }

            model.GenderOptions = await GetGendersAsync();
            model.DistrictOptions = await GetDistrictsAsync();

            var tournament = await _context.TblTournament
                .Where(t => t.TournamentId == model.TournamentId).FirstOrDefaultAsync();

            if (tournament != null)
            {
                ViewData["TournamentName"] = tournament.TournamentName;
                ViewData["Organization"] = tournament.OrganizedBy;
                ViewData["Venue"] = tournament.Venue;
                ViewData["TournamentStatus"] = Convert.ToString(tournament.IsActive);

                if (tournament.FromDt == tournament.ToDt)
                {
                    ViewData["Date"] = tournament.FromDt?.ToString("dd-MM-yyyy");
                }
                else
                {
                    ViewData["Date"] = tournament.FromDt?.ToString("dd-MM-yyyy") + " - " + tournament.ToDt?.ToString("dd-MM-yyyy");
                }
            }
            var gender = await _context.Gender
                .Where(c => c.GenderId == model.GenderId)
                .FirstOrDefaultAsync();
            var category = await _context.TblCategory
                .Where(c => c.CatId == model.CatId && c.IsActive)
                .FirstOrDefaultAsync();
            var club = await _context.TblDistLocalClubs
                .Where(c => c.ClubId == model.ClubId).OrderBy(x => x.LocalClubName).FirstOrDefaultAsync();

            var weightcategory = await _context.TblWeightCategory
                .Where(w => w.WeightCatId == model.WeightCatId).FirstOrDefaultAsync();

            var district = await _context.TblDistricts
                .Where(d => d.DistictId == model.DistictId).FirstOrDefaultAsync();
            if (tournament.MatchType == "State")
            {
                // no-op, same as the original: district stays as resolved by model.DistictId above
            }
            else
            {
                district = await _context.TblDistricts
                    .Where(d => d.DistictId == tournament.DistictId).FirstOrDefaultAsync();
            }

            if (category == null)
            {
                ModelState.AddModelError("CatId", "No matching category found.");
                return View(model);
            }

            model.CatId = category.CatId;
            if (tournament.MatchType != "State")
            {
                model.ClubName = club.LocalClubName;
            }

            byte[] photoBytes = null;
            if (model.PhotoFile != null && model.PhotoFile.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    model.PhotoFile.InputStream.CopyTo(memoryStream);
                    photoBytes = memoryStream.ToArray();
                }
            }

            var entity = new TblTournamentUserReg
            {
                TrId = model.TournamentId,
                Name = model.Name,
                FatherName = model.FatherName,
                GenderId = model.GenderId,
                MobileNo = model.MobileNo,
                Email = model.Email,
                Dob = model.Dob.Date,
                CatId = model.CatId,
                WeightCatId = model.WeightCatId,
                DistrictId = district.DistictId,
                ClubName = model.ClubName,
                AdharNumb = model.AdharNumb,
                Address = model.Address,
                Remarks = Convert.ToString("TNTA_SLM_" + +1),
                IsVerified = false,
                IsActive = model.IsActive,
                AddedDt = DateTime.Now,
                AddedBy = User.Identity?.Name ?? "admin",
                CategoryName = category.CategoryName,
                District = district.DistictName,
                Gender = gender.GenderName,
                WeighCatName = weightcategory.WeightCatName,
                Photo = photoBytes,
            };
            if ((model.CategoryName?.ToLower() == "kids" || model.CategoryName?.ToLower() == "peewee") && model.weight != null)
            {
                entity.Weight = model.weight;
            }
            else
            {
                entity.Weight = null;
            }

            _context.TblTournamentUserRegs.Add(entity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "registration_errors.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Save failed for {model.Name} / {model.MobileNo}: {ex}{Environment.NewLine}{Environment.NewLine}");
                }
                catch { }

                ModelState.AddModelError("", "Registration could not be saved - this may be a duplicate entry. Please check your details (especially Aadhaar number) and try again, or leave Aadhaar blank only if you don't have one to enter.");

                model.GenderOptions = await GetGendersAsync();
                model.DistrictOptions = await GetDistrictsAsync();
                if (tournament.MatchType != "State")
                {
                    model.ClubOptions = await GetClubsByDistrictInternal(tournament.DistictId ?? 0);
                }
                return View(model);
            }

            var inserted = await _context.TblTournamentUserRegs
                .Where(x => x.Name == entity.Name && x.MobileNo == entity.MobileNo)
                .OrderByDescending(x => x.TrUserId)
                .FirstOrDefaultAsync();
            if (inserted != null)
            {
                entity.Remarks = Convert.ToString("TNTA_" + tournament.DistictName + "_" + inserted.TrUserId);
                await _context.SaveChangesAsync();
            }

            string successMessage = "Player registered successfully!";
            try
            {
                await SendEmailAsync(model.Email, model, tournament.TournamentName, entity.Remarks, weightcategory.WeightCatName);
                successMessage += " Confirmation email sent.";
            }
            catch (Exception ex)
            {
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "email_errors.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to send registration email to {model.Email}: {ex}{Environment.NewLine}{Environment.NewLine}");
                }
                catch { }
                successMessage += " (Registration saved, but the confirmation email could not be sent - contact the organizer if you don't receive it.)";
            }

            TempData["Success"] = successMessage;

            return RedirectToAction("Register", new { token = token });
        }

        // NOTE: this method is dead code (only ever called from a commented-out line), returns
        // a string despite the "PDF" name, and sends an email as a side effect using a hardcoded
        // gmail credential + hardcoded recipient. Ported as-is syntactically so nothing is lost,
        // but it's worth deleting outright or rewriting once you're back in the code - see
        // MIGRATION_NOTES.md for the credential-rotation note this ties into.
        private string GenerateIdCardPdf(PlayerViewModel model, byte[] photoBytes, byte[] Logo1, byte[] Logo2,
            string argWeightCat, string argGender)
        {
            string base64Image = photoBytes != null
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(photoBytes)}"
            : "";
            string base64ImageLogo1 = Logo1 != null
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(Logo1)}"
                : "";
            string base64ImageLogo2 = Logo2 != null
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(Logo2)}"
                : "";
            var htmlContent = $@"
	   <html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>{ViewData["TournamentName"]}</title>
  <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"" rel=""stylesheet"">
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js""></script>
  <style>
	table {{
	  width: 100%;
	  border-collapse: collapse;
	}}
	table td {{
	  border: none;
	  padding: 6px;
	  vertical-align: top;
	}}
	.photo-box {{
	  border: 1px solid black;
	  width: 100px;
	  height: 120px;
	  text-align: center;
	  line-height: 120px;
	  margin-left: auto;
	}}
	.form-control {{
	  border: none;
	  border-bottom: solid 1px black;
	  border-radius: 0 !important;
	}}
	textarea {{
	  resize: none;
	}}
	input[type=""checkbox""] {{
  width: 18px;
  height: 18px;
  margin-right: 6px;
  vertical-align: middle;
  accent-color: #0d6efd;
}}
label.checkbox-label {{
  margin-right: 15px;
  display: inline-flex;
  align-items: center;
  font-weight: normal;
}}

  </style>
</head>
<body>
<div class=""container my-4"" id=""form-content"">
  <table>
	<tr>
	  <td style=""width: 25%; text-align: center;"">
 {(string.IsNullOrEmpty(base64ImageLogo1) ? "" : $"<img class='photo' src='{base64ImageLogo1}' alt='Photo' height='100px' widht='120px' />")}</td>
	  <td style=""width: 50%; text-align: center;"">
		<h4>{ViewData["TournamentName"]}</h4>
		<p>Date: {Convert.ToString(ViewData["Date"])}</p>
		<p> {ViewData["Organization"]} </p>
	  </td>
	  <td style=""width: 25%; text-align: center;"">
 {(string.IsNullOrEmpty(base64ImageLogo2) ? "" : $"<img class='photo' src='{base64ImageLogo2}' alt='Photo' height='100px' widht='120px' />")}</td>
	</tr>
  </table>

  <p style=""border-top: 2px solid black; border-bottom: 2px solid black; padding: 10px; text-align:center;"">
	<strong>Organised by:</strong> {ViewData["Organization"]}<br>
	<strong>Promoted by:</strong> SALEM DISTRICT AMATEUR TAEKWONDO ASSOCIATION (R)<br>
	<strong>Under the Auspicious of:</strong> TAMILNADU TAEKWONDO ASSOCIATION (R)
  </p>

  <h5 class=""text-center mt-4"">INDIVIDUAL ENTRY FORM</h5>

  <table class=""mb-3"">
	<tr>
	  <td style=""width: 75%""> <strong>GENDER - {model.Gender} </strong>
		
<br>
		<strong>CATEGORY - {model.CategoryName} </strong>
<br>
	  </td>
	  <td><div class=""photo-box""> {(string.IsNullOrEmpty(base64Image) ? "" : $"<img class='photo' src='{base64Image}' alt='Photo' height='100px' widht='120px' />")}</div></td>
	</tr>
  </table>

  <table>
	<tr>
	  <td>Weight Category</td>
	  <td><input type=""text"" class=""form-control"" name=""weight_category"" value=""{Convert.ToString(argWeightCat)}""></td>
	  <td>Weight</td>
	  <td><input type=""text"" class=""form-control"" name=""weight""></td>
	</tr>
	<tr>
	  <td>Name (in capital letter)</td>
	  <td colspan=""3""><input type=""text"" class=""form-control"" value=""{Convert.ToString(model.Name)}""></td>
	</tr>
	<tr>
	  <td>Date of Birth</td>
	  <td><input type=""date"" class=""form-control"" name=""dob"" value={Convert.ToString(model.Dob)}></td>
	  <td>Age</td>
	  <td><input type=""text"" class=""form-control"" name=""age""></td>
	</tr>
	<tr>
	  <td>Parent / Guardian Name</td>
	  <td colspan=""3""><input type=""text"" class=""form-control"" value=""{Convert.ToString(model.FatherName)}""></td>
	</tr>
	<tr>
	  <td>Name of the School</td>
	  <td colspan=""3""><input type=""text"" class=""form-control"" name=""school""></td>
	</tr>
	<tr>
	  <td>Name of the Club</td>
	  <td colspan=""3""><input type=""text"" class=""form-control"" name=""club"" value=""{Convert.ToString(model.ClubName)}"" ></td>
	</tr>
	<tr>
	  <td>Address</td>
	  <td colspan=""3""><textarea class=""form-control"" name=""address"">  {Convert.ToString(model.Address)} </textarea></td>
	</tr>
	<tr>
	  <td>Present Belt Grade</td>
	  <td><input type=""text"" class=""form-control"" name=""belt_grade""></td>
	  <td>TFI.I.C. No.</td>
	  <td><input type=""text"" class=""form-control"" name=""tfi_lic_no""></td>
	</tr>
  </table>

  <p class=""fst-italic mt-3"">Copy of Corporation / Municipal / School Date of Birth Certificate should be enclosed compulsorily. (Original Birth Certificate should be produced at the time of Weigh-in).</p>

  <h6><strong>DECLARATION</strong></h6>
  <p>I, the undersigned do hereby solemnly affirm, declare and confirm for myself, my heirs, executors & administrators that I indemnify the Promoters / Organiser / Sponsors & its Members, Officials, Participants etc., holding myself personally responsible for all damages, injuries or accidents, claims, demands etc., waiving all prerogative rights, whatsoever related to the above set forth event.</p>

  <table>
	<tr>
	  <td>Signature of Parent / Guardian:</td>
	  <td><input type=""text"" class=""form-control"" name=""guardian_signature""></td>
	  <td>Signature of Participant:</td>
	  <td><input type=""text"" class=""form-control"" name=""participant_signature""></td>
	</tr>
  </table>

  <p class=""text-center mt-5"">
	Signature of President / Secretary<br>
	District Club / Organization / Head of the Institution with Seal
  </p>
</div>
</body>
</html>";

            string htmlBody = htmlContent;

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("tournamentfixtures@gmail.com");
            mail.To.Add("gopinathbajaj@gmail.com");
            mail.Subject = "Your Invoice Page";
            mail.Body = htmlBody;
            mail.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential("tournamentfixtures@gmail.com", _config["EmailSettings:FromPassword"]);
            smtp.EnableSsl = true;

            smtp.Send(mail);

            return "Siuccess";
        }

        private async Task SendEmailAsync(string toEmail, PlayerViewModel model, string tournamentName, string Remarks, string WeighCatName)
        {
            var category = await _context.TblCategory
                .Where(c => c.CatId == model.CatId && c.IsActive)
                .FirstOrDefaultAsync();
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"]);
            var fromEmail = _config["EmailSettings:FromEmail"];
            var fromPassword = _config["EmailSettings:FromPassword"];

            var smtpClient = new SmtpClient(smtpServer)
            {
                Port = port,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = $"Registration Successful - Online Entry " + model.Name,
                Body = $"Thank you for registering for {tournamentName} <br /><br />. Your Online Entry is successfully created.<br /><br />" +
                $"Register Name : {model.Name} <br /> " +
                $"Father Name   : {model.FatherName} <br />" +
                $"Gender        : {model.Gender} <br />" +
                $"Date Of Birth : {model.Dob.ToString("dd-MM-yyyy")} <br />" +
                $"Weigt Category : {category.CategoryName} - {WeighCatName} <br />" +
                $"Local Club Name : {model.ClubName}<br />" +
                $"Email ID        : {model.Email}<br />" +
                $"Mobile No       : {model.MobileNo}<br />" +
                $"Registration Reference : {Remarks}<br />",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
