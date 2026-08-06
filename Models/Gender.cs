using System;
using System;
using System.Collections.Generic;

namespace Tounaent_Fixtures.Models;

public partial class Gender
{
    public int GenderId { get; set; }

    public string GenderName { get; set; } = null!;

    public DateTime? AddedDt { get; set; }

    public string? AddedBy { get; set; }

    public DateTime? ModifyDt { get; set; }

    public string? ModifyBy { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
