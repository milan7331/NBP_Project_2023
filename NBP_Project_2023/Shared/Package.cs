namespace NBP_Project_2023.Shared
{
    public class Package
    {
        public int Id { get; set; }
        public string PackageID { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Weight { get; set; }
        public int Price { get; set; }
        public int Sender { get; set; } //UserAccount Id
        public int Receiver { get; set; } //UserAccount Id
        public string Status { get; set; } = string.Empty;
        public DateTime EstimatedArrivalDate { get; set; }
        public int Location { get; set; } // PostId - PostalCode
    }
}
