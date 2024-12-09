namespace PatientVideoChatApp.Models
{
    public class VideoCallModal
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string? CallStatus { get; set; }
        public string? VideoCallLink { get; set; }
        public string? CallPassword { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }  
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
