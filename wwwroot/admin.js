// Admin Dashboard JavaScript
let currentAdmin = null;
let connection = null;
let members = [];
let groups = [];
let templates = [];

// Helper function for authenticated API calls
function getAuthHeaders() {
    return {
        'Content-Type': 'application/json',
        'X-ADMIN-KEY': 'secret-admin-key'
    };
}

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    console.log('🚀 Admin application starting...');
    initializeEventListeners();
    checkAuthStatus();
    console.log('✅ Admin application initialized');
});

function initializeEventListeners() {
    // Login form
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    
    // Logout
    document.getElementById('logoutBtn').addEventListener('click', handleLogout);
    
    // Navigation
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', () => navigateToSection(item.dataset.section));
    });
    
    // Dashboard refresh
    document.getElementById('refreshMembers')?.addEventListener('click', loadMembers);
    
    // Notification sending
    document.getElementById('recipientType').addEventListener('change', toggleRecipientSelectors);
    document.getElementById('sendNotification').addEventListener('click', sendQuickNotification);
    
    // Template functions
    document.getElementById('templateRecipientType').addEventListener('change', toggleTemplateRecipientSelectors);
    document.getElementById('sendTemplate').addEventListener('click', sendTemplateNotification);
    document.getElementById('createTemplateBtn')?.addEventListener('click', () => showModal('createTemplateModal'));
    document.getElementById('createTemplateForm').addEventListener('submit', createTemplate);
    
    // Group functions
    document.getElementById('createGroupBtn')?.addEventListener('click', () => showModal('createGroupModal'));
    document.getElementById('createGroupForm').addEventListener('submit', createGroup);
    
    // History
    document.getElementById('refreshHistory')?.addEventListener('click', loadNotificationHistory);
    document.getElementById('historyDays')?.addEventListener('change', loadNotificationHistory);
}

function checkAuthStatus() {
    const adminData = localStorage.getItem('adminData');
    if (adminData) {
        currentAdmin = JSON.parse(adminData);
        showDashboard();
    } else {
        showLogin();
    }
}

function showLogin() {
    document.getElementById('loginScreen').classList.remove('hidden');
    document.getElementById('adminDashboard').classList.add('hidden');
}

function showDashboard() {
    document.getElementById('loginScreen').classList.add('hidden');
    document.getElementById('adminDashboard').classList.remove('hidden');
    document.getElementById('adminName').textContent = currentAdmin.displayName;
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Load initial data
    loadDashboardData();
}

async function handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    try {
        const response = await fetch('/api/admin/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        
        const result = await response.json();
        
        if (result.success) {
            currentAdmin = result.admin;
            localStorage.setItem('adminData', JSON.stringify(currentAdmin));
            showDashboard();
        } else {
            showToast('Login failed', 'Invalid username or password', 'error');
        }
    } catch (error) {
        showToast('Login error', 'Connection failed', 'error');
    }
}

function handleLogout() {
    currentAdmin = null;
    localStorage.removeItem('adminData');
    if (connection) {
        connection.stop();
    }
    showLogin();
}

async function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect()
        .build();
    
    // Listen for member connections/disconnections
    connection.on('MemberConnected', (data) => {
        console.log('Member connected:', data);
        updateMemberStatus(data.playerId, true);
        showToast('Member Connected', `${data.playerId} joined`, 'info');
    });
    
    connection.on('MemberDisconnected', (data) => {
        console.log('Member disconnected:', data);
        updateMemberStatus(data.playerId, false);
        showToast('Member Disconnected', `${data.playerId} left`, 'info');
    });
    
    try {
        await connection.start();
        await connection.invoke('JoinAdminGroup');
        console.log('Admin SignalR connected');
    } catch (error) {
        console.error('SignalR connection failed:', error);
    }
}

async function loadDashboardData() {
    console.log('Loading dashboard data...');
    await Promise.all([
        loadStats(),
        loadMembers(),
        loadGroups(),
        loadTemplates()
    ]);
    console.log('Dashboard data loaded');
}

