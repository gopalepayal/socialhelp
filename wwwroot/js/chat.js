"use strict";

document.addEventListener("DOMContentLoaded", function () {
    const chatBody = document.getElementById("chatBody");
    const chatInput = document.getElementById("chatInput");
    const btnSend = document.getElementById("btnSend");
    
    if(!chatBody) return; // Not on the details page

    const donationId = document.getElementById("chatDonationId").value;
    const currentUserId = document.getElementById("chatUserId").value;
    const currentUserRole = document.getElementById("chatUserRole").value;
    const currentUserName = document.getElementById("chatUserName").value;

    // --- SignalR Connection ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.on("ReceiveMessage", function (rDonationId, senderId, senderName, senderRole, message, timestamp) {
        if (rDonationId.toString() === donationId) {
            appendMessage(senderId, senderRole, message, timestamp);
        }
    });

    connection.on("StatusUpdated", function (rDonationId, status) {
        if (rDonationId.toString() === donationId) {
            const statusEl = document.getElementById("pickupStatusBadge");
            if(statusEl) {
                statusEl.textContent = status;
                // Optional: show a toast toastr.info("Pickup status updated: " + status);
            }
        }
    });

    connection.start().then(function () {
        connection.invoke("JoinDonationGroup", parseInt(donationId)).catch(err => console.error(err));
        loadChatHistory();
    }).catch(function (err) {
        return console.error(err.toString());
    });

    // --- Load History ---
    function loadChatHistory() {
        fetch(`/api/Chat/${donationId}`)
            .then(res => res.json())
            .then(data => {
                chatBody.innerHTML = "";
                data.forEach(msg => {
                    appendMessage(msg.senderId, msg.senderRole, msg.message, msg.timestamp);
                });
            })
            .catch(err => console.error("Error loading chat:", err));
    }

    // --- Send Message ---
    btnSend.addEventListener("click", sendMessage);
    chatInput.addEventListener("keypress", function (e) {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    function sendMessage() {
        const message = chatInput.value.trim();
        if (!message) return;

        const chatMsg = {
            donationId: parseInt(donationId),
            senderId: currentUserId,
            senderRole: currentUserRole,
            message: message
        };

        fetch('/api/Chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(chatMsg)
        }).then(res => {
            if(res.ok) {
                chatInput.value = '';
                connection.invoke("SendMessage", parseInt(donationId), currentUserId, currentUserName, currentUserRole, message).catch(err => console.error(err));
            }
        });
    }

    // --- DOM Helpers ---
    function appendMessage(senderId, senderRole, message, timestamp) {
        const isMe = senderId === currentUserId && senderRole === currentUserRole;
        const msgDiv = document.createElement("div");
        msgDiv.className = "chat-message " + (isMe ? "me" : "other");
        
        msgDiv.innerHTML = `
            <div class="chat-meta">
                <span class="chat-role">${isMe ? 'You' : senderRole}</span>
                <span class="chat-time">${timestamp}</span>
            </div>
            <div class="chat-text">${message}</div>
        `;
        
        chatBody.appendChild(msgDiv);
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    // --- Map Integration (Leaflet) ---
    const mapEl = document.getElementById("pickupMap");
    if (mapEl) {
        const pLat = parseFloat(document.getElementById("pickupLat").value);
        const pLng = parseFloat(document.getElementById("pickupLng").value);
        const oLat = parseFloat(document.getElementById("orgLat").value);
        const oLng = parseFloat(document.getElementById("orgLng").value);

        if (!isNaN(pLat) && !isNaN(pLng)) {
            // Initialize Map
            const map = L.map('pickupMap');
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(map);

            const pickupMarker = L.marker([pLat, pLng]).addTo(map)
                .bindPopup('<b>Pickup Point</b><br>Donor Location')
                .openPopup();

            const markers = [L.latLng(pLat, pLng)];

            if (!isNaN(oLat) && !isNaN(oLng)) {
                const orgMarker = L.marker([oLat, oLng], {
                    icon: L.icon({
                        iconUrl: 'https://cdn-icons-png.flaticon.com/512/609/609803.png', // Home/Building icon
                        iconSize: [32, 32],
                        iconAnchor: [16, 32]
                    })
                }).addTo(map).bindPopup('<b>Organisation</b><br>Destination');
                
                markers.push(L.latLng(oLat, oLng));

                // Draw simple "Route" line
                const routeLine = L.polyline([
                    [pLat, pLng],
                    [oLat, oLng]
                ], {
                    color: '#6366f1',
                    weight: 3,
                    opacity: 0.6,
                    dashArray: '10, 10'
                }).addTo(map);

                map.fitBounds(L.latLngBounds(markers), { padding: [50, 50] });
            } else {
                map.setView([pLat, pLng], 14);
            }
        } else {
            mapEl.innerHTML = `<div class="d-flex h-100 align-items-center justify-content-center text-muted">
                <div class="text-center"><i class="bi bi-geo-alt-fill fs-1 text-secondary mb-2"></i><br/>Location coordinates not available.</div>
            </div>`;
        }
    }
});
