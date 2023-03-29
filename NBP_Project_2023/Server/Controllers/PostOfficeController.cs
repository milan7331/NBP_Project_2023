using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using NBP_Project_2023.Shared;

namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostOfficeController : ControllerBase
    {
        private readonly IDriver _driver;

        public PostOfficeController(IDriver driver)
        {
            _driver = driver;
        }

        [Route("CreatePostOffice")]
        [HttpPost]
        public async Task<IActionResult> CreatePostOffice(PostOffice post)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                CREATE (p:PostOffice {PostalCode: $PostalCode})
                SET p.City = $City
            ";
            var parameters = new
            {
                post.PostalCode,
                post.City
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
            
            if (result > 0) return Ok("Post Office created successfully!");
            
            return BadRequest("Post Office creation failed!");
        }

        //[Route("ConnectPostOffices/{postalCode1}/{postalCode2}")]
        //[HttpPost]
        //public async Task<IActionResult> ConnectPostOffices(int postalCode1, int postalCode2)
        //{
        //    IAsyncSession session = _driver.AsyncSession();

        //    int result;
        //    string query1 = @"
        //        MATCH (p1:PostOffice {PostalCode: $postalCode1})
        //        WITH p1
        //        MATCH (p2:PostOffice {PostalCode: $postalCode2})
        //        RETURN p1.X as x1, p1.Y as y1, p2.X as x2, p2.Y as y2
        //    ";
        //    string query2 = @"
        //        MATCH (p1: PostOffice {PostalCode: $postalCode1})
        //        WITH p1
        //        MATCH (p2: PostOffice {PostalCode: $postalCode2})
        //        MERGE (p1)-[:Road{Distance: $distance}]-(p2);
        //    ";
        //    var parameters1 = new
        //    {
        //        postalCode1,
        //        postalCode2
        //    };

        //    try
        //    {
        //        float distance = await session.ExecuteReadAsync(async tx =>
        //        {
        //            IResultCursor cursor = await tx.RunAsync(query1, parameters1);
        //            IRecord record = await cursor.SingleAsync();

        //            float x1 = record["x1"].As<float>();
        //            float y1 = record["y1"].As<float>();
        //            float x2 = record["x2"].As<float>();
        //            float y2 = record["y2"].As<float>();

        //            float dist = (float)(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        //            dist = (float)Math.Sqrt(dist);
        //            return dist;
        //        });

        //        if (distance == 0.0) return BadRequest("Missing coordinates!");

        //        result = await session.ExecuteWriteAsync(async tx =>
        //        {
        //            IResultCursor cursor = await tx.RunAsync(query2, new { postalCode1, postalCode2, distance });
        //            IResultSummary summary = await cursor.ConsumeAsync();
        //            return summary.Counters.RelationshipsCreated;
        //        });
        //    }
        //    finally
        //    {
        //        await session.CloseAsync();
        //    }
            
        //    if (result > 0) return Ok("Post offices connected!");
            
        //    return BadRequest("Error connecting post offices!");
        //}

        [Route("GetPostOffice/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOffice(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            PostOffice result;
            string query = @"
                MATCH (p:PostOffice)
                WHERE p.PostalCode = $postalCode
                RETURN p
            ";
            var parameters = new { postalCode };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    INode p =  record["p"].As<INode>();
                    return new PostOffice
                    {
                        Id = Helper.GetIDfromINodeElementId(p.ElementId.As<string>()),
                        City = p.Properties["City"].As<string>(),
                        PostalCode = p.Properties["PostalCode"].As<int>(),
                    };
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return BadRequest("PostOfficeNotFound!");
        }

        [Route("GetPostOfficeWorkers/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficeWorkers(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<int> ListOfWorkers;
            string query = @"
                MATCH (courier:Courier)-[:WorksAt]-(post:PostOffice)
                WHERE post.PostalCode = $postalCode
                RETURN ID(courier) AS id
            ";
            var parameters = new { postalCode };

            try
            {
                ListOfWorkers = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor result = await tx.RunAsync(query, parameters);
                    List<IRecord> resultsList = await result.ToListAsync();

                    List<int> workers = new();
                    if (resultsList.Count > 0)
                    {
                        foreach (var x in resultsList)
                        {
                            workers.Add(x["id"].As<int>());
                        }
                    }
                    return workers;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            return Ok(ListOfWorkers);
        }

        [Route("GetPostOfficePackages/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficePackages(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<string> packages = new();
            string query = @"
                MATCH (post:PostOffice)-[:Has]-(package:Package)
                WHERE post.PostalCode = $postalCode
                RETURN package.PackageID AS pid
            ";
            var parameters = new { postalCode };

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    List<IRecord> resultsList = await cursor.ToListAsync();

                    if (resultsList.Count > 0)
                    {
                        foreach (var x in resultsList)
                        {
                            string pid = x["pid"].As<string>();
                            packages.Add(pid);
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

        [Route("UpdatePostOffice")]
        [HttpPut]
        public async Task<IActionResult> UpdatePostOffice(PostOffice post)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result;
            string query = @"
                MATCH (p:PostOffice)
                WHERE ID(p) = $Id
                SET p.City = $City,
                p.PostalCode = $PostalCode
            ";
            var parameters = new
            {
                post.Id,
                post.City,
                post.PostalCode
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
            
            if (result) return Ok("PostOfficeUpdatedSuccessfuly!");
            
            return BadRequest("PostOffice update failed!");
        }

        [Route("MoveWorker/{courierId}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MoveWorker(int courierId, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (courier:Courier)-[w:WorksAt]-(:PostOffice)
                WHERE ID(courier) = $courierId
                DELETE w
                WITH courier
                MATCH (new:PostOffice)
                WHERE new.PostalCode = $newPostalCode
                MERGE (courier)-[:WorksAt]-(new)
            ";
            var parameters = new
            {
                courierId,
                newPostalCode
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result == 1) return Ok("Worker successfuly moved!");
            
            return BadRequest("Something went wrong moving worker");
        }

        [Route("MovePackageToAnotherPostOffice/{packageId}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MovePackageToAnotherPostOffice(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (:PostOffice)-[rel:Has]-(package:Package)
                WHERE package.PackageID = $packageId
                DELETE rel
                WITH package
                MATCH (user:UserAccount)
                WHERE user.Email = package.ReceiverEmail
                WITH package, user
                MATCH (new:PostOffice)
                WHERE new.PostalCode = user.PostalCode
                MERGE (new)-[:Has]-(package)
            ";
            var parameters = new { packageId };

            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    var summary = await cursor.ConsumeAsync();
                    if(summary.Counters.RelationshipsCreated == 1 && summary.Counters.RelationshipsDeleted == 1)
                    {
                        result = true;
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result) return Ok("Package successfuly moved!");
            
            return BadRequest("Something went wrong moving package");
        }

        [Route("ProcessPackageAtPostOffice/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> ProcessPackageAtPostOffice(string packageId)
        {
            // treba da se zove prilikom drop of paketa od strane kurira i takođe u else grani na kraju
            bool result;
            bool packageAtDestination = await CheckIfPackageIsAtDestinationPostOffice(packageId);

            if(packageAtDestination)
            {
                result = await AssignPackageToDestinationCourier(packageId);
            }
            else
            {
                await MovePackageToAnotherPostOffice(packageId);
                result = await AssignPackageToDestinationCourier(packageId);
            }

            if (result) return Ok("Package sucessfuly proccesed at post office!");

            else return BadRequest("Something went wrong processing package!");
        }

        //  nepotrebna
        [Route("RegisterPackage/{postalCode}/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterPackage(int postalCode, string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (post:PostOffice)
                WHERE post.PostalCode = $postalCode
                WITH post
                MATCH (package:Package)
                WHERE package.PackageID = $packageId
                MERGE (post)-[:Has]->(package)
            ";
            var parameters = new
            { 
                postalCode,
                packageId
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result == 1) return Ok("Package registered!");
            
            return BadRequest("Something went wrong registering package!");
        }

        [Route("RegisterWorker/{postalCode}/{courierId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterWorker(int postalCode, int courierId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (p:PostOffice)
                WHERE p.PostalCode = $postalCode
                WITH p
                MATCH (c:Courier)
                WHERE ID(c) = $courierId
                MERGE (c)-[:WorksAt]->(p)
                SET c.WorksAt = p.PostalCode
            ";
            var parameters = new
            {
                postalCode,
                courierId
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return (summary.Counters.RelationshipsCreated == 1);
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result) return Ok("Courier registered");
            
            return BadRequest("Something went wrong registering courier!");
        }

        [Route("DeletePostOffice/{postalCode}")]
        [HttpDelete]
        // metoda briše poštu ukoliko ta pošta nema veze ka ostalim čvorovima // nije detach delete!
        public async Task<IActionResult> DeletePostOffice(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (p:PostOffice)
                WHERE p.PostalCode = $postalCode
                DELETE p
            ";
            var parameters = new { postalCode };

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
            
            if (result == 1) return Ok("PostOffice deleted!");
            
            return BadRequest("Something went wrong deleting the PostOffice");
        }


        // helper metode
        private async Task<bool> CheckIfPackageIsAtDestinationPostOffice(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result = false;

            string query = @"
                MATCH (post:PostOffice)-[:Has]-(package:Package)
                WHERE package.PackageID = $packageId
                WITH post, package
                MATCH (user:UserAccount)
                WHERE user.Email = package.ReceiverEmail AND user.PostalCode = post.PostalCode
                RETURN COUNT(user) as count
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    return record["count"].As<int>() > 0;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return result;
        }

        private async Task<bool> AssignPackageToDestinationCourier(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result = false;

            string availableQuery = @"
                MATCH (post:PostOffice)-[:Has]-(package:Package)
                WHERE package.PackageID = $packageId
                WITH post
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Available' AND courier.WorksAt = post.PostalCode
                WITH courier, rand() AS r
                ORDER BY r
                LIMIT 1
                MATCH (package:Package)
                WHERE package.PackageID = $packageId
                MERGE (courier)-[:DeliveryList]->(package)
            ";
            string workingQuery = @"
                MATCH (post:PostOffice)-[:Has]-(package:Package)
                WHERE package.PackageID = $packageId
                WITH post
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Working' AND courier.WorksAt = post.PostalCode
                OPTIONAL MATCH (courier)-[list:CollectionList|DeliveryList]-()
                WITH courier, COUNT(list) AS listCount
                ORDER BY listCount
                LIMIT 1
                MATCH (package:Package)
                WHERE package.PackageID = $packageId
                MERGE (courier)-[:DeliveryList]->(package)

            ";
            string awayQuery = @"
                MATCH (post:PostOffice)-[:Has]-(package:Package)
                WHERE package.PackageID = $packageId
                WITH post
                MATCH (courier:Courier)
                WHERE courier.CourierStatus = 'Away' AND courier.WorksAt = post.PostalCode
                OPTIONAL MATCH (courier)-[list:CollectionList|DeliveryList]-()
                WITH courier, COUNT(list) AS listCount
                ORDER BY listCount
                LIMIT 1
                MATCH (package:Package)
                WHERE package.PackageID = $packageId
                MERGE (courier)-[:DeliveryList]->(package)
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

            return result;
        }
    }
}
