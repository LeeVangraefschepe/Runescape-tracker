document.getElementById('compareButton').addEventListener('click', () => {
    const selectedUser = document.getElementById('compareSelect').value;
    if (!selectedUser) {
        alert('Please select a user to compare.');
        return;
    }

    // Trigger your comparison logic here
    console.log('Comparing with:', selectedUser);

    // Get the currently active nav-link
    const activeElement = document.querySelector('.nav-link.active');
    if (!activeElement) {
        alert('No active user selected.');
        return;
    }

    // Get current user by element
    const target = activeElement.getAttribute('data-target');

    // Clear current charts
    clearCharts();
    
    fetchCompareData(target, selectedUser).then(data => {
        const skillData = processData(data);
        Object.entries(skillData).forEach(([skill, points]) => {
            drawChart(skill, points);
        });
    }).catch(err => {
        console.error(err);
    });
});