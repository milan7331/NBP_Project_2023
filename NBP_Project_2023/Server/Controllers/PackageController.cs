using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using NBP_Project_2023.Shared;

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

        [Route("CreatePackage")]
        [HttpPost]
        public async Task<IActionResult> CreatePackage(Package package)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (p:Package {PackageID:'$PackageID'}
                        SET p.Content = '$Content'
                        SET p.Description = '$Description'
                        SET p.Weight = $Weight
                        SET p.Price = $Price
                        SET p.SenderEmail = '$SenderEmail'
                        SET p.ReceiverEmail = '$ReceiverEmail'
                        SET p.PackageStatus = '$PackageStatus'
                        SET p.EstimatedArrivalDate = $EstimatedArrivalDate
                    ", new { package.PackageID, package.Content, package.Description, package.Weight, package.Price, package.SenderEmail, package.ReceiverEmail, package.PackageStatus, package.EstimatedArrivalDate });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if(result == 1) { return Ok("Package created successfuly!"); }
            return BadRequest("Error creating package!");
        }

        [Route("GetPackage/{packageId}")]
        [HttpGet]
        public async Task<IActionResult> GetPackage(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            INode result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:Package)
                        WHERE p.PackageID = '$packageId'
                        RETURN p
                    ", new { packageId });
                    IRecord record = await cursor.SingleAsync();
                    return record["p"].As<INode>();
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null)
            {
                return Ok(new Package
                {
                    Id = Int32.Parse(result.ElementId),
                    PackageID = result.Properties["PackageID"].As<string>(),
                    Content = result.Properties["Content"].As<string>(),
                    Description = result.Properties["Description"].As<string>(),
                    Weight = result.Properties["Weight"].As<float>(),
                    Price = result.Properties["Price"].As<int>(),
                    SenderEmail = result.Properties["SenderEmail"].As<string>(),
                    ReceiverEmail = result.Properties["ReceiverEmail"].As<string>(),
                    PackageStatus = result.Properties["PackageStatus"].As<string>(),
                    EstimatedArrivalDate = result.Properties["EstimatedArrivalDate"].As<DateTime>()
                });
            }
            return BadRequest("Someting went wrong retrieving package!");
        }

        [Route("GetPackageStatus/{packageId}")]
        [HttpGet]
        public async Task<IActionResult> GetPackageStatus(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            string result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:Package)
                        WHERE p.PackageID = '$packageId'
                        RETURN p.PackageStatus as s
                    ", new { packageId });
                    IRecord record = await cursor.SingleAsync();
                    return record["s"].As<string>();
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            return BadRequest("Someting went wrong retrieving package status!");
        }


        [Route("EditPackage")]
        [HttpPut]
        public async Task<IActionResult> EditPackage(Package package)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:Package)
                        WHERE ID(p) = $Id
                        SET p.PackageID = '$PackageID'
                        SET p.Content = '$Content'
                        SET p.Description = '$Description'
                        SET p.Weight = $Weight
                        SET p.Price = $Price
                        SET p.SenderEmail = '$SenderEmail'
                        SET p.ReceiverEmail = '$ReceiverEmail'
                        SET p.PackageStatus = '$PackageStatus'
                        SET p.EstimatedArrivalDate = $EstimatedArrivalDate
                    ", new { package.Id, package.PackageID, package.Content, package.Description, package.Weight, package.Price, package.SenderEmail, package.ReceiverEmail, package.PackageStatus, package.EstimatedArrivalDate });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            if (result) return Ok("Package: " + package.PackageID + "updated successfully!");
            return BadRequest("Something went wrong updating the package!");
        }

        [Route("DeletePackage/{packageId}")]
        [HttpDelete]
        public async Task<IActionResult> DeletePackage(string packageId)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (p:Package)
                        WHERE p.PackageID = '$packageId'
                        DETACH DELETE p
                    ", new { packageId });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("Package deleted successfully!");
            return BadRequest("Error deleting package!");
        }
    }
}
