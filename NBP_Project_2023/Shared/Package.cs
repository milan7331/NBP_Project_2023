namespace NBP_Project_2023.Shared
{
    public enum PackageState
    {
        WaitingForPickup = 0,
        InTransit = 1,
        AtPostOffice = 2,
        Delivered = 3
    }

    public class Package
    {
        public int Id { get; set; }
        
        public string PackageID { get; set; } = string.Empty;
        
        public string Content { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public float Weight { get; set; }
        
        public float Price { get; set; }
        
        public string SenderEmail { get; set; } = string.Empty;
        
        public string ReceiverEmail { get; set; } = string.Empty; 
        
        public DateTime EstimatedArrivalDate { get; set; }

        public string PackageStatus { get; set; } = PackageState.WaitingForPickup.ToString();
    }
}
