$(document).ready(function() {
    // Check if we're on a page with charts
    if ($('#monthlyChart').length || $('#priorityChart').length || $('#categoryChart').length || $('#userTicketsChart').length) {
        // Check if charts are already initialized by inline scripts
        if (window.chartsInitialized) {
            console.log('Charts already initialized by inline scripts, skipping dashboardCharts.js');
            return;
        }
        
        initializeCharts();
    }
    
    function initializeCharts() {
        // Initialize Monthly Chart if it exists
        if ($('#monthlyChart').length) {
            initializeMonthlyChart();
        }
        
        // Initialize Priority Chart if it exists
        if ($('#priorityChart').length) {
            initializePriorityChart();
        }
        
        // Initialize Category Chart if it exists
        if ($('#categoryChart').length) {
            initializeCategoryChart();
        }
        
        // Initialize User Tickets Chart if it exists (Admin dashboard only)
        if ($('#userTicketsChart').length) {
            initializeUserTicketsChart();
        }
    }
    
    function safeJsonParse(jsonString, defaultValue) {
        if (!jsonString || jsonString === '') {
            return defaultValue;
        }
        
        try {
            return JSON.parse(jsonString);
        } catch (e) {
            console.warn('Failed to parse JSON:', e);
            return defaultValue;
        }
    }
    
    function initializeMonthlyChart() {
        try {
            var $chart = $('#monthlyChart');
            if (!$chart.length) return;
            
            // Check if this chart is already initialized
            if ($chart.data('chart-initialized')) {
                console.log('Monthly chart already initialized, skipping');
                return;
            }
            
            var ctx = $chart[0].getContext('2d');
            var dataLabels = $chart.attr('data-labels');
            var dataOpened = $chart.attr('data-opened');
            var dataClosed = $chart.attr('data-closed');
            
            var labels = safeJsonParse(dataLabels, []);
            var openedData = safeJsonParse(dataOpened, []);
            var closedData = safeJsonParse(dataClosed, []);
            var isAdmin = $chart.attr('data-is-admin') === 'true';
            
            // If we couldn't parse the data and there's already inline chart initialization, skip
            if (labels.length === 0 && window.Chart && window.Chart.instances) {
                return;
            }
            
            var chartTitle = isAdmin ? 'Monthly Ticket Trends' : 'Your Monthly Ticket Activity';
            
            var monthlyChart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: isAdmin ? 'Opened Tickets' : 'Created Tickets',
                            data: openedData,
                            borderColor: 'rgba(75, 192, 192, 1)',
                            backgroundColor: 'rgba(75, 192, 192, 0.2)',
                            tension: 0.1,
                            fill: true
                        },
                        {
                            label: 'Closed Tickets',
                            data: closedData,
                            borderColor: 'rgba(153, 102, 255, 1)',
                            backgroundColor: 'rgba(153, 102, 255, 0.2)',
                            tension: 0.1,
                            fill: true
                        }
                    ]
                },
                options: {
                    responsive: true,
                    plugins: {
                        title: {
                            display: true,
                            text: chartTitle
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            title: {
                                display: true,
                                text: 'Number of Tickets'
                            }
                        }
                    }
                }
            });
            
            // Mark as initialized
            $chart.data('chart-initialized', true);
        } catch (error) {
            console.error('Error initializing monthly chart:', error);
        }
    }
    
    function initializePriorityChart() {
        try {
            var $chart = $('#priorityChart');
            if (!$chart.length) return;
            
            // Check if this chart is already initialized
            if ($chart.data('chart-initialized')) {
                console.log('Priority chart already initialized, skipping');
                return;
            }
            
            var ctx = $chart[0].getContext('2d');
            var dataLabels = $chart.attr('data-labels');
            var dataValues = $chart.attr('data-values');
            
            var labels = safeJsonParse(dataLabels, []);
            var data = safeJsonParse(dataValues, []);
            var isAdmin = $chart.attr('data-is-admin') === 'true';
            
            // If we couldn't parse the data and there's already inline chart initialization, skip
            if (labels.length === 0 && window.Chart && window.Chart.instances) {
                return;
            }
            
            var chartTitle = isAdmin ? 'Tickets by Priority' : 'Your Tickets by Priority';
            
            var priorityChart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: data,
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.7)',
                            'rgba(54, 162, 235, 0.7)',
                            'rgba(255, 206, 86, 0.7)',
                            'rgba(75, 192, 192, 0.7)'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        title: {
                            display: true,
                            text: chartTitle
                        },
                        legend: {
                            position: 'right'
                        }
                    }
                }
            });
            
            // Mark as initialized
            $chart.data('chart-initialized', true);
        } catch (error) {
            console.error('Error initializing priority chart:', error);
        }
    }
    
    function initializeCategoryChart() {
        try {
            var $chart = $('#categoryChart');
            if (!$chart.length) return;
            
            // Check if this chart is already initialized
            if ($chart.data('chart-initialized')) {
                console.log('Category chart already initialized, skipping');
                return;
            }
            
            var ctx = $chart[0].getContext('2d');
            var dataLabels = $chart.attr('data-labels');
            var dataValues = $chart.attr('data-values');
            
            var labels = safeJsonParse(dataLabels, []);
            var data = safeJsonParse(dataValues, []);
            var isAdmin = $chart.attr('data-is-admin') === 'true';
            
            // If we couldn't parse the data and there's already inline chart initialization, skip
            if (labels.length === 0 && window.Chart && window.Chart.instances) {
                return;
            }
            
            var chartTitle = isAdmin ? 'Top 5 Categories' : 'Your Top Categories';
            
            var categoryChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Number of Tickets',
                        data: data,
                        backgroundColor: 'rgba(54, 162, 235, 0.7)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        title: {
                            display: true,
                            text: chartTitle
                        },
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            title: {
                                display: true,
                                text: 'Number of Tickets'
                            }
                        }
                    }
                }
            });
            
            // Mark as initialized
            $chart.data('chart-initialized', true);
        } catch (error) {
            console.error('Error initializing category chart:', error);
        }
    }
    
    function initializeUserTicketsChart() {
        try {
            var $chart = $('#userTicketsChart');
            if (!$chart.length) return;
            
            // Check if this chart is already initialized
            if ($chart.data('chart-initialized')) {
                console.log('User tickets chart already initialized, skipping');
                return;
            }
            
            var ctx = $chart[0].getContext('2d');
            var dataLabels = $chart.attr('data-labels');
            var dataValues = $chart.attr('data-values');
            
            var labels = safeJsonParse(dataLabels, []);
            var data = safeJsonParse(dataValues, []);
            
            // If we couldn't parse the data and there's already inline chart initialization, skip
            if (labels.length === 0 && window.Chart && window.Chart.instances) {
                return;
            }
            
            var userTicketsChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Number of Tickets',
                        data: data,
                        backgroundColor: 'rgba(153, 102, 255, 0.7)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        title: {
                            display: true,
                            text: 'User Tickets Distribution'
                        },
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            title: {
                                display: true,
                                text: 'Number of Tickets'
                            }
                        }
                    }
                }
            });
            
            // Mark as initialized
            $chart.data('chart-initialized', true);
        } catch (error) {
            console.error('Error initializing user tickets chart:', error);
        }
    }
}); 