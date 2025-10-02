using IotTgBot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using OxyPlot;
using OxyPlot.Annotations;

public class PlotService : IPlotService
{
    public Task<byte[]> PlotMetricAsync(List<SensorReading> data, string title, string metric, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (data == null || data.Count == 0)
            {
                return CreateEmptyPng(title);
            }

            var ordered = data.OrderBy(x => x.TimestampUtc).ToList();
            DateTime minTs = DateTime.SpecifyKind(ordered.First().TimestampUtc, DateTimeKind.Utc);
            DateTime maxTs = DateTime.SpecifyKind(ordered.Last().TimestampUtc, DateTimeKind.Utc);
            var span = maxTs - minTs;
            if (span == TimeSpan.Zero) span = TimeSpan.FromMinutes(1);
            var pad = TimeSpan.FromTicks((long)(span.Ticks * 0.05));
            var from = minTs - pad;
            var to = maxTs + pad;

            // pick selector and label
            Func<SensorReading, double> selector;
            string yLabel;
            switch (metric.ToLowerInvariant())
            {
                case "humidity":
                case "hum":
                case "h":
                    selector = r => r.HumidityPct;
                    yLabel = "Humidity (%)";
                    break;
                case "pressure":
                case "pres":
                case "p":
                    selector = r => r.PressureHpa;
                    yLabel = "Pressure (hPa)";
                    break;
                default:
                    selector = r => r.TemperatureC;
                    yLabel = "Temperature (°C)";
                    break;
            }

            var model = new PlotModel { Title = title };

            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = DateTimeAxis.ToDouble(from),
                Maximum = DateTimeAxis.ToDouble(to),
                IsPanEnabled = false,
                IsZoomEnabled = false
            };
            // date format depending on span
            if ((to - from) <= TimeSpan.FromDays(1)) dateAxis.StringFormat = "HH:mm";
            else if ((to - from) <= TimeSpan.FromDays(7)) dateAxis.StringFormat = "MM-dd\nHH:mm";
            else dateAxis.StringFormat = "yyyy-MM-dd";
            model.Axes.Add(dateAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = yLabel,
                IsPanEnabled = false,
                IsZoomEnabled = false
            };
            model.Axes.Add(valueAxis);

            var line = new LineSeries { Title = yLabel, StrokeThickness = 2, MarkerType = MarkerType.None };
            foreach (var d in ordered)
            {
                var ts = DateTime.SpecifyKind(d.TimestampUtc, DateTimeKind.Utc);
                line.Points.Add(new DataPoint(DateTimeAxis.ToDouble(ts), selector(d)));
            }
            model.Series.Add(line);

