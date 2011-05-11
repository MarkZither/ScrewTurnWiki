
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class UsersStorageProviderTests : UsersStorageProviderTestScaffolding {

		public override IUsersStorageProviderV30 GetProvider() {
			AzureTableStorageUsersStorageProvider prov = new AzureTableStorageUsersStorageProvider();
			prov.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "wiki1");
			return prov;
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStorageUsersStorageProvider.UsersTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStorageUsersStorageProvider.UserGroupsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureTableStorageUsersStorageProvider.UserDataTable);
		}

	}

}
