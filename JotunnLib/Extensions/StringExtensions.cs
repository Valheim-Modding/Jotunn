namespace Jotunn.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Returns true if the string contains any of the substrings.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substrings"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string str, params string[] substrings)
        {
            foreach (var substring in substrings)
            {
                if (str.Contains(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns true if the string ends with any one of the suffixes.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="suffixes"></param>
        /// <returns></returns>
        public static bool EndsWithAny(this string str, params string[] suffixes)
        {
            foreach (var substring in suffixes)
            {
                if (str.EndsWith(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns true if the string starts with any one of the prefixes.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="prefixes"></param>
        /// <returns></returns>
        public static bool StartsWithAny(this string str, params string[] prefixes)
        {
            foreach (var substring in prefixes)
            {
                if (str.StartsWith(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     If the string ends with the suffix then return a copy of the string
        ///     with the suffix stripped, otherwise return the original string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string RemoveSuffix(this string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }

            return s;
        }

        /// <summary>
        ///     If the string starts with the prefix then return a copy of the string
        ///     with the prefix stripped, otherwise return the original string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string RemovePrefix(this string s, string prefix)
        {
            if (s.StartsWith(prefix))
            {
                return s.Substring(prefix.Length, s.Length - prefix.Length);
            }
            return s;
        }

        /// <summary>
        ///     Returns a copy of the string with the first character capitalized
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CapitalizeFirstLetter(this string s)
        {
            if (s.Length == 0)
                return s;
            else if (s.Length == 1)
                return $"{char.ToUpper(s[0])}";
            else
                return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        ///     Returns an Empty string if value is null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EmptyIfNull(this object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return value.ToString();
        }
    }
}
