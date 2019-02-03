// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Chart.js Helpers
const colors = [
    Chart.helpers.color('rgb(255, 99, 132)'),
    Chart.helpers.color('rgb(255, 159, 64)'),
    Chart.helpers.color('rgb(255, 205, 86)'),
    Chart.helpers.color('rgb(75, 192, 192)'),
    Chart.helpers.color('rgb(54, 162, 235)'),
    Chart.helpers.color('rgb(153, 102, 255)'),
    Chart.helpers.color('rgb(201, 203, 207)'),
    Chart.helpers.color('rgb(112, 138, 144)'),
    Chart.helpers.color('rgb(25, 25, 112)'),
    Chart.helpers.color('rgb(100, 149, 237)'),
    Chart.helpers.color('rgb(106, 90, 205)'),
    Chart.helpers.color('rgb(65, 105, 225)'),
    Chart.helpers.color('rgb(0, 191, 255)'),
    Chart.helpers.color('rgb(176, 196, 222)'),
    Chart.helpers.color('rgb(173, 216, 230)'),
    Chart.helpers.color('rgb(102, 205, 170)'),
    Chart.helpers.color('rgb(32, 178, 170)')
];

function getChartJsConfig(title, label) {
    return {
        type: 'line',
        data: {
            datasets: []
        },
        options: {
            title: {
                text: title
            },
            pan: {
                enabled: true,
                mode: 'x'
            },
            zoom: {
                enabled: false,
                mode: 'x'
            },
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                xAxes: [
                    {
                        type: 'time',
                        distribution: 'series',
                        time: {
                            unit: "hour",
                            displayFormats: {
                                millisecond: 'MM DD',
                                second: 'MM DD',
                                minute: 'MMM DD h:mm a',
                                hour: 'MMM DD h:mm a',
                                day: 'MM DD',
                                week: 'MM DD',
                                month: 'MM DD',
                                quarter: 'MM DD',
                                year: 'MM DD'
                            }
                        },
                        scaleLabel: {
                            display: true,
                            labelString: ''
                        }
                    }
                ],
                yAxes: [
                    {
                        ticks: {
                            beginAtZero: true,
                            stepSize: 1,
                            min: 0,
                            max: 1
                        },
                        scaleLabel: {
                            display: true,
                            labelString: label
                        }
                    }
                ]
            },
        }
    };
}

function getTimeChartForEnvironment(envName, poolUsageResults) {

    var config = getChartJsConfig(envName + " Usage", "Compute Cores");
    var datasets = [];
    var maxCores = 1;

    for (var i = 0; i < poolUsageResults.length; i++) {
        var poolUsage = poolUsageResults[i];
        var data = [];

        for (var j = 0; j < poolUsage.values.length; j++) {
            var metric = poolUsage.values[j];
            data.push(
                {
                    x:
                        metric.timestamp,
                    y:
                        metric.totalCores
                }
            );
            maxCores = Math.max(maxCores, metric.totalCores);
        }

        var color = colors[i % colors.length];
        var dataset =
        {
            label: poolUsage.poolName,
            backgroundColor: color.alpha(0.5).rgbString(),
            borderColor: color.rgbString(),
            data: data,
            pointRadius: 0,
            fill: false,
            lineTension: 0,
            borderWidth: 2
        };

        datasets.push(dataset);
    }

    config.data.datasets = datasets;
    config.options.scales.yAxes[0].ticks.stepSize = Math.max(1, Math.ceil(maxCores / 10));
    config.options.scales.yAxes[0].ticks.max = maxCores + Math.max(1, Math.ceil(maxCores / 20));

    return config;
}

function registerCheckboxEnabledFormSection(checkboxId, outerDivId) {
    var id = '#' + checkboxId;
    var divId = '#' + outerDivId;

    $(id).change(function () {
        if ($(id).is(':checked') === true) {
            console.log('checked');
            $(divId + ' :input').removeAttr('readonly');
        } else {
            $(divId + ' :input').attr('readonly', 'readonly');
            console.log('unchecked');
        }
    });

    $(id).change();
}