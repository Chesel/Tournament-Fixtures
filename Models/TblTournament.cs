using System;
using System;
using System.Collections.Generic;

namespace Tounaent_Fixtures.Models;

public partial class TblTournament
{
    public int TournamentId { get; set; }

    public string TournamentName { get; set; } = null!;

    public string OrganizedBy { get; set; } = null!;

    public string Venue { get; set; } = null!;

    public DateTime? FromDt { get; set; }

    public DateTime? ToDt { get; set; }

    public DateTime? AddedDt { get; set; }

    public string? AddedBy { get; set; }

    public DateTime? ModifyDt { get; set; }

    public string? ModifyBy { get; set; }

    public bool? IsActive { get; set; }

    public int? DistictId { get; set; }

    public string? DistictName { get; set; }
    public byte[]? Logo1 { get; set; } // Nullable if not mandatory

    public byte[]? Logo2 { get; set; } // Nullable if not mandatory

    public string? URL { get; set; }

    public string? MatchType { get; set; }
}
