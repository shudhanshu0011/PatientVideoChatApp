using PatientVideoChatApp.DapperContexts;
using PatientVideoChatApp.IRepository;
using PatientVideoChatApp.Models;
using System.Data;
using Dapper;
using System.Linq;
using System.Numerics;

namespace PatientVideoChatApp.Repository
{
    public class ApiRepository : IApiRepository
    {
        private readonly DapperContext _dapperContext;

        public ApiRepository(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public PatientModal GetPatientById(int patientId)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = "SELECT * FROM patients WHERE Id = @PatientId";
                return connection.Query<PatientModal>(query, new { PatientId = patientId }).FirstOrDefault();
            }
        }

        public DoctorModal GetDoctorById(int doctorId)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = "SELECT * FROM doctors WHERE Id = @DoctorId";
                return connection.Query<DoctorModal>(query, new { DoctorId = doctorId }).FirstOrDefault();
            }
        }

        public IEnumerable<DoctorModal> GetDoctors()
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = "SELECT * FROM doctors";
                return connection.Query<DoctorModal>(query).ToList();
            }
        }

        public IEnumerable<PatientWithCallStatus> GetPatientsByDoctor(int doctorId)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = @"
            SELECT 
                p.*, 
                vc.CallStatus AS VideoCallStatus, 
                vc.VideoCallLink AS VideoCallUrl 
            FROM patients p
            LEFT JOIN video_calls vc 
                ON p.id = vc.PatientId AND vc.DoctorId = @DoctorId AND vc.CallStatus = 'pending'
            WHERE p.DoctorId = @DoctorId";

                return connection.Query<PatientWithCallStatus>(query, new { DoctorId = doctorId }).ToList();
            }
        }

        public VideoCallModal GetVideoCallByLink(string link)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = @"
                    SELECT 
                        PatientId, 
                        DoctorId, 
                        CallStatus, 
                        VideoCallLink, 
                        CallPassword, 
                        DATE_FORMAT(StartTime, '%Y-%m-%d %H:%i:%s') AS StartTime, 
                        DATE_FORMAT(EndTime, '%Y-%m-%d %H:%i:%s') AS EndTime, 
                        DATE_FORMAT(CreatedAt, '%Y-%m-%d %H:%i:%s') AS CreatedAt, 
                        DATE_FORMAT(UpdatedAt, '%Y-%m-%d %H:%i:%s') AS UpdatedAt
                    FROM video_calls
                    WHERE VideoCallLink = @Link";

                return connection.QuerySingleOrDefault<VideoCallModal>(query, new { Link = link });
            }
        }

        public void AddVideoCall(VideoCallModal videoCall)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = @"
            INSERT INTO video_calls (PatientId, DoctorId, CallStatus, StartTime, EndTime, VideoCallLink, CallPassword)
            VALUES (@PatientId, @DoctorId, @CallStatus, @StartTime, @EndTime, @CallUrl, @CallPassword)";

                connection.Execute(query, new
                {
                    PatientId = videoCall.PatientId,
                    DoctorId = videoCall.DoctorId,
                    CallStatus = "pending",
                    StartTime = videoCall.StartTime.ToString(), 
                    EndTime = "0000-00-00 00:00:00",
                    CallUrl = videoCall.VideoCallLink,
                    CallPassword = videoCall.CallPassword
                });
            }
        }

        public VideoCallModal GetVideoCallByPatientId(string patientID)
        {
            using (IDbConnection connection = _dapperContext.CreateConnection())
            {
                string query = @"SELECT 
                                    PatientId, 
                                    DoctorId, 
                                    CallStatus, 
                                    VideoCallLink, 
                                    CallPassword, 
                                    DATE_FORMAT(StartTime, '%Y-%m-%d %H:%i:%s') AS StartTime, 
                                    DATE_FORMAT(EndTime, '%Y-%m-%d %H:%i:%s') AS EndTime, 
                                    DATE_FORMAT(CreatedAt, '%Y-%m-%d %H:%i:%s') AS CreatedAt, 
                                    DATE_FORMAT(UpdatedAt, '%Y-%m-%d %H:%i:%s') AS UpdatedAt
                                FROM video_calls WHERE PatientId = @PatientId AND CallStatus = 'pending'";
                return connection.QuerySingleOrDefault<VideoCallModal>(query, new { PatientId = patientID });
            }
        }

    }
}
