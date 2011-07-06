
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
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class SqlCEPagesStorageProvider_SqlIndexTests : IndexBaseTests {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private string dbPath = "";
		private string connString = "";

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
			prov.SetUp(MockHost(), connString);
			prov.Init(MockHost(), connString, null);

			return prov;
		}

		[TestFixtureSetUp]
		public void FixtureSetUp() {
			dbPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString()) + "ScrewTurnWikiTest.sdf";
			connString = "Data Source = '" + dbPath + "';";

			// Create database with no tables
			SqlCeEngine engine = new SqlCeEngine(connString);
			engine.CreateDatabase();
			engine.Dispose();

			SqlCeConnection connection = new SqlCeConnection(connString);
			connection.Open();
			SqlCeCommand command = connection.CreateCommand();
			command.CommandText = "create table [Version] ([Component] nvarchar(100) not null, [Version] int not null, constraint [PK_Version] primary key ([Component]));";
			command.ExecuteNonQuery();
			command.Connection.Close();
		}

		[SetUp]
		public new void SetUp() {
			base.SetUp();
		}

		[TearDown]
		public void TearDown() {
			SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();
			DbCommand command = commandBuilder.GetConnection(connString).CreateCommand();
			command.CommandText = "delete from [IndexWordMapping];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [IndexWord];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [IndexDocument];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [ContentTemplate];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Snippet];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [NavigationPath];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Message];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [PageKeyword];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [CategoryBinding];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [PageContent];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Category];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Namespace] where [Name] <> '';";
			command.ExecuteNonQuery();

			try {
				Directory.Delete(testDir, true);
			}
			catch { }
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {
			SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();
			DbCommand command = commandBuilder.GetConnection(connString).CreateCommand();
			command.CommandText = "delete from [Version];";
			command.ExecuteNonQuery();
			command.Connection.Close();
			command.Connection.Dispose();

			try {
				System.IO.File.Delete(dbPath);
			}
			catch { }
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
