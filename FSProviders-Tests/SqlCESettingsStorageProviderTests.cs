
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class SqlCESettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private string dbPath = "";
		private string connString = "";

		public override ISettingsStorageProviderV40 GetProvider() {
			SqlCESettingsStorageProvider prov = new SqlCESettingsStorageProvider();
			prov.SetUp(MockHost(), connString);
			prov.Init(MockHost(), connString, "wiki1");

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

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();
			DbCommand command = commandBuilder.GetConnection(connString).CreateCommand();
			
			command.CommandText = "delete from [AclEntry];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [OutgoingLink];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [PLuginStatus];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [RecentChange];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [MetaDataItem];";
			command.ExecuteNonQuery();

			command.CommandText = "delete from [Setting];";
			command.ExecuteNonQuery();
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

		[Test]
		public void Init() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), connString, "-");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
