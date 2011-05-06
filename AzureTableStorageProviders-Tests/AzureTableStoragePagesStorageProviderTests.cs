
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using AzureTableStorageProviders;
using ScrewTurn.Wiki.Tests;

namespace AzureTableStorageProviders_Tests {

	public class AzureTableStoragePagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		public override IPagesStorageProviderV30 GetProvider() {
			AzureTableStoragePagesStorageProvider prov = new AzureTableStoragePagesStorageProvider();
			prov.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "wiki1");
			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.CategoriesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.ContentTemplatesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.MessagesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.NamespacesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.NavigationPathsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.PagesContentsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.PagesInfoTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStoragePagesStorageProvider.SnippetsTable);
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}
		
	}

}
