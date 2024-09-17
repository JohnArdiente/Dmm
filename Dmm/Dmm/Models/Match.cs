namespace Dmm.Models
{
    public class Match
    {
        public int Id { get; set; }
        public string MeronEntryName { get; set; }
        public string MeronOwnerName { get; set; }
        public decimal MeronWeight { get; set; }
        public string MeronWingBan { get; set; }
        public string WalaEntryName { get; set; }
        public string WalaOwnerName { get; set; }
        public decimal WalaWeight { get; set; }
        public string WalaWingBan { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int TokenId { get; set; }
        public Token Token { get; set; }
    }
}
