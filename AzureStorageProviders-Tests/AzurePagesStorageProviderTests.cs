
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using ScrewTurn.Wiki.Tests;
using System.Configuration;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzurePagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		public override IPagesStorageProviderV40 GetProvider() {
			AzurePagesStorageProvider prov = new AzurePagesStorageProvider();
			prov.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");
			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.CategoriesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.ContentTemplatesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.MessagesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.NamespacesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.NavigationPathsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.PagesContentsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.PagesInfoTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.SnippetsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzurePagesStorageProvider.IndexWordMappingTable);

			TableStorage.DeleteAllBlobs(ConfigurationManager.AppSettings["AzureConnString"]);
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV40 prov = GetProvider();

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}
				
	}

}
