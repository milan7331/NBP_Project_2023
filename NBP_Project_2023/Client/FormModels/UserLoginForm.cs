using System.ComponentModel.DataAnnotations;

namespace NBP_Project_2023.Client.FormModels
{
    public class UserLoginForm
    {
        [Required(ErrorMessage = "Polje ne može ostati prazno"),
        EmailAddress(ErrorMessage = "Adresa e pošte mora biti u formatu 'adresa@mail.com'"),
        StringLength(256, ErrorMessage = "Adresa e pošte je preduga")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"),
        StringLength(32, MinimumLength = 8, ErrorMessage = "Lozinka mora biti između 8 i 32 karaktera")]
        public string Password { get; set; } = string.Empty;

    }
}
