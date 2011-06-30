
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.Tests;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using System.Data.SqlServerCe;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	[TestFixture]
	public class SqlCEPagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		//private const string ConnString = "Data Source=(local)\\SQLExpress;User ID=sa;Password=password;";
		private const string ConnString = "Persist Security Info = False; Data Source = 'ScrewTurnWikiTest.sdf';";

		public override IPagesStorageProviderV40 GetProvider() {
			SqlCEPagesStorageProvider prov = new SqlCEPagesStorageProvider();
			prov.SetUp(MockHost(), ConnString);
			prov.Init(MockHost(), ConnString, null);

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
			IPagesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConnString, null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("Data Source=(local)\\SQLExpress;User ID=inexistent;Password=password;InitialCatalog=Inexistent;", ExpectedException = typeof(InvalidConfigurationException))]
		public void SetUp_InvalidConnString(string c) {
			SqlCEPagesStorageProvider prov = new SqlCEPagesStorageProvider();
			prov.SetUp(MockHost(), c);
		}
		
	}

}
