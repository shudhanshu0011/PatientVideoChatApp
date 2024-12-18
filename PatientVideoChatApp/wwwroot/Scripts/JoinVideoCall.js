﻿$(document).ready(async function () {
    console.log("Document ready. Initializing...");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/video-chat")
        .build();

    let peerConnection = null;
    let localStream = null;
    let remoteStream = null

    try {
        await connection.start();
        console.log("Connected to SignalR successfully.");
        const doctorId = $('#doctorId').val();
        await initializeWebRTC(doctorId);
        await joinPatient();
    } catch (err) {
        console.error("SignalR connection error:", err.toString());
    }

    async function joinPatient() {
        const patientID = $("#patientId").val();
        console.log("Attempting to join patient with ID:", patientID);

        try {
            await connection.invoke("JoinPatient", patientID);
            console.log("JoinPatient invoked successfully for ID:", patientID);
        } catch (err) {
            console.error("Error invoking JoinPatient:", err.toString());
        }
    }

    connection.on("ConfirmJoin", (patientID) => {
        console.log("Confirmation received for Patient ID:", patientID);
    });

    connection.on("ReceiveOffer", async (offerData) => {
        console.log("Offer received from doctor:", offerData);

        const { offer, doctorId } = JSON.parse(offerData);
        
        await handleOffer(offer, doctorId);
    });

    async function handleOffer(offerJson, doctorId) {
        try {
            console.log("Handling received offer:", offerJson);

            const parsedOffer = typeof offerJson === "string" ? JSON.parse(offerJson) : offerJson;

            if (!parsedOffer || !parsedOffer.sdp || !parsedOffer.type) {
                throw new Error("Invalid offer object: Missing 'sdp' or 'type'.");
            }

            const rtcOffer = new RTCSessionDescription({
                type: parsedOffer.type,
                sdp: parsedOffer.sdp
            });

            await peerConnection.setRemoteDescription(rtcOffer);
            console.log("Remote description set successfully.");

            const answer = await peerConnection.createAnswer();
            console.log("Answer created:", answer);

            await peerConnection.setLocalDescription(answer);
            console.log("Local description set successfully. Sending answer...");

            const answerData = {
                answer: {
                    type: peerConnection.localDescription.type,
                    sdp: peerConnection.localDescription.sdp
                },
                patientId: $("#patientId").val(),
                doctorId: doctorId.toString()
            };

            await connection.invoke("SendAnswer", JSON.stringify(answerData));
            console.log("Answer sent successfully.");
        } catch (error) {
            console.error("Error handling offer:", error.message, error.stack);
        }
    }

    connection.on("ReceiveICECandidate", async (candidateData) => {
        console.log("Received ICE Candidate:", candidateData);

        const { candidate } = JSON.parse(candidateData);
        try {
            await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
            console.log("ICE Candidate added successfully.");
        } catch (error) {
            console.error("Error adding ICE Candidate:", error);
        }
    });

    async function initializeWebRTC(doctorId) {
        console.log("Initializing WebRTC...");

        peerConnection = new RTCPeerConnection({
            iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
        });

        console.log("PeerConnection created:", peerConnection);

        peerConnection.onicecandidate = async (event) => {
            if (event.candidate) {
                console.log("ICE Candidate generated:", event.candidate);
                const candidateData = {
                    candidate: event.candidate,
                    doctorId: doctorId
                };

                try {
                    await connection.invoke("SendICECandidate", JSON.stringify(candidateData));
                    console.log("ICE Candidate sent successfully.");
                } catch (err) {
                    console.error("Error sending ICE Candidate:", err);
                }
            }
        };

        peerConnection.ontrack = (event) => {
            console.log("Track received. Stream:", event.streams[0]);
            remoteStream = event.streams[0];
            $("#remoteVideo").get(0).srcObject = remoteStream;
        };

        try {
            console.log("Requesting media devices...");
            localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
            console.log("Local media stream obtained:", localStream);

            $("#localVideo").get(0).srcObject = localStream;

            localStream.getTracks().forEach((track) => {
                console.log("Adding track to PeerConnection:", track);
                peerConnection.addTrack(track, localStream);
            });
        } catch (error) {
            console.error("Error accessing media devices:", error);
        }
    }

    $("#sendMessageButton").click(() => {
        const message = $("#messageInput").val();
        if (dataChannel && message) {
            dataChannel.send(message);
            console.log("Message sent:", message);
        }
    });

    $("#endCallButton").click(() => {
        console.log("End call button clicked. Closing connection...");

        if (peerConnection) {
            peerConnection.close();
            console.log("PeerConnection closed.");
        }

        if (localStream) {
            localStream.getTracks().forEach((track) => {
                console.log("Stopping local track:", track);
                track.stop();
            });
        }

        $("#localVideo").get(0).srcObject = null;
        $("#remoteVideo").get(0).srcObject = null;

        console.log("Local and remote video elements cleared.");
    });
});