using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBP_Project_2023.Shared
{
    public class PostOffice
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int PostalCode { get; set; }
        public double PostX { get; set; }
        public double PostY { get; set; }
        public bool IsMainPostOffice { get; set; } = false;
        public List<int> Workers { get; set; } = new List<int>(); // Courier Id
        public List<string> Packages { get; set; } = new List<string>(); //PackageID
    }
}
