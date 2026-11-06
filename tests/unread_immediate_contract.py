"""Static check that reading a conversation immediately updates unread markers."""
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(x):return (R/x).read_text('utf-8')
app=read('frontend/src/main.jsx')
student=read('frontend/src/components/SupportInbox.jsx')
admin=read('admin/src/main.jsx')
inbox=read('admin/src/SupportAdminInbox.jsx')
checks={
'Frontend navbar refreshes after read event':"addEventListener('foundly:messages-read'" in app and 'loadNotifs().catch' in app,
'Handover room dot clears after confirmed read':'setRooms(current=>current.map(item=>item.id===id?{...item,hasUnread:false}:item))' in app,
'Reclicking selected handover triggers refresh':'if(active===r.id)loadActive(r.id)' in app,
'Student support marks opened thread read instantly':'setThreads(rows=>rows.map(row=>row.id===active?{...row,hasUnread:false}:row))' in student,
'Student support parent is notified after history GET':"dispatchEvent(new Event('foundly:messages-read'))" in student,
'Admin sidebar reacts to read event':"addEventListener('foundly:admin-support-read',check)" in admin,
'Admin thread read clears flag and notifies parent':"dispatchEvent(new Event('foundly:admin-support-read'))" in inbox and 'hasUnread:false' in inbox,
'Polling preserved on all screens':'setInterval(check,12000)' in admin and 'setInterval' in student and 'setInterval' in app,
}
for name,ok in checks.items():print(('PASS' if ok else 'FAIL')+': '+name)
assert all(checks.values()),'unread notification contract failed'
