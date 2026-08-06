using System;
using System;
using System.Collections.Generic;

namespace Tounaent_Fixtures.Models;

public partial class Registration
{
    public int RegistrationId { get; set; }

    public string Name { get; set; } = null!;

    public int GenderId { get; set; }

    public DateTime Dob { get; set; }

    public string Aadhaar { get; set; } = null!;

    public string? Height { get; set; }

    public string? Weight { get; set; }

    public string? Address { get; set; }

    public string? PinCode { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public byte[]? Photo { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Gender Gender { get; set; } = null!;
}