async function loadStats() {
    try {
        const response = await fetch('/api/admin/stats', {
            headers: getAuthHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const stats = await response.json();
        
        document.getElementById('totalMembers').textContent = stats.TotalMembers || 0;
        document.getElementById('onlineMembers').textContent = stats.OnlineMembers || 0;
        document.getElementById('todayNotifications').textContent = stats.TodayNotifications || 0;
        document.getElementById('totalGroups').textContent = stats.TotalGroups || 0;
    } catch (error) {
        console.error('Failed to load stats:', error);
    }
}

async function loadMembers() {
    try {
        console.log('🔄 Loading members...');
        const response = await fetch('/api/admin/members', {
            headers: getAuthHeaders()
        });
        
        console.log('Members API response status:', response.status);
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        members = await response.json();
        console.log('✅ Loaded members:', members);
        console.log('📊 Total members count:', members.length);
        console.log('🟢 Online members:', members.filter(m => m.isOnline).length);
        
        updateMembersDisplay();
        updateOnlineMembersDisplay();
        updateMemberSelectors();
        
        console.log('✅ Member selectors updated');
    } catch (error) {
        console.error('❌ Failed to load members:', error);
        
        // Show error in UI
        const memberOptions = document.getElementById('memberOptions');
        if (memberOptions) {
            memberOptions.innerHTML = `
                <div style="padding: 10px; color: #dc3545; text-align: center;">
                    <strong>Error loading members</strong><br>
                    ${error.message}<br>
                    <button onclick="refreshMembersList()" style="margin-top: 8px; padding: 4px 8px; background: #dc3545; color: white; border: none; border-radius: 4px;">Retry</button>
                </div>
            `;
        }
    }
}

function updateMembersDisplay() {
    const container = document.getElementById('membersTable');
    if (!container) return;
    
    container.innerHTML = `
        <div class="table-header">
            <div>Member ID</div>
            <div>Display Name</div>
            <div>Status</div>
            <div>Last Activity</div>
        </div>
    `;
    
    members.forEach(member => {
        const row = document.createElement('div');
        row.className = 'table-row';
        row.innerHTML = `
            <div>${member.playerId}</div>
            <div>${member.displayName}</div>
            <div>
                <span class="member-status ${member.isOnline ? '' : 'offline'}"></span>
                ${member.isOnline ? 'Online' : 'Offline'}
            </div>
            <div>${formatDate(member.lastActivity)}</div>
        `;
        container.appendChild(row);
    });
}

function updateOnlineMembersDisplay() {
    const container = document.getElementById('onlineMembersList');
    if (!container) return;
    
    const onlineMembers = members.filter(m => m.isOnline);
    
    container.innerHTML = onlineMembers.map(member => `
        <div class="member-item">
            <div class="member-status"></div>
            <div class="member-info">
                <div class="member-name">${member.displayName}</div>
                <div class="member-id">${member.playerId}</div>
            </div>
        </div>
    `).join('');
}

// Multi-select functionality
let selectedMembers = [];
let multiSelectInitialized = false;

function updateMemberSelectors() {
    console.log('updateMemberSelectors called with', members.length, 'members');
    
    // Always re-initialize the multi-select to ensure it has the latest data
    initializeMultiSelect();
    
    // Keep the old single selectors for template functionality
    const templateSelector = document.getElementById('templateTargetMember');
    if (templateSelector) {
        templateSelector.innerHTML = '<option value="">Select Member...</option>' +
            members.map(m => `<option value="${m.playerId}">${m.displayName} (${m.playerId})</option>`).join('');
    }
}

function initializeMultiSelect() {
    const header = document.getElementById('memberSelectHeader');
    const dropdown = document.getElementById('memberDropdown');
    const optionsContainer = document.getElementById('memberOptions');
    const searchInput = document.getElementById('memberSearch');
    
    console.log('Initializing multi-select...', {
        header: !!header,
        dropdown: !!dropdown,
        optionsContainer: !!optionsContainer,
        searchInput: !!searchInput,
        membersCount: members.length
    });
    
    if (!header || !dropdown || !optionsContainer) {
        console.error('Multi-select elements not found!');
        return;
    }
    
    // Clear any existing event listeners by removing the onclick attribute
    header.onclick = null;
    
    // Populate options first
    populateMultiSelectOptions();
    
    // Header click handler
    header.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        console.log('Multi-select header clicked');
        dropdown.classList.toggle('hidden');
        header.classList.toggle('open');
    });
    
    // Search functionality
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            filterMultiSelectOptions(e.target.value);
        });
    }
    
    // Close dropdown when clicking outside
    document.addEventListener('click', (e) => {
        if (!header.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.classList.add('hidden');
            header.classList.remove('open');
        }
    });
}

