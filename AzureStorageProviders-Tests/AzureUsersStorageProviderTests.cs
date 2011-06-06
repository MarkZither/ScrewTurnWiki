
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

		public override IUsersStorageProviderV40 GetProvider() {
			AzureUsersStorageProvider prov = new AzureUsersStorageProvider();
			prov.SetUp(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");
			return prov;
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), "unittestonazurestorage|YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureUsersStorageProvider.UsersTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureUsersStorageProvider.UserGroupsTable);
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzureUsersStorageProvider.UserDataTable);
		}

	}

}
