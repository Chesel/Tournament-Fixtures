using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tounaent_Fixtures.Models
{
    [Table("Tbl_Weight_Category")]
    public class TblWeightCategory
    {
        public int WeightCatId { get; set; }
        public string WeightCatName { get; set; } = null!;
        public int CatId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? AddedDt { get; set; }
        public string? AddedBy { get; set; }
        public DateTime? ModifyDt { get; set; }
        public string? ModifyBy { get; set; }
    }

}
