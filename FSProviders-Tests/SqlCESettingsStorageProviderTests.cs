
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
		private const string ConnString = "Data Source = 'ScrewTurnWikiTest.sdf';";

		public override ISettingsStorageProviderV40 GetProvider() {
			SqlCESettingsStorageProvider prov = new SqlCESettingsStorageProvider();
			prov.SetUp(MockHost(), ConnString);
			prov.Init(MockHost(), ConnString, "wiki1");

			return prov;
		}

		[TestFixtureSetUp]
		public void FixtureSetUp() {
			// Create database with no tables
			DbConnection cn = new SqlCeConnection(ConnString);
			try {
				cn.Open();
			}
			catch(SqlCeException) {
				SqlCeEngine engine = new SqlCeEngine(ConnString);
				engine.CreateDatabase();
				engine.Dispose();
				cn.Open();
			}
			finally {
				cn.Close();
			}
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			// Clear all tables
			DbConnection cn = new SqlCeConnection(ConnString);
			cn.Open();

			DbCommand cmd = cn.CreateCommand();
			cmd.CommandText = "delete from [AclEntry]; delete from [OutgoingLink]; delete from [PLuginStatus]; delete from [RecentChange]; delete from [MetaDataItem]; delete from [Setting];";
			try {
				cmd.ExecuteNonQuery();
			}
			catch(SqlException sqlex) {
				Console.WriteLine(sqlex.ToString());
			}

			cn.Close();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown() {
			
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConnString, "-");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Persist Security Info = False; Data Source = 'ScrewTurnWikiTest.sdf'; File Mode = 'shared read';", ExpectedException = typeof(InvalidConfigurationException))]
		public void SetUp_InvalidConnString(string c) {
			SqlCESettingsStorageProvider prov = new SqlCESettingsStorageProvider();
			prov.SetUp(MockHost(), c);
		}

	}

}
