import { useCallback, useEffect, useRef, useState } from 'react';

// Issue #15: API adapter for Issue #13. UI belongs to Issue #14.
// All requests share the Vite proxy + session cookie used by Sprint 1.
export async function chatApi(path, options = {}) {
  const response = await fetch(`/api${path}`, {
    credentials: 'same-origin',
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers },
  });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) {
    const fallback = response.status === 401
      ? 'Vui lòng đăng nhập để sử dụng hộp thư.'
      : `Không thể kết nối (HTTP ${response.status}).`;
    throw new Error(body.message || fallback);
  }
  return body;
}

const sortContacts = contacts => [...contacts].sort((a, b) =>
  new Date(b.lastMessageAt || b.createdAt) - new Date(a.lastMessageAt || a.createdAt));

export function useChatLogic({ user, initialReport = null }) {
  const userId = user?.id || null;
  const reportId = initialReport?.id || null;
  const openedReport = useRef(null);
  const [contacts, setContacts] = useState([]);
  const [directory, setDirectory] = useState([]);
  const [activeId, setActiveId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [messageTotal, setMessageTotal] = useState(0);
  const [loadingContacts, setLoadingContacts] = useState(false);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [loadingDirectory, setLoadingDirectory] = useState(true);
  const [sending, setSending] = useState(false);
  const [contactError, setContactError] = useState('');
  const [messageError, setMessageError] = useState('');
  const [directoryError, setDirectoryError] = useState('');
  const [contactsRefresh, setContactsRefresh] = useState(0);
  const [messagesRefresh, setMessagesRefresh] = useState(0);

  const refreshContacts = useCallback(() => setContactsRefresh(n => n + 1), []);
  const refreshMessages = useCallback(() => setMessagesRefresh(n => n + 1), []);

  useEffect(() => {
    let cancelled = false;
    setLoadingDirectory(true);
    setDirectoryError('');
    chatApi('/directory').then(data => {
      if (!cancelled) setDirectory(Array.isArray(data.items) ? data.items : []);
    }).catch(error => { if (!cancelled) setDirectoryError(error.message); })
      .finally(() => { if (!cancelled) setLoadingDirectory(false); });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    if (!userId) {
      openedReport.current = null;
      setContacts([]);
      setActiveId(null);
      setMessages([]);
      setLoadingContacts(false);
      setContactError('');
      return () => { cancelled = true; };
    }
    setLoadingContacts(true);
    setContactError('');
    (async () => {
      try {
        const response = await chatApi('/contacts');
        let items = Array.isArray(response.items) ? response.items : [];
        let initialContactId = null;
        if (reportId && openedReport.current !== `${userId}:${reportId}`) {
          // Open only once per viewing session; a refresh must not reopen a closed report.
          // The server resolves the owner from reportId. Never send recipientId.
          try {
            const contact = await chatApi('/contacts', {
              method: 'POST', body: JSON.stringify({ reportId }),
            });
            if (cancelled) return;
            openedReport.current = `${userId}:${reportId}`;
            initialContactId = contact.id;
            items = [contact, ...items.filter(item => item.id !== contact.id)];
          } catch (error) {
            if (!cancelled) setContactError(error.message);
          }
        }
        if (cancelled) return;
        const ordered = sortContacts(items);
        setContacts(ordered);
        setActiveId(previous => initialContactId ||
          (ordered.some(item => item.id === previous) ? previous : ordered[0]?.id || null));
      } catch (error) {
        if (!cancelled) setContactError(error.message);
      } finally {
        if (!cancelled) setLoadingContacts(false);
      }
    })();
    return () => { cancelled = true; };
  }, [userId, reportId, contactsRefresh]);

  useEffect(() => {
    let cancelled = false;
    setMessages([]);
    setMessageTotal(0);
    setMessageError('');
    if (!userId || !activeId) {
      setLoadingMessages(false);
      return () => { cancelled = true; };
    }
    setLoadingMessages(true);
    chatApi(`/messages?contactId=${encodeURIComponent(activeId)}&page=1&pageSize=50`)
      .then(data => {
        if (cancelled) return;
        setMessages(Array.isArray(data.items) ? data.items : []);
        setMessageTotal(Number(data.total) || 0);
      }).catch(error => { if (!cancelled) setMessageError(error.message); })
      .finally(() => { if (!cancelled) setLoadingMessages(false); });
    return () => { cancelled = true; };
  }, [userId, activeId, messagesRefresh]);

  async function sendMessage(content) {
    const text = content.trim();
    if (!userId || !activeId || sending || text.length < 1 || text.length > 1500) return false;
    setSending(true);
    setMessageError('');
    try {
      await chatApi('/messages', {
        method: 'POST', body: JSON.stringify({ contactId: activeId, content: text }),
      });
      // Refetch the authoritative response; no fake local messages or Date.now() IDs.
      refreshMessages();
      refreshContacts();
      return true;
    } catch (error) {
      setMessageError(error.message);
      return false;
    } finally {
      setSending(false);
    }
  }

  return {
    user, contacts, directory, activeId, setActiveId, messages, messageTotal,
    loadingContacts, loadingMessages, loadingDirectory, sending,
    contactError, messageError, directoryError,
    refreshContacts, refreshMessages, sendMessage,
  };
}
