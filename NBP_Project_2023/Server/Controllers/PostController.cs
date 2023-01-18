using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using NBP_Project_2023.Shared;

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


        //[Route("CreatePost")]
        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created)]
        //public async Task<IActionResult> CreatePost(Post post) //[FromBody] se podrazumeva za složene tipove
        //{
        //    _driver.
        //}



    }
}