function populateMultiSelectOptions() {
    const optionsContainer = document.getElementById('memberOptions');
    if (!optionsContainer) {
        console.error('❌ memberOptions container not found!');
        return;
    }
    
    console.log('🔄 Populating multi-select options...');
    console.log('📊 Members array:', members);
    console.log('📊 Members count:', members.length);
    
    optionsContainer.innerHTML = '';
    
    if (members.length === 0) {
        console.log('⚠️ No members to display');
        optionsContainer.innerHTML = `
            <div class="multi-select-empty">
                <div class="empty-icon">👥</div>
                <div class="empty-title">No Members Found</div>
                <div class="empty-description">
                    Connect some members first using the Member Portal,<br>
                    then refresh this list to see them here.
                </div>
                <button onclick="refreshMembersList()" class="refresh-btn">
                    🔄 Refresh Members
                </button>
            </div>
        `;
        return;
    }
    
    console.log('✅ Creating options for', members.length, 'members');
    
    // Sort members: online first, then by name
    const sortedMembers = [...members].sort((a, b) => {
        if (a.isOnline !== b.isOnline) {
            return b.isOnline - a.isOnline; // Online first
        }
        return a.displayName.localeCompare(b.displayName);
    });
    
    sortedMembers.forEach((member, index) => {
        console.log(`  - Creating option ${index + 1}: ${member.displayName} (${member.playerId}) - ${member.isOnline ? 'Online' : 'Offline'}`);
        
        const option = document.createElement('div');
        option.className = 'multi-select-option';
        option.dataset.playerId = member.playerId;
        
        const isSelected = selectedMembers.includes(member.playerId);
        if (isSelected) option.classList.add('selected');
        
        const lastActivity = member.lastActivity ? 
            new Date(member.lastActivity).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) : 
            'Unknown';
        
        option.innerHTML = `
            <input id="player-checkbox" type="checkbox" ${isSelected ? 'checked' : ''}>
            <div class="member-info">
                <div class="member-name">${member.displayName}</div>
                <div class="member-status">
                    ${member.playerId} • ${member.isOnline ? `🟢 Online • Last seen ${lastActivity}` : '🔴 Offline'}
                </div>
            </div>
            ${member.isOnline ? '<div class="online-indicator"></div>' : '<div class="offline-indicator"></div>'}
        `;
        
        option.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            toggleMemberSelection(member.playerId, member.displayName);
        });
        
        // Prevent checkbox from triggering the parent click
        const checkbox = option.querySelector('input[type="checkbox"]');
        checkbox.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleMemberSelection(member.playerId, member.displayName);
        });
        
        optionsContainer.appendChild(option);
    });
    
    console.log('✅ Multi-select options populated successfully');
    
    // Update header text to show member count
    updateMultiSelectDisplay();
}

function filterMultiSelectOptions(searchTerm) {
    const options = document.querySelectorAll('.multi-select-option');
    const term = searchTerm.toLowerCase();
    
    options.forEach(option => {
        const memberName = option.querySelector('.member-name').textContent.toLowerCase();
        const playerId = option.dataset.playerId.toLowerCase();
        
        if (memberName.includes(term) || playerId.includes(term)) {
            option.style.display = 'flex';
        } else {
            option.style.display = 'none';
        }
    });
}

function toggleMemberSelection(playerId, displayName) {
    const index = selectedMembers.indexOf(playerId);
    
    if (index > -1) {
        selectedMembers.splice(index, 1);
    } else {
        selectedMembers.push(playerId);
    }
    
    updateMultiSelectDisplay();
    updateSelectedMembersTags();
}

function updateMultiSelectDisplay() {
    const header = document.getElementById('memberSelectHeader');
    const placeholder = header?.querySelector('.placeholder');
    
    if (!placeholder) return;
    
    const totalMembers = members.length;
    const onlineMembers = members.filter(m => m.isOnline).length;
    
    if (selectedMembers.length === 0) {
        if (totalMembers === 0) {
            placeholder.textContent = 'No members available';
            placeholder.style.color = '#9ca3af';
        } else {
            placeholder.textContent = `Select from ${totalMembers} members (${onlineMembers} online)`;
            placeholder.style.color = '#6b7280';
        }
    } else if (selectedMembers.length === 1) {
        const member = members.find(m => m.playerId === selectedMembers[0]);
        const status = member?.isOnline ? '🟢' : '🔴';
        placeholder.textContent = member ? `${status} ${member.displayName}` : selectedMembers[0];
        placeholder.style.color = '#1f2937';
    } else {
        placeholder.textContent = `${selectedMembers.length} members selected`;
        placeholder.style.color = '#1f2937';
    }
    
    // Update visual state
    if (selectedMembers.length > 0) {
        header?.classList.add('has-selection');
    } else {
        header?.classList.remove('has-selection');
    }
    
    // Update checkboxes
    document.querySelectorAll('.multi-select-option').forEach(option => {
        const playerId = option.dataset.playerId;
        const checkbox = option.querySelector('input[type="checkbox"]');
        const isSelected = selectedMembers.includes(playerId);
        
        checkbox.checked = isSelected;
        option.classList.toggle('selected', isSelected);
    });
}

