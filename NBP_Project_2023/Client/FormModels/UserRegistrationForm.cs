using System.ComponentModel.DataAnnotations;

namespace NBP_Project_2023.Client.FormModels
{
    public class UserRegistrationForm
    {
        [Required(ErrorMessage = "Polje za ime ne može ostati prazno"), StringLength(20)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje za prezime ne može ostati prazno"), StringLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), StringLength(32, MinimumLength = 8, ErrorMessage = "Lozinka mora biti između 8 i 32 karaktera")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), StringLength(20)]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), Range(1, 1000)]
        public int StreetNumber { get; set; }

        //DODATI ZA GRAD DA BUDE JEDNA OD POSTOJEĆIH POŠTI I NIŠTA DRUGO
        [Required(ErrorMessage = "Polje ne može ostati prazno"), StringLength(30)]
        public string City { get; set; } = string.Empty;

        //ISTO I OVDE
        [Required(ErrorMessage = "Polje ne može ostati prazno"), Range(10000, 100000, ErrorMessage = "Poštanski broj je nepostojeći")]
        public int PostalCode { get; set; }

        [Required(ErrorMessage = "Polje ne može ostati prazno"), Phone(ErrorMessage = "Morate uneti telefon ispravno")]
        public string PhoneNumber { get; set; } = string.Empty;

    }
}
