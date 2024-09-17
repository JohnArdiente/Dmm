using System.ComponentModel.DataAnnotations;

namespace Dmm.Models
{
    public class Entry
    {
        public int EntryId { get; set; }
        [Required]
        public string EntryName { get; set; }
        [Required]
        public string OwnerName { get; set; }

        public decimal Bet { get; set; }

        public List<EntryData> EntryData { get; set; }
    }
}
