
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.BackupRestore.Tests {

	[TestFixture]
	public class BackupRestoreTests {

		[Test]
		public void Backup_RestoreSettingsStorageProvider_Test() {
			DummySettingsStorageProvider sourceDummySettingsStorageProvider = new DummySettingsStorageProvider();

			// Settings key -> value
			sourceDummySettingsStorageProvider.SetSetting("key1", "value1");
			sourceDummySettingsStorageProvider.SetSetting("key2", "value2");

			// PluginStatus
			sourceDummySettingsStorageProvider.SetPluginStatus("plugin1", true);
			sourceDummySettingsStorageProvider.SetPluginStatus("plugin2", false);

			// PluginConfiguration
			sourceDummySettingsStorageProvider.SetPluginConfiguration("plugin1", "configuration string for plugin 1");
			sourceDummySettingsStorageProvider.SetPluginConfiguration("plugin2", "configuration string for plugin 2");

			// Metadata
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.AccountActivationMessage, null, "Account Activation Message");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null, "PasswordResetProcedureMessage");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.LoginNotice, null, "Login Notice");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PageChangeMessage, null, "Page Change Message");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null, "Discussion Change Message");

			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.EditNotice, "ns1", "Edit Notice\r\nfor ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Footer, "ns1", "Footer for ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Header, "ns1", "Header for ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.HtmlHead, "ns1", "Html Head for ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PageFooter, "ns1", "Page Footer for ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PageHeader, "ns1", "Page Header for ns1");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Sidebar, "ns1", "Sidebar for ns1");

			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.EditNotice, null, "Edit Notice for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Footer, null, "Footer for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Header, null, "Header for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.HtmlHead, null, "Html Head for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PageFooter, null, "Page Footer for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.PageHeader, null, "Page Header for root");
			sourceDummySettingsStorageProvider.SetMetaDataItem(MetaDataItem.Sidebar, null, "Sidebar for root");

			// RecentChanges
			sourceDummySettingsStorageProvider.AddRecentChange("page1", "title1", "msg sbj 1", new DateTime(2011, 05, 30, 10, 00, 00), "user 1", ScrewTurn.Wiki.PluginFramework.Change.MessagePosted, null);
			sourceDummySettingsStorageProvider.AddRecentChange("page2", "title2", "msg sbj 2", new DateTime(2011, 05, 25, 10, 00, 00), "user 2", ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, "description");

			// Outgoing Links
			sourceDummySettingsStorageProvider.StoreOutgoingLinks("page1", new string[] { "link1", "link2" });
			sourceDummySettingsStorageProvider.StoreOutgoingLinks("page2", new string[] { "link3", "link4" });

			// ACLEntries
			sourceDummySettingsStorageProvider.AclManager.StoreEntry("res1", "act1", "sbj1", Value.Grant);
			sourceDummySettingsStorageProvider.AclManager.StoreEntry("res1", "act1", "sbj2", Value.Deny);

			// Backup DummySettingsStorageProvider
			byte[] backup = BackupRestore.BackupSettingsStorageProvider(sourceDummySettingsStorageProvider, new string[] { "ns1" }, new string[] { "plugin1", "plugin2" });

			DummySettingsStorageProvider destinationDummySettingsStorageProvider = new DummySettingsStorageProvider();
			
			// Restore DummySettigsStorageProvider
			Assert.IsTrue(BackupRestore.RestoreSettingsStorageProvider(backup, destinationDummySettingsStorageProvider));

			// Settings
			Assert.AreEqual("value1", destinationDummySettingsStorageProvider.GetSetting("key1"));
			Assert.AreEqual("value2", destinationDummySettingsStorageProvider.GetSetting("key2"));

			// PluginStatus
			Assert.AreEqual(true, destinationDummySettingsStorageProvider.GetPluginStatus("plugin1"));
			Assert.AreEqual(false, destinationDummySettingsStorageProvider.GetPluginStatus("plugin2"));

			// PluginConfiguration
			Assert.AreEqual("configuration string for plugin 1", destinationDummySettingsStorageProvider.GetPluginConfiguration("plugin1"));
			Assert.AreEqual("configuration string for plugin 2", destinationDummySettingsStorageProvider.GetPluginConfiguration("plugin2"));

			// Metadata
			Assert.AreEqual("Account Activation Message", destinationDummySettingsStorageProvider.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null));
			Assert.AreEqual("Edit Notice\r\nfor ns1", destinationDummySettingsStorageProvider.GetMetaDataItem(MetaDataItem.EditNotice, "ns1"));
			Assert.AreEqual("Header for root", destinationDummySettingsStorageProvider.GetMetaDataItem(MetaDataItem.Header, null));

			// RecentChanges
			RecentChange[] recentChanges = destinationDummySettingsStorageProvider.GetRecentChanges();
			Assert.AreEqual(recentChanges[0].Page, "page1");
			Assert.IsNull(recentChanges[0].Description);
			Assert.AreEqual(recentChanges[1].MessageSubject, "msg sbj 2");
			Assert.AreEqual((new DateTime(2011, 05, 25)).ToShortDateString(), recentChanges[1].DateTime.ToShortDateString());

			// Outgoing Links
			CollectionAssert.AreEqual(new string[] { "link1", "link2" }, destinationDummySettingsStorageProvider.GetOutgoingLinks("page1"));
			CollectionAssert.AreEqual(new string[] { "link3", "link4" }, destinationDummySettingsStorageProvider.GetOutgoingLinks("page2"));

			// ACLEntries
			AclEntry[] aclEntries = destinationDummySettingsStorageProvider.AclManager.RetrieveAllEntries();
			Assert.AreEqual(Value.Grant, aclEntries[0].Value);
			Assert.AreEqual("res1", aclEntries[0].Resource);
			Assert.AreEqual("sbj2", aclEntries[1].Subject);
			Assert.AreEqual("act1", aclEntries[1].Action);
			Assert.AreEqual(Value.Deny, aclEntries[1].Value);
		}

		[Test]
		public void Backup_RestoreGlobalSettingsStorageProvider_Test() {
			DummyGlobalSettingsStorageProvider sourceDummyGlobalSettingsStorageProvider = new DummyGlobalSettingsStorageProvider();

			// Settings key -> value
			sourceDummyGlobalSettingsStorageProvider.SetSetting("key1", "value1");
			sourceDummyGlobalSettingsStorageProvider.SetSetting("key2", "value2");

			// PluginAssemblies
			sourceDummyGlobalSettingsStorageProvider.StorePluginAssembly("plugin1", new byte[] { 1, 2, 3, 1, 2, 3 });
			sourceDummyGlobalSettingsStorageProvider.StorePluginAssembly("plugin2", new byte[] { 4, 5, 6, 4, 5, 6 });

			byte[] backupFile = BackupRestore.BackupGlobalSettingsStorageProvider(sourceDummyGlobalSettingsStorageProvider);

			DummyGlobalSettingsStorageProvider destinationDummyGlobalSettingsStorageProvider = new DummyGlobalSettingsStorageProvider();

			Assert.IsTrue(BackupRestore.RestoreGlobalSettingsStorageProvider(backupFile, destinationDummyGlobalSettingsStorageProvider));

			// Settings
			Assert.AreEqual("value1", destinationDummyGlobalSettingsStorageProvider.GetSetting("key1"));
			Assert.AreEqual("value2", destinationDummyGlobalSettingsStorageProvider.GetSetting("key2"));

			// PluginAssemblies
			CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 1, 2, 3 }, destinationDummyGlobalSettingsStorageProvider.RetrievePluginAssembly("plugin1"));
			CollectionAssert.AreEqual(new byte[] { 4, 5, 6, 4, 5, 6 }, destinationDummyGlobalSettingsStorageProvider.RetrievePluginAssembly("plugin2"));
		}

	}

	internal class DummySettingsStorageProvider : ISettingsStorageProviderV40 {

		private	Dictionary<string, string> settings = new Dictionary<string, string>();
		private Dictionary<string, bool> pluginsStatus = new Dictionary<string, bool>();
		private Dictionary<string, string> pluginsConfigurations = new Dictionary<string, string>();
		private Dictionary<string, string> metadata = new Dictionary<string, string>();
		private List<RecentChange> recentChanges = new List<RecentChange>();
		private Dictionary<string, string[]> outgoingLinks = new Dictionary<string, string[]>();
		private IAclManager aclManager;

		#region ISettingsStorageProviderV40 Members

		public string GetSetting(string name) {
			return settings[name];
		}

		public bool SetSetting(string name, string value) {
			settings[name] = value;
			return true;
		}

		public IDictionary<string, string> GetAllSettings() {
			return settings;
		}

		public void BeginBulkUpdate() {
			// Nothing to do
		}

		public void EndBulkUpdate() {
			// Nothing to do
		}

		public bool SetPluginStatus(string typeName, bool enabled) {
			pluginsStatus[typeName] = enabled;
			return true;
		}

		public bool GetPluginStatus(string typeName) {
			return pluginsStatus[typeName];
		}

		public bool SetPluginConfiguration(string typeName, string config) {
			pluginsConfigurations[typeName] = config;
			return true;
		}

		public string GetPluginConfiguration(string typeName) {
			return pluginsConfigurations[typeName];
		}


		private readonly Dictionary<MetaDataItem, string> MetaDataItemToString = new Dictionary<MetaDataItem, string>() {
			{ MetaDataItem.AccountActivationMessage, "AAM" },
			{ MetaDataItem.EditNotice, "EN" },
			{ MetaDataItem.Footer, "F" },
			{ MetaDataItem.Header, "H" },
			{ MetaDataItem.HtmlHead, "HH" },
			{ MetaDataItem.LoginNotice, "LN" },
			{ MetaDataItem.AccessDeniedNotice, "ADN" },
			{ MetaDataItem.RegisterNotice, "RN" },
			{ MetaDataItem.PageFooter, "PF" },
			{ MetaDataItem.PageHeader, "PH" },
			{ MetaDataItem.PasswordResetProcedureMessage, "PRPM" },
			{ MetaDataItem.Sidebar, "S" },
			{ MetaDataItem.PageChangeMessage, "PCM" },
			{ MetaDataItem.DiscussionChangeMessage, "DCM" },
			{ MetaDataItem.ApproveDraftMessage, "ADM" }
		};

		public string GetMetaDataItem(MetaDataItem item, string tag) {
			tag = tag + "";
			return metadata[MetaDataItemToString[item] + "|" + tag]; 
		}

		public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
			metadata[MetaDataItemToString[item] + "|" + tag] = content;
			return true;
		}

		public RecentChange[] GetRecentChanges() {
			return recentChanges.ToArray();
		}

		public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, ScrewTurn.Wiki.PluginFramework.Change change, string descr) {
			recentChanges.Add(new RecentChange(page, title, messageSubject, dateTime, user, change, descr));
			return true;
		}

		public AclEngine.IAclManager AclManager {
			get {
				if(aclManager == null) {
					aclManager = new DummyAclManager();
				}
				return aclManager;
			}
		}

		public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
			this.outgoingLinks[page] = outgoingLinks;
			return true;
		}

		public string[] GetOutgoingLinks(string page) {
			return outgoingLinks[page];
		}

		public IDictionary<string, string[]> GetAllOutgoingLinks() {
			return outgoingLinks;
		}

		public bool DeleteOutgoingLinks(string page) {
			throw new NotImplementedException();
		}

		public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
			throw new NotImplementedException();
		}

		public bool IsFirstApplicationStart() {
			throw new NotImplementedException();
		}

		#endregion

		#region IProviderV40 Members

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

		#endregion

		#region IDisposable Members

		public void Dispose() {
			throw new NotImplementedException();
		}

		#endregion
	}

	internal class DummyAclManager : IAclManager {

		private List<AclEntry> aclEntries = new List<AclEntry>();

		#region IAclManager Members

		public bool StoreEntry(string resource, string action, string subject, Value value) {
			aclEntries.Add(new AclEntry(resource, action, subject, value));
			return true;
		}

		public bool DeleteEntry(string resource, string action, string subject) {
			throw new NotImplementedException();
		}

		public bool DeleteEntriesForResource(string resource) {
			throw new NotImplementedException();
		}

		public bool DeleteEntriesForSubject(string subject) {
			throw new NotImplementedException();
		}

		public bool RenameResource(string resource, string newName) {
			throw new NotImplementedException();
		}

		public AclEntry[] RetrieveAllEntries() {
			return aclEntries.ToArray();
		}

		public AclEntry[] RetrieveEntriesForResource(string resource) {
			throw new NotImplementedException();
		}

		public AclEntry[] RetrieveEntriesForSubject(string subject) {
			throw new NotImplementedException();
		}

		public void InitializeData(AclEntry[] entries) {
			throw new NotImplementedException();
		}

		public int TotalEntries {
			get { throw new NotImplementedException(); }
		}

		public event EventHandler<AclChangedEventArgs> AclChanged;

		protected void OnAclChanged(AclEntry[] entries, ScrewTurn.Wiki.AclEngine.Change change) {
			if(AclChanged != null) {
				AclChanged(this, new AclChangedEventArgs(entries, change));
			}
		}

		#endregion
	}

	internal class DummyGlobalSettingsStorageProvider : IGlobalSettingsStorageProviderV40 {

		Dictionary<string, string> settings = new Dictionary<string, string>();
		Dictionary<string, byte[]> pluginsAssemblies = new Dictionary<string, byte[]>();

		#region IGlobalSettingsStorageProviderV40 Members

		public string GetSetting(string name) {
			return settings[name];
		}

		public IDictionary<string, string> GetAllSettings() {
			return settings;
		}

		public bool SetSetting(string name, string value) {
			settings[name] = value;
			return true;
		}

		public IList<PluginFramework.Wiki> AllWikis() {
			throw new NotImplementedException();
		}

		public string ExtractWikiName(string host) {
			throw new NotImplementedException();
		}

		public string[] ListPluginAssemblies() {
			return (from p in pluginsAssemblies select p.Key).ToArray<string>();
		}

		public bool StorePluginAssembly(string filename, byte[] assembly) {
			pluginsAssemblies[filename] = assembly;
			return true;
		}

		public byte[] RetrievePluginAssembly(string filename) {
			return pluginsAssemblies[filename];
		}

		public bool DeletePluginAssembly(string filename) {
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

		#endregion

		#region IProviderV40 Members

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

		#endregion

		#region IDisposable Members

		public void Dispose() {
			throw new NotImplementedException();
		}

		#endregion
	}
}
