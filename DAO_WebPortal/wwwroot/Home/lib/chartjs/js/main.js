
// Disable the on-canvas tooltip
Chart.defaults.pointHitDetectionRadius = 1;
Chart.defaults.plugins.tooltip.enabled = false;
Chart.defaults.plugins.tooltip.mode = 'index';
Chart.defaults.plugins.tooltip.position = 'nearest';
Chart.defaults.plugins.tooltip.external = coreui.ChartJS.customTooltips;
Chart.defaults.color = coreui.Utils.getStyle('--cui-body-color');
// console.log(Chart.defaults.color)

document.body.addEventListener('themeChange', () => {
    cardChart1.data.datasets[0].pointBackgroundColor = coreui.Utils.getStyle('--cui-primary');
    cardChart2.data.datasets[0].pointBackgroundColor = coreui.Utils.getStyle('--cui-info');
    cardChart1.update();
    cardChart2.update();


    auctionChart.options.scales.x.ticks.color = coreui.Utils.getStyle('--cui-body-color');
    auctionChart.options.scales.y.ticks.color = coreui.Utils.getStyle('--cui-body-color');
    auctionChart.update();

});
