using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBP_Project_2023.Shared
{
    public class Post
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int PostalCode { get; set; }
        public double PostXCoordinate { get; set; }
        public double PostYCoordinate { get; set; }
        public bool IsMainPostOffice { get; set; } = false;
        public List<Courier> Workers { get; set; } = new List<Courier>();
        public List<Package> Packages { get; set; } = new List<Package>();
    }
}
