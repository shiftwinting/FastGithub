using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
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

        public Func<double, string> YFormatter { get; } = value => $"{value:0.00}";

        public FlowChart()
        {
            InitializeComponent();

            this.Series.Add(this.readSeries);
            this.Series.Add(this.writeSeries);

            DataContext = this;
        }

        public void Add(FlowRate flowRate)
        {
            this.readSeries.Values.Add(flowRate.ReadRate / 1024);
            this.writeSeries.Values.Add(flowRate.WriteRate / 1024);
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
