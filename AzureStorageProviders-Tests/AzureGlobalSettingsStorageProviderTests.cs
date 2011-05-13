
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using ScrewTurn.Wiki.Plugins.AzureStorage;

namespace ScrewTurn.Wiki.Tests {

	public class GlobalSettingsStorageProviderTests : GlobalSettingsStorageProviderTestScaffolding {

		public override IGlobalSettingsStorageProviderV30 GetProvider() {
			AzureGlobalSettingsStorageProvider prov = new AzureGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");
			return prov;
		}


		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.DeleteAllBlobs("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");

			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureGlobalSettingsStorageProvider.GlobalSettingsTable);
			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureGlobalSettingsStorageProvider.PluginsTable);
			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureGlobalSettingsStorageProvider.LogsTable);
		}

		[Test]
		public void Init() {
			AzureGlobalSettingsStorageProvider prov = new AzureGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
