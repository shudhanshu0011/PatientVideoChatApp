using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PatientVideoChatApp.IRepository;
using PatientVideoChatApp.Models;
using System;
using System.Numerics;

namespace PatientVideoChatApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IApiRepository _apiRepository;
        private readonly string _baseUrl;

        public ApiController(IApiRepository apiRepository, IConfiguration configuration)
        {
            _apiRepository = apiRepository;
            _baseUrl = configuration.GetValue<string>("BaseUrl"); 
        }

        [HttpPost("schedule")]
        public IActionResult ScheduleVideoCall([FromBody] VideoCallRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request data is invalid.");
                }

                var patient = _apiRepository.GetPatientById(request.PatientId);
                var doctor = _apiRepository.GetDoctorById(request.DoctorId);

                if (patient == null || doctor == null)
                {
                    return BadRequest("Patient or Doctor not found.");
                }

                (string videoCallLink, string uniqueId) = GenerateVideoCallLink(request.PatientId, request.DoctorId);
                string callPassword = Generate6DigitPassword();

                var videoCall = new VideoCallModal
                {
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    StartTime = request.ScheduledTime.ToString(),
                    VideoCallLink = uniqueId,
                    CallPassword = callPassword,
                    CallStatus = "pending",
                    CreatedAt = DateTime.Now.ToString(),
                    UpdatedAt = DateTime.Now.ToString()
                };

                _apiRepository.AddVideoCall(videoCall);

                return Ok(new
                {
                    VideoCallLink = videoCallLink,
                    CallPassword = callPassword,
                    Message = "Video Call Scheduled Successfully!"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error scheduling video call: {ex.Message}");

                return StatusCode(500, new
                {
                    Message = "An error occurred while scheduling the video call.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("GetDoctors")]
        public ActionResult<IEnumerable<DoctorModal>> GetDoctors()
        {
            var doctors = _apiRepository.GetDoctors();
            return Ok(doctors);
        }

        [HttpGet("GetPatientsByDoctor/{doctorId}/patients")]
        public ActionResult<IEnumerable<PatientWithCallStatus>> GetPatientsByDoctor(int doctorId)
        {
            var patients = _apiRepository.GetPatientsByDoctor(doctorId);
            return Ok(patients);
        }

        private (string videoCallLink, string uniqueId) GenerateVideoCallLink(int patientId, int doctorId)
        {
            string uniqueId = $"{patientId}-{doctorId}-{DateTime.UtcNow.Ticks}";

            string videoCallLink = $"{_baseUrl}JoinVideoCall/{uniqueId}";

            return (videoCallLink, uniqueId);
        }

        private string Generate6DigitPassword()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

    }

    public class VideoCallRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string? ScheduledTime { get; set; }
    }
}
