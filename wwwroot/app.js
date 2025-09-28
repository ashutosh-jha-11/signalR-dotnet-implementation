// Simple client UI and SignalR wiring.
// Comments added to guide future devs.

const connUrl = '/hubs/notifications';
let connection = null;
let currentUser = null;

const toastContainer = document.getElementById('toast-container');
const historyEl = document.getElementById('history');

// format date for display
function prettifyDate(d){
  try { return new Date(d).toLocaleString(); } catch { return d; }
}

// add notification to history panel
function addHistory(item){
  const el = document.createElement('div');
  el.className = 'item';
  el.innerHTML = `<div style="flex:1">
    <div class="title">${escapeHtml(item.title)}</div>
    <div class="msg">${escapeHtml(item.message)}</div>
  </div>
  <div style="margin-left:12px" class="meta">${prettifyDate(item.createdAt)}</div>`;
  historyEl.prepend(el);
}

// create an accessible toast with dismiss
function showToast(title, message, createdAt, id){
  const el = document.createElement('div');
  el.className = 'toast';
  el.setAttribute('role','status');
  el.innerHTML = `<div style="flex:1">
    <div class="title">${escapeHtml(title)}</div>
    <div class="meta">${escapeHtml(message)}</div>
  </div>
  <div style="display:flex;flex-direction:column;gap:6px">
    <button aria-label="Dismiss notification" data-id="${id}" class="dismiss">Dismiss</button>
  </div>`;
  toastContainer.appendChild(el);

  el.querySelector('.dismiss').addEventListener('click', async (e)=>{
    el.remove();
    try { await connection.invoke('AckNotification', id); } catch (err) { console.error(err); }
  });

  // auto remove after 12s
  setTimeout(()=> el.remove(), 12000);
}

// simple escaping for inserted HTML (avoid XSS)
function escapeHtml(unsafe){ return unsafe ? unsafe.replace(/[&<"']/g, function(m){return {'&':'&amp;','<':'&lt;','"':'&quot;',"'":"&#039;"}[m];}) : '' }

async function connectHub(user){
  if (!user) return;
  connection = new signalR.HubConnectionBuilder()
    .withUrl(connUrl + `?user=${encodeURIComponent(user)}`)
    .withAutomaticReconnect()
    .build();

  // when server pushes notifications
  connection.on('ReceiveNotification', payload => {
    addHistory(payload);
    showToast(payload.title, payload.message, payload.createdAt, payload.id);
    // ack the notification delivery so server can mark it delivered/seen as needed.
    connection.invoke('AckNotification', payload.id).catch(()=>{});
  });

  try {
    await connection.start();
    console.log('SignalR connected');
  } catch (err) {
    console.error('SignalR connection failed, retrying in 2s', err);
    setTimeout(()=>connectHub(user), 2000);
  }
}

// UI wiring: connect/disconnect + admin panel
document.getElementById('loginBtn').addEventListener('click', async ()=>{
  const u = document.getElementById('userId').value.trim();
  if(!u) return alert('Enter Player ID (e.g. player123)');
  currentUser = u;
  document.getElementById('loginBtn').classList.add('hidden');
  document.getElementById('logoutBtn').classList.remove('hidden');
  document.getElementById('userId').disabled = true;
  await connectHub(currentUser);
});

document.getElementById('logoutBtn').addEventListener('click', ()=>{
  currentUser = null;
  document.getElementById('loginBtn').classList.remove('hidden');
  document.getElementById('logoutBtn').classList.add('hidden');
  document.getElementById('userId').disabled = false;
  if(connection) connection.stop();
});

// Admin demo action: send to specific player
document.getElementById('sendToPlayer').addEventListener('click', async ()=>{
  const playerId = document.getElementById('targetPlayerId').value.trim();
  const title = document.getElementById('playerTitle').value.trim();
  const msg = document.getElementById('playerMsg').value.trim();
  if(!playerId || !title || !msg) return alert('Enter player ID, title and message');

  const res = await fetch('/api/notifications/send-to-player', {
    method:'POST',
    headers: { 'Content-Type':'application/json', 'X-ADMIN-KEY':'secret-admin-key' },
    body: JSON.stringify({ playerId, title, message: msg })
  });

  if(res.ok){
    alert(`Notification sent to player: ${playerId}`);
    document.getElementById('playerTitle').value='';
    document.getElementById('playerMsg').value='';
  } else {
    alert('Failed to send notification to player');
  }
});

// Admin demo action: send to multiple players
document.getElementById('sendToPlayers').addEventListener('click', async ()=>{
  const playerIdsStr = document.getElementById('multiPlayerIds').value.trim();
  const title = document.getElementById('multiTitle').value.trim();
  const msg = document.getElementById('multiMsg').value.trim();
  if(!playerIdsStr || !title || !msg) return alert('Enter player IDs, title and message');

  const playerIds = playerIdsStr.split(',').map(id => id.trim()).filter(id => id);
  if(playerIds.length === 0) return alert('Enter valid player IDs');

  const res = await fetch('/api/notifications/send-to-players', {
    method:'POST',
    headers: { 'Content-Type':'application/json', 'X-ADMIN-KEY':'secret-admin-key' },
    body: JSON.stringify({ playerIds, title, message: msg })
  });

  if(res.ok){
    alert(`Notification sent to ${playerIds.length} players`);
    document.getElementById('multiPlayerIds').value='';
    document.getElementById('multiTitle').value='';
    document.getElementById('multiMsg').value='';
  } else {
    alert('Failed to send notification to players');
  }
});

// Admin demo action: send broadcast
document.getElementById('sendAdmin').addEventListener('click', async ()=>{
  const title = document.getElementById('adminTitle').value.trim();
  const msg = document.getElementById('adminMsg').value.trim();
  if(!title || !msg) return alert('Enter title and message');

  const res = await fetch('/api/notifications/broadcast', {
    method:'POST',
    headers: { 'Content-Type':'application/json', 'X-ADMIN-KEY':'secret-admin-key' },
    body: JSON.stringify({ title, message: msg })
  });

  if(res.ok){
    alert('Broadcast sent to all connected players');
    document.getElementById('adminTitle').value='';
    document.getElementById('adminMsg').value='';
  } else {
    alert('Failed to send broadcast');
  }
});
