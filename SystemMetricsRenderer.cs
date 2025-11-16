using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TaskbarSystemMonitor
{
    public class SystemMetricsRenderer
    {
        private readonly Form _form;
        private readonly Queue<SystemMetricsData> _historyData;
        private readonly int _maxHistorySize = 60; // Keep 1 minute of data

        // Colors for different metrics
        private readonly Color _cpuColor = Color.FromArgb(255, 100, 149);      // Pink/Red
        private readonly Color _ramColor = Color.FromArgb(100, 255, 149);      // Green
        private readonly Color _diskColor = Color.FromArgb(149, 100, 255);     // Purple
        private readonly Color _networkColor = Color.FromArgb(255, 255, 100);  // Yellow
        private readonly Color _gpuColor = Color.FromArgb(100, 149, 255);      // Blue
        private readonly Color _textColor = Color.White;
        private readonly Color _backgroundColor = Color.FromArgb(180, 0, 0, 0); // Semi-transparent black

        // Layout constants
        private const int MARGIN = 2;
        private const int GRAPH_HEIGHT = 20;
        private const int TEXT_HEIGHT = 12;
        private const int METRIC_WIDTH = 58;

        public SystemMetricsRenderer(Form form)
        {
            _form = form;
            _historyData = new Queue<SystemMetricsData>();
        }

        public void UpdateMetrics(SystemMetricsData metrics)
        {
            _historyData.Enqueue(metrics);

            // Keep only the last N data points
            while (_historyData.Count > _maxHistorySize)
            {
                _historyData.Dequeue();
            }
        }

        public void Render(Graphics g)
        {
            if (_historyData.Count == 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var currentMetrics = _historyData.Last();

            // Draw background
            using (var backgroundBrush = new SolidBrush(_backgroundColor))
            {
                g.FillRectangle(backgroundBrush, 0, 0, _form.Width, _form.Height);
            }

            // Calculate layout
            int totalWidth = _form.Width - (MARGIN * 2);
            int yPos = MARGIN;

            // Draw each metric
            DrawMetric(g, "CPU", currentMetrics.CpuUsage, _cpuColor, 0, yPos, METRIC_WIDTH);
            DrawMetric(g, "RAM", currentMetrics.RamUsage, _ramColor, METRIC_WIDTH, yPos, METRIC_WIDTH);
            DrawMetric(g, "DISK", GetDiskUsage(currentMetrics), _diskColor, METRIC_WIDTH * 2, yPos, METRIC_WIDTH);
            DrawMetric(g, "NET", GetNetworkUsage(currentMetrics), _networkColor, METRIC_WIDTH * 3, yPos, METRIC_WIDTH);
            DrawMetric(g, "GPU", currentMetrics.GpuUsage, _gpuColor, METRIC_WIDTH * 4, yPos, METRIC_WIDTH);
        }

        private void DrawMetric(Graphics g, string label, float value, Color color, int x, int y, int width)
        {
            // Clamp value to 0-100 range
            value = Math.Max(0, Math.Min(100, value));

            // Draw metric graph
            DrawMiniGraph(g, label, value, color, x, y, width);
        }

        private void DrawMiniGraph(Graphics g, string label, float currentValue, Color color, int x, int y, int width)
        {
            using (var font = new Font("Segoe UI", 7, FontStyle.Bold))
            using (var textBrush = new SolidBrush(_textColor))
            using (var borderPen = new Pen(color, 1))
            {
                // Draw label
                g.DrawString(label, font, textBrush, x + 1, y);

                // Draw current value
                string valueText = $"{currentValue:F0}%";
                var valueSize = g.MeasureString(valueText, font);
                g.DrawString(valueText, font, textBrush, x + width - valueSize.Width - 1, y);

                // Draw mini graph area
                int graphY = y + TEXT_HEIGHT + 1;
                int graphWidth = width - 2;

                // Draw dark background
                using (var bgBrush = new SolidBrush(Color.FromArgb(80, 20, 20, 20)))
                {
                    g.FillRectangle(bgBrush, x + 1, graphY, graphWidth, GRAPH_HEIGHT);
                }

                // Draw border
                g.DrawRectangle(borderPen, x + 1, graphY, graphWidth, GRAPH_HEIGHT);

                // Draw filled area under the graph line first
                if (_historyData.Count > 1)
                {
                    DrawFilledGraphArea(g, color, x + 1, graphY, graphWidth, GRAPH_HEIGHT, GetMetricHistory(label));
                }

                // Draw the graph line on top of the filled area
                if (_historyData.Count > 1)
                {
                    DrawHistoryLine(g, color, x + 1, graphY, graphWidth, GRAPH_HEIGHT, GetMetricHistory(label));
                }

                // Draw current value indicator dot
                float currentY = graphY + GRAPH_HEIGHT - (GRAPH_HEIGHT * currentValue / 100.0f);
                using (var dotBrush = new SolidBrush(Color.FromArgb(255, color.R, color.G, color.B)))
                {
                    g.FillEllipse(dotBrush, x + graphWidth - 2, currentY - 2, 4, 4);
                }

                // Add highlight ring around the dot
                using (var ringPen = new Pen(Color.FromArgb(160, Color.White), 1))
                {
                    g.DrawEllipse(ringPen, x + graphWidth - 2, currentY - 2, 4, 4);
                }
            }
        }

        private void DrawFilledGraphArea(Graphics g, Color color, int x, int y, int width, int height, List<float> values)
        {
            if (values.Count < 2) return;

            var points = new List<PointF>();

            // Start from bottom left
            points.Add(new PointF(x, y + height));

            // Add all the data points
            for (int i = 0; i < values.Count; i++)
            {
                float xPos = x + (width * i / (float)Math.Max(1, values.Count - 1));
                float yPos = y + height - (height * values[i] / 100.0f);
                points.Add(new PointF(xPos, yPos));
            }

            // End at bottom right
            points.Add(new PointF(x + width, y + height));

            if (points.Count > 2)
            {
                // Create filled polygon with gradient
                Rectangle fillRect = new Rectangle(x, y, width, height);
                using (var gradientBrush = new LinearGradientBrush(
                    fillRect,
                    Color.FromArgb(120, color.R, color.G, color.B), // Semi-transparent at top
                    Color.FromArgb(40, color.R, color.G, color.B),  // More transparent at bottom
                    LinearGradientMode.Vertical))
                {
                    g.FillPolygon(gradientBrush, points.ToArray());
                }

                // Add subtle glossy overlay
                using (var glossBrush = new LinearGradientBrush(
                    new Rectangle(x, y, width, height / 2),
                    Color.FromArgb(30, Color.White),
                    Color.FromArgb(5, Color.White),
                    LinearGradientMode.Vertical))
                {
                    var topPoints = new List<PointF>();
                    topPoints.Add(new PointF(x, y + height));

                    for (int i = 0; i < values.Count; i++)
                    {
                        float xPos = x + (width * i / (float)Math.Max(1, values.Count - 1));
                        float yPos = y + height - (height * values[i] / 100.0f);
                        topPoints.Add(new PointF(xPos, Math.Max(y + height / 2, yPos)));
                    }

                    topPoints.Add(new PointF(x + width, y + height));

                    if (topPoints.Count > 2)
                    {
                        g.FillPolygon(glossBrush, topPoints.ToArray());
                    }
                }
            }
        }

        private void DrawHistoryLine(Graphics g, Color color, int x, int y, int width, int height, List<float> values)
        {
            if (values.Count < 2) return;

            using (var pen = new Pen(color, 1.5f))
            {
                var points = new List<PointF>();

                for (int i = 0; i < values.Count; i++)
                {
                    float xPos = x + (width * i / (float)Math.Max(1, values.Count - 1));
                    float yPos = y + height - (height * values[i] / 100.0f);
                    points.Add(new PointF(xPos, yPos));
                }

                if (points.Count > 1)
                {
                    g.DrawLines(pen, points.ToArray());
                }
            }
        }

        private List<float> GetMetricHistory(string metricName)
        {
            var values = new List<float>();

            foreach (var data in _historyData)
            {
                float value = metricName switch
                {
                    "CPU" => data.CpuUsage,
                    "RAM" => data.RamUsage,
                    "DISK" => GetDiskUsage(data),
                    "NET" => GetNetworkUsage(data),
                    "GPU" => data.GpuUsage,
                    _ => 0
                };

                values.Add(value);
            }

            return values;
        }

        private float GetDiskUsage(SystemMetricsData data)
        {
            // Combine read and write to show total disk activity
            // Scale to a reasonable percentage (adjust divisor as needed)
            float totalBytes = data.DiskRead + data.DiskWrite;
            return Math.Min(100, totalBytes / (10 * 1024 * 1024)); // Scale based on 10MB/s max
        }

        private float GetNetworkUsage(SystemMetricsData data)
        {
            // Combine sent and received to show total network activity
            // Scale to a reasonable percentage (adjust divisor as needed)
            float totalBytes = data.NetworkSent + data.NetworkReceived;
            return Math.Min(100, totalBytes / (1024 * 1024)); // Scale based on 1MB/s max
        }
    }
}