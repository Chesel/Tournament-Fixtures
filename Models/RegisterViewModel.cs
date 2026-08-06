using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Name { get; set; }

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, System.ComponentModel.DataAnnotations.Compare("Password"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Please select a gender")]
    [Display(Name = "Gender")]
    public int GenderId { get; set; }

    public List<SelectListItem> GenderOptions { get; set; } = new();

    public string Gender => GenderId == 1 ? "Male" : GenderId == 2 ? "Female" : "Other";

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; } = DateTime.Today;

    public string Category
    {
        get
        {

            var age = Age;
            if (age < 7) return "Kids";
            if (age <= 11) return "SubJunior";
            if (age <= 14) return "Cadet";
            if (age <= 17) return "Junior";
             return "Senior";
        }
    }

    public int Age => DateOfBirth != default
        ? (int)((DateTime.Today - DateOfBirth).TotalDays / 365.25)
        : 0;
    
    [Required(ErrorMessage = "Aadhaar number is required")]
    public string Aadhaar { get; set; }
    public string Height { get; set; }
    public string Weight { get; set; }
    public string Address { get; set; }
    public string PinCode { get; set; }
    public string Phone { get; set; }

    public HttpPostedFileBase Photo { get; set; }
}
