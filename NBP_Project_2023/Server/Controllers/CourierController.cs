using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NBP_Project_2023.Shared;
using Neo4j.Driver;

namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourierController : ControllerBase
    {
        private readonly IDriver _driver;

        public CourierController(IDriver driver)
        {
            _driver = driver;
        }

        [Route("CreateCourier")]
        [HttpPost]
        public async Task<IActionResult> CreateCourier(Courier Courier)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (c:Courier {FirstName: '$FirstName'})
                        SET c.LastName = '$LastName'
                        SET c.CourierStatus = '$CourierStatus'
                    ", new { Courier.FirstName, Courier.LastName, Courier.CourierStatus });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok(Courier);
            return BadRequest("User registration failed!");
        }

        [Route("AddWorkplace/{CourierID}/{PostalCode}")]
        [HttpPost]
        public async Task<IActionResult> AddWorkplace(int CourierID, int PostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierID)
                        WITH c
                        MATCH (p:PostOffice{PostalCode:$PostalCode})
                        MERGE (c)-[:WorksAt]-(p)
                    ", new { CourierID, PostalCode});
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if(summary.Counters.RelationshipsCreated > 0) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Courier workplace added!");
            else return BadRequest("Something went wrong adding courier workplace!");
        }

        [Route("GetCourier/{CourierId}")]
        [HttpGet]
        public async Task<IActionResult> GetCourier(int CourierID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int PostalCode = -1;
            INode result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierID)-[:WorksAt]-(p:PostOffice)
                        RETURN c, p.PostalCode as code
                    ", new { CourierID });
                    IRecord record = await cursor.SingleAsync();
                    PostalCode = record["code"].As<int>();
                    return record["c"].As<INode>();
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null)
            {
                return Ok(new Courier
                {
                    Id = Int32.Parse(result.ElementId),
                    FirstName = result.Properties["FirstName"].ToString() ?? "",
                    LastName = result.Properties["LastName"].ToString() ?? "",
                    CourierStatus = result.Properties["CourierStatus"].ToString() ?? "",
                    WorksAt = PostalCode
                });
            }
            return BadRequest("This Courier doesn't exist!");
        }

        [Route("GetCourierPackages/{CourierID}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierPackages(int CourierID)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<string> result = new();
            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierID)-[:Has]-(p:Package)
                        RETURN p.PackageID as PackageID
                    ", new { CourierID });
                    List<IRecord> records = await cursor.ToListAsync();
                    foreach (var record in records)
                    {
                        result.Add(record["PackageID"].As<string>());
                    }
                });
            }
            finally { await session.CloseAsync(); }
            return Ok(result);
        }

        [Route("EditCourier")]
        [HttpPut]
        public async Task<IActionResult> EditCourier(Courier Courier)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $Id)
                        SET c.FirstName = '$FirstName'
                        SET c.LastName = '$LastName'
                        SET c.CourierStatus = '$CourierStatus'
                        ", new { Courier.Id, Courier.FirstName, Courier.LastName, Courier.CourierStatus });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("User: " + Courier.FirstName + " " + Courier.LastName + " updated successfully!");
            return BadRequest("Something went wrong updating the courier!");
        }

        [Route("ChangeWorkplace/{CourierID}/{NewPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> ChangeWorkplace(int CourierID, int NewPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierID)-[w:WorksAt]-(:PostOffice)
                        DELETE w
                        WITH c
                        MERGE (c)-[:WorksAt]-(:PostOffice{PostalCode:$newPostalCode})
                    ", new { CourierID, NewPostalCode });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsCreated == 1 && summary.Counters.RelationshipsDeleted == 1) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Courier workplace changed!");
            return BadRequest("Something went wrong changing the workplace!");
        }

        [Route("DeliverPackageToPostOffice/{CourierID}/{PackageID}")]
        [HttpPut]
        public async Task<IActionResult> DeliverPackageToPostOffice(int CourierID, string PackageID)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $CourierID)-[h:Has]-(p:Package{PackageID:'$PackageID'})
                        DELETE h
                        WITH c, p
                        MATCH (c)-[:WorksAt]-(post:PostOffice)
                        MERGE (post)-[:Has]-(p)
                    ", new {PackageID, CourierID});
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 1 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Package delivered successfully!");
            return BadRequest("Something went wrong delivering package!");
        }
    }
}
// KURIR DELIVER I TAKE PACKAGE??