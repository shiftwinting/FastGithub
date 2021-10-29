using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Windows.Controls;

namespace FastGithub.UI
{
    /// <summary>
    /// LineChart.xaml 的交互逻辑
    /// </summary>
    public partial class LineChart : UserControl
    {
        public SeriesCollection SeriesCollection { get; } = new SeriesCollection();

        public string[] Labels { get; set; }

        public Func<double, string> YFormatter { get; set; }= value => value.ToString("0.00");

        public LineSeries ReadSeries { get; } = new LineSeries();

        public LineSeries WriteSeries { get; } = new LineSeries();

        public LineChart()
        { 
            InitializeComponent();
          
            this.SeriesCollection.Add(this.ReadSeries);
            this.SeriesCollection.Add(this.WriteSeries);

            DataContext = this;
        }


    }
}