function updateSelectedMembersTags() {
    const container = document.getElementById('selectedMembers');
    if (!container) return;
    
    container.innerHTML = '';
    
    selectedMembers.forEach(playerId => {
        const member = members.find(m => m.playerId === playerId);
        if (!member) return;
        
        const tag = document.createElement('div');
        tag.className = 'selected-member-tag';
        tag.innerHTML = `
            ${member.displayName}
            <span class="remove" onclick="removeMemberSelection('${playerId}')">&times;</span>
        `;
        
        container.appendChild(tag);
    });
}

function removeMemberSelection(playerId) {
    const index = selectedMembers.indexOf(playerId);
    if (index > -1) {
        selectedMembers.splice(index, 1);
        updateMultiSelectDisplay();
        updateSelectedMembersTags();
    }
}

// Manual refresh function for debugging
async function refreshMembersList() {
    console.log('Manual refresh triggered');
    
    const button = event?.target?.closest('.refresh-members-btn') || document.querySelector('.refresh-members-btn');
    const icon = button?.querySelector('.refresh-icon');
    const text = button?.querySelector('.refresh-text');
    
    if (button) {
        button.classList.add('loading');
        button.disabled = true;
        if (text) text.textContent = 'Loading...';
    }
    
    try {
        await loadMembers();
        console.log('Members refreshed, count:', members.length);
        
        // Force re-initialization of multi-select
        initializeMultiSelect();
        
        // Show success state
        if (button) {
            button.classList.remove('loading');
            button.style.background = 'linear-gradient(135deg, #10b981 0%, #059669 100%)';
            if (text) text.textContent = 'Refreshed!';
            if (icon) icon.textContent = '✅';
            
            setTimeout(() => {
                if (text) text.textContent = 'Refresh';
                if (icon) icon.textContent = '🔄';
                button.disabled = false;
            }, 2000);
        }
        
    } catch (error) {
        console.error('Failed to refresh members:', error);
        
        // Show error state
        if (button) {
            button.classList.remove('loading');
            button.style.background = 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)';
            if (text) text.textContent = 'Error';
            if (icon) icon.textContent = '❌';
            
            setTimeout(() => {
                if (text) text.textContent = 'Refresh';
                if (icon) icon.textContent = '🔄';
                button.style.background = 'linear-gradient(135deg, #10b981 0%, #059669 100%)';
                button.disabled = false;
            }, 3000);
        }
    }
}

function updateMemberStatus(playerId, isOnline) {
    const member = members.find(m => m.playerId === playerId);
    if (member) {
        member.isOnline = isOnline;
        updateMembersDisplay();
        updateOnlineMembersDisplay();
        loadStats(); // Refresh stats
    }
}

