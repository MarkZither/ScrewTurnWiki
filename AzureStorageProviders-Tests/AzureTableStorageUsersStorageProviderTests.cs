
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
			prov.SetUp(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");
			return prov;
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureTableStorageUsersStorageProvider.UsersTable);
			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureTableStorageUsersStorageProvider.UserGroupsTable);
			TableStorage.TruncateTable("unittestonazurestorage", "YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureTableStorageUsersStorageProvider.UserDataTable);
		}

	}

}
