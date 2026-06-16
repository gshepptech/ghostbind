using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.App;

public sealed class EnumLabelConverter : IValueConverter
{
    public object? Convert(object value, System.Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            OutputButton b => b.DisplayLabel(),
            DualSenseButton b => b.DisplayLabel(),
            ActivatorMode m => m.DisplayLabel(),
            CurveType c => c.DisplayLabel(),
            null => "",
            _ => value.ToString() ?? "",
        };

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture) =>
        DependencyProperty.UnsetValue;
}
