
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class FilesStorageProviderTests : FilesStorageProviderTestScaffolding {

		public override IFilesStorageProviderV30 GetProvider() {
			AzureFilesStorageProvider prov = new AzureFilesStorageProvider();
			prov.SetUp(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");

			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.DeleteAllBlobs("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");

			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureFilesStorageProvider.FileRetrievalStatsTable);
		}

		[Test]
		public void Init() {
			IFilesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
