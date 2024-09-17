namespace Dmm.Models
{
    public class Token
    {
        public int TokenId { get; set; }
        public Guid Value { get; set; }
        public DateTime CreateAt { get; set; }

        public ICollection<Match> Matches { get; set; }
        public ICollection<ManualMatches> ManualMatches { get; set; }
    }
}
