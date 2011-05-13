
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
			prov.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "");
			return prov;
		}


		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.DeleteAllBlobs("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");

			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureGlobalSettingsStorageProvider.GlobalSettingsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureGlobalSettingsStorageProvider.PluginsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureGlobalSettingsStorageProvider.LogsTable);
		}

		[Test]
		public void Init() {
			AzureGlobalSettingsStorageProvider prov = new AzureGlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
