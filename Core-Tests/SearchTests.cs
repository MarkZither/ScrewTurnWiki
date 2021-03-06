﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using Rhino.Mocks;
using Lucene.Net.Store;
using System.IO;

namespace ScrewTurn.Wiki.Tests
{

	[TestFixture]
	public class SearchTests
	{

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		[SetUp]
		public void SetUp()
		{
			if(!System.IO.Directory.Exists(testDir))
			{
				System.IO.Directory.CreateDirectory(testDir);
			}
		}

		[TearDown]
		public void TearDown()
		{
			System.IO.Directory.Delete(testDir, true);
		}

		[Test]
		public void AddPageTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();
			
			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");


			string pageTitle = "This page is the title of the page";
			string pageContent = "This is the content of the page";

			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, DateTime.Now), pageTitle,
												"user-test", DateTime.Now, "comment to last editing", pageContent, null, "Description of the page");

			Assert.IsTrue(SearchClass.IndexPage(page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.Title }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");

			PageDocument documentPage = results[0].Document as PageDocument;

			Assert.AreEqual("This <b class=\"searchkeyword\">page</b> is the title of the <b class=\"searchkeyword\">page</b>", documentPage.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <b class=\"searchkeyword\">page</b>", documentPage.HighlightedContent, "Wrong content");
		}

		[Test]
		public void AddMultiplePageTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title of the page";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent(new PageInfo("pagefullname1", pagesStorageProvider, DateTime.Now), pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent(new PageInfo("pagefullname2", pagesStorageProvider, DateTime.Now), pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.Title }, "page", SearchOptions.AtLeastOneWord);

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
		public void SearchIntoMultipleFieldsTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent(new PageInfo("pagefullname1", pagesStorageProvider, DateTime.Now), pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent(new PageInfo("pagefullname2", pagesStorageProvider, DateTime.Now), pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

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
		public void UnindexPageTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent(new PageInfo("pagefullname1", pagesStorageProvider, DateTime.Now), pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent(new PageInfo("pagefullname2", pagesStorageProvider, DateTime.Now), pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Lucene.Net.Store.Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search( new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

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

			results = SearchClass.Search(new SearchField[] { SearchField.Title, SearchField.Content }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			PageDocument doc3 = results[0].Document as PageDocument;
			Assert.AreEqual("This is the title of the second <b class=\"searchkeyword\">page</b>", doc3.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <b class=\"searchkeyword\">page</b>", doc3.HighlightedContent, "Wrong content");
		}

		[Test]
		public void AddMessageTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string messageSubject = "This is the subject of the message";
			string messageBody = "This is the body of the message";

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, dt), "title", "user-test", dt, "", "content", new string[0], "");
			Message message = new Message(1, "user-test", messageSubject, dt, messageBody);

			Assert.IsTrue(SearchClass.IndexMessage(message, page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Message, results[0].DocumentType, "Wrong document type");

			MessageDocument documentMessage = results[0].Document as MessageDocument;

			Assert.AreEqual("This is the subject of the message", documentMessage.Subject, "Wrong title");
			Assert.AreEqual("This is the body of the <b class=\"searchkeyword\">message</b>", documentMessage.HighlightedBody, "Wrong content");
		}

		[Test]
		public void UnindexMessageTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();

			mocks.ReplayAll();
			
			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string messageSubject = "This is the subject of the message";
			string messageBody = "This is the body of the message";

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, dt), "title", "user-test", dt, "", "content", new string[0], "");
			Message message = new Message(1, "user-test", messageSubject, dt, messageBody);

			Assert.IsTrue(SearchClass.IndexMessage(message, page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Message, results[0].DocumentType, "Wrong document type");

			MessageDocument documentMessage = results[0].Document as MessageDocument;

			Assert.AreEqual("This is the subject of the message", documentMessage.Subject, "Wrong title");
			Assert.AreEqual("This is the body of the <b class=\"searchkeyword\">message</b>", documentMessage.HighlightedBody, "Wrong content");

			Assert.IsTrue(SearchClass.UnindexMessage(1, page));

			results = SearchClass.Search(new SearchField[] { SearchField.Content }, "message", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void AddPageAttachmentTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();

			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, dt), "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file_name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");
		}

		[Test]
		public void UnindexPageAttachmentTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
		
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");


			DateTime dt = DateTime.Now;
			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, dt), "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.UnindexPageAttachment(fileName, page));

			results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void RenamePageAttachmentTest()
		{
			IPagesStorageProviderV30 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV30>();
			
			mocks.ReplayAll();

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			DateTime dt = DateTime.Now;
			PageContent page = new PageContent(new PageInfo("pagefullname", pagesStorageProvider, dt), "title", "user-test", dt, "", "content", new string[0], "");
			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexPageAttachment(fileName, filePath, page));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			PageAttachmentDocument pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual(fileName, pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.RenamePageAttachment(page, fileName, "file name_2"));

			results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Attachment, results[0].DocumentType, "Wrong document type");

			pageAttachmentDocument = results[0].Document as PageAttachmentDocument;

			Assert.AreEqual("file name_2", pageAttachmentDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", pageAttachmentDocument.HighlightedFileContent, "Wrong file content");

		}

		[Test]
		public void AddFileTest()
		{
			
			
			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string fileName = "file_name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");
		}

		[Test]
		public void UnindexFileTest()
		{
			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.UnindexFile(fileName));

			results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(0, results.Count, "Wrong result length");
		}

		[Test]
		public void RenameFileTest()
		{
			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			string fileName = "file name_1";

			string filePath = Path.Combine(testDir, "test.txt");
			using(StreamWriter writer = File.CreateText(filePath))
			{
				writer.Write("This is the content of a file");
			}

			Assert.IsTrue(SearchClass.IndexFile(fileName, filePath));

			List<SearchResult> results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			FileDocument fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual(fileName, fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

			Assert.IsTrue(SearchClass.RenameFile(fileName, "file name_2"));

			results = SearchClass.Search(new SearchField[] { SearchField.FileName, SearchField.FileContent }, "file", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.File, results[0].DocumentType, "Wrong document type");

			fileDocument = results[0].Document as FileDocument;

			Assert.AreEqual("file name_2", fileDocument.FileName, "Wrong file name");
			Assert.AreEqual("This is the content of a <b class=\"searchkeyword\">file</b>", fileDocument.HighlightedFileContent, "Wrong file content");

		}

		private class DummyIndexDirectoryProvider : IIndexDirectoryProviderV30
		{

			#region IIndexDirectoryProviderV30 Members

			private static Lucene.Net.Store.Directory directory;

			public Lucene.Net.Store.Directory GetDirectory()
			{
				return directory;
			}

			#endregion

			#region IProviderV30 Members

			public string CurrentWiki
			{
				get
				{
					return "wiki1";
				}
			}

			public void Init(IHostV30 host, string config)
			{
				// Nothing to do.
			}

			public void SetUp(IHostV30 host, string config)
			{
				directory = new RAMDirectory();
			}

			public ComponentInformation Information
			{
				get
				{
					return new ComponentInformation("DummyIndexDirectoryProvider", "Threeplicate", "1.0", "", "");
				}
			}

			public string ConfigHelpHtml
			{
				get
				{
					return "";
				}
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				// Nothing to do.
			}

			public void Shutdown()
			{
				// Nothing to do.
			}

			#endregion
		}
	}
}