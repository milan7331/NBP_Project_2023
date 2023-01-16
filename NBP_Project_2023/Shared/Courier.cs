﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBP_Project_2023.Shared
{
    public class Courier
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public enum Status { Active, Inactive }
        public int WorksAt { get; set; } //PostId drugim rečima
        public List<Package> Packages { get; set; } = new List<Package>();
    }
}
