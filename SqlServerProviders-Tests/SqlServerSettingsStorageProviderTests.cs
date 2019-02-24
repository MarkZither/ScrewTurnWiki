
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlServer.Tests {

	[TestFixture]
	public class SqlServerSettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private string ConnString = "Data Source=(local)\\MSSQLSERVER2016;Integrated Security=SSPI;";
		private const string InitialCatalog = "Initial Catalog=ScrewTurnWikiTest;";

		public override ISettingsStorageProviderV30 GetProvider() {
			SqlServerSettingsStorageProvider prov = new SqlServerSettingsStorageProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);
			return prov;
		}

		[OneTimeSetUp]
		public void FixtureSetUp() {
			bool.TryParse(Environment.GetEnvironmentVariable("APPVEYOR"), out bool isAppveyor);
			if(isAppveyor)
			{
				ConnString = "Server=(local)\\SQL2016;Integrated Security=SSPI;";
			}
			// Create database with no tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "if (select count(*) from sys.databases where [Name] = 'ScrewTurnWikiTest') = 0 begin create database [ScrewTurnWikiTest] end";
			cmd.ExecuteNonQuery();

			cn.Close();
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			// Clear all tables
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "use [ScrewTurnWikiTest]; delete from [AclEntry]; delete from [OutgoingLink]; delete from [PLuginStatus]; delete from [PluginAssembly]; delete from [RecentChange]; delete from [MetaDataItem]; delete from [Log];delete from [Setting];";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();
		}

		[OneTimeTearDown]
		public void FixtureTearDown() {
			// Delete database
			SqlConnection cn = new SqlConnection(ConnString);
			cn.Open();

			SqlCommand cmd = cn.CreateCommand();
			cmd.CommandText = "alter database [ScrewTurnWikiTest] set single_user with rollback immediate";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cmd = cn.CreateCommand();
			cmd.CommandText = "drop database [ScrewTurnWikiTest]";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), ConnString + InitialCatalog);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("")]
		[TestCase("blah")]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;")]
		public void Init_InvalidConnString(string c)
		{
			Assert.Throws<InvalidConfigurationException>(() =>
			{
				ISettingsStorageProviderV30 prov = GetProvider();
				prov.Init(MockHost(), c);
			});
		}

	}

}
