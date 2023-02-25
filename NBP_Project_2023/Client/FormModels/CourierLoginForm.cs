using System.ComponentModel.DataAnnotations;

namespace NBP_Project_2023.Client.FormModels
{
    public class CourierLoginForm
    {
        [Required(ErrorMessage = "Polje ne može ostati prazno"),
        StringLength(20, ErrorMessage = "Ime ne može biti duže od 20 karaktera")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"),
        StringLength(30, ErrorMessage = "Prezime ne može biti duže od 30 karaktera")]
        public string LastName { get; set; } = string.Empty;
    }
}
