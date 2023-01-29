using Microsoft.AspNetCore.Http;
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
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (p:PostOffice {PostalCode: $PostalCode})
                        SET p.City = $City
                        SET p.X = $PostX
                        SET p.Y = $PostY
                        SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new { post.PostalCode, post.City, post.PostX, post.PostY, post.IsMainPostOffice });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result > 0) return Ok("Post Office created successfully!");
            else return BadRequest("Post Office creation failed!");
        }

        [Route("ConnectPostOffices/{postalCode1}/{postalCode2}")]
        [HttpPost]
        public async Task<IActionResult> ConnectPostOffices(int postalCode1, int postalCode2)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                float distance = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p1:PostOffice {PostalCode: $postalCode1})
                        WITH p1
                        MATCH (p2:PostOffice {PostalCode: $postalCode2})
                        RETURN p1.X as x1, p1.Y as y1, p2.X as x2, p2.Y as y2
                    ", new { postalCode1, postalCode2 });
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
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p1: PostOffice {PostalCode: $postalCode1})
                        WITH p1
                        MATCH (p2: PostOffice {PostalCode: $postalCode2})
                        MERGE (p1)-[:Road{Distance: $distance}]-(p2);
                    ", new { postalCode1, postalCode2, distance });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result > 0) return Ok("Post offices connected!");
            else return BadRequest("Error connecting post offices!");
        }

        [Route("GetPostOffice/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOffice(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            PostOffice result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice)
                        WHERE p.PostalCode = $postalCode
                        RETURN p
                    ", new { postalCode });
                    IRecord record = await cursor.SingleAsync();
                    INode p =  record["p"].As<INode>();
                    return new PostOffice
                    {
                        Id = unchecked((int)p.Id),
                        City = p.Properties["City"].As<string>(),
                        PostalCode = p.Properties["PostalCode"].As<int>(),
                        PostX = p.Properties["X"].As<float>(),
                        PostY = p.Properties["Y"].As<float>(),
                        IsMainPostOffice = p.Properties["IsMainPostOffice"].As<bool>()
                    };
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            else return BadRequest("PostOfficeNotFound!");
        }

        [Route("GetPostOfficeWorkers/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficeWorkers(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<int> ListOfWorkers;
            try
            {
                ListOfWorkers = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor result = await tx.RunAsync(@"
                        MATCH (p:PostOffice)-[:WorksAt]-(c:Courier)
                        WHERE p.PostalCode = $postalCode
                        RETURN ID(c) AS id
                    ", new { postalCode });
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
            finally { await session.CloseAsync(); }
            return Ok(ListOfWorkers);
        }

        [Route("GetPostOfficePackages/{postalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficePackages(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<string> ListOfPackages;
            try
            {
                ListOfPackages = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor result = await tx.RunAsync(@"
                        MATCH (post:PostOffice)-[]-(p:Package)
                        WHERE post.PostalCode = $postalCode
                        RETURN p.PackageID AS pid
                    ", new { postalCode });
                    List<IRecord> resultsList = await result.ToListAsync();

                    List<string> packages = new();
                    if (resultsList.Count > 0)
                    {
                        foreach (var x in resultsList)
                        {
                            string pid = x["pid"].As<string>();
                            packages.Add(pid);
                        }
                    }
                    return packages;
                });
            }
            finally { await session.CloseAsync(); }
            return Ok(ListOfPackages);
        }

        [Route("UpdatePostOffice")]
        [HttpPut]
        public async Task<IActionResult> UpdatePostOffice(PostOffice post)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice)
                        WHERE ID(p) = $Id
                        SET p.City = $City
                        SET p.PostalCode = $PostalCode
                        SET p.X = $PostX
                        SET p.Y = $PostY
                        SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new { post.Id, post.City, post.PostalCode, post.PostX, post.PostY, post.IsMainPostOffice });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            await session.CloseAsync();
            if (result) return Ok("PostOfficeUpdatedSuccessfuly!");
            else return BadRequest("PostOffice update failed!");
        }

        [Route("MoveWorker/{courierId}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MoveWorker(int courierId, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierId)-[w:WorksAt]-(:PostOffice)
                        DELETE w
                        WITH c
                        MATCH (new:PostOffice{PostalCode:$newPostalCode})
                        MERGE (c)-[:WorksAt]-(new)
                    ", new { courierId, newPostalCode });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Worker successfuly moved!");
            else return NotFound();
        }

        [Route("MovePackageToAnotherPostOffice/{packageID}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MovePackageToAnotherPostOffice(string packageID, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (:PostOffice)-[rel:Has]-(p:Package WHERE p.PackageID = $packageID)
                        DELETE rel
                        WITH p
                        MATCH (new:PostOffice{PostalCode:$newPostalCode})
                        MERGE (new)-[:Has]-(p)
                    ", new { packageID, newPostalCode });
                    var summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Package successfuly moved!");
            else return NotFound();
        }

        [Route("RegisterPackage/{postalCode}/{packageID}")]
        [HttpPut]
        public async Task<IActionResult> RegisterPackage(int postalCode, string packageID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (:PostOffice{PostalCode:$postalCode})-[:Has]-(:Package{PackageID:$packageID})
                    ", new { postalCode, packageID });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Package registered!");
            else return NotFound();
        }

        [Route("RegisterWorker/{postalCode}/{workerId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterWorker(int postalCode, int workerID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice{PostalCode:$postalCode})
                        WITH p
                        MATCH (c:Courier WHERE ID(c) = $workerID)
                        MERGE (c)-[:WorksAt]-(p)
                    ", new { postalCode, workerID });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Courier registered");
            else return NotFound();
        }

        [Route("DeletePostOffice/{postalCode}")]
        [HttpDelete]
        //Bitno da je da u pošti koja se briše nema paketa i radnika!
        public async Task<IActionResult> DeletePostOffice(int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice WHERE p.PostalCode = $postalCode)
                        DELETE p
                    ", new { postalCode });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("PostOffice deleted!");
            else return NotFound();
        }
    }
}
