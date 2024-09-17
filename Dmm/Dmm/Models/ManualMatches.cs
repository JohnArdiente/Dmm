using System.ComponentModel.DataAnnotations;

namespace Dmm.Models
{
    public class ManualMatches
    {
        public int Id { get; set; }
        public int EntryId1 { get; set; }
        public string EntryName1 { get; set; }
        public decimal Weight1 { get; set; }
        public int EntryId2 { get; set; }
        public string EntryName2 { get; set; }
        public decimal Weight2 { get; set; }
        public int TokenId { get; set; }
        public Token Token { get; set; }
    }
}
