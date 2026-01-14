document.addEventListener('click', function (e) {
  const btn = e.target.closest('.rate-btn');
  if (!btn) return;
  const itemId = btn.getAttribute('data-item-id');
  const helpful = btn.getAttribute('data-helpful') === 'true';
  fetch('/Help/RateAnswer', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ itemId, helpful })
  })
    .then(r => r.json())
    .then(data => {
      if (data && data.ok) {
        const label = document.getElementById('rating-' + itemId);
        if (label) label.textContent = `👍 ${data.helpful} · 👎 ${data.notHelpful}`;
      }
    })
    .catch(() => { /* ignore errors for now */ });
});

