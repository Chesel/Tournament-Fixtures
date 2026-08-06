using System;
namespace Tounaent_Fixtures.Models
{
    public partial class TblCategory
    {
        public int CatId { get; set; }

        public string CategoryName { get; set; } = null!;

        public int GenId { get; set; }

        public bool IsActive { get; set; }

        public DateTime? AddedDt { get; set; }

        public string? AddedBy { get; set; }

        public DateTime? ModifyDt { get; set; }

        public string? ModifyBy { get; set; }
    }

}
