using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
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
            string query = @"
                CREATE (u:UserAccount {Email: $Email})
                SET u.FirstName = $FirstName,
                u.LastName = $LastName,
                u.Password = $Password,
                u.Street = $Street,
                u.StreetNumber = $StreetNumber,
                u.City = $City,
                u.PostalCode = $PostalCode,
                u.PhoneNumber = $PhoneNumber
            ";
            var parameters = new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.Password,
                user.Street,
                user.StreetNumber,
                user.City,
                user.PostalCode,
                user.PhoneNumber
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
            
            if (result == 1) return Ok("User registered successfully!");
            
            return BadRequest("User registration failed!");
        }

        [Route("GetUserAccount/{email}")]
        [HttpGet]
        public async Task<IActionResult> GetUserAccount(string email)
        {
            IAsyncSession session = _driver.AsyncSession();

            UserAccount result;
            string query = @"
                MATCH (u:UserAccount)
                WHERE u.Email = $email
                RETURN u
            ";
            var parameters = new { email };

            try
            {
                result = await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();
                    INode u = record["u"].As<INode>();
                    return new UserAccount
                    {
                        Id = Helper.GetIDfromINodeElementId(u.ElementId.As<string>()),
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
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("LogIn/{email}/{password}")]
        [HttpGet]
        public async Task<IActionResult> LogIn(string email, string password)
        {
            IAsyncSession session = _driver.AsyncSession();

            UserAccount result;
            string query = @"
                MATCH (u:UserAccount)
                WHERE u.Email = $email AND u.Password = $password
                RETURN u
            ";
            var parameters = new
            {
                email,
                password
            };

            try
            {
                 result = await session.ExecuteReadAsync(async tx =>
                 {
                     IResultCursor cursor = await tx.RunAsync(query, parameters);
                     IRecord record = await cursor.SingleAsync();
                     INode u = record["u"].As<INode>();
                     return new UserAccount
                     {
                         Id = Helper.GetIDfromINodeElementId(u.ElementId.As<string>()),
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
            finally
            {
                await session.CloseAsync();
            }
            
            if (result != null) return Ok(result);
            
            return BadRequest("This UserAccount doesn't exist!");
        }

        [Route("CheckIfUserAccountExists/{email}")]
        [HttpGet]
        public async Task<IActionResult> CheckIfUserAccountExists(string email)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result = false;
            string query = @"
                MATCH (u:UserAccount)
                WHERE u.Email = $email
                RETURN COUNT(u) > 0 AS node_exists
            ";
            var parameters = new { email };

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    IResultCursor cursor = await tx.RunAsync(query, parameters);
                    IRecord record = await cursor.SingleAsync();

                    if (record != null && record["node_exists"].As<int>() > 0)
                    {
                        result = true;
                    }
                });
            }
            finally
            {
                await session.CloseAsync();
            }
            
            return Ok(result);
        }

        [Route("EditUserAccount")]
        [HttpPut]
        public async Task<IActionResult> EditUserAccount(UserAccount user)
        {
            IAsyncSession session = _driver.AsyncSession();

            bool result;
            string query = @"
                MATCH (u:UserAccount)
                WHERE ID(u) = $Id
                SET u.FirstName = $FirstName,
                u.LastName = $LastName,
                u.Email = $Email,
                u.Password = $Password,
                u.Street = $Street,
                u.StreetNumber = $StreetNumber,
                u.City = $City,
                u.PostalCode = $PostalCode,
                u.PhoneNumber = $PhoneNumber
            ";
            var parameters = new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Password,
                user.Street,
                user.StreetNumber,
                user.City,
                user.PostalCode,
                user.PhoneNumber
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

            if (result) return Ok($"User: {user.FirstName} {user.LastName} updated successfully!");
            
            return BadRequest("Something went wrong updating the user!");
        }

        [Route("DeleteUserAccount/{email}/{password}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteUserAccount(string email, string password)
        {
            IAsyncSession session = _driver.AsyncSession();

            int result;
            string query = @"
                MATCH (u:UserAccount)
                WHERE u.Email = $email AND u.Password = $password
                DETACH DELETE u
            ";
            var parameters = new
            {
             email,
             password
            };

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
            
            if (result == 1) return Ok("User deleted successfully!");
            
            return BadRequest("Error deleting user!");
        }
    }
}
