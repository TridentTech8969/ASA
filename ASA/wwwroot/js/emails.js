(function () {
    connection.start().catch(console.error);


    $(document).ready(function () {
        table = $('#emailsTable').DataTable({
            ajax: { url: '/api/emails', dataSrc: 'data' },
            deferRender: true,
            pageLength: 25,
            order: [[2, 'desc']],
            columns: [
                { data: null, render: r => `<div><strong>${escapeHtml(r.FromName || r.FromEmail)}</strong><div class='text-muted small'>${escapeHtml(r.FromEmail)}</div></div>` },
                { data: 'Subject', render: d => `<span title='${escapeHtml(d)}'>${escapeHtml(truncate(d, 90))}</span>` },
                { data: 'ReceivedLocal' },
                { data: 'Unread', render: d => d ? '<span class="badge bg-warning text-dark">Unread</span>' : '' },
                { data: 'Labels', render: arr => (arr || []).map(l => `<span class='badge bg-light text-dark me-1'>${escapeHtml(l)}</span>`).join('') },
                { data: 'HasAttachments', render: d => d ? '📎' : '' },
                { data: null, orderable: false, render: r => `<button class='btn btn-sm btn-primary' data-id='${r.Id}' onclick='openDetail("${r.Id}")'>Open</button>` }
            ]
        });


        $('#searchBox').on('input', function () { table.search(this.value).draw(); });
    });


    window.openDetail = function (id) {
        fetch(`/api/emails/${encodeURIComponent(id)}`)
            .then(r => r.json())
            .then(showDetail)
            .catch(console.error);
    }


    function showDetail(d) {
        const el = document.getElementById('emailDetail');
        const attachments = (d.Attachments || []).map(a =>
            `<li><a href='/api/emails/${encodeURIComponent(d.Id)}/attachment?fileName=${encodeURIComponent(a.FileName)}'>${escapeHtml(a.FileName)} (${formatSize(a.SizeBytes)})</a></li>`
        ).join('');


        const body = d.HtmlBody && d.HtmlBody.trim().length > 0 ? d.HtmlBody : `<pre>${escapeHtml(d.TextBody || '')}</pre>`;


        el.innerHTML = `
<div class='mb-2'>
<div><strong>From:</strong> ${escapeHtml(d.FromName || d.FromEmail)} &lt;${escapeHtml(d.FromEmail)}&gt;</div>
<div><strong>Subject:</strong> ${escapeHtml(d.Subject)}</div>
<div><strong>Received:</strong> ${escapeHtml(d.ReceivedLocal)} IST</div>
<div class='mt-1'>${(d.Labels || []).map(l => `<span class='badge bg-light text-dark me-1'>${escapeHtml(l)}</span>`).join('')}</div>
</div>
<hr/>
<div class='mb-3' style='max-height:45vh; overflow:auto'>${body}</div>
${(d.Attachments && d.Attachments.length) ? `<div><strong>Attachments:</strong><ul>${attachments}</ul></div>` : ''}
`;


        const offcanvas = new bootstrap.Offcanvas('#detailPane');
        offcanvas.show();
    }


    function truncate(s, n) { return (s || '').length > n ? (s.substring(0, n - 1) + '…') : (s || ''); }
    function escapeHtml(s) { return (s || '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', '\'': '&#39;' }[c])); }
    function formatSize(x) { if (!x) return ''; const kb = x / 1024; if (kb < 1024) return kb.toFixed(1) + " KB"; return (kb / 1024).toFixed(1) + " MB"; }
})();