using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NBP_Project_2023.Shared;
using Neo4j.Driver;

namespace NBP_Project_2023.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        private readonly IDriver _driver;

        public UserAccountController(IDriver driver)
        {
            _driver = driver;
        }

        [Route("RegisterUserAccount")]
        [HttpPost]
        public async Task<IActionResult> RegisterUserAccount(UserAccount User)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (u:UserAccount {Email: '$Email'})
                        SET u.FirstName = '$FirstName'
                        SET u.LastName = '$LastName'
                        SET u.Password = '$Password'
                        SET u.Street = '$Street'
                        SET u.StreetNumber = $StreetNumber
                        SET u.City = '$City'
                        SET u.PostalCode = $PostalCode
                        SET u.PhoneNumber = '$PhoneNumber'
                    ", new { User.Email, User.FirstName, User.LastName, User.Password, User.Street, User.StreetNumber, User.City, User.PostalCode, User.PhoneNumber });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok(User);
            return BadRequest("User registration failed!");
        }

        [Route("GetUserAccount/{Email}")]
        [HttpGet]
        public async Task<IActionResult> GetUserAccount(string Email)
        {
            IAsyncSession session = _driver.AsyncSession();
            INode result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = '$Email'
                        RETURN u
                    ", new { Email });
                    IRecord record = await cursor.SingleAsync();
                    return record["u"].As<INode>();
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null)
            {
                return Ok(new UserAccount
                {
                    Id = Int32.Parse(result.ElementId),
                    FirstName = result.Properties["FirstName"].ToString() ?? "",
                    LastName = result.Properties["LastName"].ToString() ?? "",
                    Email = result.Properties["Email"].ToString() ?? "",
                    Password = result.Properties["Password"].ToString() ?? "",
                    Street = result.Properties["Street"].ToString() ?? "",
                    StreetNumber = Int32.Parse(result.Properties["StreetNumber"].ToString() ?? "0"),
                    City = result.Properties["City"].ToString() ?? "",
                    PostalCode = Int32.Parse(result.Properties["PostalCode"].ToString() ?? "0"),
                    PhoneNumber = result.Properties["PhoneNumber"].ToString() ?? ""
                });
            }
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("SignIn/{Email}/{Password}")]
        [HttpGet]
        public async Task<IActionResult> SignIn(string Email, string Password)
        {
            IAsyncSession session = _driver.AsyncSession();
            INode result;
            try
            {
                 result = await session.ExecuteReadAsync(async tx =>
                 {
                     IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = '$Email', u.Password = '$Password'
                        RETURN u
                     ", new {Email, Password});
                     IRecord record = await cursor.SingleAsync();
                     return record["u"].As<INode>();
                 });
            }
            finally { await session.CloseAsync(); }
            if (result != null)
            {
                return Ok(new UserAccount
                {
                    Id = Int32.Parse(result.ElementId),
                    FirstName = result.Properties["FirstName"].ToString() ?? "",
                    LastName = result.Properties["LastName"].ToString() ?? "",
                    Email = result.Properties["Email"].ToString() ?? "",
                    Password = result.Properties["Password"].ToString() ?? "",
                    Street = result.Properties["Street"].ToString() ?? "",
                    StreetNumber = Int32.Parse(result.Properties["StreetNumber"].ToString() ?? "0"),
                    City = result.Properties["City"].ToString() ?? "",
                    PostalCode = Int32.Parse(result.Properties["PostalCode"].ToString() ?? "0"),
                    PhoneNumber = result.Properties["PhoneNumber"].ToString() ?? ""
                });
            }
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("EditUserAccount")]
        [HttpPut]
        public async Task<IActionResult> EditUserAccount(UserAccount User)
        {
            IAsyncSession session = _driver.AsyncSession();
            bool result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE ID(u) = $Id
                        SET u.FirstName = '$FirstName'
                        SET u.LastName = '$LastName'
                        SET u.Email = '$Email'
                        SET u.Password = '$Password'
                        SET u.Street = '$Street'
                        SET u.StreetNumber = $StreetNumber
                        SET u.City = '$City'
                        SET u.PostalCode = $PostalCode
                        SET u.PhoneNumber = '$PhoneNumber'
                    ", new { User.Id, User.FirstName, User.LastName, User.Email, User.Password, User.Street, User.StreetNumber, User.City, User.PostalCode, User.PhoneNumber });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            if(result) return Ok("User: " + User.FirstName + " " + User.LastName + " updated successfully!");
            return BadRequest("Something went wrong updating the user!");
        }

        [Route("DeleteUserAccount/{Email}/{Password}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteUserAccount(string Email, string Password)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = '$Email', u.Password = '$Password'
                        DETACH DELETE u
                    ", new { Email, Password });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesDeleted;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("User deleted successfully!");
            return BadRequest("Error deleting user!");
        }
    }
}