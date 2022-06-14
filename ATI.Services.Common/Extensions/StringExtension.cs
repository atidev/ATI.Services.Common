using System;
using System.ComponentModel;
using System.Globalization;

namespace ATI.Services.Common.Extensions
{
    public static class StringExtension
    {
        private static readonly Type StringType = typeof(string);

        /// <summary>
        /// Пробует преобразовать строку тип <see cref="T"/>. Возвращает флаг, свидетельствующий об успешности преобразования.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="value">Преобразованное значение в случае успеха операции.</param>
        public static bool TryConvert<T>(this string str, out T value)
        {
            value = default;
            var converter = TypeDescriptor.GetConverter(typeof(T));

            if (!converter.CanConvertFrom(StringType))
                return false;

            value = (T)converter.ConvertFromString(null, CultureInfo.GetCultureInfo("ru-RU"), str);
            return true;
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
    }
}
