// Initialize SignalR connection for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
            // Retry with exponential backoff, max 10 seconds
            return Math.min(retryContext.previousRetryCount * 1000, 10000);
        }
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();
    
// Dashboard update handler
connection.on('DashboardDataUpdated', function(projectId) {
    console.log('Dashboard update received', projectId ? `for project ${projectId}` : 'for all projects');
    updateDashboardData(projectId);
});

// Function to update dashboard data
function updateDashboardData(projectId) {
    // Only update if we're on the dashboard page
    if (!document.querySelector('.dashboard-container')) {
        return;
    }
    
    // Show loading indicator in the alerts container
    const alertsContainer = document.querySelector('.alerts-container');
    if (alertsContainer) {
        const loadingAlert = document.createElement('div');
        loadingAlert.className = 'alert alert-info alert-dismissible fade show';
        loadingAlert.setAttribute('role', 'alert');
        loadingAlert.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="spinner-border spinner-border-sm me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span>Updating dashboard data...</span>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        alertsContainer.appendChild(loadingAlert);
        
        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            if (loadingAlert && loadingAlert.parentNode) {
                const bsAlert = new bootstrap.Alert(loadingAlert);
                bsAlert.close();
            }
        }, 5000);
    }
    
    // Fetch updated dashboard data
    const url = projectId ? `/Home/GetDashboardData?projectId=${projectId}` : '/Home/GetDashboardData';
    
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Update summary cards
            updateSummaryCards(data);
            
            // Update charts
            updateTaskStatusChart(data.taskStatusSummary);
            updateProjectProgressChart(data.projectProgress);
            
            // Show success notification
            if (alertsContainer) {
                const successAlert = document.createElement('div');
                successAlert.className = 'alert alert-success alert-dismissible fade show';
                successAlert.setAttribute('role', 'alert');
                successAlert.innerHTML = `
                    <div class="d-flex align-items-center">
                        <i class="bi bi-check-circle-fill me-2"></i>
                        <span>Dashboard updated successfully!</span>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                `;
                alertsContainer.appendChild(successAlert);
                
                // Auto-dismiss after 5 seconds
                setTimeout(() => {
                    if (successAlert && successAlert.parentNode) {
                        const bsAlert = new bootstrap.Alert(successAlert);
                        bsAlert.close();
                    }
                }, 5000);
            }
        })
        .catch(error => {
            console.error('Error updating dashboard:', error);
            
            // Show error notification
            if (alertsContainer) {
                const errorAlert = document.createElement('div');
                errorAlert.className = 'alert alert-danger alert-dismissible fade show';
                errorAlert.setAttribute('role', 'alert');
                errorAlert.innerHTML = `
                    <div class="d-flex align-items-center">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i>
                        <span>Error updating dashboard. Please refresh the page.</span>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                `;
                alertsContainer.appendChild(errorAlert);
                
                // Auto-dismiss after 10 seconds
                setTimeout(() => {
                    if (errorAlert && errorAlert.parentNode) {
                        const bsAlert = new bootstrap.Alert(errorAlert);
                        bsAlert.close();
                    }
                }, 10000);
            }
        });
}

// Function to update summary cards
function updateSummaryCards(data) {
    // Update total projects
    const totalProjectsElement = document.querySelector('.total-projects');
    if (totalProjectsElement) {
        totalProjectsElement.textContent = data.totalProjects;
    }
    
    // Update total tasks
    const totalTasksElement = document.querySelector('.total-tasks');
    if (totalTasksElement) {
        totalTasksElement.textContent = data.totalTasks;
    }
    
    // Update completed tasks
    const completedTasksElement = document.querySelector('.completed-tasks');
    if (completedTasksElement) {
        completedTasksElement.textContent = data.completedTasks;
    }
    
    // Update completion rate
    const completionRateElement = document.querySelector('.completion-rate');
    if (completionRateElement) {
        completionRateElement.textContent = `${Math.round(data.completionRate)}%`;
    }
}

// Function to update task status chart
function updateTaskStatusChart(statusData) {
    const taskStatusChart = Chart.getChart('taskStatusChart');
    if (taskStatusChart) {
        taskStatusChart.data.datasets[0].data = [
            statusData.toDoCount,
            statusData.inProgressCount,
            statusData.underReviewCount,
            statusData.completedCount,
            statusData.blockedCount
        ];
        taskStatusChart.update();
    }
}

// Function to update project progress chart
function updateProjectProgressChart(projectsData) {
    const projectProgressChart = Chart.getChart('projectProgressChart');
    if (projectProgressChart) {
        projectProgressChart.data.labels = projectsData.map(p => p.projectTitle);
        projectProgressChart.data.datasets[0].data = projectsData.map(p => p.completionPercentage);
        projectProgressChart.update();
    }
}

