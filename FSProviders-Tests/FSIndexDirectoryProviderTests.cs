
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using Rhino.Mocks;
using Lucene.Net.Store;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class FSIndexDirectoryProviderTests {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		protected IHostV40 MockHost() {
			if(!System.IO.Directory.Exists(testDir)) System.IO.Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		[TearDown]
		public void TearDown() {
			System.IO.Directory.Delete(testDir, true);
		}

		[Test]
		public void AddPageTest() {
			IPagesStorageProviderV40 pagesStorageProvider = mocks.DynamicMock<IPagesStorageProviderV40>();
			Expect.Call(pagesStorageProvider.CurrentWiki).Return("wiki1").Repeat.Any();

			mocks.ReplayAll();

			Collectors.InitCollectors();
			Collectors.AddProvider(typeof(FSIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(FSIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(FSIndexDirectoryProvider), "");

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
			Collectors.AddProvider(typeof(FSIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(FSIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(FSIndexDirectoryProvider), "");

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
			Collectors.AddProvider(typeof(FSIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(FSIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(FSIndexDirectoryProvider), "");

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
			Collectors.AddProvider(typeof(FSIndexDirectoryProvider), System.Reflection.Assembly.GetAssembly(typeof(FSIndexDirectoryProvider)), "", typeof(IIndexDirectoryProviderV40));
			Host.Instance = new Host();
			Host.Instance.OverridePublicDirectory(testDir);

			ProviderLoader.SetUp<IIndexDirectoryProviderV40>(typeof(FSIndexDirectoryProvider), "");

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

	}
}