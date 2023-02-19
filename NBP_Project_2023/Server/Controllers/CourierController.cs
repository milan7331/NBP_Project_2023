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
        public async Task<IActionResult> CreateCourier(Courier courier)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (c:Courier {FirstName: $FirstName})
                        SET c.LastName = $LastName
                        SET c.CourierStatus = $CourierStatus
                    ", new { courier.FirstName, courier.LastName, courier.CourierStatus });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("User registered successfully!");
            return BadRequest("User registration failed!");
        }

        [Route("AddWorkplace/{courierID}/{postalCode}")]
        [HttpPost]
        public async Task<IActionResult> AddWorkplace(int courierID, int postalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierID)
                        WITH c
                        MATCH (p:PostOffice{PostalCode:$postalCode})
                        MERGE (c)-[:WorksAt]-(p)
                    ", new { courierID, postalCode});
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if(summary.Counters.RelationshipsCreated > 0) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Courier workplace added!");
            else return BadRequest("Something went wrong adding courier workplace!");
        }

        [Route("GetCourier/{courierID}")]
        [HttpGet]
        public async Task<IActionResult> GetCourier(int courierID)
        {
            IAsyncSession session = _driver.AsyncSession();
            Courier result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierID)-[:WorksAt]-(p:PostOffice)
                        RETURN c, p.PostalCode as code
                    ", new { courierID });
                    IRecord record = await cursor.SingleAsync();
                    int PostalCode = record["code"].As<int>();
                    INode c = record["c"].As<INode>();
                    return new Courier
                    {
                        Id = unchecked((int)c.Id),
                        FirstName = c.Properties["FirstName"].As<string>(),
                        LastName = c.Properties["LastName"].As<string>(),
                        CourierStatus = c.Properties["CourierStatus"].As<string>(),
                        WorksAt = PostalCode
                    };
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            return BadRequest("This Courier doesn't exist!");
        }

        [Route("GetCourierLogin/{firstName}/{lastName}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierLogin(string firstName, string lastName)
        {
            IAsyncSession session = _driver.AsyncSession();
            Courier result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier)-[:WorksAt]-(p:PostOffice)
                        WHERE c.FirstName = $firstName AND c.LastName = $lastName
                        RETURN c, p.PostalCode as code
                    ", new { firstName, lastName });
                    IRecord record = await cursor.SingleAsync();
                    int PostalCode = record["code"].As<int>();
                    INode c = record["c"].As<INode>();
                    return new Courier
                    {
                        Id = unchecked((int)c.Id),
                        FirstName = c.Properties["FirstName"].As<string>(),
                        LastName = c.Properties["LastName"].As<string>(),
                        CourierStatus = c.Properties["CourierStatus"].As<string>(),
                        WorksAt = PostalCode
                    };
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            return BadRequest("This Courier doesn't exist!");
        }

        [Route("GetCourierPackages/{courierID}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierPackages(int courierID)
        {
            IAsyncSession session = _driver.AsyncSession();
            List<string> result = new();
            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierID)-[:Has]-(p:Package)
                        RETURN p.PackageID as PackageID
                    ", new { courierID });
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
        public async Task<IActionResult> EditCourier(Courier courier)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $Id)
                        SET c.FirstName = $FirstName
                        SET c.LastName = $LastName
                        SET c.CourierStatus = $CourierStatus
                        ", new { courier.Id, courier.FirstName, courier.LastName, courier.CourierStatus });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("User: " + courier.FirstName + " " + courier.LastName + " updated successfully!");
            return BadRequest("Something went wrong updating the courier!");
        }

        [Route("ChangeWorkplace/{courierID}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> ChangeWorkplace(int courierID, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierID)-[w:WorksAt]-(:PostOffice)
                        DELETE w
                        WITH c
                        MERGE (c)-[:WorksAt]-(:PostOffice{PostalCode:$newPostalCode})
                    ", new { courierID, newPostalCode });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsCreated == 1 && summary.Counters.RelationshipsDeleted == 1) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Courier workplace changed!");
            return BadRequest("Something went wrong changing the workplace!");
        }

        [Route("DeliverPackageToPostOffice/{courierID}/{packageID}")]
        [HttpPut]
        public async Task<IActionResult> DeliverPackageToPostOffice(int courierID, string packageID)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier WHERE ID(c) = $courierID)-[h:Has]-(p:Package{PackageID:$packageID})
                        DELETE h
                        WITH c, p
                        MATCH (c)-[:WorksAt]-(post:PostOffice)
                        MERGE (post)-[:Has]-(p)
                    ", new {packageID, courierID});
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 1 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Package delivered successfully!");
            return BadRequest("Something went wrong delivering package!");
        }

        [Route("DeleteCourier/{courierID}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCourier(int courierID)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (c:Courier)
                        WHERE ID(c) = $courierID
                        DETACH DELETE c
                    ", new { courierID });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Courier deleted successfully!");
            return BadRequest("Error deleting courier!");
        }
    }
}
// KURIR INAČE MOZEŽ DA URADI TAKE PACKAGE I LEAVE PACKAGE??