            return ExportToPng(model);
        }, ct);
    }

    public Task<byte[]> PlotCombinedAsync(List<SensorReading> data, string title, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (data == null || data.Count == 0) return CreateEmptyPng(title);

            var ordered = data.OrderBy(x => x.TimestampUtc).ToList();

            DateTime minTs = DateTime.SpecifyKind(ordered.First().TimestampUtc, DateTimeKind.Utc);
            DateTime maxTs = DateTime.SpecifyKind(ordered.Last().TimestampUtc, DateTimeKind.Utc);
            var span = maxTs - minTs;
            if (span == TimeSpan.Zero) span = TimeSpan.FromMinutes(1);
            var pad = TimeSpan.FromTicks((long)(span.Ticks * 0.05));
            var from = minTs - pad;
            var to = maxTs + pad;

            double tMin = ordered.Min(r => r.TemperatureC);
            double tMax = ordered.Max(r => r.TemperatureC);
            double hMin = ordered.Min(r => r.HumidityPct);
            double hMax = ordered.Max(r => r.HumidityPct);
            double pMin = ordered.Min(r => r.PressureHpa);
            double pMax = ordered.Max(r => r.PressureHpa);

            double padT = Math.Max(0.5, (tMax - tMin) * 0.1);
            double padH = Math.Max(0.5, (hMax - hMin) * 0.1);
            double padP = Math.Max(1.0, (pMax - pMin) * 0.05);

            var model = new PlotModel
            {
                Title = title,
                // увеличим правый отступ, чтобы правые оси и легенда помещались
                Padding = new OxyThickness(8, 8, 80, 8)
            };

            // X axis
            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = DateTimeAxis.ToDouble(from),
                Maximum = DateTimeAxis.ToDouble(to),
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
            };
            dateAxis.StringFormat = (to - from) <= TimeSpan.FromDays(1) ? "HH:mm" : "yyyy-MM-dd";
            model.Axes.Add(dateAxis);

            // Left axis: Temperature
            var tempAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Temperature (°C)",
                Minimum = tMin - padT,
                Maximum = tMax + padT,
                Key = "tempAxis",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };
            model.Axes.Add(tempAxis);

            // Right axis: Humidity (closer to plot)
            var humAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                Title = "Humidity (%)",
                Minimum = hMin - padH,
                Maximum = hMax + padH,
                Key = "humAxis",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                PositionTier = 0 // ближе к графику
            };
            var approxHumStep = (humAxis.Maximum - humAxis.Minimum) / 6.0;
            if (approxHumStep > 0) humAxis.MajorStep = Math.Round(approxHumStep, 1);
            model.Axes.Add(humAxis);

            // Right axis: Pressure (выведем наружу справа)
            var presAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                Title = "Pressure (hPa)",
                Minimum = pMin - padP,
                Maximum = pMax + padP,
                Key = "presAxis",
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                PositionTier = 1 // будет рисоваться правее humAxis
            };
            var approxPresStep = (presAxis.Maximum - presAxis.Minimum) / 6.0;
            if (approxPresStep > 0) presAxis.MajorStep = Math.Round(approxPresStep, 1);
            presAxis.StringFormat = "F1";
            model.Axes.Add(presAxis);

            // Temperature series (left axis)
            var tempSeries = new LineSeries
            {
                Title = "T °C",
                StrokeThickness = 2,
                Color = OxyColors.Green,
                YAxisKey = tempAxis.Key
            };
            foreach (var d in ordered)
                tempSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(d.TimestampUtc), d.TemperatureC));
            model.Series.Add(tempSeries);

            // Humidity series (right axis humAxis)
            var humSeries = new LineSeries
            {
                Title = "H %",
                StrokeThickness = 2,
                Color = OxyColors.Orange,
                YAxisKey = humAxis.Key
            };
            foreach (var d in ordered)
                humSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(d.TimestampUtc), d.HumidityPct));
            model.Series.Add(humSeries);

            // Pressure series (right axis presAxis)
            var presSeries = new LineSeries
            {
                Title = "P hPa",
                StrokeThickness = 2,
                Color = OxyColors.Red,
                YAxisKey = presAxis.Key
            };
            foreach (var d in ordered)
                presSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(d.TimestampUtc), d.PressureHpa));
            model.Series.Add(presSeries);

            // ---- Легенда (замена LineAnnotation на короткие LineSeries + TextAnnotation) ----
            {
                double xLeft = DateTimeAxis.ToDouble(from) + (DateTimeAxis.ToDouble(to) - DateTimeAxis.ToDouble(from)) * 0.03;
                double xRight = DateTimeAxis.ToDouble(from) + (DateTimeAxis.ToDouble(to) - DateTimeAxis.ToDouble(from)) * 0.12;
                double deltaX = (DateTimeAxis.ToDouble(to) - DateTimeAxis.ToDouble(from)) * 0.01;

                double legendYTop = tempAxis.Maximum - (tempAxis.Maximum - tempAxis.Minimum) * 0.02;
                double rowGap = (tempAxis.Maximum - tempAxis.Minimum) * 0.06;

                // helper: добавляет короткую цветную полосу как LineSeries (без Title -> не в легенде)
                void AddLegendLine(OxyColor color, double y)
                {
                    var s = new LineSeries
                    {
                        Color = color,
                        StrokeThickness = 3,
                        LineStyle = LineStyle.Solid,
                        MarkerType = MarkerType.None,
                        Title = null // чтобы не добавляться в легенду
                    };
                    s.Points.Add(new DataPoint(xLeft, y));
                    s.Points.Add(new DataPoint(xRight, y));
                    model.Series.Add(s);
                }

                // helper: добавляет текстовую подпись
                void AddLegendText(string text, double y)
                {
                    var ta = new TextAnnotation
                    {
                        Text = text,
                        TextPosition = new DataPoint(xRight + deltaX, y),
                        StrokeThickness = 0,
                        TextColor = OxyColors.Black,
                        FontSize = 10,
                        Layer = AnnotationLayer.AboveSeries
                    };
                    model.Annotations.Add(ta);
                }

                // Temperature (green)
                AddLegendLine(OxyColors.Green, legendYTop);
                AddLegendText("Temperature (°C)", legendYTop);

                // Humidity (orange)
                var y2 = legendYTop - rowGap;
                AddLegendLine(OxyColors.Orange, y2);
                AddLegendText("Humidity (%)", y2);

                // Pressure (red)
                var y3 = legendYTop - rowGap * 2;
                AddLegendLine(OxyColors.Red, y3);
                AddLegendText("Pressure (hPa)", y3);
            }
            // ----------------------------------------------------

            return ExportToPng(model);
        }, ct);
    }


    private static byte[] CreateEmptyPng(string title)
    {
        var m = new PlotModel { Title = title };
        return ExportToPng(m);
    }

    private static byte[] ExportToPng(PlotModel model)
    {
        using var ms = new MemoryStream();
        var exporter = new PngExporter { Width = 1000, Height = 450 };
        exporter.Export(model, ms);
        return ms.ToArray();
    }
}