using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> RegisterUserAccount(UserAccount user)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MERGE (u:UserAccount {Email: $Email})
                        SET u.FirstName = $FirstName
                        SET u.LastName = $LastName
                        SET u.Password = $Password
                        SET u.Street = $Street
                        SET u.StreetNumber = $StreetNumber
                        SET u.City = $City
                        SET u.PostalCode = $PostalCode
                        SET u.PhoneNumber = $PhoneNumber
                    ", new { user.Email, user.FirstName, user.LastName, user.Password, user.Street, user.StreetNumber, user.City, user.PostalCode, user.PhoneNumber });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.NodesCreated;
                });
            }
            finally { await session.CloseAsync(); }
            if (result == 1) return Ok("User registered successfully!");
            return BadRequest("User registration failed!");
        }

        [Route("GetUserAccount/{email}")]
        [HttpGet]
        public async Task<IActionResult> GetUserAccount(string email)
        {
            IAsyncSession session = _driver.AsyncSession();
            UserAccount result;
            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = $email
                        RETURN u
                    ", new { email });
                    IRecord record = await cursor.SingleAsync();
                    INode u = record["u"].As<INode>();
                    return new UserAccount
                    {
                        Id = unchecked((int)u.Id),
                        FirstName = u.Properties["FirstName"].As<string>(),
                        LastName = u.Properties["LastName"].As<string>(),
                        Email = u.Properties["Email"].As<string>(),
                        Password = u.Properties["Password"].As<string>(),
                        Street = u.Properties["Street"].As<string>(),
                        StreetNumber = u.Properties["StreetNumber"].As<int>(),
                        City = u.Properties["City"].As<string>(),
                        PostalCode = u.Properties["PostalCode"].As<int>(),
                        PhoneNumber = u.Properties["PhoneNumber"].As<string>()
                    };
                });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("SignIn/{email}/{password}")]
        [HttpGet]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            IAsyncSession session = _driver.AsyncSession();
            UserAccount result;
            try
            {
                 result = await session.ExecuteReadAsync(async tx =>
                 {
                     IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = $email, u.Password = $password
                        RETURN u
                     ", new { email, password });
                     IRecord record = await cursor.SingleAsync();
                     INode u = record["u"].As<INode>();
                     return new UserAccount
                     {
                         Id = unchecked((int)u.Id),
                         FirstName = u.Properties["FirstName"].As<string>(),
                         LastName = u.Properties["LastName"].As<string>(),
                         Email = u.Properties["Email"].As<string>(),
                         Password = u.Properties["Password"].As<string>(),
                         Street = u.Properties["Street"].As<string>(),
                         StreetNumber = u.Properties["StreetNumber"].As<int>(),
                         City = u.Properties["City"].As<string>(),
                         PostalCode = u.Properties["PostalCode"].As<int>(),
                         PhoneNumber = u.Properties["PhoneNumber"].As<string>()
                     };
                 });
            }
            finally { await session.CloseAsync(); }
            if (result != null) return Ok(result);
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("EditUserAccount")]
        [HttpPut]
        public async Task<IActionResult> EditUserAccount(UserAccount user)
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
                        SET u.FirstName = $FirstName
                        SET u.LastName = $LastName
                        SET u.Email = $Email
                        SET u.Password = $Password
                        SET u.Street = $Street
                        SET u.StreetNumber = $StreetNumber
                        SET u.City = $City
                        SET u.PostalCode = $PostalCode
                        SET u.PhoneNumber = $PhoneNumber
                    ", new { user.Id, user.FirstName, user.LastName, user.Email, user.Password, user.Street, user.StreetNumber, user.City, user.PostalCode, user.PhoneNumber });
                    IResultSummary summary = await cursor.ConsumeAsync();
                    return summary.Counters.ContainsUpdates;
                });
            }
            finally { await session.CloseAsync(); }
            if(result) return Ok("User: " + user.FirstName + " " + user.LastName + " updated successfully!");
            return BadRequest("Something went wrong updating the user!");
        }

        [Route("DeleteUserAccount/{email}/{password}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteUserAccount(string email, string password)
        {
            IAsyncSession session = _driver.AsyncSession();
            int result;
            try
            {
                result = await session.ExecuteWriteAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(@"
                        MATCH (u:UserAccount)
                        WHERE u.Email = $Email, u.Password = $Password
                        DETACH DELETE u
                    ", new { email, password });
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
