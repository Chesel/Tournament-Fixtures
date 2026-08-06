using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Tounaent_Fixtures.Models;

public partial class TblDistrict
{
    //[Key]
    //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // <-- this is important
    public int DistictId { get; set; }

    public string DistictName { get; set; } = null!;

    public bool IsActive { get; set; }

    public int StateId { get; set; }

    public DateTime? AddedDt { get; set; }

    public string? AddedBy { get; set; }

    public DateTime? ModifyDt { get; set; }

    public string? ModifyBy { get; set; }
    [NotMapped]
    public List<SelectListItem> StateOptions { get; set; } = new();
}
