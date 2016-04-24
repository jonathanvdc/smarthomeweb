using System;

namespace SmartHomeWeb
{
	/// <summary>
	/// A collection of helper functions to handle form data.
	/// </summary>
	public static class FormHelpers
	{
		/// <summary>
		/// Gets an untrimmed version of the given form's value for
		/// the given key.
		/// </summary>
		public static string GetRawString(dynamic Form, string Key)
		{
			return (string)Form[Key];
		}

		/// <summary>
		/// Gets a trimmed version of the given form's value for
		/// the given key.
		/// </summary>
		public static string GetString(dynamic Form, string Key)
		{
			return GetRawString(Form, Key).Trim();
		}

		/// <summary>
		/// Parses the given string as a date.
		/// </summary>
		public static DateTime? ParseDateOrNull(string Value)
		{
			DateTime result;
			if (DateTime.TryParse(Value, out result))
				return result;
			else
				return null;
		}

		/// <summary>
		/// Parses the given form's value for the given key
		/// as a date.
		/// </summary>
		public static DateTime? GetDate(dynamic Form, string Key)
		{
			return ParseDateOrNull(GetString(Form, Key));
		}

		/// <summary>
		/// Parses the given string as a double-precision 
		/// floating-point number.
		/// </summary>
		public static double? ParseDoubleOrNull(string Value)
		{
			double result;
			if (double.TryParse(Value, out result))
				return result;
			else
				return null;
		}

		/// <summary>
		/// Parses the given form's value for the given key
		/// as a double-precision floating-point number.
		/// </summary>
		public static double? GetDouble(dynamic Form, string Key)
		{
			return ParseDoubleOrNull(GetString(Form, Key));
		}
	}
}

