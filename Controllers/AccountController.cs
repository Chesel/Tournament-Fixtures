using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;

using Tounaent_Fixtures.Models;
using Tounaent_Fixtures;

public class AccountController : Controller
{
    private readonly AppConfig _config;
    private readonly ApplicationDbContext _context;

    public AccountController(AppConfig config, ApplicationDbContext context)
    {
        _config = config;
        _context = context;
    }

    public async Task<ActionResult> Register()
    {
        var model = new RegisterViewModel
        {
            GenderOptions = await GetGendersAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
        model.GenderOptions = await GetGendersAsync();

        if (!ModelState.IsValid)
            return View(model);

        byte[] photoBytes = null;
        if (model.Photo != null && model.Photo.ContentLength > 0)
        {
            using (var ms = new MemoryStream())
            {
                model.Photo.InputStream.CopyTo(ms);
                photoBytes = ms.ToArray();
            }
        }

        var registration = new Registration
        {
            Name = model.Name,
            GenderId = model.GenderId,
            Dob = model.DateOfBirth,
            Aadhaar = model.Aadhaar,
            Height = model.Height,
            Weight = model.Weight,
            Address = model.Address,
            PinCode = model.PinCode,
            Phone = model.Phone,
            Email = model.Email,
            Photo = photoBytes,
            CreatedDate = DateTime.Now
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // byte[] idCardPdf = GenerateIdCardPdf(model, photoBytes);
        // await SendEmailAsync(model.Email, idCardPdf);

        TempData["Message"] = "Registration successful! A confirmation email with your ID card has been sent.";
        return RedirectToAction("Register");
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

    private async Task SendEmailAsync(string toEmail, byte[] pdfBytes)
    {
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
            Subject = "Registration Successful - ID Card",
            Body = "Thank you for registering! Your ID card is attached.",
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        using (var stream = new MemoryStream(pdfBytes))
        {
            stream.Position = 0;
            var attachment = new Attachment(stream, "ID_Card.pdf", "application/pdf");
            mailMessage.Attachments.Add(attachment);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
