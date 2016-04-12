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
		/// Parses the given form's value for the given key
		/// as a date.
		/// </summary>
		public static DateTime? GetDate(dynamic Form, string Key)
		{
			string dtStr = GetString(Form, Key);

			DateTime result;
			if (DateTime.TryParse(dtStr, out result))
				return result;
			else
				return null;
		}
	}
}

