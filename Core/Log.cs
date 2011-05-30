
using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Records and retrieves Log Entries.
	/// </summary>
	public static class Log {

		/// <summary>
		/// The system username ('SYSTEM').
		/// </summary>
		public const string SystemUsername = "SYSTEM";

		/// <summary>
		/// Writes an Entry in the Log.
		/// </summary>
		/// <param name="message">The Message.</param>
		/// <param name="type">The Type of the Entry.</param>
		/// <param name="user">The User that generated the Entry.</param>
		/// <param name="wiki">The wiki, <c>null</c> if is an application level log.</param>
		public static void LogEntry(string message, EntryType type, string user, string wiki) {
			try {
				GlobalSettings.Provider.LogEntry(message, type, user, wiki);
			}
			catch { }
		}

		/// <summary>
		/// Reads all the Log Entries (newest to oldest).
		/// </summary>
		/// <returns>The Entries.</returns>
		public static List<LogEntry> ReadEntries() {
			List<LogEntry> entries = new List<LogEntry>(GlobalSettings.Provider.GetLogEntries());
			entries.Reverse();
			return entries;

		}

		/// <summary>
		/// Clears the Log.
		/// </summary>
		public static void ClearLog() {
			GlobalSettings.Provider.ClearLog();
		}

	}

}
