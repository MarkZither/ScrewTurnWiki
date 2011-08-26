
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class FormatterTests {

	private const string Input =
@"'''bold''' ''italic'' __underlined__ --striked-- [page1]\r\n[page2|title]
* item 1
* item 2

second line";

	private const string ExpectedOutput =
@"<b>bold</b> <i>italic</i> <u>underlined</u> <strike>striked</strike> <a class=""pagelink"" href=""page1.ashx"" title=""Page 1"">page1</a>\r\n" +
"<a class=\"unknownlink\" href=\"page2.ashx\" title=\"page2\">title</a>\n<ul><li>item 1</li><li>item 2<br /></li></ul><br />second line\n";



		private MockRepository mocks;

		[Test]
		[TestCase("{wikititle}","Title\n")]
		[TestCase("@@rigatesto1\r\nriga2@@","<pre>rigatesto1\r\nriga2</pre>\n")]
		[TestCase(Input,ExpectedOutput)]
		public void Format(string input, string output) {
			FormattingContext context = FormattingContext.PageContent;
			string currentPageFullName = null;
			string[] linkedPages = null;

			string _input = Formatter.Format(null, input, false, context, currentPageFullName, out linkedPages, false);

			// Ignore \r characters
			// Ignore \n characters

			Assert.AreEqual(output, _input, "Formatter output is different from expected output");
		}

		[SetUp]
		public void SetUp() {
			mocks = new MockRepository();
			
			System.Web.HttpContext.Current = new System.Web.HttpContext(new DummyRequest());

			Collectors.InitCollectors();

			Collectors.AddGlobalSettingsStorageProvider(typeof(DummyGlobalSettingsStorageProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyGlobalSettingsStorageProvider)));

			Collectors.AddProvider(typeof(DummySettingsStorageProvider), System.Reflection.Assembly.GetAssembly(typeof(DummySettingsStorageProvider)), "", typeof(ISettingsStorageProviderV40));
			
			Collectors.AddProvider(typeof(DummyPagesStorageProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyPagesStorageProvider)), "", typeof(IPagesStorageProviderV40));
			
			Host.Instance = new Host();
		}

		[TearDown]
		public void TearDown() {
			mocks.VerifyAll();
		}

		private class DummyGlobalSettingsStorageProvider : IGlobalSettingsStorageProviderV40 {

			#region IGlobalSettingsStorageProviderV40 Members

			public string GetSetting(string name) {
				switch(name) {
					case "DefaultPagesProvider":
						return typeof(DummyPagesStorageProvider).FullName;
					default:
						return null;
				}
			}

			public IDictionary<string, string> GetAllSettings() {
				throw new NotImplementedException();
			}

			public bool SetSetting(string name, string value) {
				throw new NotImplementedException();
			}

			public IList<Wiki.PluginFramework.Wiki> AllWikis() {
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
				if(typeName == typeof(DummyPagesStorageProvider).FullName) return true;
				return false;
			}

			public bool SetPluginConfiguration(string typeName, string config) {
				throw new NotImplementedException();
			}

			public string GetPluginConfiguration(string typeName) {
				if(typeName == typeof(DummyPagesStorageProvider).FullName) return "";
				return null;
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
				// Nothing to-do
			}

			public void SetUp(IHostV40 host, string config) {
				// Nothing to-do
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

		private class DummySettingsStorageProvider : ISettingsStorageProviderV40 {

			#region ISettingsStorageProviderV40 Members

			public string GetSetting(string name) {
				switch(name) {
					case "ProcessSingleLineBreaks":
						return "false";
					case "WikiTitle":
						return "Title";
					case "DefaultPagesProvider":
						return typeof(DummyPagesStorageProvider).FullName;
					default:
						return null;
				}
			}

			public bool SetSetting(string name, string value) {
				throw new NotImplementedException();
			}

			public IDictionary<string, string> GetAllSettings() {
				throw new NotImplementedException();
			}

			public void BeginBulkUpdate() {
				throw new NotImplementedException();
			}

			public void EndBulkUpdate() {
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

			public string GetMetaDataItem(MetaDataItem item, string tag) {
				throw new NotImplementedException();
			}

			public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
				throw new NotImplementedException();
			}

			public RecentChange[] GetRecentChanges() {
				throw new NotImplementedException();
			}

			public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, Change change, string descr) {
				throw new NotImplementedException();
			}

			public AclEngine.IAclManager AclManager {
				get { throw new NotImplementedException(); }
			}

			public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
				throw new NotImplementedException();
			}

			public string[] GetOutgoingLinks(string page) {
				throw new NotImplementedException();
			}

			public IDictionary<string, string[]> GetAllOutgoingLinks() {
				throw new NotImplementedException();
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
				//Nothing TODO
			}

			public void SetUp(IHostV40 host, string config) {
				//Nothing TODO
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
				//Nothing TODO
			}

			#endregion

		}

	}

	public class DummyRequest : System.Web.HttpWorkerRequest{

		public override void EndOfRequest() {
		}

		public override void FlushResponse(bool finalFlush) {
		}

		public override string GetHttpVerbName() {
			return "GET";
		}

		public override string GetHttpVersion() {
			return "1.1";
		}

		public override string GetLocalAddress() {
			return "http://localhost/";
		}

		public override int GetLocalPort() {
			return 80;
		}

		public override string GetQueryString() {
			return "";
		}

		public override string GetRawUrl() {
			return "/Default.aspx";
		}

		public override string GetRemoteAddress() {
			return "127.0.0.1";
		}

		public override int GetRemotePort() {
			return 45695;
		}

		public override string GetUriPath() {
			return "/";
		}

		public override void SendKnownResponseHeader(int index, string value) {
		}

		public override void SendResponseFromFile(IntPtr handle, long offset, long length) {
		}

		public override void SendResponseFromFile(string filename, long offset, long length) {
		}

		public override void SendResponseFromMemory(byte[] data, int length) {
		}

		public override void SendStatus(int statusCode, string statusDescription) {
		}

		public override void SendUnknownResponseHeader(string name, string value) {
		}
	}

	public class DummyPagesStorageProvider : IPagesStorageProviderV40 {

		#region IPagesStorageProviderV40 Members

		public NamespaceInfo GetNamespace(string name) {
			throw new NotImplementedException();
		}

		public NamespaceInfo[] GetNamespaces() {
			throw new NotImplementedException();
		}

		public NamespaceInfo AddNamespace(string name) {
			throw new NotImplementedException();
		}

		public NamespaceInfo RenameNamespace(NamespaceInfo nspace, string newName) {
			throw new NotImplementedException();
		}

		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, string pageFullName) {
			throw new NotImplementedException();
		}

		public bool RemoveNamespace(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent MovePage(string pageFullName, NamespaceInfo destination, bool copyCategories) {
			throw new NotImplementedException();
		}

		public CategoryInfo GetCategory(string fullName) {
			throw new NotImplementedException();
		}

		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public CategoryInfo[] GetCategoriesForPage(string pageFullName) {
			throw new NotImplementedException();
		}

		public CategoryInfo AddCategory(string nspace, string name) {
			throw new NotImplementedException();
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

		public PageContent GetPage(string fullName) {
			if(fullName == "page1") {
				return new PageContent(fullName, this, DateTime.Now, "Page 1", "", DateTime.Now, "", "", new string[] { }, "");
			}
			return null;
		}

		public PageContent[] GetPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent[] GetUncategorizedPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent GetDraft(string fullName) {
			throw new NotImplementedException();
		}

		public bool DeleteDraft(string fullName) {
			throw new NotImplementedException();
		}

		public int[] GetBackups(string fullName) {
			throw new NotImplementedException();
		}

		public PageContent GetBackupContent(string fullName, int revision) {
			throw new NotImplementedException();
		}

		public bool SetBackupContent(PageContent content, int revision) {
			throw new NotImplementedException();
		}

		public PageContent SetPageContent(string nspace, string pageName, DateTime creationDateTime, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode) {
			throw new NotImplementedException();
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

		public bool RebindPage(string pageFullName, string[] categories) {
			throw new NotImplementedException();
		}

		public Message[] GetMessages(string pageFullName) {
			throw new NotImplementedException();
		}

		public int GetMessageCount(string pageFullName) {
			throw new NotImplementedException();
		}

		public bool BulkStoreMessages(string pageFullName, Message[] messages) {
			throw new NotImplementedException();
		}

		public int AddMessage(string pageFullName, string username, string subject, DateTime dateTime, string body, int parent) {
			throw new NotImplementedException();
		}

		public bool RemoveMessage(string pageFullName, int id, bool removeReplies) {
			throw new NotImplementedException();
		}

		public bool ModifyMessage(string pageFullName, int id, string username, string subject, DateTime dateTime, string body) {
			throw new NotImplementedException();
		}

		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public NavigationPath AddNavigationPath(string nspace, string name, string[] pages) {
			throw new NotImplementedException();
		}

		public NavigationPath ModifyNavigationPath(NavigationPath path, string[] pages) {
			throw new NotImplementedException();
		}

		public bool RemoveNavigationPath(NavigationPath path) {
			throw new NotImplementedException();
		}

		public Snippet[] GetSnippets() {
			throw new NotImplementedException();
		}

		public Snippet AddSnippet(string name, string content) {
			throw new NotImplementedException();
		}

		public Snippet ModifySnippet(string name, string content) {
			throw new NotImplementedException();
		}

		public bool RemoveSnippet(string name) {
			throw new NotImplementedException();
		}

		public ContentTemplate[] GetContentTemplates() {
			throw new NotImplementedException();
		}

		public ContentTemplate AddContentTemplate(string name, string content) {
			throw new NotImplementedException();
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
			get { return false; }
		}

		#endregion

		#region IProviderV40 Members

		public string CurrentWiki {
			get { throw new NotImplementedException(); }
		}

		public void Init(IHostV40 host, string config, string wiki) {
			// Nothing to do
		}

		public void SetUp(IHostV40 host, string config) {
			// Nothing to do
		}

		public ComponentInformation Information {
			get {
				return new ComponentInformation("dummy", "", "", "", "");
			}
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
