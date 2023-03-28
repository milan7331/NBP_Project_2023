using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using NBP_Project_2023.Shared;

namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IDriver _driver;
        
        public PackageController(IDriver driver)
        {
            _driver = driver;
        }

        // kreira pošiljku i poziva metodu za njenu dodelu odgovarajućem kuriru
        [Route("CreatePackage")]
        [HttpPost]
        public async Task<IActionResult> CreatePackage(Package package)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                CREATE (p:Package)
                SET p.PackageID = $PackageID, 
                p.Content = $Content,
                p.Description = $Description,
                p.Weight = $Weight,
                p.Price = $Price,
                p.SenderEmail = $SenderEmail,
                p.ReceiverEmail = $ReceiverEmail,
                p.EstimatedArrivalDate = $EstimatedArrivalDate,
                p.PackageStatus = $PackageStatus
            ";
            var parameters = new
            {
                package.PackageID,
                package.Content,
                package.Description,
                package.Weight,
                package.Price,
                package.SenderEmail,
                package.ReceiverEmail,
                package.EstimatedArrivalDate,
                package.PackageStatus
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result == 1)
            {
                await LinkPackageToSender(package.PackageID);
                await AssignPackageToCourier(package.PackageID);
                return Ok("Package created, linked and assigned successfuly!");
            }
            
            return BadRequest("Error creating package!");
        }

        [Route("LinkPackageToSender/{packageId}")]
        [HttpPost]
        public async Task<IActionResult> LinkPackageToSender(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result = false;

            string query = @"
                MATCH (package:Package)
                WHERE package.PackageID = $packageId
                WITH package
                MATCH (user:UserAccount)
                WHERE user.Email = package.SenderEmail
                MERGE (user)-[:Has]->(package)
            ";

            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated == 1;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result)
            {
                return Ok("Package linked to sender");
            }

            return BadRequest("Error linking package to sender!");
        }

        // novokreirana pošiljka se dodeljuje na listu taskova odabranom kuriru
        // proverava se da li postoji slobodan kurir bez posla i dodeljuje se njemu
        // u suprotnom se dodaje kuriru koji ima najmanje trenutnih taskova (pošiljki)
        // i na kraju ako ni jedan od uslova nije ispunjen dodaje se task kuriru koji trenutno ne radi
        // jer paketi moraju negde da se povežu a moguće je da vikendom niko ne radi recimo
        [Route("AssignPackageToCourier/{packageId}")]
        [HttpPost]
        public async Task<IActionResult> AssignPackageToCourier(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result = false;

            string availableQuery = @"
                MATCH (user:UserAccount)-[:Has]->(package:Package)
                WHERE package.PackageID = $packageId
                WITH user
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Available' AND courier.WorksAt = user.PostalCode
                WITH courier, rand() AS r
                ORDER BY r
                LIMIT 1
                MATCH (package:Package {PackageID: $packageId})
                MERGE (courier)-[:CollectionList]->(package)
            ";
            string workingQuery = @"
                MATCH (user:UserAccount)-[:Has]->(package:Package)
                WHERE package.PackageID = $packageId
                WITH user
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Working' AND courier.WorksAt = user.PostalCode
                OPTIONAL MATCH (courier)-[list:CollectionList|DeliveryList]-()
                WITH courier, COUNT(list) AS listCount
                ORDER BY listCount
                LIMIT 1
                MATCH (package:Package {PackageID: $packageId})
                MERGE (courier)-[:CollectionList]->(package)

            ";
            string awayQuery = @"
                MATCH (user:UserAccount)-[:Has]->(package:Package)
                WHERE package.PackageID = $packageId
                WITH user
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Away' AND courier.WorksAt = user.PostalCode
                OPTIONAL MATCH (courier)-[list:CollectionList|DeliveryList]-()
                WITH courier, COUNT(list) AS listCount
                ORDER BY listCount
                LIMIT 1
                MATCH (package:Package {PackageID: $packageId})
                MERGE (courier)-[:CollectionList]->(package)
            ";

            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(availableQuery, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (!summary.Counters.ContainsUpdates)
                    {
                        cursor = await tx.RunAsync(workingQuery, parameters);
                        summary = await cursor.ConsumeAsync();
                    }
                    if (!summary.Counters.ContainsUpdates)
                    {
                        cursor = await tx.RunAsync(awayQuery, parameters);
                        summary = await cursor.ConsumeAsync();
                    }
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result)
            {
                return Ok("PackageAssignedToCourier");
            }

            return BadRequest("Error assigning package!");
        }

        [Route("GetPackage/{packageId}")]
        [HttpGet]
        public async Task<IActionResult> GetPackage(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            Package? result = null;
            string query = @"
                MATCH (p:Package)
                WHERE p.PackageID = $packageId
                RETURN p
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    INode p = record["p"].As<INode>();
                    return new Package
                    {
                        Id = Helper.GetIDfromINodeElementId(p.ElementId.As<string>()),
                        PackageID = p.Properties["PackageID"].As<string>(),
                        Content = p.Properties["Content"].As<string>(),
                        Description = p.Properties["Description"].As<string>(),
                        Weight = p.Properties["Weight"].As<float>(),
                        Price = p.Properties["Price"].As<float>(),
                        SenderEmail = p.Properties["SenderEmail"].As<string>(), 
                        ReceiverEmail = p.Properties["ReceiverEmail"].As<string>(),
                        EstimatedArrivalDate = p.Properties["EstimatedArrivalDate"].As<DateTime>(),
                        PackageStatus = p.Properties["PackageStatus"].As<string>()
                    };
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return NotFound("The package doesn't exist");
        }
    
        [Route("GetPackageStatus/{packageId}")]
        [HttpGet]
        public async Task<IActionResult> GetPackageStatus(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            string result;
            string query = @"
                MATCH (p:Package)
                WHERE p.PackageID = $packageId
                RETURN p.PackageStatus as s
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    return record["s"].As<string>();
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return BadRequest("Someting went wrong retrieving package status!");
        }


        // trenutno redundantna metoda, package status je dovoljno detaljan da možemo da zaključimo isto

        //[Route("GetPackageLocation/{packageId}")]
        //[HttpGet]
        //public async Task<IActionResult> GetPackageLocation(string packageId)
        //{
        //    IAsyncSession session = _driver.AsyncSession();

        //    List<string> result = new();
        //    string query = @"
        //        MATCH (x)-[:Has]-(p:Package{PackageID:$packageId})
        //        RETURN
        //        CASE LABELS(x)
        //            WHEN ['PostOffice'] THEN ['PostOffice', x.PostalCode]
        //            WHEN ['Courier'] THEN ['Courier', ID(x)]
        //        END AS result
        //    ";
        //    var parameters = new { packageId };

        //    try
        //    {
        //        result = await session.ExecuteReadAsync(async tx =>
        //        {
        //            IResultCursor cursor = await tx.RunAsync(query, parameters);
        //            IRecord record = await cursor.SingleAsync();
        //            return record["result"].As<List<string>>();
        //        });
        //    }
        //    finally
        //    {
        //        await session.CloseAsync();
        //    }
            
        //    if (result != null) return Ok(result);
            
        //    return BadRequest("Something went wrong determining package location!");
        //}


        [Route("GetSentPackages/{email}")]
        [HttpGet]
        public async Task<IActionResult> GetSentPackages(string email)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<Package> packages = new();
            string query = "MATCH (p:Package{SenderEmail:$email}) RETURN p";
            var parameters = new { email };

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    List<IRecord> records = await cursor.ToListAsync();

                    if(records.Count > 0)
                    {
                        foreach (var record in records)
                        {
                            INode node = record["p"].As<INode>();
                            Package package = new()
                            {
                                Id = Helper.GetIDfromINodeElementId(node.ElementId.As<string>()),
                                PackageID = node.Properties["PackageID"].As<string>(),
                                Content = node.Properties["Content"].As<string>(),
                                Description = node.Properties["Description"].As<string>(),
                                Weight = node.Properties["Weight"].As<float>(),
                                Price = node.Properties["Price"].As<float>(),
                                SenderEmail = node.Properties["SenderEmail"].As<string>(),
                                ReceiverEmail = node.Properties["ReceiverEmail"].As<string>(),
                                EstimatedArrivalDate = node.Properties["EstimatedArrivalDate"].As<DateTime>(),
                                PackageStatus = node.Properties["PackageStatus"].As<string>()
                            };
                            packages.Add(package);
                        }
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            return Ok(packages);
        }

        [Route("GetPackages/{email}")]
        [HttpGet]
        public async Task<IActionResult> GetPackages(string email)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<Package> packages= new();
            string query = "MATCH (p:Package{ReceiverEmail:$email}) RETURN p";
            var parameters = new { email };

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    List<IRecord> records = await cursor.ToListAsync();

                    if (records.Count > 0)
                    {
                        foreach (var record in records)
                        {
                            INode node = record["p"].As<INode>();
                            Package package = new()
                            {
                                Id = Helper.GetIDfromINodeElementId(node.ElementId.As<string>()),
                                PackageID = node.Properties["PackageID"].As<string>(),
                                Content = node.Properties["Content"].As<string>(),
                                Description = node.Properties["Description"].As<string>(),
                                Weight = node.Properties["Weight"].As<float>(),
                                Price = node.Properties["Price"].As<float>(),
                                SenderEmail = node.Properties["SenderEmail"].As<string>(),
                                ReceiverEmail = node.Properties["ReceiverEmail"].As<string>(),
                                EstimatedArrivalDate = node.Properties["EstimatedArrivalDate"].As<DateTime>(),
                                PackageStatus = node.Properties["PackageStatus"].As<string>()
                            };
                            packages.Add(package);
                        }
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }
      
            return Ok(packages);
        }


        // vraća dve liste svih pošilki koje kurir treba da odradi
        [Route("GetCourierPackageList/{courierId}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierPackageList(int courierId)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<Package> collectionPackages = new();
            List<Package> deliveryPackages = new();

            string query = @"
                MATCH (c:Courier)-[:CollectionList]-(p:Package)
                WHERE ID(c) = $courierId
                RETURN p AS package, 'collection' AS type
                UNION
                MATCH (c:Courier)-[:DeliveryList]-(p:Package)
                WHERE ID(c) = $courierId
                RETURN p AS package, 'delivery' AS type
            ";

            var parameters = new { courierId };

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    List<IRecord> records = await cursor.ToListAsync();
                    foreach (var record in records)
                    {
                        INode p = record["package"].As<INode>();
                        Package package = new()
                        {
                            Id = Helper.GetIDfromINodeElementId(p.ElementId.As<string>()),
                            PackageID = p.Properties["PackageID"].As<string>(),
                            Content = p.Properties["Content"].As<string>(),
                            Description = p.Properties["Description"].As<string>(),
                            Weight = p.Properties["Weight"].As<float>(),
                            Price = p.Properties["Price"].As<float>(),
                            SenderEmail = p.Properties["SenderEmail"].As<string>(),
                            ReceiverEmail = p.Properties["ReceiverEmail"].As<string>(),
                            EstimatedArrivalDate = p.Properties["EstimatedArrivalDate"].As<DateTime>(),
                            PackageStatus = p.Properties["PackageStatus"].As<string>()
                        };
                        if (record["type"].As<string>() == "collection")
                        {
                            collectionPackages.Add(package);
                        }
                        else
                        {
                            deliveryPackages.Add(package);
                        }
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            var result = new
            {
                CollectionPackages = collectionPackages,
                DeliveryPackages = deliveryPackages
            };

            return Ok(result);
        }

        [Route("EditPackage")]
        [HttpPut]
        public async Task<IActionResult> EditPackage(Package package)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result;
            string query = @"
                MATCH (p:Package)
                WHERE ID(p) = $Id
                SET p.PackageID = $PackageID,
                p.Content = $Content,
                p.Description = $Description,
                p.Weight = $Weight,
                p.Price = $Price,
                p.SenderEmail = $SenderEmail,
                p.ReceiverEmail = $ReceiverEmail,
                p.EstimatedArrivalDate = $EstimatedArrivalDate,
                p.PackageStatus = $PackageStatus
            ";
            var parameters = new
            {
                package.Id,
                package.PackageID,
                package.Content,
                package.Description,
                package.Weight,
                package.Price,
                package.SenderEmail,
                package.ReceiverEmail,
                package.EstimatedArrivalDate,
                package.PackageStatus
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result) return Ok("Package: " + package.PackageID + "updated successfully!");
            
            return BadRequest("Something went wrong updating the package!");
        }

        [Route("DeletePackage/{packageId}")]
        [HttpDelete]
        public async Task<IActionResult> DeletePackage(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (p:Package)
                WHERE p.PackageID = $packageId
                DETACH DELETE p
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesDeleted;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result == 1) return Ok("Package deleted successfully!");
            
            return BadRequest("Error deleting package!");
        }

    }
}
