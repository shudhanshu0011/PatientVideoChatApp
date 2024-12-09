using PatientVideoChatApp.Models;
using System.Numerics;

namespace PatientVideoChatApp.IRepository
{
    public interface IApiRepository
    {
        PatientModal GetPatientById(int patientId);

        DoctorModal GetDoctorById(int doctorId);

        IEnumerable<DoctorModal> GetDoctors();

        IEnumerable<PatientWithCallStatus> GetPatientsByDoctor(int doctorId);

        VideoCallModal GetVideoCallByLink(string link);

        void AddVideoCall(VideoCallModal videoCall);

        public VideoCallModal GetVideoCallByPatientId(string patientID);
    }
}
