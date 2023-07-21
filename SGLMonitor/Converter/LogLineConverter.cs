using MDPGen.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace SGLMonitor.Converter
{
    public class LogLineConverter: MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var entry = (Tuple<TraceType, string>) value;
            return entry?.Item2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class LogLineBrushConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush))
                throw new ArgumentException("LogLineBrushConverter must be applied to a Brush type.");

            var entry = (Tuple<TraceType, string>)value;
            switch (entry?.Item1)
            {
                case TraceType.Warning:
                    return Brushes.DarkOrange;
                case TraceType.Error:
                    return Brushes.DarkRed;
                case TraceType.Diagnostic:
                    return Brushes.DarkGray;
                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
