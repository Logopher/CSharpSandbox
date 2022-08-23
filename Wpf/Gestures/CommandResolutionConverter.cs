using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.Gestures
{
    public class CommandResolutionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = (string)parameter;
            var dict = (IDictionary<string, ICommand>)value;
            if (!dict.TryGetValue(key, out ICommand? command))
            {
                Debugger.Break();
            }
            return command;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var command = (ICommand)parameter;
            var dict = (IDictionary<string, ICommand>)value;
            var matches = dict.Where(kv => kv.Value == command).ToList();
            if(matches.Count != 1)
            {
                Debugger.Break();
            }
            return matches.FirstOrDefault();
        }
    }
}
