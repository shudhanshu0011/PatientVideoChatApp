namespace PatientVideoChatApp.Hubs
{
    public interface IVideoHub
    {
        Task ConfirmJoin(string userId);
        Task ShowError(string errorMessage);
        Task NotifyParticipantJoined(string patientId);
        Task NotifyParticipantLeft(string patientId);
        Task ReceivePatientList(List<string> patientList);
        Task ReceiveOffer(string offerJson);
        Task ReceiveAnswer(string answerJson);
        Task ReceiveICECandidate(string candidateJson);
        public Task JoinPatient(string patientID);
        public Task JoinDoctor(string doctorID);
        public Task SendOffer(string offerJson);
        public Task SendAnswer(string answerJson);
        public Task SendICECandidate(string candidateJson);
        public Task SendConfirmation(string patientID);
        public Task UpdateDoctorPatientList(string doctorID);
        public Task GetUpdatedPatientList(string doctorID);
    }
}
