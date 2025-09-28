// Member Portal JavaScript
let currentMember = null;
let connection = null;
let notifications = [];
let connectionStartTime = null;

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    initializeEventListeners();
    checkMemberStatus();
});

function initializeEventListeners() {
    // Member login
    document.getElementById('memberLoginForm').addEventListener('submit', handleMemberLogin);
    
    // Disconnect
    document.getElementById('disconnectBtn').addEventListener('click', handleDisconnect);
    
    // Notification controls
    document.getElementById('clearHistory').addEventListener('click', clearNotificationHistory);
    document.getElementById('markAllRead').addEventListener('click', markAllNotificationsRead);
}

function checkMemberStatus() {
    const memberData = localStorage.getItem('memberData');
    if (memberData) {
        currentMember = JSON.parse(memberData);
        showMemberDashboard();
    } else {
        showMemberLogin();
    }
}

function showMemberLogin() {
    document.getElementById('loginScreen').classList.remove('hidden');
    document.getElementById('memberDashboard').classList.add('hidden');
}

function showMemberDashboard() {
    document.getElementById('loginScreen').classList.add('hidden');
    document.getElementById('memberDashboard').classList.remove('hidden');
    
    // Update member info
    document.getElementById('memberDisplayName').textContent = currentMember.displayName || currentMember.playerId;
    document.getElementById('memberPlayerId').textContent = currentMember.playerId;
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Update connection info
    updateConnectionInfo();
    
    // Load member data
    loadMemberData();
}

async function handleMemberLogin(e) {
    e.preventDefault();
    
    const playerId = document.getElementById('playerId').value.trim();
    const displayName = document.getElementById('displayName').value.trim();
    
    if (!playerId) {
        showToast('Error', 'Please enter your Player ID', 'error');
        return;
    }
    
    currentMember = {
        playerId: playerId,
        displayName: displayName || playerId,
        connectedAt: new Date().toISOString()
    };
    
    localStorage.setItem('memberData', JSON.stringify(currentMember));
    showMemberDashboard();
}

function handleDisconnect() {
    currentMember = null;
    localStorage.removeItem('memberData');
    
    if (connection) {
        connection.stop();
    }
    
    // Clear notifications
    notifications = [];
    updateNotificationsDisplay();
    
    showMemberLogin();
}

