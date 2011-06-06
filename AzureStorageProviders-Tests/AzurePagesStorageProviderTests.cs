
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzurePagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		public override IPagesStorageProviderV40 GetProvider() {
			AzurePagesStorageProvider prov = new AzurePagesStorageProvider();
			prov.SetUp(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");
			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.CategoriesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.ContentTemplatesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.MessagesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.NamespacesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.NavigationPathsTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.PagesContentsTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.PagesInfoTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.SnippetsTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.IndexWordMappingTable);
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV40 prov = GetProvider();

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}
		
	}

}
