
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using Rhino.Mocks;
using Lucene.Net.Store;
using System.IO;
using System.Configuration;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzureIndexDirectoryProviderTests {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		protected IHostV40 MockHost() {
			if(!System.IO.Directory.Exists(testDir)) System.IO.Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		[SetUp]
		public void SetUp() {
			if(!System.IO.Directory.Exists(testDir)) System.IO.Directory.CreateDirectory(testDir);
		}

		[TearDown]
		public void TearDown() {
			System.IO.Directory.Delete(testDir, true);

			TableStorage.DeleteAllBlobs(ConfigurationManager.AppSettings["AzureConnString"]);
		}

		[Test]
		public void AddPageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string pageTitle = "This is the title of the page";
			string pageContent = "This is the content of the page";

			PageContent page = new PageContent("pagefullname", pagesStorageProvider, DateTime.Now, pageTitle,
												"user-test", DateTime.Now, "comment to last editing", pageContent, null, "Description of the page");


			Assert.IsTrue(SearchClass.IndexPage(page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");

			PageDocument documentPage = results[0].Document as PageDocument;

			Assert.AreEqual("This is the title of the <b class=\"searchkeyword\">page</b>", documentPage.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <b class=\"searchkeyword\">page</b>", documentPage.HighlightedContent, "Wrong content");
		}

		[Test]
		public void AddMultiplePageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string pageTitle1 = "This is the title of the page";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			PageDocument doc1 = results[0].Document as PageDocument;
			Assert.AreEqual("This is the title of the <b class=\"searchkeyword\">page</b>", doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <b class=\"searchkeyword\">page</b>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			PageDocument doc2 = results[1].Document as PageDocument;
			Assert.AreEqual("This is the title of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedContent, "Wrong content");
		}

		[Test]
		public void SearchIntoMultipleFieldsTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			PageDocument doc1 = results[1].Document as PageDocument;
			Assert.AreEqual(string.Empty, doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <b class=\"searchkeyword\">page</b>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			PageDocument doc2 = results[0].Document as PageDocument;
			Assert.AreEqual("This is the title of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedContent, "Wrong content");
		}

		[Test]
		public void UnindexPageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			PageDocument doc1 = results[1].Document as PageDocument;
			Assert.AreEqual(string.Empty, doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <b class=\"searchkeyword\">page</b>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			PageDocument doc2 = results[0].Document as PageDocument;
			Assert.AreEqual("This is the title of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <b class=\"searchkeyword\">page</b>", doc2.HighlightedContent, "Wrong content");

			Assert.IsTrue(SearchClass.UnindexPage(page1));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			PageDocument doc3 = results[0].Document as PageDocument;
			Assert.AreEqual("This is the title of the second <b class=\"searchkeyword\">page</b>", doc3.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <b class=\"searchkeyword\">page</b>", doc3.HighlightedContent, "Wrong content");
		}

		[Test]
		public void AddMessageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string messageSubject = "This is the subject of the message";
			string messageBody = "This is the body of the message";

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent("pagefullname", pagesStorageProvider, dt, "title", "user-test", dt, "", "content", new string[0], "");
			Message message = new Message(1, "user-test", messageSubject, dt, messageBody);

			Assert.IsTrue(SearchClass.IndexMessage(message, page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Message, results[0].DocumentType, "Wrong document type");

			MessageDocument documentMessage = results[0].Document as MessageDocument;

			Assert.AreEqual("This is the subject of the message", documentMessage.Subject, "Wrong title");
			Assert.AreEqual("This is the body of the <b class=\"searchkeyword\">message</b>", documentMessage.HighlightedBody, "Wrong content");
		}


		[Test]
		public void UnindexMessageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string messageSubject = "This is the subject of the message";
			string messageBody = "This is the body of the message";

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent("pagefullname", pagesStorageProvider, dt, "title", "user-test", dt, "", "content", new string[0], "");
			Message message = new Message(1, "user-test", messageSubject, dt, messageBody);

			Assert.IsTrue(SearchClass.IndexMessage(message, page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Message, results[0].DocumentType, "Wrong document type");

			MessageDocument documentMessage = results[0].Document as MessageDocument;

			Assert.AreEqual("This is the subject of the message", documentMessage.Subject, "Wrong title");
			Assert.AreEqual("This is the body of the <b class=\"searchkeyword\">message</b>", documentMessage.HighlightedBody, "Wrong content");

			Assert.IsTrue(SearchClass.UnindexMessage(1, page));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void AddPageAttachmentTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent("pagefullname", pagesStorageProvider, dt, "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file_name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");
		}

		[Test]
		public void UnindexPageAttachmentTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent("pagefullname", pagesStorageProvider, dt, "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.UnindexPageAttachment(fileName, page));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void RenamePageAttachmentTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent("pagefullname", pagesStorageProvider, dt, "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.RenamePageAttachment(page, fileName, "file name_2"));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual("file name_2", pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

		}

		[Test]
		public void AddFileTest() {
			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string fileName = "file_name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath, "wiki1"));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");
		}

		[Test]
		public void UnindexFileTest() {
			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath, "wiki1"));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.UnindexFile(fileName, "wiki1"));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void RenameFileTest() {
			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(AzureIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(AzureIndexDirectoryProvider)), ConfigurationManager.AppSettings["AzureConnString"], typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(AzureIndexDirectoryProvider), ConfigurationManager.AppSettings["AzureConnString"]);

			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath)) {
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath, "wiki1"));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.RenameFile("wiki1", fileName, "file name_2"));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.Title, SearchField.Content }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual("file name_2", fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

		}

	}
}