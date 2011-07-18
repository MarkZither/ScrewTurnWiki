
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using Rhino.Mocks;
using Lucene.Net.Store;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class SearchTests {

		private MockRepository mocks = new MockRepository();

		[Test]
		public void AddPageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(DummyIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle = "This page is the title of the page";
			string pageContent = "This is the content of the page";

			PageContent page = new PageContent("pagefullname", pagesStorageProvider, DateTime.Now, pageTitle,
												"user-test", DateTime.Now, "comment to last editing", pageContent, null, "Description of the page");

			Assert.IsTrue(SearchClass.IndexPage(page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");

			DocumentPage documentPage = results[0].Document as DocumentPage;

			Assert.AreEqual("This <span class=\"searchResult\">page</span> is the title of the <span class=\"searchResult\">page</span>", documentPage.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <span class=\"searchResult\">page</span>", documentPage.HighlightedContent, "Wrong content");
		}

		[Test]
		public void AddMultiplePageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(DummyIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title of the page";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			DocumentPage doc1 = results[0].Document as DocumentPage;
			Assert.AreEqual("This is the title of the <span class=\"searchResult\">page</span>", doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <span class=\"searchResult\">page</span>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			DocumentPage doc2 = results[1].Document as DocumentPage;
			Assert.AreEqual("This is the title of the second <span class=\"searchResult\">page</span>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <span class=\"searchResult\">page</span>", doc2.HighlightedContent, "Wrong content");
		}

		[Test]
		public void SearchIntoMultipleFieldsTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(DummyIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			DocumentPage doc1 = results[1].Document as DocumentPage;
			Assert.AreEqual(string.Empty, doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <span class=\"searchResult\">page</span>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			DocumentPage doc2 = results[0].Document as DocumentPage;
			Assert.AreEqual("This is the title of the second <span class=\"searchResult\">page</span>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <span class=\"searchResult\">page</span>", doc2.HighlightedContent, "Wrong content");
		}

		[Test]
		public void UnindexPageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(DummyIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(DummyIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(DummyIndexDirectoryProvider), "");

			string pageTitle1 = "This is the title";
			string pageContent1 = "This is the content of the page";
			PageContent page1 = new PageContent("pagefullname1", pagesStorageProvider, DateTime.Now, pageTitle1,
												"user-test", DateTime.Now, "comment to last editing", pageContent1, null, "Description of the page");
			string pageTitle2 = "This is the title of the second page";
			string pageContent2 = "This is the content of the second page";
			PageContent page2 = new PageContent("pagefullname2", pagesStorageProvider, DateTime.Now, pageTitle2,
												"user-test", DateTime.Now, "comment to last editing", pageContent2, null, "Description of the page");

			Directory directory = new RAMDirectory();
			Assert.IsTrue(SearchClass.IndexPage(page1));
			Assert.IsTrue(SearchClass.IndexPage(page2));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[1].DocumentType, "Wrong document type");
			DocumentPage doc1 = results[1].Document as DocumentPage;
			Assert.AreEqual(string.Empty, doc1.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the <span class=\"searchResult\">page</span>", doc1.HighlightedContent, "Wrong content");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			DocumentPage doc2 = results[0].Document as DocumentPage;
			Assert.AreEqual("This is the title of the second <span class=\"searchResult\">page</span>", doc2.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <span class=\"searchResult\">page</span>", doc2.HighlightedContent, "Wrong content");

			Assert.IsTrue(SearchClass.UnindexPage(page1));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page", SearchOptions.AtLeastOneWord);

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(DocumentType.Page, results[0].DocumentType, "Wrong document type");
			DocumentPage doc3 = results[0].Document as DocumentPage;
			Assert.AreEqual("This is the title of the second <span class=\"searchResult\">page</span>", doc3.HighlightedTitle, "Wrong title");
			Assert.AreEqual("This is the content of the second <span class=\"searchResult\">page</span>", doc3.HighlightedContent, "Wrong content");
		}

		private class DummyIndexDirectoryProvider : IIndexDirectoryProviderV40 {

			#region IIndexDirectoryProviderV40 Members

			private static Directory directory;

			public Directory GetDirectory() {
				return directory;
			}

			#endregion

			#region IProviderV40 Members

			public string CurrentWiki {
				get { return "wiki1"; }
			}

			public void Init(IHostV40 host, string config, string wiki) {
				// Nothing to do.
			}

			public void SetUp(IHostV40 host, string config) {
				directory = new RAMDirectory();
			}

			public ComponentInformation Information {
				get { return new ComponentInformation("DummyIndexDirectoryProvider", "Threeplicate", "1.0", "", ""); }
			}

			public string ConfigHelpHtml {
				get { return ""; }
			}

			#endregion

			#region IDisposable Members

			public void Dispose() {
				// Nothing to do.
			}

			#endregion
		}
	}
}