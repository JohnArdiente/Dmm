namespace Dmm.Models
{
    public class EntryUpdateModel
    {
        public int EntryId { get; set; }
        public string EntryName { get; set; }
        public string OwnerName { get; set; }
        public decimal Bet { get; set; }
        public List<EntryDataModel> EntryData { get; set; }
    }
}
