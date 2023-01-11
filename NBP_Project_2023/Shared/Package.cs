namespace NBP_Project_2023.Shared
{
    public class Package
    {
        public int Id { get; set; }
        public string PackageID { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Weight { get; set; }
        public UserAccount Sender { get; set; }
        public UserAccount Receiver { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Price { get; set; }
    }
}
