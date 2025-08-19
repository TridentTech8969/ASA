(function () {
    let table;

    // SignalR connection for live updates
    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/email").build();

    connection.on("NewEmails", function (newEmails) {
        const badge = document.getElementById('badgeNew');
        badge.style.display = 'inline-block';
        setTimeout(() => { badge.style.display = 'none'; }, 3000);

        if (table) {
            table.ajax.reload(null, false); // Reload table data
        }
    });

    connection.start().catch(function (err) {
        console.error('SignalR connection error:', err);
    });

    $(document).ready(function () {
        table = $('#emailsTable').DataTable({
            ajax: {
                url: '/api/emails',
                dataSrc: function (json) {
                    console.log('API Response:', json); // Debug: see the actual data structure
                    return json.data || json; // Handle both {data: [...]} and [...] formats
                },
                error: function (xhr, error, code) {
                    console.error('DataTables Ajax error:', error, code);
                    console.error('Response:', xhr.responseText);
                }
            },
            deferRender: true,
            pageLength: 25,
            order: [[2, 'desc']], // Sort by received date desc
            columns: [
                {
                    data: null,
                    render: function (row) {
                        console.log('Row data:', row); // Debug: see individual row structure
                        return `<div>
                            <strong>${escapeHtml(row.fromName || row.FromName || row.fromEmail || row.FromEmail || 'Unknown')}</strong>
                            <div class='text-muted small'>${escapeHtml(row.fromEmail || row.FromEmail || '')}</div>
                        </div>`;
                    }
                },
                {
                    data: null,
                    render: function (row) {
                        const subject = row.subject || row.Subject || 'No Subject';
                        return `<span title='${escapeHtml(subject)}'>${escapeHtml(truncate(subject, 90))}</span>`;
                    }
                },
                {
                    data: null,
                    render: function (row) {
                        // Try different possible property names
                        const received = row.receivedLocal || row.ReceivedLocal ||
                            row.received || row.Received ||
                            row.receivedUtc || row.ReceivedUtc ||
                            'Unknown';
                        return escapeHtml(received);
                    }
                },
                {
                    data: null,
                    render: function (row) {
                        const unread = row.unread || row.Unread || false;
                        return unread ? '<span class="badge bg-warning text-dark">Unread</span>' : '';
                    }
                },
                {
                    data: null,
                    render: function (row) {
                        const labels = row.labels || row.Labels || [];
                        return labels.map(label =>
                            `<span class='badge bg-light text-dark me-1'>${escapeHtml(label)}</span>`
                        ).join('');
                    }
                },
                {
                    data: null,
                    render: function (row) {
                        const hasAttachments = row.hasAttachments || row.HasAttachments || false;
                        return hasAttachments ? '📎' : '';
                    }
                },
                {
                    data: null,
                    orderable: false,
                    render: function (row) {
                        const id = row.id || row.Id || '';
                        return `<button class='btn btn-sm btn-primary' onclick='openDetail("${id}")'>Open</button>`;
                    }
                }
            ]
        });

        // Search functionality
        $('#searchBox').on('input', function () {
            table.search(this.value).draw();
        });
    });

    // Global function to open email detail
    window.openDetail = function (id) {
        console.log('Opening detail for email ID:', id);

        fetch(`/api/emails/${encodeURIComponent(id)}`)
            .then(response => {
                console.log('Response status:', response.status);

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const contentType = response.headers.get('content-type');
                if (!contentType || !contentType.includes('application/json')) {
                    return response.text().then(text => {
                        console.error('Response is not JSON:', text);
                        throw new Error('Response is not JSON');
                    });
                }

                return response.json();
            })
            .then(emailData => {
                console.log('Email data received:', emailData);
                showDetail(emailData);
            })
            .catch(error => {
                console.error('Error fetching email detail:', error);
                alert('Error loading email details. Please check the console for more information.');
            });
    };

    function showDetail(emailData) {
        const detailElement = document.getElementById('emailDetail');

        const attachments = (emailData.Attachments || emailData.attachments || []).map(attachment =>
            `<li><a href='/api/emails/${encodeURIComponent(emailData.Id || emailData.id)}/attachment?fileName=${encodeURIComponent(attachment.FileName || attachment.fileName)}'>
                ${escapeHtml(attachment.FileName || attachment.fileName)}${attachment.SizeBytes || attachment.sizeBytes ? ` (${formatSize(attachment.SizeBytes || attachment.sizeBytes)})` : ''}
            </a></li>`
        ).join('');

        const htmlBody = emailData.HtmlBody || emailData.htmlBody || '';
        const textBody = emailData.TextBody || emailData.textBody || '';

        const body = htmlBody && htmlBody.trim().length > 0
            ? htmlBody
            : `<pre>${escapeHtml(textBody)}</pre>`;

        detailElement.innerHTML = `
            <div class='mb-2'>
                <div><strong>From:</strong> ${escapeHtml((emailData.FromName || emailData.fromName) || (emailData.FromEmail || emailData.fromEmail))} &lt;${escapeHtml(emailData.FromEmail || emailData.fromEmail)}&gt;</div>
                <div><strong>Subject:</strong> ${escapeHtml(emailData.Subject || emailData.subject)}</div>
                <div><strong>Received:</strong> ${escapeHtml(emailData.ReceivedLocal || emailData.receivedLocal)} IST</div>
                <div class='mt-1'>
                    ${((emailData.Labels || emailData.labels) || []).map(label =>
            `<span class='badge bg-light text-dark me-1'>${escapeHtml(label)}</span>`
        ).join('')}
                </div>
            </div>
            <hr/>
            <div class='mb-3' style='max-height:45vh; overflow:auto'>${body}</div>
            ${((emailData.Attachments || emailData.attachments) && (emailData.Attachments || emailData.attachments).length) ?
                `<div><strong>Attachments:</strong><ul>${attachments}</ul></div>` : ''}
        `;

        const offcanvas = new bootstrap.Offcanvas('#detailPane');
        offcanvas.show();
    }

    // Utility functions
    function truncate(str, maxLength) {
        return (str || '').length > maxLength ? (str.substring(0, maxLength - 1) + '…') : (str || '');
    }

    function escapeHtml(unsafe) {
        return (unsafe || '').replace(/[&<>"']/g, function (match) {
            const escapeMap = {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
            };
            return escapeMap[match];
        });
    }

    function formatSize(bytes) {
        if (!bytes) return '';
        const kb = bytes / 1024;
        if (kb < 1024) return kb.toFixed(1) + " KB";
        return (kb / 1024).toFixed(1) + " MB";
    }
})();