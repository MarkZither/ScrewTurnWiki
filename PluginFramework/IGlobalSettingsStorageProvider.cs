
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// The interface that must be implemented by a Storage Provider containing Global application configuration settings.
	/// </summary>
	public interface IGlobalSettingsStorageProviderV30 : IProviderV30 {

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		string GetSetting(string name);

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		IDictionary<string, string> GetAllSettings();

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		bool SetSetting(string name, string value);

		/// <summary>
		/// Alls the wikis.
		/// </summary>
		/// <returns>A list of wiki identifiers.</returns>
		IList<Wiki> AllWikis();

		/// <summary>
		/// Extracts the name of the wiki from the given host.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <returns>The name of the wiki</returns>
		string ExtractWikiName(string host);

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		/// <returns></returns>
		string[] ListPluginAssemblies();

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> or <paramref name="assembly"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> or <paramref name="assembly"/> are empty.</exception>
		bool StorePluginAssembly(string filename, byte[] assembly);

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		byte[] RetrievePluginAssembly(string filename);

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		bool DeletePluginAssembly(string filename);

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <param name="wiki">The wiki, <c>null</c> if is an application level log.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="message"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="message"/> or <paramref name="user"/> are empty.</exception>
		void LogEntry(string message, EntryType entryType, string user, string wiki);

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <returns>The Log Entries.</returns>
		LogEntry[] GetLogEntries();

		/// <summary>
		/// Clear the Log.
		/// </summary>
		void ClearLog();

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		int LogSize { get; }

	}

	/// <summary>
	/// Represents a Log Entry.
	/// </summary>
	public class LogEntry {

		private EntryType type;
		private DateTime dateTime;
		private string message;
		private string user;
		private string wiki;

		/// <summary>
		/// Initializes a new instance of the <b>LogEntry</b> class.
		/// </summary>
		/// <param name="type">The type of the Entry</param>
		/// <param name="dateTime">The DateTime.</param>
		/// <param name="message">The Message.</param>
		/// <param name="user">The User.</param>
		/// <param name="wiki">The Wiki.</param>
		public LogEntry(EntryType type, DateTime dateTime, string message, string user, string wiki) {
			this.type = type;
			this.dateTime = dateTime;
			this.message = message;
			this.user = user;
			this.wiki = wiki;
		}

		/// <summary>
		/// Gets the EntryType.
		/// </summary>
		public EntryType EntryType {
			get { return type; }
		}

		/// <summary>
		/// Gets the DateTime.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
		}

		/// <summary>
		/// Gets the Message.
		/// </summary>
		public string Message {
			get { return message; }
		}

		/// <summary>
		/// Gets the User.
		/// </summary>
		public string User {
			get { return user; }
		}

		/// <summary>
		/// Gets the wiki.
		/// </summary>
		public string Wiki {
			get { return wiki; }
		}

	}

	/// <summary>
	/// Enumerates the Types of Log Entries.
	/// </summary>
	public enum EntryType {
		/// <summary>
		/// Represents a simple Message.
		/// </summary>
		General,
		/// <summary>
		/// Represents a Warning.
		/// </summary>
		Warning,
		/// <summary>
		/// Represents an Error.
		/// </summary>
		Error
	}

	/// <summary>
	/// Lists legal logging level values.
	/// </summary>
	public enum LoggingLevel {
		/// <summary>
		/// All messages are logged.
		/// </summary>
		AllMessages = 3,
		/// <summary>
		/// Warnings and errors are logged.
		/// </summary>
		WarningsAndErrors = 2,
		/// <summary>
		/// Errors only are logged.
		/// </summary>
		ErrorsOnly = 1,
		/// <summary>
		/// Logging is completely disabled.
		/// </summary>
		DisableLog = 0
	}
}
