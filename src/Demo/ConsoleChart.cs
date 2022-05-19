using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Demo
{
    public class ConsoleChart
    {
        private readonly List<ConsoleChartEntry> _entrys;
        private readonly int _height;
        private readonly int _width;
        private readonly StringBuilder[] _buffer;
        private int _startConsoleHeight;
        private int _startConsoleWidth;
        private Timer _watchWindowSizeTimer;

        public ConsoleChart(int height = 0, int width = 0)
        {
            _startConsoleHeight = Console.WindowHeight;
            _startConsoleWidth = Console.WindowWidth;
            
            if (height == 0)
                height = _startConsoleHeight;
            if (width == 0)
                width = _startConsoleWidth;
            
            _entrys = new List<ConsoleChartEntry>();
            _height = height;
            _width = width;
            _buffer = new StringBuilder[height];
            for (int row = 0; row < _buffer.Length; row++)
            {
                _buffer[row] = new StringBuilder(width+5);
            }

            _watchWindowSizeTimer = new System.Threading.Timer(WatchWindowSizeTimerCallback, null, 500, 500);
        }

        private void WatchWindowSizeTimerCallback(object state)
        {
            var height = Console.WindowHeight;
            var width = Console.WindowWidth;
            if (width != _startConsoleWidth)
            {
                // Width changed
                var offset = _startConsoleWidth - width;
                
            }
        }

        public void Add(double value)
        {
            _entrys.Add(new ConsoleChartEntry
            {
                Timestamp = DateTime.Now,
                Value = value
            });

            RedrawChart();
        }

        private void RedrawChart()
        {

            var transformed = Transform(_width - 9, _height -2 , _entrys);
            DrawScaleY(_buffer, transformed);
            //DrawScaleX(_buffer, _entrys);
            var bufferWidth = Console.BufferWidth;
            
            Console.SetCursorPosition(0,0);
            var sb = new StringBuilder(bufferWidth * Console.BufferHeight);
            
            for (int row = 0; row < _height; row++)
            {
                sb.Append( _buffer[row].ToString().PadRight(bufferWidth, ' ') );
                _buffer[row].Clear();
            }
            var buffer = Encoding.UTF8.GetBytes(sb.ToString());
            using (var stdout = Console.OpenStandardOutput(_width * _height))
            {
                stdout.Write(buffer, 0, buffer.Length);
            }
        }

        private IEnumerable<TransformedConsoleChartEntry> Transform(int width, int height, IEnumerable<ConsoleChartEntry> entrys)
        {
            Func<double, double[], int> nearestIndex = (value, values) =>
            {
                if (value <= values.Last())
                    return values.Length - 1;
                if (value >= values.First())
                    return 0;
                for (int i = 1; i < values.Length; i++)
                {
                    if (values[i-1] >= value && values[i] <= value)
                        return i;
                }
                return -1;
            };
                
            var colCount = width;

            var transformed = new List<TransformedConsoleChartEntry>(colCount);

            var orderedByAge = entrys
                .OrderBy(e => e.Timestamp)
                .ToArray();

            if (orderedByAge.Count() > colCount)
                orderedByAge = orderedByAge.Skip(orderedByAge.Count() - colCount).ToArray();

            var entrysCount = orderedByAge.Count();
            var min = orderedByAge.Min(e => e.Value);
            var max = orderedByAge.Max(e => e.Value);

            if (max == min)
            {
                min -= min / 100;
                max += max / 100;
            }
            var rowCount = height;
            var stepY = (max - min) / (rowCount - 1);

            var yscales = new double[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                yscales[row] = max - (row * stepY);
            }

            for (int col = 0; col < colCount; col++)
            {
                var entrysOnX = orderedByAge.ElementAtOrDefault(col);
                if(entrysOnX.Timestamp == default)
                    break;
                
                var transform = new TransformedConsoleChartEntry(entrysOnX)
                {
                    Column = col
                };
                var nearestYIndex = nearestIndex(transform.Value, yscales);
                transform.Row = nearestYIndex;
                transformed.Add(transform);
            }
            
            
            return transformed;
        }

        private void DrawScaleY(StringBuilder[] buffer, IEnumerable<TransformedConsoleChartEntry> entrys)
        {
            var pipe = "│";
            var rowCount = buffer.Length - 2;

            for (int row = 0; row < rowCount; row++)
            {
                var entrysOnRow = entrys.Where(e => e.Row == row);
                if (!entrysOnRow.Any())
                {
                    buffer[row]
                        .Append(" ")
                        .Append(" ".PadRight(7))
                        .Append(pipe);
                    continue;
                }
                
                buffer[row]
                    .Append(" ")
                    .Append(entrysOnRow.First().Value.ToString("0.##").PadRight(7))
                    .Append("┤");

                var startPos = buffer[row].Length;

                foreach (var chartEntry in entrysOnRow.OrderBy(e => e.Column))
                {
                    var colDiff = (startPos - buffer[row].Length) + chartEntry.Column;
                    if(colDiff > 0)
                        buffer[row]
                            .Append("".PadLeft(colDiff));
                    
                    buffer[row]    
                        .Append("X");
                }

            }

            var sb = buffer[rowCount];
            sb.Append("─".PadLeft(8, '─'))
                .Append('┼')
                .Append("─".PadLeft(_width - 9, '─'));
        }
        
        private void DrawScaleX(StringBuilder[] buffer, IEnumerable<ConsoleChartEntry> entrys)
        {
            var row = buffer.Length-2;

            var sb = buffer[row];
            sb.Append("─".PadLeft(8, '─'))
                .Append('┼')
                .Append("─".PadLeft(_width - 9, '─'));

            row = buffer.Length-1;
            
            var entryCount = entrys.Count();
            var maxWidth = _width - 10;
            var center = maxWidth / 2;
            
            /*
             * Erster Eintrag startet ab mitte des Diagramms.
             * Danach läuft das Diagramm voll, bis es 10 Spalten vor Ende erreicht
             * Danach wird jeder weitere Eintrag einen Links zur Mitte verschoben.
             * Ist der erste Eintrag bei Spalte 10 angelang, wird die Reihe bis zum Ende fortgesetzt
             * Ist das Diagramm voll, dann werden die ersten einträge gelöscht
             */

            if (entryCount > maxWidth)
                entrys = entrys.Skip(maxWidth - entryCount);

            var oldest = entrys.Min(e => e.Timestamp);
            var newest = entrys.Max(e => e.Timestamp);

            var diff = newest - oldest;
            var step = diff / (maxWidth / 10);
            
            
            sb = buffer[row];
            sb.Append("─".PadLeft(8, '─'))
                .Append('┼')
                .Append("─".PadLeft(_width - 9, '─'));

        }

        private double[] GetYScales(int count, IEnumerable<ConsoleChartEntry> entrys)
        {
            var min = entrys.Min(e => e.Value);
            var max = entrys.Max(e => e.Value);

            var rowCount = count;
            var step = (max - min) / rowCount;
            
            var yscales = new double[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                yscales[row] = max - (row * step);
            }

            return yscales;
        }

    }

    internal struct ConsoleChartEntry
    {
        public double Value { get; internal set; }
        public DateTime Timestamp { get; internal set; }
    }
    
    internal struct TransformedConsoleChartEntry
    {
        public double Value { get; internal set; }
        public DateTime Timestamp { get; internal set; }
        public int Column { get; set; }
        public int Row { get; set; }

        public TransformedConsoleChartEntry(ConsoleChartEntry entry)
        {
            Value = entry.Value;
            Timestamp = entry.Timestamp;

            Column = -1;
            Row = -1;
        }
        
        public TransformedConsoleChartEntry(IEnumerable<ConsoleChartEntry> entrys)
        {
            Value = entrys.Sum(e => e.Value) / entrys.Count();
            Timestamp = entrys.Min(e => e.Timestamp);
            
            Column = -1;
            Row = -1;
        }
    }
}