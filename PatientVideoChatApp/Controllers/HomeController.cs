using Microsoft.AspNetCore.Mvc;
using PatientVideoChatApp.IRepository;
using PatientVideoChatApp.Models;
using System.Diagnostics;

namespace PatientVideoChatApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApiRepository _apiRepository;

        public HomeController(IApiRepository apiRepository)
        {
            _apiRepository = apiRepository;
        }

        [HttpGet("Home/JoinVideoCall/{linkID}")]
        public IActionResult JoinVideoCall(string linkID)
        {
            var videoCall = _apiRepository.GetVideoCallByLink(linkID);

            if (videoCall == null)
            {
                return NotFound("Video call not found.");
            }

            return View("JoinVideoCall", videoCall);
        }
    }
}
