using System.ComponentModel.DataAnnotations;

namespace NBP_Project_2023.Client.FormModels
{
    public class LoginForm
    {
        [Required(ErrorMessage = "Polje ne može ostati prazno")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno")]
        public string LastName { get; set; } = string.Empty;
    }
}
