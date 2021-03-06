﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NTMiner.Converters {
    [ValueConversion(typeof(double), typeof(Brush))]
    public class ProfitToBrushConverter : IValueConverter {
        private static readonly SolidColorBrush s_green = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush s_red = new SolidColorBrush(Colors.Red);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double d) {
                if (d > 0) {
                    return s_green;
                }
            }
            return s_red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
