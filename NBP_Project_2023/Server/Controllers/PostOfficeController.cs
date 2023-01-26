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
        public async Task<IActionResult> CreatePostOffice(PostOffice Post)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (p:PostOffice {PostalCode: $PostalCode})
                        SET p.City = '$City'
                        SET p.X = $PostX
                        SET p.Y = $PostY
                        SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new { Post.PostalCode, Post.City, Post.PostX, Post.PostY, Post.IsMainPostOffice });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result > 0) return Ok("Node created successfully!");
            else return BadRequest("Node creation (Post) failed!");
        }

        [Route("ConnectPostOffices/{PostalCode1}/{PostalCode2}")]
        [HttpPost]
        public async Task<IActionResult> ConnectPostOffices(int PostalCode1, int PostalCode2)
        {

            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                float x1 = 0.0f;
                float y1 = 0.0f;
                float x2 = 0.0f;
                float y2 = 0.0f;
                float distance = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p1:PostOffice {PostalCode: $PostalCode1})
                        MATCH (p2:PostOffice {PostalCode: $PostalCode2})
                        RETURN p1.X as x1, p1.Y as y1, p2.X as x2, p2.Y as y2
                    ", new { PostalCode1, PostalCode2 });
                    IRecord record = await cursor.SingleAsync();

                    x1 = record["x1"].As<float>();
                    y1 = record["y1"].As<float>();
                    x2 = record["x2"].As<float>();
                    y2 = record["y2"].As<float>();

                    float dist = (float)(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                    dist = (float)Math.Sqrt(dist);
                    return dist;
                });

                if (x1 == 0.0f || y1 == 0.0f || x2 == 0.0f || y2 == 0.0f)
                    return BadRequest("Missing coordinates!");

                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p1: PostOffice {PostalCode: $PostalCode1})
                        MATCH (p2: PostOffice {PostalCode: $PostalCode2})
                        MERGE (p1)-[:Road{Distance: $distance}]-(p2);
                    ", new { PostalCode1, PostalCode2, distance });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result > 0) return Ok("Post offices connected!");
            else return BadRequest("Error connecting post offices!");
        }

        [Route("GetPostOffice/{PostalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOffice(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            INode result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice)
                        WHERE p.PostalCode = $PostalCode
                        RETURN p
                    ", new { PostalCode });
                    IRecord record = await cursor.SingleAsync();
                    return record["p"].As<INode>();
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null)
            {
                return Ok(new PostOffice
                {
                    Id = Int32.Parse(result.ElementId),
                    City = result.Properties["City"].ToString() ?? "",
                    PostalCode = (int)result.Properties["PostalCode"],
                    PostX = (float)result.Properties["X"],
                    PostY = (float)result.Properties["Y"],
                    IsMainPostOffice = (bool)result.Properties["IsMainPostOffice"]
                });
            }
            else return BadRequest("PostOfficeNotFound!");
        }

        [Route("GetPostOfficeWorkers/{PostalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficeWorkers(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<int> ListOfWorkers;
            try
            {
                ListOfWorkers = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor result = await tx.RunAsync(@"
                        MATCH (:PostOffice {PostalCode:" + PostalCode + "})-[:WorksAt]-(c:Courier) RETURN ID(c) AS id");
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

        [Route("GetPostOfficePackages/{PostalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficePackages(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<string> ListOfPackages;
            try
            {
                ListOfPackages = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor result = await tx.RunAsync(@"
                        MATCH (:PostOffice {PostalCode:" + PostalCode + "})-[]-(p:Package) RETURN p.PackageID AS pid");
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
        public async Task<IActionResult> UpdatePostOffice(PostOffice Post)
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
                        SET p.City = '$City'
                        SET p.PostalCode = $PostalCode
                        SET p.X = $PostX
                        SET p.Y = $PostY
                        SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new { Post.Id, Post.City, Post.PostalCode, Post.PostX, Post.PostY, Post.IsMainPostOffice });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            await session.CloseAsync();
            if (result) return Ok("PostOfficeUpdatedSuccessfuly!");
            else return BadRequest("PostOffice update failed!");
        }

        [Route("MoveWorker/{CourierId}/{NewPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MoveWorker(int CourierId, int NewPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierId)-[w:WorksAt]-(:PostOffice)
                        DELETE w
                        WITH c
                        MATCH (new:PostOffice{PostalCode:$NewPostalCode})
                        MERGE (c)-[:WorksAt]-(new)
                    ", new { CourierId, NewPostalCode });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Worker successfuly moved!");
            else return NotFound();
        }

        [Route("MovePackageToAnotherPostOffice/{PackageID}/{NewPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MovePackageToAnotherPostOffice(string PackageID, int NewPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (:PostOffice)-[rel:Has]-(p:Package WHERE p.PackageID = '$PackageID')
                        DELETE rel
                        WITH p
                        MATCH (new:PostOffice{PostalCode:$NewPostalCode})
                        MERGE (new)-[:Has]-(p)
                    ", new { PackageID, NewPostalCode });
                    var summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Package successfuly moved!");
            else return NotFound();
        }

        [Route("RegisterPackage/{PostalCode}/{PackageID}")]
        [HttpPut]
        public async Task<IActionResult> RegisterPackage(int PostalCode, string PackageID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (:PostOffice{PostalCode:$PostalCode})-[:Has]-(:Package{PackageID:'$PackageID'})
                    ", new { PostalCode, PackageID });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Package registered!");
            else return NotFound();
        }

        [Route("RegisterWorker/{PostalCode}/{WorkerId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterWorker(int PostalCode, int WorkerID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice{PostalCode:$PostalCode})
                        WITH p
                        MATCH (c:Courier WHERE ID(c) = $WorkerID)
                        MERGE (c)-[:WorksAt]-(p)
                    ", new { PostalCode, WorkerID });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.RelationshipsCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Courier registered");
            else return NotFound();
        }

        [Route("DeletePostOffice/{PostalCode}")]
        [HttpDelete]
        //Bitno da je da u pošti koja se briše nema paketa i radnika!
        public async Task<IActionResult> DeletePostOffice(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:PostOffice WHERE p.PostalCode = $PostalCode)
                        DELETE p
                    ", new { PostalCode });
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
