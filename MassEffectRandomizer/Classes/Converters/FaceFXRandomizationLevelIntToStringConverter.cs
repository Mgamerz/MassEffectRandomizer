﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MassEffectRandomizer.Classes.Converters
{
    class FaceFXRandomizationLevelIntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value == null)
                return "Error";
            double i = (double) value;
            int setting = (int)i;
            switch (setting)
            {
                case 1:
                    return "Oblivion";
                case 2:
                    return "Knights of the old Republic";
                case 3:
                    return "Sonic Adventure";
                default:
                    return "Source Filmmaker";
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null; //we don't convert back
        }
    }
}
