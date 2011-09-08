﻿
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
		public void Backup_RestorePagesStorageProvider_Test() {
			DummyPagesStorageProvider sourceDummyPagesStorageProvider = new DummyPagesStorageProvider();

			DateTime date = DateTime.UtcNow;

			// Namespace
			NamespaceInfo ns1 = new NamespaceInfo("ns1", sourceDummyPagesStorageProvider, "MainPage");
			sourceDummyPagesStorageProvider.AddNamespace(ns1.Name);
			sourceDummyPagesStorageProvider.SetNamespaceDefaultPage(ns1, ns1.DefaultPageFullName);
			
			// Categories
			sourceDummyPagesStorageProvider.AddCategory(ns1.Name, "cat1");
			sourceDummyPagesStorageProvider.AddCategory(ns1.Name, "cat2");
			
			// Navigation Paths
			sourceDummyPagesStorageProvider.AddNavigationPath(ns1.Name, "np1", new string[] { "MainPage", "Page1" });

			// Pages with old revision and draft
			PageContent oldMainPage = sourceDummyPagesStorageProvider.SetPageContent(ns1.Name, "MainPage", date.AddDays(-1), "Old Title of MainPage", "OldTestUser", date.AddDays(-1), "OldComment", "Old Content of MainPage", new string[] { "k1" }, "Old Description", SaveMode.Normal);
			PageContent mainPage = sourceDummyPagesStorageProvider.SetPageContent(ns1.Name, "MainPage", date, "Title of MainPage", "TestUser", date, "Comment", "Content of MainPage", new string[] { "k1", "k2" }, "Description", SaveMode.Backup);
			PageContent draftMainPage = sourceDummyPagesStorageProvider.SetPageContent(ns1.Name, "MainPage", date.AddDays(1), "Draft Title of MainPage", "DraftTestUser", date.AddDays(1), "DraftComment", "Draft Content of MainPage", new string[] { "k1", "k2", "k3" }, "Draft Description", SaveMode.Draft);

			// Messages
			int messageId = sourceDummyPagesStorageProvider.AddMessage("MainPage", "Testuser", "Subject", date, "Body", -1);
			int replyId = sourceDummyPagesStorageProvider.AddMessage("MainPage", "ReplyTestuser", "RE: Subject", date.AddDays(1), "Reply Body", messageId);

			// Content Templates
			ContentTemplate contentTemplate = sourceDummyPagesStorageProvider.AddContentTemplate("ContentTemplate1", "Content");

			// Snippets
			Snippet snippet = sourceDummyPagesStorageProvider.AddSnippet("Snippet1", "Content");

			byte[] backupFile = BackupRestore.BackupPagesStorageProvider(sourceDummyPagesStorageProvider);

			DummyPagesStorageProvider destinationDummyPagesStorageProvider = new DummyPagesStorageProvider();

			Assert.IsTrue(BackupRestore.RestorePagesStorageProvider(backupFile, destinationDummyPagesStorageProvider));

			// Namespaces
			NamespaceInfo[] namespaces = destinationDummyPagesStorageProvider.GetNamespaces();
			Assert.AreEqual(1, namespaces.Length);
			Assert.AreEqual(ns1.Name, namespaces[0].Name);
			Assert.AreEqual(ns1.DefaultPageFullName, namespaces[0].DefaultPageFullName);

			// Categories
			CategoryInfo[] categories = destinationDummyPagesStorageProvider.GetCategories(namespaces[0]);
			Assert.AreEqual(2, categories.Length);
			Assert.AreEqual("ns1.cat1", categories[0].FullName);
			Assert.AreEqual("ns1.cat2", categories[1].FullName);

			// Navigation Paths
			NavigationPath[] navigationPaths = destinationDummyPagesStorageProvider.GetNavigationPaths(namespaces[0]);
			Assert.AreEqual(1, navigationPaths.Length);
			Assert.AreEqual("ns1.np1", navigationPaths[0].FullName);
			CollectionAssert.AreEqual(new string[] { "MainPage", "Page1" }, navigationPaths[0].Pages);

			// Pages
			PageContent[] pages = destinationDummyPagesStorageProvider.GetPages(ns1);
			Assert.AreEqual(1, pages.Length);
			Assert.AreEqual(mainPage.FullName, pages[0].FullName);
			Assert.AreEqual(mainPage.Comment, pages[0].Comment);
			Assert.AreEqual(mainPage.Content, pages[0].Content);
			Assert.AreEqual(mainPage.CreationDateTime.Hour, pages[0].CreationDateTime.Hour);
			Assert.AreEqual(mainPage.Description, pages[0].Description);
			CollectionAssert.AreEqual(mainPage.Keywords, pages[0].Keywords);
			Assert.AreEqual(mainPage.LastModified.Hour, pages[0].LastModified.Hour);
			CollectionAssert.AreEqual(mainPage.LinkedPages, pages[0].LinkedPages);
			Assert.AreEqual(mainPage.Title, pages[0].Title);
			Assert.AreEqual(mainPage.User, pages[0].User);

			// PageDraft
			PageContent draft = destinationDummyPagesStorageProvider.GetDraft(mainPage.FullName);
			Assert.IsNotNull(draft);
			Assert.AreEqual(draftMainPage.Comment, draft.Comment);
			Assert.AreEqual(draftMainPage.Content, draft.Content);
			Assert.AreEqual(draftMainPage.CreationDateTime.Hour, draft.CreationDateTime.Hour);
			Assert.AreEqual(draftMainPage.Description, draft.Description);
			Assert.AreEqual(draftMainPage.FullName, draft.FullName);
			CollectionAssert.AreEqual(draftMainPage.Keywords, draft.Keywords);
			Assert.AreEqual(draftMainPage.LastModified.Hour, draft.LastModified.Hour);
			CollectionAssert.AreEqual(draftMainPage.LinkedPages, draft.LinkedPages);
			Assert.AreEqual(draftMainPage.Title, draft.Title);
			Assert.AreEqual(draftMainPage.User, draft.User);

			// Page Revisions
			int[] revs = destinationDummyPagesStorageProvider.GetBackups(mainPage.FullName);
			Assert.AreEqual(1, revs.Length);
			PageContent pageBackup = destinationDummyPagesStorageProvider.GetBackupContent(mainPage.FullName, revs[0]);
			Assert.AreEqual(oldMainPage.Comment, pageBackup.Comment);
			Assert.AreEqual(oldMainPage.Content, pageBackup.Content);
			Assert.AreEqual(oldMainPage.CreationDateTime.Hour, pageBackup.CreationDateTime.Hour);
			Assert.AreEqual(oldMainPage.Description, pageBackup.Description);
			Assert.AreEqual(oldMainPage.FullName, pageBackup.FullName);
			CollectionAssert.AreEqual(oldMainPage.Keywords, pageBackup.Keywords);
			Assert.AreEqual(oldMainPage.LastModified.Hour, pageBackup.LastModified.Hour);
			CollectionAssert.AreEqual(oldMainPage.LinkedPages, pageBackup.LinkedPages);
			Assert.AreEqual(oldMainPage.Title, pageBackup.Title);
			Assert.AreEqual(oldMainPage.User, pageBackup.User);

			// Messages
			Message[] messages = destinationDummyPagesStorageProvider.GetMessages(mainPage.FullName);
			Assert.AreEqual(1, messages.Length);
			Assert.AreEqual("Body", messages[0].Body);
			Assert.AreEqual(date.Hour, messages[0].DateTime.Hour);
			Assert.AreEqual(messageId, messages[0].ID);
			Assert.AreEqual("Subject", messages[0].Subject);
			Assert.AreEqual("Testuser", messages[0].Username);
			Assert.AreEqual(1, messages[0].Replies.Length);
			Assert.AreEqual("Reply Body", messages[0].Replies[0].Body);
			Assert.AreEqual(date.AddDays(1).Hour, messages[0].Replies[0].DateTime.Hour);
			Assert.AreEqual(replyId, messages[0].Replies[0].ID);
			Assert.AreEqual("RE: Subject", messages[0].Replies[0].Subject);
			Assert.AreEqual("ReplyTestuser", messages[0].Replies[0].Username);

			// ContentTemplates
			ContentTemplate[] contentTemplates = destinationDummyPagesStorageProvider.GetContentTemplates();
			Assert.AreEqual(1, contentTemplates.Length);
			Assert.AreEqual(contentTemplate.Name, contentTemplates[0].Name);
			Assert.AreEqual(contentTemplate.Content, contentTemplates[0].Content);

			// Snippets
			Snippet[] snippets = destinationDummyPagesStorageProvider.GetSnippets();
			Assert.AreEqual(1, snippets.Length);
			Assert.AreEqual(snippet.Name, snippets[0].Name);
			Assert.AreEqual(snippet.Content, snippets[0].Content);
		}
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

	internal class DummyPagesStorageProvider :IPagesStorageProviderV40 {
		private List<NamespaceInfo> namespaces;
		private List<CategoryInfo> categories;
		private List<NavigationPath> navigationPaths;
		private List<Snippet> snippets;
		private List<ContentTemplate> contentTemplates;
		private PageContent page;
		private PageContent draft;
		private PageContent backup;
		private int revision;
		private List<Message> messages;

		#region IPagesStorageProviderV40 Members

		public NamespaceInfo GetNamespace(string name) {
			return namespaces.FirstOrDefault(n => n.Name == name);
		}

		public NamespaceInfo[] GetNamespaces() {
			return namespaces.ToArray();
		}

		public NamespaceInfo AddNamespace(string name) {
			if(namespaces == null) {
				namespaces = new List<NamespaceInfo>();
			}
			NamespaceInfo ns = new NamespaceInfo(name, this, null);
			namespaces.Add(ns);
			return ns;
		}

		public NamespaceInfo RenameNamespace(NamespaceInfo nspace, string newName) {
			throw new NotImplementedException();
		}

		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, string pageFullName) {
			NamespaceInfo ns = namespaces.FirstOrDefault(n => n.Name == nspace.Name);
			if(ns != null) {
				ns.DefaultPageFullName = pageFullName;
			}
			return ns;
		}

		public bool RemoveNamespace(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent MovePage(string pageFullName, NamespaceInfo destination, bool copyCategories) {
			throw new NotImplementedException();
		}

		public CategoryInfo GetCategory(string fullName) {
			return categories.FirstOrDefault(c => c.FullName == fullName);
		}

		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			if(nspace == null) {
				return categories.FindAll(c => NameTools.GetNamespace(c.FullName) == null).ToArray();
			}
			return categories.FindAll(c => NameTools.GetNamespace(c.FullName) == nspace.Name).ToArray();
		}

		public CategoryInfo[] GetCategoriesForPage(string pageFullName) {
			throw new NotImplementedException();
		}

		public CategoryInfo AddCategory(string nspace, string name) {
			if(categories == null) {
				categories = new List<CategoryInfo>();
			}
			CategoryInfo cat = new CategoryInfo(NameTools.GetFullName(nspace, name), this);
			categories.Add(cat);
			return cat;
		}

		public CategoryInfo RenameCategory(CategoryInfo category, string newName) {
			throw new NotImplementedException();
		}

		public bool RemoveCategory(CategoryInfo category) {
			throw new NotImplementedException();
		}

		public CategoryInfo MergeCategories(CategoryInfo source, CategoryInfo destination) {
			throw new NotImplementedException();
		}

		public bool RebindPage(string pageFullName, string[] categories) {
			throw new NotImplementedException();
		}

		public PageContent GetPage(string fullName) {
			return page;
		}

		public PageContent[] GetPages(NamespaceInfo nspace) {
			if(nspace != null && nspace.Name == NameTools.GetNamespace(page.FullName)) {
				return new PageContent[] { page };
			}
			return new PageContent[0];
		}

		public PageContent[] GetUncategorizedPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent GetDraft(string fullName) {
			return draft;
		}

		public bool DeleteDraft(string fullName) {
			throw new NotImplementedException();
		}

		public int[] GetBackups(string fullName) {
			return new int[] { revision };
		}

		public PageContent GetBackupContent(string fullName, int revision) {
			if(revision == this.revision) {
				return backup;
			}
			return null;
		}

		public bool SetBackupContent(PageContent content, int revision) {
			backup = content;
			this.revision = revision;
			return true;
		}

		public PageContent SetPageContent(string nspace, string pageName, DateTime creationDateTime, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode) {
			PageContent temp = new PageContent(NameTools.GetFullName(nspace, pageName), this, creationDateTime, title, username, dateTime, comment, content, keywords, description);
			if(saveMode == SaveMode.Normal) {
				page = temp;
				return page;
			}
			if(saveMode == SaveMode.Draft) {
				draft = temp;
				return draft;
			}
			if(saveMode == SaveMode.Backup) {
				revision = 12;
				backup = page;
				page = temp;
				return page;
			}
			return null;
		}

		public PageContent RenamePage(string fullName, string newName) {
			throw new NotImplementedException();
		}

		public bool RollbackPage(string pageFullName, int revision) {
			throw new NotImplementedException();
		}

		public bool DeleteBackups(string pageFullName, int revision) {
			throw new NotImplementedException();
		}

		public bool RemovePage(string pageFullName) {
			throw new NotImplementedException();
		}

		public Message[] GetMessages(string pageFullName) {
			return messages.ToArray();
		}

		public int GetMessageCount(string pageFullName) {
			throw new NotImplementedException();
		}

		public bool BulkStoreMessages(string pageFullName, Message[] messages) {
			this.messages = new List<Message>(messages);
			return true;
		}

		public int AddMessage(string pageFullName, string username, string subject, DateTime dateTime, string body, int parent) {
			if(messages == null) {
				messages = new List<Message>();
			}
			if(parent == -1) {
				int messageId = messages.Count + 1;
				messages.Add(new Message(messageId, username, subject, dateTime, body));
				return messageId;
			}
			else {
				int messageId = messages.Count + 1;
				Message message = messages.FirstOrDefault(m => m.ID == parent);
				if(message != null) {
					Message reply = new Message(messageId, username, subject, dateTime, body);
					message.Replies = new Message[] { reply };
				}
				return messageId;
			}
		}

		public bool RemoveMessage(string pageFullName, int id, bool removeReplies) {
			throw new NotImplementedException();
		}

		public bool ModifyMessage(string pageFullName, int id, string username, string subject, DateTime dateTime, string body) {
			throw new NotImplementedException();
		}

		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			if(nspace == null) {
				return navigationPaths.FindAll(np => NameTools.GetNamespace(np.FullName) == null).ToArray();
			}
			return navigationPaths.FindAll(np => NameTools.GetNamespace(np.FullName) == nspace.Name).ToArray();
		}

		public NavigationPath AddNavigationPath(string nspace, string name, string[] pages) {
			if(navigationPaths == null) {
				navigationPaths = new List<NavigationPath>();
			}
			NavigationPath np = new NavigationPath(NameTools.GetFullName(nspace, name), this);
			np.Pages = pages;
			navigationPaths.Add(np);
			return np;
		}

		public NavigationPath ModifyNavigationPath(NavigationPath path, string[] pages) {
			throw new NotImplementedException();
		}

		public bool RemoveNavigationPath(NavigationPath path) {
			throw new NotImplementedException();
		}

		public Snippet[] GetSnippets() {
			return snippets.ToArray();
		}

		public Snippet AddSnippet(string name, string content) {
			if(snippets == null) {
				snippets = new List<Snippet>();
			}
			Snippet snippet = new Snippet(name, content, this);
			snippets.Add(snippet);
			return snippet;
		}

		public Snippet ModifySnippet(string name, string content) {
			throw new NotImplementedException();
		}

		public bool RemoveSnippet(string name) {
			throw new NotImplementedException();
		}

		public ContentTemplate[] GetContentTemplates() {
			return contentTemplates.ToArray();
		}

		public ContentTemplate AddContentTemplate(string name, string content) {
			if(contentTemplates == null) {
				contentTemplates = new List<ContentTemplate>();
			}
			ContentTemplate ct = new ContentTemplate(name, content, this);
			contentTemplates.Add(ct);
			return ct;
		}

		public ContentTemplate ModifyContentTemplate(string name, string content) {
			throw new NotImplementedException();
		}

		public bool RemoveContentTemplate(string name) {
			throw new NotImplementedException();
		}

		#endregion

		#region IStorageProviderV40 Members

		public bool ReadOnly {
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
			
		}

		#endregion
	}
}