// Theme Management
const themeManager = {
    init: function() {
        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) {
            const currentTheme = localStorage.getItem('theme') || 'light';
            
            // Set initial theme
            if (currentTheme === 'dark') {
                document.documentElement.setAttribute('data-theme', 'dark');
                document.body.classList.add('dark-theme');
            } else {
                document.documentElement.setAttribute('data-theme', 'light');
                document.body.classList.remove('dark-theme');
            }
            
            themeToggle.setAttribute('aria-pressed', currentTheme === 'dark');
            
            themeToggle.addEventListener('click', () => {
                const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
                const newTheme = isDarkTheme ? 'light' : 'dark';
                
                // Toggle theme
                document.documentElement.setAttribute('data-theme', newTheme);
                document.body.classList.toggle('dark-theme', newTheme === 'dark');
                
                localStorage.setItem('theme', newTheme);
                themeToggle.setAttribute('aria-pressed', newTheme === 'dark');
                
                notificationSystem.showNotification(
                    `Switched to ${newTheme} theme`,
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
connection.on("ReceiveNotification", (message, type) => {
    notificationSystem.showNotification(message, type || 'info');
});

// Handle project membership notifications
connection.on("ProjectMembershipChanged", (projectId, projectTitle, action) => {
    let message = '';
    let type = 'info';
    
    if (action === 'added') {
        message = `You were added to project: ${projectTitle}`;
        type = 'info';
    } else if (action === 'removed') {
        message = `You were removed from project: ${projectTitle}`;
        type = 'warning';
    }
    
    if (message) {
        const alert = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                <i class="bi bi-info-circle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        
        const alertsContainer = document.querySelector('.alerts-container');
        if (alertsContainer) {
            alertsContainer.innerHTML += alert;
        } else {
            notificationSystem.showNotification(message, type);
        }
    }
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

connection.on('CommentAdded', function (taskId, comment) {
    console.log('Comment added:', taskId, comment);

    // Get current user name
    const currentUserName = document.querySelector('.navbar .nav-link.text-dark')?.textContent.trim() || 'User';

    // Check if we're on the task details page or project page
    const isTaskDetailPage = document.querySelector('input[name="TaskId"]') !== null;
    const isProjectPage = document.querySelector('.project-page') !== null; // Add a specific class to your project page body if needed

    let targetContainer;

    if (isTaskDetailPage) {
        // Handle task detail page
        targetContainer = document.querySelector('.comments-container');
    }
    if (isProjectPage) {
        // Handle project page
        targetContainer = document.querySelector(`#task-${taskId} .comments-container`);

        // Update the comment count in the button for project page
        const commentButton = document.querySelector(`button[data-bs-target="#task-${taskId}"]`);
        if (commentButton) {
            const countText = commentButton.textContent;
            const count = parseInt(countText.match(/\d+/) || '0') + 1;
            commentButton.innerHTML = `<i class="bi bi-chat-dots"></i> View Comments (${count})`;
        }
    }

    if (targetContainer) {
        // Remove the 'no comments yet' message if it exists
        const noCommentsMessage = targetContainer.querySelector('.text-muted');
        if (noCommentsMessage) {
            const noCommentsText = noCommentsMessage.textContent.trim();
            if (noCommentsText === 'No comments yet') {
                if (noCommentsMessage.parentElement.classList.contains('text-center')) {
                    noCommentsMessage.parentElement.remove();
                } else {
                    noCommentsMessage.remove();
                }
            }
        }

        // Create and prepend the new comment
        const commentCard = createCommentElement(comment.text, comment.authorName, comment.timestamp);
        targetContainer.insertBefore(commentCard, targetContainer.firstChild);
    }

    // Show notification if comment was not made by current user
    if (comment.authorName !== currentUserName) {
        notificationSystem.showNotification(`New comment on task #${taskId} from ${comment.authorName}`, 'info');
    }

    // Announce for screen readers
    const announcer = document.createElement('div');
    announcer.setAttribute('aria-live', 'polite');
    announcer.className = 'visually-hidden';
    announcer.textContent = `New comment added by ${comment.authorName}`;
    document.body.appendChild(announcer);
    setTimeout(() => announcer.remove(), 3000);
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
function createCommentElement(text, authorName, timestamp) {
    const div = document.createElement('div');
    div.className = 'card mb-2';
    div.setAttribute('role', 'article');
    div.setAttribute('aria-label', `Comment by ${authorName}`);
    div.innerHTML = `
        <div class="card-body py-2 px-3">
            <p class="mb-1">${escapeHtml(text)}</p>
            <small class="text-muted">${authorName} - ${new Date(timestamp).toLocaleString()}</small>
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
    console.log('Posting comment to task:', taskId, 'Text:', commentText);
    
    fetch('/Tasks/AddComment', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ taskId, text: commentText })
    })
    .then(response => {
        console.log('Response status:', response.status);
        return response.json();
    })
    .then(data => {
        console.log('Response data:', data);
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

// Team Member Management
function initTeamMemberManagement() {
    // Add member form submission
    const addMemberForms = document.querySelectorAll('.add-member-form');
    addMemberForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            const projectId = this.getAttribute('data-project-id');
            const userId = this.querySelector('select[name="userId"]').value;
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            if (!userId) {
                notificationSystem.showNotification('Please select a user to add', 'warning');
                return;
            }
            
            fetch(`/Projects/AddMember/${projectId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: `userId=${encodeURIComponent(userId)}`
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to add team member');
                }
                return response.text();
            })
            .then(html => {
                // Reload the team members section
                const teamMembersSection = document.querySelector('.team-members-section');
                if (teamMembersSection) {
                    teamMembersSection.innerHTML = html;
                    // Re-initialize event handlers for the new content
                    initTeamMemberManagement();
                }
                notificationSystem.showNotification('Team member added successfully', 'success');
            })
            .catch(error => {
                console.error('Error:', error);
                notificationSystem.showNotification('Failed to add team member', 'danger');
            });
        });
    });
}

// Project Filter Functionality
function initProjectFilters() {
    // Handle project filter dropdowns
    const projectFilterForms = document.querySelectorAll('.project-filter-form');
    projectFilterForms.forEach(form => {
        const select = form.querySelector('select[name="projectId"]');
        if (select) {
            select.addEventListener('change', function() {
                form.submit();
            });
        }
    });
}

// Comment Form Handling
function initCommentForms() {
    const commentForms = document.querySelectorAll('.comment-form');
    commentForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            const taskId = this.querySelector('input[name="TaskId"]').value;
            const commentText = this.querySelector('textarea[name="Content"]').value;
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            if (!commentText.trim()) {
                notificationSystem.showNotification('Please enter a comment', 'warning');
                return;
            }
            
            // Disable submit button to prevent double submission
            const submitButton = this.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Posting...';
            
            fetch('/Comments/Create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ TaskId: taskId, Content: commentText })
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Clear the textarea
                    this.querySelector('textarea[name="Content"]').value = '';

                    // Don't add the comment here, let SignalR handle it
                    notificationSystem.showNotification('Comment posted successfully', 'success');

                    // Re-enable submit button
                    submitButton.disabled = false;
                    submitButton.innerHTML = '<i class="bi bi-chat-dots"></i> Post Comment';
                } else {
                    notificationSystem.showNotification(data.message || 'Failed to post comment', 'danger');
                }
                
                // Re-enable submit button
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="bi bi-chat-dots"></i> Post Comment';
            })
            .catch(error => {
                console.error('Error:', error);
                notificationSystem.showNotification('An error occurred while posting the comment', 'danger');
                
                // Re-enable submit button
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="bi bi-chat-dots"></i> Post Comment';
            });
        });
    });
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
    
    // Initialize team member management
    initTeamMemberManagement();
    
    // Initialize project filters
    initProjectFilters();
    
    // Initialize comment forms
    initCommentForms();

    // Start SignalR connection after a short delay to ensure auth is ready
    setTimeout(() => {
        startSignalRConnection();
    }, 1000);
});

