﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace NBP_Project_2023.Server.Controllers
{
    //test komentar
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IDriver _driver;
        
        public PackageController(IDriver driver)
        {
            _driver = driver;
        }
    }
}
// IZ POŠTEMOŽE SAMO U POŠTU U DRUGOM GRADU
// KURIR INAČE MOZEŽ DA URADI TAKE PACKAGE I LEAVE PACKAGE??