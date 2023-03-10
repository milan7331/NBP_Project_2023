namespace NBP_Project_2023.Shared
{
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
    }
}
