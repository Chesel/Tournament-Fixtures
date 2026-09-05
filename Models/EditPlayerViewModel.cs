using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Tounaent_Fixtures.Models
{
    public class EditPlayerViewModel
    {
        public int TrUserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please select a weight category")]
        public int WeightCatId { get; set; }

        [Required(ErrorMessage = "Please select a district")]
        public int DistrictId { get; set; }

        public List<SelectListItem> WeightCatOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DistrictOptions { get; set; } = new List<SelectListItem>();
    }
}