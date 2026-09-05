using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tounaent_Fixtures.Models
{
    public class PlayerViewModel 
    {
        [Key] // ← This attribute marks it as the primary key

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        public string FatherName { get; set; }


        [Required(ErrorMessage = "Please select a gender")]
        [Display(Name = "Gender")]
        public int GenderId { get; set; }

        public List<SelectListItem> GenderOptions { get; set; } = new();

        public string Gender => GenderId == 1 ? "Male" : GenderId == 2 ? "Female" : "Other";

        [Required]
        public string MobileNo { get; set; }

        [Display(Name = "Upload Photo")]
        public HttpPostedFileBase? PhotoFile { get; set; } // for file input in the view



        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Column(TypeName = "date")]

        public DateTime Dob { get; set; } = DateTime.Today;
        public int Age => Dob != null ? (int)((DateTime.Now - Dob).TotalDays / 365.25) : 0;


        [Required]
        public int CatId { get; set; }
        public string CategoryName
        {
            get
            {
                var age = Age;
                if (age < 7) return "Kids";
                else if (age <= 11) return "SubJunior";
                else if (age <= 14) return "Cadet";
                else if (age <= 17) return "Junior";
                else if (age > 17) return "Senior";
                else return "---Select Category---";
            }
        }


        [Required]
        public int WeightCatId { get; set; }

        [NotMapped]
        public List<SelectListItem> WeightCatOptions { get; set; } = new();


        public int DistictId { get; set; }

        public List<SelectListItem> DistrictOptions { get; set; } = new();

        public string DistrictName { get; set; } = string.Empty;


        [Required]
        public int ClubId { get; set; }

        [NotMapped]
        public List<SelectListItem> ClubOptions { get; set; } = new();
        public string ClubName {  get; set; }

        public string AdharNumb { get; set; }

        [Required]
        public string Address { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public int TournamentId { get; set; }
        public int? weight { get; set; }

        public string? MatchType { get; set; }


    }
    public class PlayerExportViewModel
    {
        public int TrUserId { get; set; }
        public int TrId { get; set; }
        public string? Name { get; set; }
        public string? FatherName { get; set; }
        public string? Gender { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public DateTime? Dob { get; set; }
        public string? CategoryName { get; set; }
        public string? WeighCatName { get; set; }
        public int? WeightCatId { get; set; }      // ADD THIS

        public string? District { get; set; }
        public int? DistrictId { get; set; }        // ADD THIS

        public string? ClubName { get; set; }
        public string? Address { get; set; }

        public string? Remarks { get; set; }

        public int? weight { get; set; }
    }

}
