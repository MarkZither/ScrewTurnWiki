
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

namespace ScrewTurn.Wiki.Plugins.FSPlugins.Tests {

	[TestFixture]
	public class SqlCEGlobalSettingsStorageProviderTests : GlobalSettingsStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Persist Security Info = False; Data Source = 'ScrewTurnWikiTest.sdf';";

		public override IGlobalSettingsStorageProviderV40 GetProvider() {
			SqlCEGlobalSettingsStorageProvider prov = new SqlCEGlobalSettingsStorageProvider();
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
			IGlobalSettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConnString, "-");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void Setup_InvalidConnString(string c) {
			SqlCEGlobalSettingsStorageProvider prov = new SqlCEGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), c);
		}

	}

}
