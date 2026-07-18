// ats_dashboard.js - Handles ATS dashboard UI population
document.addEventListener('DOMContentLoaded', function () {
    // Check if data container exists (i.e., we have analysis results)
    var dataEl = document.getElementById('dataContainer');
    if (!dataEl) return;
    // Parse data attributes (stored as JSON strings where needed)
    var atsScore = parseFloat(dataEl.getAttribute('data-ats')) || 0;
    var confidence = parseFloat(dataEl.getAttribute('data-confidence')) || 0;
    var skills = JSON.parse(dataEl.getAttribute('data-skills') || '[]');
    var education = JSON.parse(dataEl.getAttribute('data-education') || '[]');
    var certifications = JSON.parse(dataEl.getAttribute('data-certifications') || '[]');
    var strengths = JSON.parse(dataEl.getAttribute('data-strengths') || '[]');
    var weaknesses = JSON.parse(dataEl.getAttribute('data-weaknesses') || '[]');
    var experience = JSON.parse(dataEl.getAttribute('data-experience') || '[]');
    var missingSkills = [];
    // Required .NET skill set (hard‑coded for demo)
    var required = ["C#", "\.NET", "ASP.NET", "ASP.NET Core", "Entity Framework", "SQL Server", "Azure", "Microservices", "Blazor", "Web API", "LINQ", "Azure DevOps"];
    required.forEach(function (req) {
        var found = skills.some(function (s) { return new RegExp(req, "i").test(s); });
        if (!found) missingSkills.push(req);
    });
    // Helper to set gauge
    function setGauge(id, value) {
        var gauge = document.querySelector('#' + id + ' .gauge-fill');
        var num = document.getElementById(id + 'Num');
        if (gauge) gauge.style.setProperty('--gp', Math.min(value, 100));
        if (num) num.textContent = Math.round(value);
    }
    setGauge('atsGauge', atsScore);
    setGauge('confidenceGauge', confidence);
    // Populate skill cloud
    var skillCloud = document.getElementById('skillCloud');
    if (skillCloud) {
        skills.forEach(function (s) {
            var span = document.createElement('span');
            span.className = 'skill-tag';
            span.textContent = s;
            skillCloud.appendChild(span);
        });
    }
    // Missing skills cloud
    var missingEl = document.getElementById('missingSkills');
    if (missingEl) {
        missingSkills.forEach(function (s) {
            var span = document.createElement('span');
            span.className = 'skill-tag';
            span.textContent = s;
            missingEl.appendChild(span);
        });
    }
    // Education list
    var eduList = document.getElementById('educationList');
    if (eduList) {
        education.forEach(function (item) {
            var li = document.createElement('li');
            li.textContent = item;
            eduList.appendChild(li);
        });
    }
    // Certifications list
    var certList = document.getElementById('certList');
    if (certList) {
        certifications.forEach(function (c) {
            var li = document.createElement('li');
            li.textContent = c;
            certList.appendChild(li);
        });
    }
    // Strengths / Weaknesses
    var strengthsList = document.getElementById('strengthsList');
    if (strengthsList) {
        strengths.forEach(function (s) {
            var li = document.createElement('li');
            li.innerHTML = '<i class="fas fa-check icon"></i>' + s;
            strengthsList.appendChild(li);
        });
    }
    var weaknessesList = document.getElementById('weaknessesList');
    if (weaknessesList) {
        weaknesses.forEach(function (w) {
            var li = document.createElement('li');
            li.innerHTML = '<i class="fas fa-times icon"></i>' + w;
            weaknessesList.appendChild(li);
        });
    }
    // Experience timeline (simple vertical list)
    var expTimeline = document.getElementById('expTimeline');
    if (expTimeline) {
        experience.forEach(function (exp) {
            var div = document.createElement('div');
            div.className = 'timeline-item';
            div.textContent = exp;
            expTimeline.appendChild(div);
        });
    }
    // Initialize Chart.js radar if canvas exists and skill scores available
    if (typeof Chart !== 'undefined') {
        var radarCanvas = document.getElementById('radarChart');
        if (radarCanvas && skills.length) {
            // For demo, create dummy skill scores (0‑100) based on presence
            var labels = skills.map(function (s) { return s; });
            var dataVals = skills.map(function () { return Math.random() * 80 + 20; });
            new Chart(radarCanvas, {
                type: 'radar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Skill Proficiency',
                        data: dataVals,
                        backgroundColor: 'rgba(16,185,129,0.2)',
                        borderColor: 'rgba(16,185,129,0.8)',
                        pointBackgroundColor: '#10B981'
                    }]
                },
                options: {
                    scales: { r: { beginAtZero: true, max: 100 } },
                    plugins: { legend: { display: false } }
                }
            });
        }
    }
});
