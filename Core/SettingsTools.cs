
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains useful tools for settings classes.
	/// </summary>
	public static class SettingsTools {

		#region Basic Settings and Associated Data

		/// <summary>
		/// Gets an integer.
		/// </summary>
		/// <param name="value">The string value.</param>
		/// <param name="def">The default value, returned when string parsing fails.</param>
		/// <returns>The result.</returns>
		public static int GetInt(string value, int def) {
			if(value == null) return def;
			int i = def;
			int.TryParse(value, out i);
			return i;
		}

		/// <summary>
		/// Gets a boolean.
		/// </summary>
		/// <param name="value">The string value.</param>
		/// <param name="def">The default value, returned when parsing fails.</param>
		/// <returns>The result.</returns>
		public static bool GetBool(string value, bool def) {
			if(value == null) return def;
			else {
				if(value.ToLowerInvariant() == "yes") return true;
				bool b = def;
				bool.TryParse(value, out b);
				return b;
			}
		}

		/// <summary>
		/// Prints a boolean.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The string result.</returns>
		public static string PrintBool(bool value) {
			return value ? "yes" : "no";
		}

		/// <summary>
		/// Gets a string.
		/// </summary>
		/// <param name="value">The raw string.</param>
		/// <param name="def">The default value, returned when the raw string is <c>null</c>.</param>
		/// <returns>The result.</returns>
		public static string GetString(string value, string def) {
			if(string.IsNullOrEmpty(value)) return def;
			else return value;
		}

		#endregion
	}
}
