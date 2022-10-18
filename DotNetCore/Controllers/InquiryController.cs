using Microsoft.AspNetCore.Mvc;

namespace DotNetCore.Controllers
{
    public class InquiryController : ControllerBase
    {
        [HttpGet("/healthz")]
        public string Init()
        {
            return "success";
        }
    }
}