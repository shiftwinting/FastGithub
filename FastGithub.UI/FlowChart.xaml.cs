using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FastGithub.UI
{
    /// <summary>
    /// FlowChart.xaml 的交互逻辑
    /// </summary>
    public partial class FlowChart : UserControl
    {
        private readonly LineSeries readSeries = new LineSeries
        {
            Title = "上行速率",
            PointGeometry = null,
            Values = new ChartValues<double>()
        };

        private readonly LineSeries writeSeries = new LineSeries()
        {
            Title = "下行速率",
            PointGeometry = null,
            Values = new ChartValues<double>()
        };

        public SeriesCollection Series { get; } = new SeriesCollection();

        public List<string> Labels { get; } = new List<string>();

        public Func<double, string> YFormatter { get; } = value => $"{FlowRate.ToNetworkSizeString((long)value)}/s";

        public FlowChart()
        {
            InitializeComponent();

            this.Series.Add(this.readSeries);
            this.Series.Add(this.writeSeries);

            DataContext = this;
            this.InitFlowChart();
        }

        private async void InitFlowChart()
        {
            var httpClient = new HttpClient();
            while (true)
            {
                try
                {
                    await this.GetFlowRateAsync(httpClient);
                }
                catch (Exception)
                {
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d));
                }
            }
        }

        private async Task GetFlowRateAsync(HttpClient httpClient)
        {
            var response = await httpClient.GetAsync("http://127.0.0.1/flowRates");
            var json = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var flowRate = Newtonsoft.Json.JsonConvert.DeserializeObject<FlowRate>(json);

            this.textBlockRead.Text = FlowRate.ToNetworkSizeString(flowRate.TotalRead);
            this.textBlockWrite.Text = FlowRate.ToNetworkSizeString(flowRate.TotalWrite);

            this.readSeries.Values.Add(flowRate.ReadRate);
            this.writeSeries.Values.Add(flowRate.WriteRate);
            this.Labels.Add(DateTime.Now.ToString("HH:mm:ss"));

            if (this.Labels.Count > 60)
            {
                this.readSeries.Values.RemoveAt(0);
                this.writeSeries.Values.RemoveAt(0);
                this.Labels.RemoveAt(0);
            }
        }
    }
}
