
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.AclEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	/// <summary>
	/// Implements a dummy Settings Storage Provider to use for testing.
	/// </summary>
	public class TestGlobalSettingsStorageProvider : IGlobalSettingsStorageProviderV40 {

		public string GetSetting(string name) {
			throw new NotImplementedException();
		}

		public IDictionary<string, string> GetAllSettings() {
			throw new NotImplementedException();
		}

		public bool SetSetting(string name, string value) {
			throw new NotImplementedException();
		}

		public PluginFramework.Wiki[] GetAllWikis() {
			throw new NotImplementedException();
		}

		public string ExtractWikiName(string host) {
			throw new NotImplementedException();
		}

		public string[] ListPluginAssemblies() {
			throw new NotImplementedException();
		}

		public bool StorePluginAssembly(string filename, byte[] assembly) {
			throw new NotImplementedException();
		}

		public byte[] RetrievePluginAssembly(string filename) {
			throw new NotImplementedException();
		}

		public bool DeletePluginAssembly(string filename) {
			throw new NotImplementedException();
		}

		public bool SetPluginStatus(string typeName, bool enabled) {
			throw new NotImplementedException();
		}

		public bool GetPluginStatus(string typeName) {
			throw new NotImplementedException();
		}

		public bool SetPluginConfiguration(string typeName, string config) {
			throw new NotImplementedException();
		}

		public string GetPluginConfiguration(string typeName) {
			throw new NotImplementedException();
		}

		public void LogEntry(string message, EntryType entryType, string user, string wiki) {
			throw new NotImplementedException();
		}

		public LogEntry[] GetLogEntries() {
			throw new NotImplementedException();
		}

		public void ClearLog() {
			throw new NotImplementedException();
		}

		public int LogSize {
			get { throw new NotImplementedException(); }
		}

		public string CurrentWiki {
			get { throw new NotImplementedException(); }
		}

		public void Init(IHostV40 host, string config, string wiki) {
			throw new NotImplementedException();
		}

		public void SetUp(IHostV40 host, string config) {
			throw new NotImplementedException();
		}

		public ComponentInformation Information {
			get { throw new NotImplementedException(); }
		}

		public string ConfigHelpHtml {
			get { throw new NotImplementedException(); }
		}

		public void Dispose() {
			throw new NotImplementedException();
		}
	}

}
