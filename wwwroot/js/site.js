// Initialize SignalR connection for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

// Theme Management
const themeManager = {
    init: function() {
        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) {
            const currentTheme = localStorage.getItem('theme') || 'light';
            document.body.classList.toggle('high-contrast', currentTheme === 'high-contrast');
            themeToggle.setAttribute('aria-pressed', currentTheme === 'high-contrast');
            
            themeToggle.addEventListener('click', () => {
                const isHighContrast = document.body.classList.toggle('high-contrast');
                localStorage.setItem('theme', isHighContrast ? 'high-contrast' : 'light');
                themeToggle.setAttribute('aria-pressed', isHighContrast);
                notificationSystem.showNotification(
                    `Switched to ${isHighContrast ? 'high contrast' : 'light'} theme`,
                    'info'
                );
            });
        }
    }
};

// Notification System
const notificationSystem = {
    showNotification: function(message, type = 'info') {
        const notificationArea = document.querySelector('.notification-area');
        const notification = document.createElement('div');
        const id = `notification-${Date.now()}`;
        notification.id = id;
        notification.className = `notification-toast alert alert-${type} alert-dismissible fade show`;
        notification.setAttribute('role', 'alert');
        notification.setAttribute('aria-live', 'polite');
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" 
                    aria-label="Close notification" onclick="notificationSystem.removeNotification('${id}')"></button>
        `;
        notificationArea.appendChild(notification);

        // Announce notification for screen readers
        const announcer = document.createElement('span');
        announcer.className = 'visually-hidden';
        announcer.setAttribute('aria-live', 'polite');
        announcer.textContent = `New notification: ${message}`;
        notification.appendChild(announcer);

        // Auto-dismiss after 5 seconds
        setTimeout(() => this.removeNotification(id), 5000);

        // Ensure notifications are keyboard accessible
        const closeButton = notification.querySelector('.btn-close');
        closeButton.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                this.removeNotification(id);
            }
        });
    },

    removeNotification: function(id) {
        const notification = document.getElementById(id);
        if (notification) {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 150);
        }
    }
};

// SignalR Event Handlers
connection.on("ReceiveNotification", (message) => {
    notificationSystem.showNotification(message);
});

connection.on("TaskUpdated", (taskId, status, taskTitle) => {
    const taskElement = document.querySelector(`[data-task-id="${taskId}"]`);
    if (taskElement) {
        const statusElement = taskElement.querySelector('.task-status');
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `task-status badge bg-${getStatusColor(status)}`;
        }
        notificationSystem.showNotification(`Task "${taskTitle}" status updated to ${status}`, 'info');
    }
});

connection.on("ProjectCreated", (projectName) => {
    notificationSystem.showNotification(`New project created: ${projectName}`, 'success');
    // Refresh projects list if on projects page
    if (window.location.pathname.includes('/Projects')) {
        window.location.reload();
    }
});

connection.on("CommentAdded", (taskId, comment) => {
    const commentsList = document.querySelector(`#comments-${taskId}`);
    if (commentsList) {
        const commentElement = createCommentElement(comment);
        commentsList.appendChild(commentElement);
        notificationSystem.showNotification(`New comment added to task #${taskId}`, 'info');
    }
});

connection.on("FileUploaded", (taskId, fileName) => {
    const filesList = document.querySelector(`#files-${taskId}`);
    if (filesList) {
        updateFilesList(taskId);
        notificationSystem.showNotification(`File uploaded: ${fileName}`, 'success');
    }
});

// Search Functionality with Accessibility
let searchTimeout = null;
const searchInput = document.querySelector('#taskSearch');
const searchResults = document.querySelector('#searchResults');

if (searchInput && searchResults) {
    // Initialize search UI
    searchResults.setAttribute('aria-label', 'Search results');
    searchInput.setAttribute('aria-controls', 'searchResults');
    searchInput.setAttribute('aria-expanded', 'false');

    // Handle input events with debounce
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            const query = this.value.trim();
            if (query.length >= 2) {
                searchTasks(query);
            } else {
                clearSearchResults();
            }
        }, 300);
    });

    // Handle keyboard navigation
    searchInput.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            clearSearchResults();
            this.value = '';
        } else if (e.key === 'ArrowDown' && searchResults.style.display === 'block') {
            e.preventDefault();
            const firstResult = searchResults.querySelector('a');
            if (firstResult) firstResult.focus();
        }
    });

    // Close search results when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
            clearSearchResults();
        }
    });
}

function searchTasks(query) {
    fetch(`/Tasks/Search?q=${encodeURIComponent(query)}`)
        .then(response => response.text())
        .then(html => {
            if (searchResults) {
                searchResults.innerHTML = html || '<div class="p-2">No results found</div>';
                searchResults.style.display = 'block';
                searchInput.setAttribute('aria-expanded', 'true');

                // Make results keyboard navigable
                const links = searchResults.querySelectorAll('a');
                links.forEach(link => {
                    link.addEventListener('keydown', (e) => {
                        if (e.key === 'ArrowDown') {
                            e.preventDefault();
                            const next = e.target.parentElement.nextElementSibling;
                            if (next) next.querySelector('a').focus();
                        } else if (e.key === 'ArrowUp') {
                            e.preventDefault();
                            const prev = e.target.parentElement.previousElementSibling;
                            if (prev) {
                                prev.querySelector('a').focus();
                            } else {
                                searchInput.focus();
                            }
                        } else if (e.key === 'Escape') {
                            clearSearchResults();
                            searchInput.focus();
                        }
                    });
                });
            }
        })
        .catch(error => {
            console.error('Search error:', error);
            notificationSystem.showNotification('Error performing search', 'danger');
        });
}

