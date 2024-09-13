using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;

namespace ArchSitePlan.UI
{
    public class LVCTestViewModel : INotifyPropertyChanged
    {
        public SeriesCollection TestSeries { get; set; }
        public string X { get; set; } = "Test Binding";

        public LVCTestViewModel()
        {
            TestSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<double> { 4, 6, 5, 2 ,4 }
                },
                new LineSeries
                {
                    Title = "Series 2",
                    Values = new ChartValues<double> { 6, 7, 3, 4 ,6 },
                    PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Series 3",
                    Values = new ChartValues<double> { 4,2,7,2,7 },
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 15
                }
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