async function initializeSignalR() {
    connectionStartTime = new Date();
    
    connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hubs/notifications?user=${encodeURIComponent(currentMember.playerId)}`)
        .withAutomaticReconnect()
        .build();
    
    // Listen for notifications
    connection.on('ReceiveNotification', (notification) => {
        console.log('Received notification:', notification);
        
        // Add to notifications list
        const notificationItem = {
            ...notification,
            receivedAt: new Date().toISOString(),
            isRead: false,
            isNew: true
        };
        
        notifications.unshift(notificationItem);
        updateNotificationsDisplay();
        updateNotificationStats();
        
        // Show toast notification
        showToast(notification.title, notification.message, 'notification');
        
        // Acknowledge the notification
        connection.invoke('AckNotification', notification.id);
        
        // Mark as not new after a delay
        setTimeout(() => {
            notificationItem.isNew = false;
            updateNotificationsDisplay();
        }, 5000);
    });
    
    connection.onreconnecting(() => {
        updateConnectionStatus('Reconnecting...');
    });
    
    connection.onreconnected(() => {
        updateConnectionStatus('Connected');
        showToast('Connection Restored', 'You are now reconnected', 'success');
    });
    
    connection.onclose(() => {
        updateConnectionStatus('Disconnected');
    });
    
    try {
        await connection.start();
        updateConnectionStatus('Connected');
        console.log('Member SignalR connected');
    } catch (error) {
        console.error('SignalR connection failed:', error);
        updateConnectionStatus('Connection Failed');
        showToast('Connection Error', 'Failed to connect to notification service', 'error');
    }
}

function updateConnectionStatus(status) {
    const statusText = document.querySelector('.status-text');
    const statusIndicator = document.querySelector('.status-indicator');
    
    if (statusText) {
        statusText.textContent = status;
    }
    
    if (statusIndicator) {
        statusIndicator.className = 'status-indicator';
        if (status === 'Connected') {
            statusIndicator.classList.add('online');
        } else {
            statusIndicator.classList.add('offline');
        }
    }
}

function updateConnectionInfo() {
    const connectedSince = document.getElementById('connectedSince');
    const lastActivity = document.getElementById('lastActivity');
    
    if (connectionStartTime) {
        connectedSince.textContent = formatTime(connectionStartTime);
    }
    
    lastActivity.textContent = 'Now';
    
    // Update connection time every minute
    setInterval(() => {
        if (connectionStartTime) {
            connectedSince.textContent = formatTime(connectionStartTime);
        }
    }, 60000);
    
    // Update last activity every 30 seconds
    setInterval(() => {
        lastActivity.textContent = 'Just now';
    }, 30000);
}

async function loadMemberData() {
    // In a real application, you would load member-specific data here
    // For now, we'll just update the displays
    updateNotificationsDisplay();
    updateNotificationStats();
    loadMemberGroups();
}

function loadMemberGroups() {
    // For demo purposes, show some sample groups
    const memberGroups = document.getElementById('memberGroups');
    
    // In a real app, you'd fetch this from the API
    const groups = [
        { name: 'VIP Players', color: '#gold', memberCount: 15 },
        { name: 'Active Players', color: '#blue', memberCount: 142 }
    ];
    
    if (groups.length > 0) {
        memberGroups.innerHTML = groups.map(group => `
            <div class="group-item" style="border-left-color: ${group.color}">
                <div>
                    <div class="group-name">${group.name}</div>
                    <div class="group-members-count">${group.memberCount} members</div>
                </div>
            </div>
        `).join('');
    } else {
        memberGroups.innerHTML = '<p style="color: #64748b; font-size: 0.9rem;">You are not in any groups yet.</p>';
    }
}

function updateNotificationsDisplay() {
    const notificationsList = document.getElementById('notificationsList');
    const emptyState = document.getElementById('emptyState');
    
    if (notifications.length === 0) {
        notificationsList.classList.add('hidden');
        emptyState.classList.remove('hidden');
    } else {
        notificationsList.classList.remove('hidden');
        emptyState.classList.add('hidden');
        
        notificationsList.innerHTML = notifications.map(notification => `
            <div class="notification-item ${!notification.isRead ? 'unread' : ''}" data-id="${notification.id}">
                <div class="notification-header">
                    <h4 class="notification-title">${escapeHtml(notification.title)}</h4>
                    <span class="notification-time">${formatRelativeTime(notification.receivedAt)}</span>
                </div>
                <p class="notification-message">${escapeHtml(notification.message)}</p>
                <div class="notification-actions">
                    ${!notification.isRead ? `<button class="mark-read-btn-small" onclick="markNotificationRead('${notification.id}')">Mark as Read</button>` : ''}
                    <button class="dismiss-btn" onclick="dismissNotification('${notification.id}')">Dismiss</button>
                </div>
            </div>
        `).join('');
    }
}

function updateNotificationStats() {
    const today = new Date().toDateString();
    const todayNotifications = notifications.filter(n => 
        new Date(n.receivedAt).toDateString() === today
    );
    
    document.getElementById('todayCount').textContent = todayNotifications.length;
    document.getElementById('totalCount').textContent = notifications.length;
    
    // Update connection time
    if (connectionStartTime) {
        const duration = Math.floor((new Date() - connectionStartTime) / 60000);
        document.getElementById('connectionTime').textContent = 
            duration < 1 ? 'Just now' : `${duration} min${duration > 1 ? 's' : ''}`;
    }
}

function markNotificationRead(notificationId) {
    const notification = notifications.find(n => n.id === notificationId);
    if (notification) {
        notification.isRead = true;
        updateNotificationsDisplay();
        updateNotificationStats();
    }
}

function dismissNotification(notificationId) {
    const index = notifications.findIndex(n => n.id === notificationId);
    if (index !== -1) {
        notifications.splice(index, 1);
        updateNotificationsDisplay();
        updateNotificationStats();
    }
}

function clearNotificationHistory() {
    if (notifications.length === 0) return;
    
    if (confirm('Are you sure you want to clear all notification history?')) {
        notifications = [];
        updateNotificationsDisplay();
        updateNotificationStats();
        showToast('History Cleared', 'All notifications have been removed', 'success');
    }
}

function markAllNotificationsRead() {
    const unreadCount = notifications.filter(n => !n.isRead).length;
    
    if (unreadCount === 0) {
        showToast('No Unread Notifications', 'All notifications are already read', 'info');
        return;
    }
    
    notifications.forEach(notification => {
        notification.isRead = true;
    });
    
    updateNotificationsDisplay();
    updateNotificationStats();
    showToast('All Read', `Marked ${unreadCount} notifications as read`, 'success');
}

// Utility functions
function formatTime(date) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatRelativeTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    return date.toLocaleDateString();
}

function escapeHtml(unsafe) {
    return unsafe ? unsafe.replace(/[&<"']/g, function(m) {
        return {'&':'&amp;','<':'&lt;','"':'&quot;',"'":"&#039;"}[m];
    }) : '';
}

function showToast(title, message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div style="flex:1">
            <div class="title">${escapeHtml(title)}</div>
            <div class="meta">${escapeHtml(message)}</div>
        </div>
        <button onclick="this.parentElement.remove()" aria-label="Dismiss notification">×</button>
    `;
    
    container.appendChild(toast);
    
    // Auto remove after 5 seconds for non-notification toasts
    if (type !== 'notification') {
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 5000);
    } else {
        // For notification toasts, remove after 10 seconds
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 10000);
    }
}

// Update last activity when user interacts
document.addEventListener('click', () => {
    document.getElementById('lastActivity').textContent = 'Now';
});

document.addEventListener('keypress', () => {
    document.getElementById('lastActivity').textContent = 'Now';
});
