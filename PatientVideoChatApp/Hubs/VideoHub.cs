using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PatientVideoChatApp.IRepository;

namespace PatientVideoChatApp.Hubs
{
    public class VideoHub : Hub<IVideoHub>
    {
        private static readonly ConcurrentDictionary<string, (string PatientId, string DoctorId)> _connections = new();
        private readonly IApiRepository _apiRepository;

        public VideoHub(IApiRepository apiRepository)
        {
            _apiRepository = apiRepository;
        }

        public async Task JoinPatient(string patientID)
        {
            string userId = Context.ConnectionId;

            var videoCall = _apiRepository.GetVideoCallByPatientId(patientID);

            if (videoCall == null || videoCall.CallStatus != "pending")
            {
                await Clients.Caller.ShowError("Video call is not scheduled.");
                return;
            }

            _connections[userId] = (patientID, videoCall.DoctorId.ToString());

            await SendConfirmation(patientID);

            await Clients.User(videoCall.DoctorId.ToString()).NotifyParticipantJoined(patientID);
            await UpdateDoctorPatientList(videoCall.DoctorId.ToString());
        }

        public async Task JoinDoctor(string doctorID)
        {
            string userId = Context.ConnectionId;

            _connections[userId] = (null, doctorID);

            var patientList = _connections
                .Where(kvp => kvp.Value.DoctorId == doctorID && kvp.Value.PatientId != null)
                .Select(kvp => kvp.Value.PatientId)
                .ToList();

            await Clients.Caller.ReceivePatientList(patientList);
            await Clients.Caller.ConfirmJoin(doctorID);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = Context.ConnectionId;

            if (_connections.TryRemove(userId, out var userInfo))
            {
                var patientID = userInfo.PatientId;
                var doctorID = userInfo.DoctorId;

                if (patientID != null && doctorID != null)
                {
                    await Clients.User(doctorID).NotifyParticipantLeft(patientID);
                    await UpdateDoctorPatientList(doctorID);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendOffer(string offerJson)
        {
            var offerData = JsonSerializer.Deserialize<OfferData>(offerJson);
            if (offerData == null || string.IsNullOrEmpty(offerData.patientId))
            {
                await Clients.Caller.ShowError("Invalid offer data");
                return;
            }

            var patientConnection = _connections.FirstOrDefault(c => c.Value.PatientId == offerData.patientId && c.Value.DoctorId == offerData.doctorId).Key;

            if (patientConnection != null)
            {
                await Clients.Client(patientConnection).ReceiveOffer(offerJson);
            }
            else
            {
                await Clients.Caller.ShowError($"Patient {offerData.patientId} is not connected.");
            }
        }

        public async Task SendAnswer(string answerJson)
        {
            var answerData = JsonSerializer.Deserialize<AnswerData>(answerJson);
            if (answerData == null || string.IsNullOrEmpty(answerData.answer.sdp) || string.IsNullOrEmpty(answerData.doctorId))
            {
                await Clients.Caller.ShowError("Invalid answer data.");
                return;
            }

            var doctorConnection = _connections.FirstOrDefault(c => c.Value.PatientId == null && c.Value.DoctorId == answerData.doctorId).Key;

            if (doctorConnection != null)
            {
                await Clients.Client(doctorConnection).ReceiveAnswer(answerJson);
            }
            else
            {
                await Clients.Caller.ShowError($"Patient {answerData.patientId} is not connected.");
            }
        }

        public async Task SendICECandidate(string candidateJson)
        {
            var candidateData = JsonSerializer.Deserialize<IceCandidateData>(candidateJson);
            if (candidateData == null || string.IsNullOrEmpty(candidateData.doctorId))
            {
                await Clients.Caller.ShowError("Invalid ICE candidate data");
                return;
            }

            string targetConnectionId;

            if (!string.IsNullOrEmpty(candidateData.patientId))
            {
                targetConnectionId = _connections.FirstOrDefault(c =>
                    c.Value.PatientId == candidateData.patientId &&
                    c.Value.DoctorId == candidateData.doctorId).Key;
            }
            else
            {
                targetConnectionId = _connections.FirstOrDefault(c =>
                    c.Value.PatientId == null &&
                    c.Value.DoctorId == candidateData.doctorId).Key;
            }

            if (targetConnectionId != null)
            {
                await Clients.Client(targetConnectionId).ReceiveICECandidate(candidateJson);
            }
            else
            {
                await Clients.Caller.ShowError($"Target for ICE candidate is not connected.");
            }
        }

        private async Task SendConfirmation(string patientID)
        {
            await Clients.Caller.ConfirmJoin(patientID);
        }

        private async Task UpdateDoctorPatientList(string doctorID)
        {
            var doctorConnectionId = _connections
                .FirstOrDefault(kvp => kvp.Value.DoctorId == doctorID).Key;

            var patientList = _connections
                .Where(kvp => kvp.Value.DoctorId == doctorID && kvp.Value.PatientId != null)
                .Select(kvp => kvp.Value.PatientId)
                .ToList();

            await Clients.Client(doctorConnectionId).ReceivePatientList(patientList);
        }

        public async Task GetUpdatedPatientList(string doctorID)
        {
            var patientList = _connections
                .Where(kvp => kvp.Value.DoctorId == doctorID && kvp.Value.PatientId != null)
                .Select(kvp => kvp.Value.PatientId)
                .ToList();

            await Clients.Caller.ReceivePatientList(patientList);
        }
    }

    public class OfferData
    {
        public string patientId { get; set; }
        public string doctorId { get; set; }
        public string offer { get; set; }
    }

    public class AnswerData
    {
        public Answer answer { get; set; }
        public string patientId { get; set; }
        public string doctorId { get; set; }
    }

    public class Answer
    {
        public string type { get; set; }
        public string sdp { get; set; }
    }

    public class IceCandidateData
    {
        public string doctorId { get; set; } 
        public string patientId { get; set; }
        public string iceCandidate { get; set; }
    }

}
