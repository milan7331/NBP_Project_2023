using System.ComponentModel.DataAnnotations;

namespace NBP_Project_2023.Client.FormModels
{
    public class PackageCreationForm
    {
        [Required(ErrorMessage = "Polje ne može ostati prazno"), EmailAddress(ErrorMessage = "Uneta adresa e-pošte nije u validnom formatu")]
        public string ReceiverEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), StringLength(50)]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), StringLength(150)]
        public string Description { get; set;} = string.Empty;

        [Required(ErrorMessage = "Polje ne može ostati prazno"), Range(0.1, 30.0, ErrorMessage = "Težina pošiljke mora biti u rasponu od 0.1kg do 30kg")]
        public float Weight { get; set; } = 0.1f;

        [Required(ErrorMessage = "Polje za otkupninu ne može ostati prazno"), Range(0.0, 1000000.0, ErrorMessage = "Vrednost pošiljke ne može biti negativan broj")]
        public float Price { get; set; } = 0.0f;

    }
}