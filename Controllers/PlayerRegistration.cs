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
    public class PlayerRegistrationController : Controller
    {
        private readonly AppConfig _config;
        private readonly ApplicationDbContext _context;
        private static readonly object _pdfLock = new object();

        public PlayerRegistrationController(AppConfig config, ApplicationDbContext context)
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
            var clubsJson = clubs.Select(c => new { value = c.Value, text = c.Text });
            return Json(clubsJson, JsonRequestBehavior.AllowGet);
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
                .Select(w => new
                {
                    value = w.WeightCatId.ToString(),
                    text = w.WeightCatName
                })
                .ToListAsync();

            return Json(weights, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Register(PlayerViewModel model, int tr_id = 0)
        {
            string token = UrlEncryptionHelper.Encrypt(model.TournamentId.ToString());

            if (!string.IsNullOrWhiteSpace(model.AdharNumb))
            {
                bool exists = await _context.TblTournamentUserRegs
    .AnyAsync(p => p.AdharNumb == model.AdharNumb && p.TrId == model.TournamentId);

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
                            TournamentId = model.TournamentId,
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
                // Covers DB-level rejections that aren't caught by the app-level checks above -
                // most likely a unique constraint on AdharNumb (SQL Server only allows one row
                // with an empty/NULL value in a uniquely-constrained column, so a second blank
                // Aadhaar submission collides here even though the app-level check above
                // deliberately skips validation for blank Aadhaar numbers).
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

            byte[] pdfBytes = null;
            try
            {
                //pdfBytes = GenerateIdCardPdfBytes(model, photoBytes, tournament.Logo1, tournament.Logo2,
                //    weightcategory.WeightCatName, gender.GenderName);
            }
            catch (Exception ex)
            {
                // PDF generation failing shouldn't block registration or the email - just send
                // the email without an attachment and log what went wrong.
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "pdf_errors.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PDF generation failed for {model.Name}: {ex}{Environment.NewLine}{Environment.NewLine}");
                }
                catch { }
            }

            try
            {
                await SendEmailAsync(model.Email, model, tournament.TournamentName, entity.Remarks, weightcategory.WeightCatName, pdfBytes);
                successMessage += pdfBytes != null ? " Confirmation email with ID card sent." : " Confirmation email sent.";
            }
            catch (Exception ex)
            {
                // Registration itself already succeeded above - a broken email shouldn't undo that.
                // Log the real error so SMTP problems are actually visible instead of silently
                // disappearing or crashing the whole request.
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "email_errors.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to send registration email to {model.Email}: {ex}{Environment.NewLine}{Environment.NewLine}");
                }
                catch
                {
                    // If even logging fails, don't let that take down the request either.
                }
                successMessage += " (Registration saved, but the confirmation email could not be sent - contact the organizer if you don't receive it.)";
            }

            TempData["Success"] = successMessage;

            return RedirectToAction("Register", new { token = token });
        }

        // Renders the ID card as an actual PDF via IronPdf. Previously this method didn't use
        // IronPdf at all despite the name - it built HTML and emailed it directly, and was never
        // even called from anywhere. Now it's wired into the real registration flow below.
        // Renders the ID card as an actual PDF using iTextSharp-LGPL - free, pure managed C#,
        // no native dependencies, so nothing that can silently fail on shared hosting the way
        // native-DLL-based libraries (DinkToPdf, QuestPDF) can. Builds the layout directly via
        // iTextSharp's own table API rather than parsing HTML/CSS, for predictable output.
        private byte[] GenerateIdCardPdfBytes(PlayerViewModel model, byte[] photoBytes, byte[] Logo1, byte[] Logo2,
            string argWeightCat, string argGender)
        {
            using (var ms = new MemoryStream())
            {
                var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 30, 30, 30, 30);
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms);
                document.Open();

                var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 16);
                var normalFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10);
                var boldFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 10);
                var smallFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8);

                // --- Header: logo | tournament name/date | logo ---
                var headerTable = new iTextSharp.text.pdf.PdfPTable(3) { WidthPercentage = 100 };
                headerTable.SetWidths(new float[] { 1f, 2f, 1f });

                headerTable.AddCell(BuildLogoCell(Logo1));

                var titleCell = new iTextSharp.text.pdf.PdfPCell
                {
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
                };
                titleCell.AddElement(new iTextSharp.text.Paragraph(Convert.ToString(ViewData["TournamentName"]), titleFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                titleCell.AddElement(new iTextSharp.text.Paragraph("Date: " + Convert.ToString(ViewData["Date"]), normalFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                titleCell.AddElement(new iTextSharp.text.Paragraph(Convert.ToString(ViewData["Organization"]), normalFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                headerTable.AddCell(titleCell);

                headerTable.AddCell(BuildLogoCell(Logo2));
                document.Add(headerTable);

                document.Add(new iTextSharp.text.Paragraph(" "));

                // --- Organised by / promoted by block ---
                var orgPara = new iTextSharp.text.Paragraph();
                orgPara.Add(new iTextSharp.text.Chunk("Organised by: ", boldFont));
                orgPara.Add(new iTextSharp.text.Chunk(Convert.ToString(ViewData["Organization"]) + "\n", smallFont));
                orgPara.Add(new iTextSharp.text.Chunk("Promoted by: ", boldFont));
                orgPara.Add(new iTextSharp.text.Chunk("SALEM DISTRICT AMATEUR TAEKWONDO ASSOCIATION (R)\n", smallFont));
                orgPara.Add(new iTextSharp.text.Chunk("Under the Auspicious of: ", boldFont));
                orgPara.Add(new iTextSharp.text.Chunk("TAMILNADU TAEKWONDO ASSOCIATION (R)", smallFont));
                orgPara.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                document.Add(orgPara);

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph("INDIVIDUAL ENTRY FORM", titleFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                document.Add(new iTextSharp.text.Paragraph(" "));

                // --- Gender/Category + photo ---
                var genderCatTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
                genderCatTable.SetWidths(new float[] { 3f, 1f });

                var genderCatCell = new iTextSharp.text.pdf.PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER };
                genderCatCell.AddElement(new iTextSharp.text.Paragraph($"GENDER - {model.Gender}", boldFont));
                genderCatCell.AddElement(new iTextSharp.text.Paragraph($"CATEGORY - {model.CategoryName}", boldFont));
                genderCatTable.AddCell(genderCatCell);
                genderCatTable.AddCell(BuildLogoCell(photoBytes));
                document.Add(genderCatTable);

                document.Add(new iTextSharp.text.Paragraph(" "));

                // --- Details table ---
                var detailsTable = new iTextSharp.text.pdf.PdfPTable(4) { WidthPercentage = 100 };
                detailsTable.SetWidths(new float[] { 1.5f, 2.5f, 1.5f, 2.5f });

                AddDetailRow(detailsTable, "Weight Category", Convert.ToString(argWeightCat), "Weight", "", normalFont, boldFont);
                AddDetailRow(detailsTable, "Name", Convert.ToString(model.Name), "DOB", model.Dob.ToString("dd-MM-yyyy"), normalFont, boldFont);
                AddDetailRow(detailsTable, "Parent/Guardian", Convert.ToString(model.FatherName), "Age", "", normalFont, boldFont);
                AddDetailRow(detailsTable, "Club", Convert.ToString(model.ClubName), "Belt Grade", "", normalFont, boldFont);
                AddDetailRow(detailsTable, "Address", Convert.ToString(model.Address), "TFI.I.C. No.", "", normalFont, boldFont);

                document.Add(detailsTable);

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph(
                    "Copy of Corporation / Municipal / School Date of Birth Certificate should be enclosed compulsorily. " +
                    "(Original Birth Certificate should be produced at the time of Weigh-in).",
                    smallFont));

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph("DECLARATION", boldFont));
                document.Add(new iTextSharp.text.Paragraph(
                    "I, the undersigned do hereby solemnly affirm, declare and confirm for myself, my heirs, executors & " +
                    "administrators that I indemnify the Promoters / Organiser / Sponsors & its Members, Officials, Participants " +
                    "etc., holding myself personally responsible for all damages, injuries or accidents, claims, demands etc., " +
                    "waiving all prerogative rights, whatsoever related to the above set forth event.",
                    smallFont));

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph("Signature of Parent / Guardian: _______________________     Signature of Participant: _______________________", smallFont));

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph("Signature of President / Secretary", smallFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                document.Add(new iTextSharp.text.Paragraph("District Club / Organization / Head of the Institution with Seal", smallFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });

                document.Close();
                return ms.ToArray();
            }
        }

        private iTextSharp.text.pdf.PdfPCell BuildLogoCell(byte[] imageBytes)
        {
            var cell = new iTextSharp.text.pdf.PdfPCell
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER,
                VerticalAlignment = iTextSharp.text.Element.ALIGN_MIDDLE,
                FixedHeight = 90f
            };
            if (imageBytes != null && imageBytes.Length > 0)
            {
                try
                {
                    var img = iTextSharp.text.Image.GetInstance(imageBytes);
                    img.ScaleToFit(90f, 90f);
                    cell.AddElement(img);
                }
                catch
                {
                    // If the image bytes are somehow invalid, just leave the cell blank rather
                    // than fail the whole PDF.
                }
            }
            return cell;
        }

        private void AddDetailRow(iTextSharp.text.pdf.PdfPTable table, string label1, string value1, string label2, string value2,
            iTextSharp.text.Font normalFont, iTextSharp.text.Font boldFont)
        {
            table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(label1, boldFont)) { Padding = 4 });
            table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(value1 ?? "", normalFont)) { Padding = 4 });
            table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(label2, boldFont)) { Padding = 4 });
            table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(value2 ?? "", normalFont)) { Padding = 4 });
        }

        private async Task SendEmailAsync(string toEmail, PlayerViewModel model, string tournamentName, string Remarks, string WeighCatName, byte[] pdfBytes = null)
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

            if (pdfBytes != null)
            {
                var attachment = new Attachment(new MemoryStream(pdfBytes), "ID_Card.pdf", "application/pdf");
                mailMessage.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}