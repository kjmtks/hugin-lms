
var chart = null;

function drawActivityActionChart(id, title, xs, ys, ylabel) {
    var c = document.getElementById(id);
    var parent = c.parentElement;
    var box = parent.getBoundingClientRect();
    c.setAttribute("width", box.width);
    c.setAttribute("height", 300);

    if (chart != null) {
        chart.destroy();
    }

    chart = new Chart(c.getContext('2d'), {
        type: 'line',
        data: {
            labels: xs,
            datasets: [
                {
                    data: ys,
                    borderColor: 'rgba(54, 162, 235, 1)',
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderWidth: 1
                },
            ]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: title
                },
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: ylabel
                    },
                    suggestedMin: 0,
                    suggestedMax: 20
                }
            }
        }
    });
}
