
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

		public override IPagesStorageProviderV30 GetProvider() {
			AzurePagesStorageProvider prov = new AzurePagesStorageProvider();
			prov.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "");
			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.CategoriesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.ContentTemplatesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.MessagesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.NamespacesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.NavigationPathsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.PagesContentsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.PagesInfoTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzurePagesStorageProvider.SnippetsTable);
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV30 prov = GetProvider();

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}
		
	}

}
