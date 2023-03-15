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
                SET p.City = $City,
                p.X = $PostX,
                p.Y = $PostY,
                p.IsMainPostOffice = $IsMainPostOffice
            ";
            var parameters = new
            {
                post.PostalCode,
                post.City,
                post.PostX,
                post.PostY,
                post.IsMainPostOffice
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

        [Route("ConnectPostOffices/{postalCode1}/{postalCode2}")]
        [HttpPost]
        public async Task<IActionResult> ConnectPostOffices(int postalCode1, int postalCode2)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query1 = @"
                MATCH (p1:PostOffice {PostalCode: $postalCode1})
                WITH p1
                MATCH (p2:PostOffice {PostalCode: $postalCode2})
                RETURN p1.X as x1, p1.Y as y1, p2.X as x2, p2.Y as y2
            ";
            string query2 = @"
                MATCH (p1: PostOffice {PostalCode: $postalCode1})
                WITH p1
                MATCH (p2: PostOffice {PostalCode: $postalCode2})
                MERGE (p1)-[:Road{Distance: $distance}]-(p2);
            ";
            var parameters1 = new
            {
                postalCode1,
                postalCode2
            };

            try
            {
                float distance = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query1, parameters1);
                    IRecord record = await cursor.SingleAsync();

                    float x1 = record["x1"].As<float>();
                    float y1 = record["y1"].As<float>();
                    float x2 = record["x2"].As<float>();
                    float y2 = record["y2"].As<float>();

                    float dist = (float)(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                    dist = (float)Math.Sqrt(dist);
                    return dist;
                });

                if (distance == 0.0) return BadRequest("Missing coordinates!");

                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query2, new { postalCode1, postalCode2, distance });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result > 0) return Ok("Post offices connected!");
            
            return BadRequest("Error connecting post offices!");
        }

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
                        PostX = p.Properties["X"].As<float>(),
                        PostY = p.Properties["Y"].As<float>(),
                        IsMainPostOffice = p.Properties["IsMainPostOffice"].As<bool>()
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
                MATCH (p:PostOffice)-[:WorksAt]-(c:Courier)
                WHERE p.PostalCode = $postalCode
                RETURN ID(c) AS id
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
                MATCH (post:PostOffice)-[]-(p:Package)
                WHERE post.PostalCode = $postalCode
                RETURN p.PackageID AS pid
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
                p.PostalCode = $PostalCode,
                p.X = $PostX,
                p.Y = $PostY,
                p.IsMainPostOffice = $IsMainPostOffice
            ";
            var parameters = new
            {
                post.Id,
                post.City,
                post.PostalCode,
                post.PostX,
                post.PostY,
                post.IsMainPostOffice
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
                MATCH (c:Courier WHERE ID(c) = $courierId)-[w:WorksAt]-(:PostOffice)
                DELETE w
                WITH c
                MATCH (new:PostOffice{PostalCode:$newPostalCode})
                MERGE (c)-[:WorksAt]-(new)
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
        public async Task<IActionResult> MovePackageToAnotherPostOffice(string packageId, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (:PostOffice)-[rel:Has]-(p:Package WHERE p.PackageID = $packageId)
                DELETE rel
                WITH p
                MATCH (new:PostOffice{PostalCode:$newPostalCode})
                MERGE (new)-[:Has]-(p)
            ";
            var parameters = new
            {
                packageId,
                newPostalCode
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    var summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result == 1) return Ok("Package successfuly moved!");
            
            return BadRequest("Something went wrong moving package");
        }

        [Route("RegisterPackage/{postalCode}/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterPackage(int postalCode, string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = "MERGE (:PostOffice{PostalCode:$postalCode})-[:Has]-(:Package{PackageID:$packageId})";
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

        [Route("RegisterWorker/{postalCode}/{workerId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterWorker(int postalCode, int workerID)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (p:PostOffice{PostalCode:$postalCode})
                WITH p
                MATCH (c:Courier WHERE ID(c) = $workerID)
                MERGE (c)-[:WorksAt]-(p)
            ";
            var parameters = new
            {
                postalCode,
                workerID
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
            
            if (result == 1) return Ok("Courier registered");
            
            return BadRequest("Something went wrong registering courier!");
        }

        [Route("DeletePostOffice/{postalCode}")]
        [HttpDelete]
        //Bitno da je da u pošti koja se briše nema paketa i radnika!
        public async Task<IActionResult> DeletePostOffice(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (p:PostOffice WHERE p.PostalCode = $postalCode)
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
    }
}
