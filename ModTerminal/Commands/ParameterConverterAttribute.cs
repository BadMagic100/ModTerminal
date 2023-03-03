using System;

namespace ModTerminal.Commands
{
    /// <summary>
    /// Converts a single string value to a value of a preferred target type.
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Converts a string value to an arbitrary type, or throws <see cref="InvalidCastException"/> if it cannot.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A converted value.</returns>
        object? Convert(string value);
    }

    /// <summary>
    /// An attribute which describes how an argument of non-standard type can be parsed from a string value.
    /// It is assumed that the specified converter can correctly convert to the type of the annotated parameter.
    /// </summary>
    /// <remarks>
    /// When converting, you only convert single (ideally non-null) values. So for some type U,
    /// the same converter can be used to convert values for U[], Nullable&lt;U&gt; and U parameters.
    /// </remarks>
    /// <typeparam name="T">The type of IValueConverter to use to convert values.</typeparam>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParameterConverterAttribute<T> : Attribute where T : IValueConverter, new() { }
}
