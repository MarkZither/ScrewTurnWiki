
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

			string pageTitle = "This is the title of the page";
			string pageContent = "This is the content of the page";

			PageContent page = new PageContent("pagefullname", pagesStorageProvider, DateTime.Now, pageTitle,
												"user-test", DateTime.Now, "comment to last editing", pageContent, null, "Description of the page");


			Assert.IsTrue(SearchClass.IndexPage(page));

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle }, "page");

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(pageTitle, results[0].Title, "Wrong title");
			Assert.AreEqual(pageContent, results[0].Content, "Wrong content");
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

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle }, "page");

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(pageTitle1, results[0].Title, "Wrong title");
			Assert.AreEqual(pageContent1, results[0].Content, "Wrong content");

			Assert.AreEqual(pageTitle2, results[1].Title, "Wrong title");
			Assert.AreEqual(pageContent2, results[1].Content, "Wrong content");
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

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page");

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(pageTitle1, results[1].Title, "Wrong title");
			Assert.AreEqual(pageContent1, results[1].Content, "Wrong content");

			Assert.AreEqual(pageTitle2, results[0].Title, "Wrong title");
			Assert.AreEqual(pageContent2, results[0].Content, "Wrong content");
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

			List<SearchResult> results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page");

			Assert.AreEqual(2, results.Count, "Wrong result length");

			Assert.AreEqual(page1.Provider.CurrentWiki, results[1].Wiki, "Wrong wiki");
			Assert.AreEqual(page1.FullName, results[1].PageFullName, "Wrong page full name");
			Assert.AreEqual(pageTitle1, results[1].Title, "Wrong title");
			Assert.AreEqual(pageContent1, results[1].Content, "Wrong content");

			Assert.AreEqual(page2.Provider.CurrentWiki, results[0].Wiki, "Wrong wiki");
			Assert.AreEqual(page2.FullName, results[0].PageFullName, "Wrong page full name");
			Assert.AreEqual(pageTitle2, results[0].Title, "Wrong title");
			Assert.AreEqual(pageContent2, results[0].Content, "Wrong content");

			Assert.IsTrue(SearchClass.UnindexPage(page1));

			results = SearchClass.Search("wiki1", new SearchField[] { SearchField.PageTitle, SearchField.PageContent }, "page");

			Assert.AreEqual(1, results.Count, "Wrong result length");

			Assert.AreEqual(pageTitle2, results[0].Title, "Wrong title");
			Assert.AreEqual(pageContent2, results[0].Content, "Wrong content");
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