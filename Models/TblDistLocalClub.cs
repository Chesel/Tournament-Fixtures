using System;
using System;
using System.Collections.Generic;

namespace Tounaent_Fixtures.Models;

public partial class TblDistLocalClub
{
    public int ClubId { get; set; }

    public string LocalClubName { get; set; } = null!;

    public int DistictId { get; set; }

    public bool IsActive { get; set; }

    public int StateId { get; set; }

    public DateTime? AddedDt { get; set; }

    public string? AddedBy { get; set; }

    public DateTime? ModifyDt { get; set; }

    public string? ModifyBy { get; set; }
}
