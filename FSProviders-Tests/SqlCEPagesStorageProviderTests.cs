
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlServerCe;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class SqlCEPagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private string dbPath = "";
		private string connString = "";

		public override IPagesStorageProviderV40 GetProvider() {
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

		[TearDown]
		public new void TearDown() {
			base.TearDown();

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
			catch {}
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), connString, null);

			Assert.IsNotNull(prov.Information, "Information should not be null");

			prov.Dispose();
		}

	}

}
