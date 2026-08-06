using System;
using System;
using System.Collections.Generic;

namespace Tounaent_Fixtures.Models;

public partial class TblTournamentUserReg
{
    public int TrUserId { get; set; }

    public int UserId { get; set; }

    public int TrId { get; set; }

    public string Name { get; set; } = null!;

    public string FatherName { get; set; } = null!;

    public int GenderId { get; set; }

    public string Gender { get; set; } = null!;

    public string MobileNo { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime Dob { get; set; }

    public int CatId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int WeightCatId { get; set; }

    public string WeighCatName { get; set; } = null!;

    public int DistrictId { get; set; }

    public string District { get; set; } = null!;

    public string ClubName { get; set; } = null!;

    public string AdharNumb { get; set; }

    public string Address { get; set; } = null!;

    public bool IsVerified { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; }

    public DateTime? AddedDt { get; set; }

    public string? AddedBy { get; set; }

    public DateTime? ModifyDt { get; set; }

    public string? ModifyBy { get; set; }

    public byte[]? Photo { get; set; }

    public int? Weight { get; set; }
}
