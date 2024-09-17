using System.ComponentModel.DataAnnotations;

namespace Dmm.Models
{
    public class EntryData
    {
        public int Id { get; set; }

        [Required]
        public decimal Weight { get; set; }

        public string WingBan { get; set; }

        public int EntryId { get; set; }

        public Entry Entry { get; set; }
    }
}
