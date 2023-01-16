using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBP_Project_2023.Shared
{
    public class Post
    {
        // TREBA dodati svašta nakon dogovora!!
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int PostalCode { get; set; }
        public List<Courier> Workers { get; set; } = new List<Courier>();
        public List<Package> Packages { get; set; } = new List<Package>();
    }
}
