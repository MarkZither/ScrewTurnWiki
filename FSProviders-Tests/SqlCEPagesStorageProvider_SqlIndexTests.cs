
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.SearchEngine.Tests;
using System.Data.SqlServerCe;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class SqlCEPagesStorageProvider_SqlIndexTests : IndexBaseTests {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Persist Security Info = False; Data Source = 'ScrewTurnWikiTest.sdf';";

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		private delegate string ToStringDelegate(string wiki, string p, string input);

		protected IHostV40 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider = mocks.DynamicMock<IGlobalSettingsStorageProviderV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();
			Expect.Call(host.GetGlobalSettingsStorageProvider()).Return(globalSettingsStorageProvider).Repeat.Any();
			Expect.Call(globalSettingsStorageProvider.AllWikis()).Return(new List<PluginFramework.Wiki>() { new PluginFramework.Wiki("root", new List<string>() { "localhost" }) });
			Expect.Call(host.PrepareContentForIndexing(null, null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(string wiki, string p, string input) { return input; }).Repeat.Any();
			Expect.Call(host.PrepareTitleForIndexing(null, null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(string wiki, string p, string input) { return input; }).Repeat.Any();

			mocks.Replay(host);
			mocks.Replay(globalSettingsStorageProvider);

			return host;
		}

		public IPagesStorageProviderV40 GetProvider() {
			SqlCEPagesStorageProvider prov = new SqlCEPagesStorageProvider();
			prov.SetUp(MockHost(), ConnString);
			prov.Init(MockHost(), ConnString, null);

			return prov;
		}

		[SetUp]
		public new void SetUp() {
			base.SetUp();

			// Create database with no tables
			SqlCeEngine engine = new SqlCeEngine(ConnString);
			engine.CreateDatabase();
			engine.Dispose();
		}

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch { }
			System.IO.File.Delete("ScrewTurnWikiTest.sdf");
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {

		}

		/// <summary>
		/// Gets the instance of the index to test.
		/// </summary>
		/// <returns>The instance of the index.</returns>
		protected override IIndex GetIndex() {
			SqlCEPagesStorageProvider prov = GetProvider() as SqlCEPagesStorageProvider;
			prov.SetFlags(true);
			return prov.Index;
		}

	}

}
