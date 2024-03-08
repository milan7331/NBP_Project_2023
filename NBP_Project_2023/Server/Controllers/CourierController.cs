using Microsoft.AspNetCore.Mvc;
using NBP_Project_2023.Shared;
using NBP_Project_2023.Server.Services;
using Neo4j.Driver;



namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourierController : ControllerBase
    {
        private readonly IDriver _driver;
        private readonly RedisCachingService _redisCachingService;

        public CourierController(IDriver driver, RedisCachingService redisCachingService)
        {
            _driver = driver;
            _redisCachingService = redisCachingService;
        }

        [Route("CreateCourier")]
        [HttpPost]
        public async Task<IActionResult> CreateCourier(Courier courier)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result = 0;
            string query = @"
                CREATE (c:Courier {FirstName: $FirstName})
                SET c.LastName = $LastName,
                c.CourierStatus = $CourierStatus
            ";
            var parameters = new
            {
                courier.FirstName,
                courier.LastName,
                courier.CourierStatus
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
            finally {
                await session.CloseAsync();
            }

            if (result == 1) return Ok("User registered successfully!");

            return BadRequest("User registration failed!");
        }

        [Route("GetCourier/{courierId}")]
        [HttpGet]
        public async Task<IActionResult> GetCourier(int courierId)
        {
            Courier? result = await _redisCachingService.GetStringDataAsync<Courier>(courierId.ToString());

            if (result != null)
            {
                Console.WriteLine("redis blok je izvršen i ovo je vratio jebeni redis:\n");
                Console.WriteLine($"{result.FirstName}:{result.LastName}:{result.Id}");
                return Ok(result);
            }

            // if the object is not found in the cache retrieve it from the db and cache it in the end

            IAsyncSession session = _driver.AsyncSession();

            
            string query = @"
                MATCH (c:Courier WHERE ID(c) = $courierId)-[:WorksAt]-(p:PostOffice)
                RETURN c, p.PostalCode as code
            ";
            var parameters = new { courierId };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    int PostalCode = record["code"].As<int>();
                    INode c = record["c"].As<INode>();
                    return new Courier
                    {
                        Id = Helper.GetIDfromINodeElementId(c.ElementId.As<string>()),
                        FirstName = c.Properties["FirstName"].As<string>(),
                        LastName = c.Properties["LastName"].As<string>(),
                        CourierStatus = c.Properties["CourierStatus"].As<string>(),
                        WorksAt = PostalCode
                    };
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result != null)
            {
                await _redisCachingService.SetStringDataAsync<Courier>(courierId.ToString(), result);
                return Ok(result);
            }
            
            return NotFound("This Courier doesn't exist!");
        }

        [Route("GetCourierLogin/{firstName}/{lastName}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierLogin(string firstName, string lastName)
        {
            IAsyncSession session = _driver.AsyncSession();

            Courier result;
            string query = @"
                MATCH (c:Courier)-[:WorksAt]-(p:PostOffice)
                WHERE c.FirstName = $firstName AND c.LastName = $lastName
                RETURN c, p.PostalCode as code
            ";
            var parameters = new
            {
                firstName,
                lastName
            };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    int PostalCode = record["code"].As<int>();
                    INode c = record["c"].As<INode>();
                    return new Courier
                    {
                        Id = Helper.GetIDfromINodeElementId(c.ElementId.As<string>()),
                        FirstName = c.Properties["FirstName"].As<string>(),
                        LastName = c.Properties["LastName"].As<string>(),
                        CourierStatus = c.Properties["CourierStatus"].As<string>(),
                        WorksAt = PostalCode
                    };
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return NotFound("This Courier doesn't exist!");
        }

        [Route("GetCourierPackages/{courierId}")]
        [HttpGet]
        public async Task<IActionResult> GetCourierPackages(int courierId)
        {
            IAsyncSession session = _driver.AsyncSession();

            List<string> result = new();
            string query = @"
                MATCH (c:Courier WHERE ID(c) = $courierId)-[:Has]-(p:Package)
                RETURN p.PackageID as PackageID
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
                        result.Add(record["PackageID"].As<string>());
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            return Ok(result);
        }

        [Route("EditCourier")]
        [HttpPut]
        public async Task<IActionResult> EditCourier(Courier courier)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result;
            string query = @"
                MATCH (c:Courier WHERE ID(c) = $Id)
                SET c.FirstName = $FirstName
                SET c.LastName = $LastName
                SET c.CourierStatus = $CourierStatus
            ";
            var parameters = new
            {
                courier.Id,
                courier.FirstName,
                courier.LastName,
                courier.CourierStatus
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
            
            if (result) return Ok($"User: {courier.FirstName} {courier.LastName} updated successfully!");
            
            return NotFound("The courier doesn't exist!");
        }

        [Route("ChangeWorkplace/{courierId}/{newPostalCode}")]
        [HttpPut]
        public async Task<IActionResult> ChangeWorkplace(int courierId, int newPostalCode)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result;
            string query = @"
                MATCH (c:Courier WHERE ID(c) = $courierId)-[w:WorksAt]-(:PostOffice)
                DELETE w
                WITH c
                MATCH (post:PostOffice)
                WHERE post.PostalCode = $newPostalCode
                MERGE (c)-[:WorksAt]-(post)
            ";
            var parameters = new
            {
                courierId, newPostalCode
            };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsCreated == 1 && summary.Counters.RelationshipsDeleted == 1) return true;
                    return false;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result) return Ok("Courier workplace changed!");
            
            return BadRequest("Something went wrong changing the workplace!");
        }

        [Route("PickupPackageFromPostOffice/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> PickupPackageFromPostOffice(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (courier:Courier)-[:DeliveryList]-(package:Package)
                WHERE package.PackageID = $packageId
                SET courier.CourierStatus = 'Working', package.PackageStatus = 'BeingDeliveredToDestination'
                WITH courier, package
                MATCH (package)-[h:Has]-()
                DELETE h
                WITH courier, package
                MERGE (courier)-[:Has]->(package)
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 1 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result) return Ok("Package picked up from post office successfully!");

            return BadRequest("Something went wrong picking up package from post office!");
        }

        [Route("PickupPackageFromSender/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> PickupPackageFromSender(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (courier:Courier)-[:CollectionList]-(package:Package)
                WHERE package.PackageID = $packageId
                SET courier.CourierStatus = 'Working', package.PackageStatus = 'BeingDeliveredToPostOffice'
                WITH package, courier
                MATCH (package)-[h:Has]-()
                DELETE h
                WITH package, courier
                MERGE (courier)-[:Has]->(package)
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 1 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result) return Ok("Package picked up from sender successfully!");

            return BadRequest("Something went wrong picking up package from sender!");
        }

        [Route("DeliverPackageToPostOffice/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> DeliverPackageToPostOffice(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (courier:Courier)-[h]-(package:Package)
                WHERE package.PackageID = $packageId
                SET package.PackageStatus = 'AtPostOffice', courier.CourierStatus = 'Available'
                DELETE h
                WITH courier, package
                MATCH (post:PostOffice)
                WHERE post.PostalCode = courier.WorksAt
                MERGE (post)-[:Has]->(package)
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 2 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result) return Ok("Package delivered to post office successfully!");

            return BadRequest("Something went wrong delivering package to post office!");
        }

        [Route("DeliverPackageToDestination/{packageId}")]
        [HttpPut]
        public async Task<IActionResult> DeliverPackageToDestination(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (courier:Courier)-[h]-(package:Package)
                WHERE package.PackageID = $packageId
                SET package.PackageStatus = 'Delivered', courier.CourierStatus = 'Available'
                DELETE h
                WITH courier, package
                MATCH (user:UserAccount)
                WHERE user.PostalCode = courier.WorksAt AND user.Email = package.ReceiverEmail
                MERGE (user)-[:Has]->(package)
            ";
            var parameters = new { packageId };

            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IResultSummary summary = await cursor.ConsumeAsync();
                    if (summary.Counters.RelationshipsDeleted == 2 && summary.Counters.RelationshipsCreated == 1) return true;
                    return false;
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            if (result) return Ok("Package delivered to user successfully!");

            return BadRequest("Something went wrong delivering package to user!");
        }

        [Route("DeleteCourier/{courierId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCourier(int courierId)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (c:Courier)
                WHERE ID(c) = $courierId
                DETACH DELETE c
            ";
            var parameters = new { courierId };
            
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

            if (result == 1) return Ok("Courier deleted successfully!");
            
            return NotFound("Error deleting courier!");
        }
    }
}

