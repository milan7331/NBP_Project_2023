using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using NBP_Project_2023.Shared;
using System.Collections.Generic;

namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IDriver _driver;

        public PostController(IDriver driver)
        {
            _driver = driver;
        }


        [Route("CreatePostOffice")]
        [HttpPost]
        public async Task<IActionResult> CreatePostOffice(PostOffice post)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MERGE (p:PostOffice {PostalCode: $PostalCode})
                    SET p.City = $City
                    SET p.X = $PostX
                    SET p.Y = $PostY
                    SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new {
                    post.City,
                    post.PostalCode,
                    post.PostX,
                    post.PostY,
                    post.IsMainPostOffice
                }
                );
                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.NodesCreated;
            });
            await session.CloseAsync();
            if (result > 0) return Ok("Node created successfully!");
            else return BadRequest("Node creation (Post) failed!");
        }

        [Route("ConnectPostOffices/{PostalCode1}/{PostalCode2}")]
        [HttpPost]
        public async Task<IActionResult> ConnectPostOffices(int PostalCode1, int PostalCode2)
        {
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;

            IAsyncSession session = _driver.AsyncSession();
            double distance = await session.ExecuteReadAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                MATCH (p1: POST {PostalCode: $PostalCode1})
                MATCH (p2: POST {PostalCode: $PostalCode2})
                RETURN p1.X as x1, p1.Y as y1, p2.X as x2, p2.Y as y2
                ", new {PostalCode1, PostalCode2});
                List<IRecord> records = await cursor.ToListAsync();

                x1 = records.Select(x => x["x1"].As<double>()).First();  //FirstOrDefault????
                y1 = records.Select(x => x["y1"].As<double>()).First();
                x2 = records.Select(x => x["x2"].As<double>()).First();
                y2 = records.Select(x => x["y2"].As<double>()).First();

                double dist = Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2);
                dist = Math.Sqrt(dist);
                return dist;
            });

            if (x1 == 0.0 || y1 == 0.0 || x2 == 0.0 || y2 == 0.0)
                return BadRequest("Missing coordinates!");

            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (p1: PostOffice {PostalCode: $PostalCode1})
                    MATCH (p2: PostOffice {PostalCode: $PostalCode2})
                    MERGE (p1)-[:Road{Distance: $distance}]-(p2);
                    ", new { PostalCode1, PostalCode2, distance });

                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.RelationshipsCreated;
            });
            await session.CloseAsync();
            if (result > 0) return Ok("Post offices connected!");
            else return BadRequest("Error connecting post offices!");
        }

        [Route("GetPostOffice/{PostalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOffice(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            INode result = await session.ExecuteReadAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync("MATCH (node:PostOffice {PostalCode:" + PostalCode + "}) RETURN node");
                IRecord record = await cursor.SingleAsync();
                return record["node"].As<INode>();
            });
            await session.CloseAsync();
            if (result != null)
            {
                return Ok(new PostOffice
                {
                    Id = Int32.Parse(result.ElementId),
                    City = result.Properties["City"].ToString() ?? "",
                    PostalCode = (int)result.Properties["PostalCode"],
                    PostX = (double)result.Properties["X"],
                    PostY = (double)result.Properties["Y"],
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
            List<int> ListOfWorkers = await session.ExecuteReadAsync(async tx =>
            {
                IResultCursor result = await tx.RunAsync("MATCH (:PostOffice {PostalCode:" + PostalCode + "})-[:WorksAt]-(c:Courier) RETURN ID(c) AS cid");
                List<IRecord> resultsList = await result.ToListAsync();

                List<int> workers = new List<int>();
                if (resultsList.Count > 0)
                {
                    foreach (var x in resultsList)
                    {
                        string cid = x["cid"].As<string>();
                        workers.Add(Int32.Parse(cid));
                    }
                }
                return workers;
            });
            await session.CloseAsync();
            return Ok(ListOfWorkers);
        }

        [Route("GetPostOfficePackages/{PostalCode}")]
        [HttpGet]
        public async Task<IActionResult> GetPostOfficePackages(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<string> ListOfPackages = await session.ExecuteReadAsync(async tx =>
            {
                IResultCursor result = await tx.RunAsync("MATCH (:PostOffice {PostalCode:" + PostalCode + "})-[]-(p:Package) RETURN p.PackageID AS pid");
                List<IRecord> resultsList = await result.ToListAsync();

                List<string> packages = new List<string>();
                if (resultsList.Count > 0)
                {
                    foreach (var x in resultsList)
                    {
                        string pid = x["pid"].ToString() ?? "";
                        packages.Add(pid);
                    }
                }
                return packages;
            });
            await session.CloseAsync();
            return Ok(ListOfPackages);
        }

        [Route("UpdatePostOffice")]
        [HttpPut]
        public async Task<IActionResult> UpdatePostOffice(PostOffice post)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool updateSuccess = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (p:PostOffice)
                    WHERE ID(p) = $id
                    SET p.City = $city
                    SET p.PostalCode = $postal
                    SET p.X = $PostX
                    SET p.Y = $PostY
                    SET p.IsMainPostOffice = $IsMainPostOffice
                    ", new {post.Id, post.City, post.PostalCode, post.PostX, post.PostY, post.IsMainPostOffice});
                
                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.ContainsUpdates;
            });
            await session.CloseAsync();
            if (updateSuccess) return Ok("PostOfficeUpdatedSuccessfuly!");
            else return BadRequest("PostOffice update failed!");
        }

        [Route("MoveWorker/{CourierId}/{OldPostalCode}/{NewPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MoveWorker(int CourierId, int OldPostalCode, int NewPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (c:Courier)
                    WHERE ID(c) = $CourierId
                    MATCH (old:PostOffice{PostalCode:$OldPostalCode})
                    MATCH (new:PostOffice{PostalCode:$NewPostalCode})
                    MATCH (c)-[rel:WorksAt]-(old)
                    DELETE rel
                    MERGE (c)-[:WorksAt]-(new)
                ", new {CourierId, OldPostalCode, NewPostalCode});
                
                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.RelationshipsDeleted;
            });
            await session.CloseAsync();
            if (result == 1) return Ok("Worker successfuly moved!");
            else return NotFound();
        }

        [Route("MovePackageToAnotherPostOffice/{PackageID}/{OldPostalCode}/{NewPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> MovePackageToAnotherPostOffice(string PackageID, int OldPostalCode, int NewPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (p:Package{PackageID:$PackageId})
                    MATCH (old:PostOffice{PostalCode:$OldPostalCode})
                    MATCH (new:PostOffice{PostalCode:$NewPostalCode})
                    MATCH (old)-[rel:Has]-(p)
                    DELETE rel
                    MERGE (new)-[:Has]-(p)
                ", new {PackageID, OldPostalCode, NewPostalCode});

                var summary = await cursor.ConsumeAsync();
                return summary.Counters.RelationshipsDeleted;
            });
            await session.CloseAsync();
            if (result == 1) return Ok("Package successfuly moved!");
            else return NotFound();
        }

        [Route("RegisterPackage/{PostalCode}/{PackageID}")]
        [HttpPut]
        public async Task<IActionResult> RegisterPackage(int PostalCode, string PackageID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (post:PostOffice{PostalCode:$PostalCode})
                    MATCH (package:Package{PackageID:$packageID})
                    MERGE (post)-[:Has]-(package)
                ", new {PostalCode, PackageID});

                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.RelationshipsCreated;
            });
            await session.CloseAsync();
            if (result == 1) return Ok("Package registered!");
            else return NotFound();
        }

        [Route("RegisterWorker/{PostalCode}/{WorkerId}")]
        [HttpPut]
        public async Task<IActionResult> RegisterWorker(int PostalCode, string WorkerID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (post:PostOffice{PostalCode:$PostalCode})
                    MATCH (courier:Courier)
                    WHERE ID(courier) = $WorkerID
                    MERGE (courier)-[:WorksAt]-(post)
                ", new {PostalCode,WorkerID});

                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.RelationshipsCreated;
            });
            await session.CloseAsync();
            if (result == 1) return Ok("Courier registered");
            else return NotFound();
        }

        [Route("DeletePostOffice/{PostalCode}")]
        [HttpDelete]
        //Bitno da je da u pošti koja se briše nema paketa!!
        //Otkačiti sve radnike prvo
        public async Task<IActionResult> DeletePostOffice(int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result = await session.ExecuteWriteAsync(async tx =>
            {
                IResultCursor cursor = await tx.RunAsync(@"
                    MATCH (p:PostOffice)
                    WHERE p.PostalCode = $PostalCode
                    DETACH DELETE p
                ", new { PostalCode });

                IResultSummary summary = await cursor.ConsumeAsync();
                return summary.Counters.NodesDeleted;
            });
            await session.CloseAsync();
            if (result == 1) return Ok("PostOffice deleted!");
            else return NotFound();
        }
    }
}