async function loadGroups() {
    try {
        const response = await fetch('/api/admin/groups', {
            headers: getAuthHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        groups = await response.json();
        
        updateGroupsDisplay();
        updateGroupSelectors();
    } catch (error) {
        console.error('Failed to load groups:', error);
    }
}

function updateGroupsDisplay() {
    const container = document.getElementById('groupsContainer');
    if (!container) return;
    
    container.innerHTML = groups.map(group => `
        <div class="group-card" style="border-left-color: ${group.color}">
            <div class="group-header">
                <h4 class="group-name">${group.name}</h4>
            </div>
            <p class="group-description">${group.description}</p>
            <div class="group-members">${group.memberCount} members</div>
        </div>
    `).join('');
}

function updateGroupSelectors() {
    const selectors = ['targetGroup', 'templateTargetGroup'];
    
    selectors.forEach(selectorId => {
        const selector = document.getElementById(selectorId);
        if (selector) {
            selector.innerHTML = '<option value="">Select Group...</option>' +
                groups.map(g => `<option value="${g.id}">${g.name} (${g.memberCount} members)</option>`).join('');
        }
    });
}

async function loadTemplates() {
    try {
        const response = await fetch('/api/admin/templates', {
            headers: getAuthHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        templates = await response.json();
        
        updateTemplatesDisplay();
        updateTemplateSelector();
    } catch (error) {
        console.error('Failed to load templates:', error);
    }
}

function updateTemplatesDisplay() {
    const container = document.getElementById('templatesContainer');
    if (!container) return;
    
    container.innerHTML = templates.map(template => `
        <div class="template-card">
            <div class="template-header">
                <h4 class="template-name">${template.name}</h4>
                <span class="template-category">${template.category}</span>
            </div>
            <div class="template-content">
                <div class="template-title">${template.title}</div>
                <div class="template-message">${template.message}</div>
            </div>
            <div class="template-actions">
                <button class="edit-btn" onclick="editTemplate('${template.id}')">Edit</button>
                <button class="delete-btn" onclick="deleteTemplate('${template.id}')">Delete</button>
            </div>
        </div>
    `).join('');
}

function updateTemplateSelector() {
    const selector = document.getElementById('templateSelect');
    if (selector) {
        selector.innerHTML = '<option value="">Select Template...</option>' +
            templates.map(t => `<option value="${t.id}">${t.name} - ${t.title}</option>`).join('');
    }
}

function navigateToSection(sectionId) {
    // Update navigation
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.toggle('active', item.dataset.section === sectionId);
    });
    
    // Update content
    document.querySelectorAll('.content-section').forEach(section => {
        section.classList.toggle('active', section.id === sectionId);
    });
    
    // Load section-specific data
    switch (sectionId) {
        case 'dashboard':
            loadStats();
            break;
        case 'members':
            loadMembers();
            break;
        case 'groups':
            loadGroups();
            break;
        case 'templates':
            loadTemplates();
            break;
        case 'history':
            loadNotificationHistory();
            break;
    }
}

function toggleRecipientSelectors() {
    const type = document.getElementById('recipientType').value;
    const memberSelector = document.getElementById('memberSelector');
    const groupSelector = document.getElementById('groupSelector');
    
    memberSelector.classList.toggle('hidden', type !== 'member');
    groupSelector.classList.toggle('hidden', type !== 'group');
}

function toggleTemplateRecipientSelectors() {
    const type = document.getElementById('templateRecipientType').value;
    const memberSelector = document.getElementById('templateMemberSelector');
    const groupSelector = document.getElementById('templateGroupSelector');
    
    memberSelector.classList.toggle('hidden', type !== 'member');
    groupSelector.classList.toggle('hidden', type !== 'group');
}

async function sendQuickNotification() {
    const type = document.getElementById('recipientType').value;
    const title = document.getElementById('notificationTitle').value;
    const message = document.getElementById('notificationMessage').value;
    
    if (!title || !message) {
        showToast('Error', 'Please fill in title and message', 'error');
        return;
    }
    
    try {
        let endpoint = '';
        let body = { title, message };
        
        if (type === 'member') {
            if (selectedMembers.length === 0) {
                showToast('Error', 'Please select at least one member', 'error');
                return;
            }
            
            if (selectedMembers.length === 1) {
                // Single member
                endpoint = '/api/admin/notifications/send-to-member';
                body.playerId = selectedMembers[0];
            } else {
                // Multiple members
                endpoint = '/api/notifications/send-to-players';
                body.playerIds = selectedMembers;
            }
        } else if (type === 'group') {
            const groupId = document.getElementById('targetGroup').value;
            if (!groupId) {
                showToast('Error', 'Please select a group', 'error');
                return;
            }
            endpoint = '/api/admin/notifications/send-to-group';
            body.groupId = groupId;
        } else {
            endpoint = '/api/notifications/broadcast';
        }
        
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-ADMIN-KEY': 'secret-admin-key'
            },
            body: JSON.stringify(body)
        });
        
        const result = await response.json();
        
        if (result.success) {
            const memberCount = selectedMembers.length;
            const successMessage = type === 'member' 
                ? `Notification sent to ${memberCount} member${memberCount > 1 ? 's' : ''}`
                : 'Notification sent successfully';
            showToast('Success', successMessage, 'success');
            
            // Clear form
            document.getElementById('notificationTitle').value = '';
            document.getElementById('notificationMessage').value = '';
            selectedMembers = [];
            updateMultiSelectDisplay();
            updateSelectedMembersTags();
        } else {
            showToast('Error', result.message || 'Failed to send notification', 'error');
        }
    } catch (error) {
        showToast('Error', 'Failed to send notification', 'error');
    }
}

