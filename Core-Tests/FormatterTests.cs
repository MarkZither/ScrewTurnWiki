
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
			PageInfo currentPage = null;
			string[] linkedPages = null;

			string _input = Formatter.Format(input, false, context, currentPage, out linkedPages, false);

			// Ignore \r characters
			// Ignore \n characters

			Assert.AreEqual(output, _input, "Formatter output is different from expected output");
		}

		[SetUp]
		public void SetUp() {
			mocks = new MockRepository();

			ISettingsStorageProviderV30 settingsProvider = mocks.StrictMock<ISettingsStorageProviderV30>();
			Expect.Call(settingsProvider.GetSetting("ProcessSingleLineBreaks")).Return("false").Repeat.Any();
			Expect.Call(settingsProvider.GetSetting("WikiTitle")).Return("Title").Repeat.Any();

			Collectors.SettingsProvider = settingsProvider;

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(DummyPagesStorageProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyPagesStorageProvider)), typeof(IPagesStorageProviderV30), true);

			Expect.Call(settingsProvider.GetSetting("DefaultPagesProvider")).Return(typeof(DummyPagesStorageProvider).FullName).Repeat.Any();
			Expect.Call(settingsProvider.GetPluginStatus(typeof(DummyPagesStorageProvider).FullName)).Return(true).Repeat.Any();
			Expect.Call(settingsProvider.GetPluginConfiguration(typeof(DummyPagesStorageProvider).FullName)).Return("").Repeat.Any();

			Host.Instance = new Host();
						
			settingsProvider.LogEntry("", EntryType.General, "");
			LastCall.On(settingsProvider).IgnoreArguments().Repeat.Any();

			mocks.Replay(settingsProvider);

			//System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter(new System.IO.StreamWriter(new System.IO.MemoryStream()));
			//System.Web.Hosting.SimpleWorkerRequest request = new System.Web.Hosting.SimpleWorkerRequest("Default.aspx", "?Page=MainPage", writer);
			System.Web.HttpContext.Current = new System.Web.HttpContext(new DummyRequest());
		}

		[TearDown]
		public void TearDown() {
			mocks.VerifyAll();
		}

	}

	public class DummyRequest : System.Web.HttpWorkerRequest {

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
			return "http://localhost/Default.aspx";
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

	public class DummyPagesStorageProvider : IPagesStorageProviderV30 {

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

		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, PageInfo page) {
			throw new NotImplementedException();
		}

		public bool RemoveNamespace(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageInfo MovePage(PageInfo page, NamespaceInfo destination, bool copyCategories) {
			throw new NotImplementedException();
		}

		public CategoryInfo GetCategory(string fullName) {
			throw new NotImplementedException();
		}

		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public CategoryInfo[] GetCategoriesForPage(PageInfo page) {
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

		public SearchEngine.SearchResultCollection PerformSearch(SearchEngine.SearchParameters parameters) {
			throw new NotImplementedException();
		}

		public void RebuildIndex() {
			throw new NotImplementedException();
		}

		public void GetIndexStats(out int documentCount, out int wordCount, out int occurrenceCount, out long size) {
			throw new NotImplementedException();
		}

		public bool IsIndexCorrupted {
			get { throw new NotImplementedException(); }
		}

		public PageInfo GetPage(string fullName) {
			if(fullName == "page1") {
				return new PageInfo(fullName, this, DateTime.Now);
			}
			return null;
		}

		public PageInfo[] GetPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageInfo[] GetUncategorizedPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public PageContent GetContent(PageInfo page) {
			if(page.FullName == "page1") {
				return new PageContent(page, "Page 1", "", DateTime.Now, "", "", new string[] { }, "");
			}
			else return null;
		}

		public PageContent GetDraft(PageInfo page) {
			throw new NotImplementedException();
		}

		public bool DeleteDraft(PageInfo page) {
			throw new NotImplementedException();
		}

		public int[] GetBackups(PageInfo page) {
			throw new NotImplementedException();
		}

		public PageContent GetBackupContent(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		public bool SetBackupContent(PageContent content, int revision) {
			throw new NotImplementedException();
		}

		public PageInfo AddPage(string nspace, string name, DateTime creationDateTime) {
			throw new NotImplementedException();
		}

		public PageInfo RenamePage(PageInfo page, string newName) {
			throw new NotImplementedException();
		}

		public bool ModifyPage(PageInfo page, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode) {
			throw new NotImplementedException();
		}

		public bool RollbackPage(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		public bool DeleteBackups(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		public bool RemovePage(PageInfo page) {
			throw new NotImplementedException();
		}

		public bool RebindPage(PageInfo page, string[] categories) {
			throw new NotImplementedException();
		}

		public Message[] GetMessages(PageInfo page) {
			throw new NotImplementedException();
		}

		public int GetMessageCount(PageInfo page) {
			throw new NotImplementedException();
		}

		public bool BulkStoreMessages(PageInfo page, Message[] messages) {
			throw new NotImplementedException();
		}

		public bool AddMessage(PageInfo page, string username, string subject, DateTime dateTime, string body, int parent) {
			throw new NotImplementedException();
		}

		public bool RemoveMessage(PageInfo page, int id, bool removeReplies) {
			throw new NotImplementedException();
		}

		public bool ModifyMessage(PageInfo page, int id, string username, string subject, DateTime dateTime, string body) {
			throw new NotImplementedException();
		}

		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		public NavigationPath AddNavigationPath(string nspace, string name, PageInfo[] pages) {
			throw new NotImplementedException();
		}

		public NavigationPath ModifyNavigationPath(NavigationPath path, PageInfo[] pages) {
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

		public bool ReadOnly {
			get { return false; }
		}

		public void Init(IHostV30 host, string config) { }

		public void SetUp() { }

		void IDisposable.Dispose() { }

		public ComponentInformation Information {
			get {
				return new ComponentInformation("dummy", "", "", "", "");
			}
		}

		public string ConfigHelpHtml {
			get { throw new NotImplementedException(); }
		}

	}

}
