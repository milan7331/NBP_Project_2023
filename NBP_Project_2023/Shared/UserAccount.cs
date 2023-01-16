namespace NBP_Project_2023.Shared
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public int StreetNumber { get; set; }
        public string City { get; set; } = string.Empty;
        public int PostalCode { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