async function sendTemplateNotification() {
    const templateId = document.getElementById('templateSelect').value;
    const type = document.getElementById('templateRecipientType').value;
    
    if (!templateId) {
        showToast('Error', 'Please select a template', 'error');
        return;
    }
    
    try {
        let body = { templateId };
        
        if (type === 'member') {
            const playerId = document.getElementById('templateTargetMember').value;
            if (!playerId) {
                showToast('Error', 'Please select a member', 'error');
                return;
            }
            body.playerIds = [playerId];
        } else if (type === 'group') {
            const groupId = document.getElementById('templateTargetGroup').value;
            if (!groupId) {
                showToast('Error', 'Please select a group', 'error');
                return;
            }
            body.groupId = groupId;
        }
        
        const response = await fetch('/api/admin/notifications/send-template', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Success', 'Template notification sent successfully', 'success');
        } else {
            showToast('Error', 'Failed to send template notification', 'error');
        }
    } catch (error) {
        showToast('Error', 'Failed to send template notification', 'error');
    }
}

async function createGroup(e) {
    e.preventDefault();
    
    const name = document.getElementById('groupName').value;
    const description = document.getElementById('groupDescription').value;
    const color = document.getElementById('groupColor').value;
    
    try {
        const response = await fetch('/api/admin/groups', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, description, color })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Success', 'Group created successfully', 'success');
            closeModal('createGroupModal');
            loadGroups();
            // Clear form
            document.getElementById('createGroupForm').reset();
        } else {
            showToast('Error', 'Failed to create group', 'error');
        }
    } catch (error) {
        showToast('Error', 'Failed to create group', 'error');
    }
}

async function createTemplate(e) {
    e.preventDefault();
    
    const name = document.getElementById('templateName').value;
    const category = document.getElementById('templateCategory').value;
    const title = document.getElementById('templateTitle').value;
    const message = document.getElementById('templateMessage').value;
    const metadataJson = document.getElementById('templateMetadata').value;
    
    try {
        const response = await fetch('/api/admin/templates', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, category, title, message, metadataJson })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Success', 'Template created successfully', 'success');
            closeModal('createTemplateModal');
            loadTemplates();
            // Clear form
            document.getElementById('createTemplateForm').reset();
        } else {
            showToast('Error', 'Failed to create template', 'error');
        }
    } catch (error) {
        showToast('Error', 'Failed to create template', 'error');
    }
}

async function deleteTemplate(templateId) {
    if (!confirm('Are you sure you want to delete this template?')) return;
    
    try {
        const response = await fetch(`/api/admin/templates/${templateId}`, {
            method: 'DELETE'
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Success', 'Template deleted successfully', 'success');
            loadTemplates();
        } else {
            showToast('Error', 'Failed to delete template', 'error');
        }
    } catch (error) {
        showToast('Error', 'Failed to delete template', 'error');
    }
}

async function loadNotificationHistory() {
    const days = document.getElementById('historyDays')?.value || 7;
    
    try {
        const response = await fetch(`/api/admin/notifications/history?days=${days}`);
        const notifications = await response.json();
        
        updateHistoryDisplay(notifications);
    } catch (error) {
        console.error('Failed to load notification history:', error);
    }
}

function updateHistoryDisplay(notifications) {
    const container = document.getElementById('historyTable');
    if (!container) return;
    
    container.innerHTML = `
        <div class="table-header">
            <div>Title</div>
            <div>Message</div>
            <div>Created</div>
            <div>Type</div>
        </div>
    `;
    
    notifications.forEach(notification => {
        const row = document.createElement('div');
        row.className = 'table-row';
        row.innerHTML = `
            <div>${notification.title}</div>
            <div>${notification.message}</div>
            <div>${formatDate(notification.createdAt)}</div>
            <div>${notification.isBroadcast ? 'Broadcast' : 'Targeted'}</div>
        `;
        container.appendChild(row);
    });
}

// Utility functions
function showModal(modalId) {
    document.getElementById(modalId).classList.remove('hidden');
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.add('hidden');
}

function formatDate(dateString) {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleString();
}

function showToast(title, message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div style="flex:1">
            <div class="title">${title}</div>
            <div class="meta">${message}</div>
        </div>
        <button onclick="this.parentElement.remove()">×</button>
    `;
    
    container.appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.remove();
        }
    }, 5000);
}
