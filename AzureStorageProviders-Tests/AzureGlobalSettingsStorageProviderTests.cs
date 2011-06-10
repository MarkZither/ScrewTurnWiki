
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using ScrewTurn.Wiki.Plugins.AzureStorage;
using System.Configuration;

namespace ScrewTurn.Wiki.Tests {

	public class GlobalSettingsStorageProviderTests : GlobalSettingsStorageProviderTestScaffolding {

		public override IGlobalSettingsStorageProviderV40 GetProvider() {
			AzureGlobalSettingsStorageProvider prov = new AzureGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");
			return prov;
		}


		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.DeleteAllBlobs(ConfigurationManager.AppSettings["AzureConnString"]);

			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureGlobalSettingsStorageProvider.GlobalSettingsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureGlobalSettingsStorageProvider.LogsTable);
		}

		[Test]
		public void Init() {
			AzureGlobalSettingsStorageProvider prov = new AzureGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
