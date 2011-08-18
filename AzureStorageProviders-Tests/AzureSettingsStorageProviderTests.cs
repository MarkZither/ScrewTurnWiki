
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;
using System.Configuration;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzureSettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		public override ISettingsStorageProviderV40 GetProvider() {
			AzureSettingsStorageProvider settingsProvider = new AzureSettingsStorageProvider();
			settingsProvider.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			settingsProvider.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");

			return settingsProvider;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.SettingsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.MetadataTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.OutgoingLinksTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.RecentChangesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.AclEntriesTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureSettingsStorageProvider.PluginsTable);
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		public void Init_InvalidConnString(string c) {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), c, null);
		}
	}
}
