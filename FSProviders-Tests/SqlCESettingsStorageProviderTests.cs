
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
		private const string ConnString = "Persist Security Info = False; Data Source = 'ScrewTurnWikiTest.sdf'; File Mode = 'shared read';";

		public override ISettingsStorageProviderV40 GetProvider() {
			SqlCESettingsStorageProvider prov = new SqlCESettingsStorageProvider();
			prov.SetUp(MockHost(), ConnString);
			prov.Init(MockHost(), ConnString, "wiki1");

			return prov;
		}

		[SetUp]
		public void SetUp() {
			// Create database with no tables
			SqlCeEngine engine = new SqlCeEngine(ConnString);
			engine.CreateDatabase();
			engine.Dispose();
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			System.IO.File.Delete("ScrewTurnWikiTest.sdf");
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
		[TestCase("Persist Security Info = False; Data Source = 'ScrewTurnWikiTest2.sdf'; File Mode = 'shared read';", ExpectedException = typeof(InvalidConfigurationException))]
		public void SetUp_InvalidConnString(string c) {
			SqlCESettingsStorageProvider prov = new SqlCESettingsStorageProvider();
			prov.SetUp(MockHost(), c);
		}

	}

}
