using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Linq;
using System.Windows;

namespace SchedulingAssistantCSharp
{
    public partial class OptimizationProgressWindow : Window
    {
        public ChartValues<double> BestScores { get; set; }
        public ChartValues<double> Steps { get; set; }


        public Func<double, string> StepFormatter { get; set; }
        public Func<double, string> ScoreFormatter { get; set; }

        private LineSeries progressSeries;

        public OptimizationProgressWindow()
        {
            InitializeComponent();

            BestScores = new ChartValues<double>();
            Steps = new ChartValues<double>();

            progressSeries = new LineSeries
            {
                Title = "Best Score",
                Values = new ChartValues<double>(),
                PointGeometry = DefaultGeometries.Circle,
                StrokeThickness = 2,
                PointGeometrySize = 4,
                LineSmoothness = 1 // Sharp lines between points
            };

            ProgressChart.Series = new SeriesCollection { progressSeries };

            StepFormatter = val => $"{val:F1}";
            ScoreFormatter = val => $"{val:F1}";

            DataContext = this;
        }
        public void AddProgressPoint(double step, double bestScore)
        {
            Steps.Add(step);
            BestScores.Add(bestScore);

            // Update series (X = Temperature, Y = BestScore)
            progressSeries.Values.Add(bestScore);

            // Update X-axis range to clearly show decreasing temperature
            ProgressChart.AxisX[0].MinValue = Math.Max(step - 300, 0);
            ProgressChart.AxisX[0].MaxValue = step + 10;

            // Update Y-axis to fit best scores clearly
            ProgressChart.AxisY[0].MinValue = bestScore * 0.9;
            ProgressChart.AxisY[0].MaxValue = bestScore * 1.1;
        }
    }
}