function clearSearchResults() {
    if (searchResults) {
        searchResults.innerHTML = '';
        searchResults.style.display = 'none';
        searchInput.setAttribute('aria-expanded', 'false');
    }
}

// Task Status Updates
function updateTaskStatus(taskId, newStatus) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    fetch(`/Tasks/UpdateStatus/${taskId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ status: newStatus })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            const statusElement = document.querySelector(`[data-task-id="${taskId}"] .task-status`);
            if (statusElement) {
                statusElement.textContent = newStatus;
                statusElement.className = `task-status badge bg-${getStatusColor(newStatus)}`;
            }
            notificationSystem.showNotification('Task status updated successfully', 'success');
        } else {
            notificationSystem.showNotification(data.message || 'Failed to update task status', 'danger');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        notificationSystem.showNotification('An error occurred while updating task status', 'danger');
    });
}

// Comment System
function createCommentElement(comment) {
    const div = document.createElement('div');
    div.className = 'card mb-2';
    div.setAttribute('role', 'article');
    div.innerHTML = `
        <div class="card-body">
            <p class="card-text">${escapeHtml(comment.text)}</p>
            <small class="text-muted">
                ${comment.authorName} - ${new Date(comment.timestamp).toLocaleString()}
            </small>
        </div>
    `;
    return div;
}

function postComment(taskId, commentText) {
    if (!commentText.trim()) {
        notificationSystem.showNotification('Please enter a comment', 'warning');
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    fetch('/Tasks/AddComment', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ taskId, text: commentText })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            document.querySelector('#commentText').value = '';
            notificationSystem.showNotification('Comment posted successfully', 'success');
        } else {
            notificationSystem.showNotification(data.message || 'Failed to post comment', 'danger');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        notificationSystem.showNotification('An error occurred while posting the comment', 'danger');
    });
}

// File Upload System
function handleFileUpload(taskId, fileInput) {
    const files = fileInput.files;
    if (files.length === 0) return;

    const maxFileSize = 5 * 1024 * 1024; // 5MB
    const allowedTypes = ['image/jpeg', 'image/png', 'application/pdf', 'application/msword',
                         'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];

    // Validate files
    for (let i = 0; i < files.length; i++) {
        if (files[i].size > maxFileSize) {
            notificationSystem.showNotification(`File ${files[i].name} exceeds 5MB limit`, 'warning');
            return;
        }
        if (!allowedTypes.includes(files[i].type)) {
            notificationSystem.showNotification(`File ${files[i].name} type not allowed`, 'warning');
            return;
        }
    }

    const formData = new FormData();
    for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
    }
    formData.append('taskId', taskId);

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    fetch('/Tasks/UploadFiles', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            fileInput.value = '';
            updateFilesList(taskId);
            notificationSystem.showNotification('Files uploaded successfully', 'success');
        } else {
            notificationSystem.showNotification(data.message || 'Failed to upload files', 'danger');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        notificationSystem.showNotification('An error occurred while uploading files', 'danger');
    });
}

function updateFilesList(taskId) {
    fetch(`/Tasks/GetFiles/${taskId}`)
        .then(response => response.text())
        .then(html => {
            const filesList = document.querySelector(`#files-${taskId}`);
            if (filesList) {
                filesList.innerHTML = html;
                // Make file list keyboard accessible
                const downloadLinks = filesList.querySelectorAll('a[download]');
                downloadLinks.forEach(link => {
                    link.setAttribute('role', 'button');
                    link.addEventListener('keydown', (e) => {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            link.click();
                        }
                    });
                });
            }
        })
        .catch(error => {
            console.error('Error:', error);
            notificationSystem.showNotification('Error updating files list', 'danger');
        });
}

// Utility Functions
function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getStatusColor(status) {
    const statusColors = {
        'Not Started': 'secondary',
        'In Progress': 'primary',
        'Under Review': 'info',
        'Completed': 'success',
        'Blocked': 'danger'
    };
    return statusColors[status] || 'secondary';
}

// Initialize features
document.addEventListener('DOMContentLoaded', function() {
    // Initialize theme
    themeManager.init();

    // Initialize tooltips
    const tooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltips.forEach(tooltip => new bootstrap.Tooltip(tooltip));

    // Initialize popovers
    const popovers = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popovers.forEach(popover => new bootstrap.Popover(popover));

    // Start SignalR connection
    connection.start().catch(err => {
        console.error('SignalR Connection Error:', err);
        notificationSystem.showNotification('Failed to connect to real-time updates', 'danger');
    });
});
