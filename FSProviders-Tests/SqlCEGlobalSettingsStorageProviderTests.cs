
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.FSProviders;
using ScrewTurn.Wiki.Tests;
using System.Data.SqlServerCe;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.FSPlugins.Tests {

	[TestFixture]
	public class SqlCEGlobalSettingsStorageProviderTests : GlobalSettingsStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private string dbPath = "";
		private string connString = "";

		public override IGlobalSettingsStorageProviderV40 GetProvider() {
			SqlCEGlobalSettingsStorageProvider prov = new SqlCEGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), connString);
			prov.Init(MockHost(), connString, "wiki1");

			return prov;
		}

		[TestFixtureSetUp]
		public void FixtureSetUp() {
			dbPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString()) + "ScrewTurnWikiTest.sdf";
			connString = "Persist Security Info = False; Data Source = '" + dbPath + "';";

			// Create database with no tables
			SqlCeEngine engine = new SqlCeEngine(connString);
			engine.CreateDatabase();
			engine.Dispose();
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();
			SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();
			DbCommand command = commandBuilder.GetConnection(connString).CreateCommand();

			command.CommandText = "delete from [PluginAssembly];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Log];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [GlobalSetting];";
			command.ExecuteNonQuery();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {
			SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();
			DbCommand command = commandBuilder.GetCommand(connString, "delete from [Version];", new List<SqlCommon.Parameter>());
			command.ExecuteNonQuery();
			command.Connection.Close();
			command.Connection.Dispose();

			try {
				System.IO.File.Delete(dbPath);
			}
			catch { }
		}

		[Test]
		public void Init() {
			IGlobalSettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), connString, "-");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}
	}

}
