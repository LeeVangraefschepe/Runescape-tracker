async function fetchData(username) {
    const host = window.location.hostname;
    const res = await fetch(`http://${host}:999/api/skillvalues?username=${username}`);
    return await res.json();
}

async function fetchCompareData(username, otherUsername) {
    const host = window.location.hostname;
    const res = await fetch(`http://${host}:999/api/skilldifference?username=${username}&otherUser=${otherUsername}`);
    return await res.json();
}

function getRandomColor(seed) {
    const hash = Array.from(seed).reduce((acc, c) => acc + c.charCodeAt(0), 0);
    const hue = hash % 360;
    return `hsl(${hue}, 70%, 50%)`;
}

function clearCharts() {
    document.getElementById('charts').innerHTML = '';
}

function drawChart(skill, dataPoints) {
    const container = document.createElement('div');
    container.className = 'chart-container';
    const canvas = document.createElement('canvas');
    container.appendChild(canvas);
    const heading = document.createElement('h2');
    heading.textContent = skill;
    document.getElementById('charts').appendChild(heading);
    document.getElementById('charts').appendChild(container);



    const image = new Image();
    image.src = `https://runescape.wiki/images/${skill}_detail.png?0f0af`;
    image.crossOrigin = 'anonymous';

    image.onload = () => renderChart(image);
    image.onerror = () => renderChart(null);

    function renderChart(icon) {
        const backgroundIconPlugin = {
            id: 'backgroundIcon',
            afterDraw(chart) {
                if (!icon) return;
                const ctx = chart.ctx;
                const { chartArea } = chart;
                const size = 100;
                const x = (chartArea.left + chartArea.right) / 2 - size / 2;
                const y = (chartArea.top + chartArea.bottom) / 2 - size / 2;
                ctx.save();
                ctx.globalAlpha = 0.2;
                ctx.drawImage(icon, x, y, size, size);
                ctx.restore();
            }
        };

        new Chart(canvas, {
            type: 'line',
            data: {
                datasets: [{
                    label: `${skill} Total XP`,
                    data: dataPoints,
                    borderColor: getRandomColor(skill),
                    backgroundColor: 'rgba(0, 0, 0, 0.05)',
                    fill: true,
                    tension: 0.1,
                    pointRadius: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            tooltipFormat: 'MMM d, yyyy HH:mm',
                            unit: 'day'
                        },
                        ticks: { color: '#444' },
                        grid: { color: '#eee' }
                    },
                    y: {
                        title: { display: false, text: 'Total XP' },
                        beginAtZero: false,
                        ticks: { color: '#444' },
                        grid: { color: '#eee' }
                    }
                }
            },
            plugins: [backgroundIconPlugin]
        });
    }
}

function processData(rawData) {
    // Ensure entries are sorted
    rawData.sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));

    const skillMap = {};

    for (const entry of rawData) {
        const timestamp = entry.timestamp;
        for (const [skill, xp] of Object.entries(entry.skillXp)) {
            if (!skillMap[skill]) skillMap[skill] = [];
            skillMap[skill].push({
                x: timestamp,
                y: xp
            });
        }
    }

    return skillMap;
}