// Function to start SignalR connection with retry logic
async function startSignalRConnection() {
    // Check if user is authenticated by looking for auth-related elements
    const isAuthenticated = document.querySelector('.logout-form') !== null;
    
    if (!isAuthenticated) {
        console.log('User not authenticated. SignalR connection not started.');
        return; // Don't attempt to connect if not authenticated
    }
    
    try {
        await connection.start();
        console.log('SignalR Connected.');
        notificationSystem.showNotification('Connected to real-time updates', 'success');
    } catch (err) {
        console.error('SignalR Connection Error:', err);
        
        let errorMessage = 'Failed to connect to real-time updates';
        if (err.message.includes('WebSocket failed to connect')) {
            errorMessage += '. WebSocket connection failed. Trying fallback transport...';
        } else if (err.message.includes('404')) {
            errorMessage += '. Server endpoint not found. Please refresh the page.';
        } else if (err.message.includes('401')) {
            errorMessage += '. Authentication required. Please sign in again.';
        }
        
        notificationSystem.showNotification(errorMessage, 'warning');
        
        // Retry after 5 seconds
        setTimeout(() => startSignalRConnection(), 5000);
    }
}

// Handle connection status changes
connection.onclose(async (error) => {
    if (error) {
        console.error('SignalR connection closed with error:', error);
    }
    notificationSystem.showNotification('Connection lost. Reconnecting...', 'warning');
    await startSignalRConnection();
});

// Handle reconnecting event
connection.onreconnecting(error => {
    console.log('SignalR reconnecting:', error);
    notificationSystem.showNotification('Reconnecting to real-time updates...', 'info');
});

// Handle reconnected event
connection.onreconnected(connectionId => {
    console.log('SignalR reconnected. Connection ID:', connectionId);
    notificationSystem.showNotification('Reconnected to real-time updates', 'success');
});

// Set active class on current nav link
document.addEventListener('DOMContentLoaded', function() {
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href')?.toLowerCase();
        if (href && (currentPath === href || currentPath.startsWith(href) && href !== '/')) {
            link.classList.add('active');
        }
    });
});
