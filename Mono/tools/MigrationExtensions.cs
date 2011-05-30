using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

using MySql.Data.MySqlClient;

static class MigrationExtensions
{
	public static string ToMySqlDateTime (this DateTime dt)
	{
		return String.Format ("{0:yyyyMMddHHmmss}", dt.ToUniversalTime ());
	}
	
	public static MySqlCommand GetCommand (this MySqlConnection conn, string commandText, params Tuple <string, object>[] parameters)
	{
		var cmd = conn.CreateCommand ();
		cmd.CommandText = commandText;

		if (parameters == null || parameters.Length == 0)
			return cmd;

		foreach (var p in parameters) {
			if (p == null)
				continue;
			
			cmd.Parameters.AddWithValue ("?" + p.Item1, p.Item2);
		}

		return cmd;
	}

	public static T ExecuteScalar<T> (this MySqlCommand command, T defaultValue, bool close = false) {
		object temp = null;

		try {
			temp = command.ExecuteScalar();
		} catch (DbException ex) {
			Console.Error.WriteLine ("Exception '{0}' while executing scalar: {1}", ex.GetType (), ex.Message);
		} finally {
			if (close)
				try {
					command.Connection.Close ();
				} catch {
				}
		}

		if (temp == null)
			return defaultValue;
		
		if (typeof (int) == typeof(T))
			return (T)(int.Parse (temp.ToString ()) as object);
		if (typeof (string) == typeof (T))
			return (T)(temp.ToString () as object);
		return (T)temp;
	}

	public static string TinyBlobToString (this byte[] blob)
	{
		if (blob == null || blob.Length == 0)
			return String.Empty;

		return Encoding.UTF8.GetString (blob);
	}
	
	public static string SqlEncode (this string text)
	{
		if (String.IsNullOrEmpty (text))
			return String.Empty;

		return text.
			Replace ("\\", "\\\\").
			Replace ("\"", "\\\"").
			Replace ("'", "\\'").
			Replace ("\n", "\\n").
			Replace ("\r", "\\r").
			Replace ("\t", "\\t");
	}

	public static string SqlEncodeForName (this string text)
	{
		if (String.IsNullOrEmpty (text))
			return String.Empty;
		string ret = text.SqlEncode ();
		if (ret [0] == '.')
			ret = ret.Substring (1);
		return ret.
			Replace (".", "_").
			Replace (":", "_").
			Replace (" ", "_");
	}
	
	public static string SqlAndWikiEncode (this string text)
	{
		if (String.IsNullOrEmpty (text))
			return String.Empty;

		string ret = text.SqlEncode ();
		return ret.
			Replace ("_", " ");
	}

	public static DateTime ParseDekiTime (this string dekiTime)
	{
		int year, month, day, hour, minute, second;

		year = Int32.Parse (dekiTime.Substring (0, 4));
		month = Int32.Parse (dekiTime.Substring (4, 2));
		day = Int32.Parse (dekiTime.Substring (6, 2));
		hour = Int32.Parse (dekiTime.Substring (8, 2));
		minute = Int32.Parse (dekiTime.Substring (10, 2));
		second = Int32.Parse (dekiTime.Substring (12, 2));

		try {
			return new DateTime (year, month, day, hour, minute, second);
		} catch {
			return DateTime.MinValue;
		}
	}
}