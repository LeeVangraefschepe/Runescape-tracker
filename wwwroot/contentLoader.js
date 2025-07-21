async function populateContent() {
    try {
        const sidebar = document.querySelector('.sidebar');
        
        // Dynamically build the URL based on current location
        const host = window.location.hostname;
        const apiUrl = `http://${host}:999/api/users`;

        const response = await fetch(apiUrl);
        if (!response.ok) throw new Error(`Failed to fetch: ${response.status}`);

        const usernames = await response.json();

        if (sidebar) {
            // Remove existing nav-links (optional if starting fresh)
            const existingLinks = sidebar.querySelectorAll('.nav-link');
            existingLinks.forEach(link => link.remove());

            // Create and append each user entry
            usernames.forEach(username => {
                const userDiv = document.createElement('div');
                userDiv.className = 'nav-link';
                userDiv.setAttribute('data-target', username);
                userDiv.setAttribute('onclick', 'switchSection(this)');

                const userImg = document.createElement('img');
                userImg.src = 'images/default_chat.png';
                userImg.className = 'nav-icon';

                userDiv.appendChild(userImg);
                userDiv.appendChild(document.createTextNode(username));

                sidebar.appendChild(userDiv);
            });
        }

        // Populate compare dropdown
        const compareSelect = document.getElementById('compareSelect');
        if (compareSelect) {
            compareSelect.innerHTML = '<option value="" disabled selected>Select a user</option>';
            usernames.forEach(username => {
                const option = document.createElement('option');
                option.value = username;
                option.textContent = username;
                compareSelect.appendChild(option);
            });
        }

    } catch (error) {
        console.error('Error loading sidebar users:', error);
    }
}

// Load users on DOMContentLoaded
document.addEventListener('DOMContentLoaded', populateContent);
