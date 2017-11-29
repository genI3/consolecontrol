using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ConsoleControl.WPF.Converters
{
    /// <summary>
    /// A class that allows you to get the font size based 
    /// on the width of the text block and the font family.
    /// </summary>
    public class WidthToFontSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the font size of a text 
        /// block based on the input parameters.
        /// </summary>
        /// <param name="values">The Width and FontFamily of text block.</param>
        /// <param name="targetType">The type of the returning value. 
        /// It must be a <see cref="double"/>.</param>
        /// <param name="parameter">Unused parameter.</param>
        /// <param name="culture">Unused parameter.</param>
        /// <returns>The calculated font size or <see cref="DependencyProperty.UnsetValue"/>.</returns>
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(double))
                return DependencyProperty.UnsetValue;

            if (values.Length != 2)
                return DependencyProperty.UnsetValue;

            if (!(values[0] is double aWidth) || aWidth < 0.0d)
                return DependencyProperty.UnsetValue;

            if (!(values[1] is FontFamily font) || font == null)
                return DependencyProperty.UnsetValue;

            if (new Typeface(font.Source).TryGetGlyphTypeface(out var glyphTypeface))
            {
                var width = glyphTypeface.AdvanceWidths.Select(i => i.Value).Max();

                return aWidth / ((maxLineLength + 1) * width * 1.02d);
            }

            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// The <see cref="IMultiValueConverter.ConvertBack(object, Type[], object, System.Globalization.CultureInfo)"/> stub.
        /// </summary>                    
        /// <returns>Always null.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        /// <summary>
        /// The max length of text line.
        /// </summary>
        const double maxLineLength = 80.0d;
    }
}
