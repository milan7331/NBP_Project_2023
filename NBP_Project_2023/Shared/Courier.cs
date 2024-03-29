﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBP_Project_2023.Shared
{
    public enum CourierState
    {
        Away = 0,
        Available = 1,
        Working = 2
    }

    public class Courier
    {
        
        public int Id { get; set; }
        
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        public string CourierStatus { get; set; } = CourierState.Away.ToString(); 
        
        public int WorksAt { get; set; } //PostalCode
        
        public List<string> Packages { get; set; } = new List<string>(); //PackageID
    }
}
