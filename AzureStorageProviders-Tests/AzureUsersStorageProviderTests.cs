
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;
using System.Configuration;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class UsersStorageProviderTests : UsersStorageProviderTestScaffolding {

		public override IUsersStorageProviderV40 GetProvider() {
			AzureUsersStorageProvider prov = new AzureUsersStorageProvider();
			prov.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");
			return prov;
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureUsersStorageProvider.UsersTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureUsersStorageProvider.UserGroupsTable);
			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureUsersStorageProvider.UserDataTable);
		}

	}

}
