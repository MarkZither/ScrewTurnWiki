
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzureSettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		public override ISettingsStorageProviderV40 GetProvider() {
			AzureSettingsStorageProvider settingsProvider = new AzureSettingsStorageProvider();
			settingsProvider.SetUp(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			settingsProvider.Init(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");

			return settingsProvider;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.SettingsTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.MetadataTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.OutgoingLinksTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.RecentChangesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.AclEntriesTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureSettingsStorageProvider.PluginsTable);
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		public void Init_InvalidConnString(string c) {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), c, null);
		}
	}
}
