
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
	public class AzureFilesStorageProviderTests : FilesStorageProviderTestScaffolding {

		public override IFilesStorageProviderV40 GetProvider() {
			AzureFilesStorageProvider prov = new AzureFilesStorageProvider();
			prov.SetUp(MockHost(), ConfigurationManager.AppSettings["AzureConnString"]);
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "");

			return prov;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.DeleteAllBlobs(ConfigurationManager.AppSettings["AzureConnString"]);

			TableStorage.TruncateTable(ConfigurationManager.AppSettings["AzureConnString"], AzureFilesStorageProvider.FileRetrievalStatsTable);
		}

		[Test]
		public void Init() {
			IFilesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), ConfigurationManager.AppSettings["AzureConnString"], "wiki1");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
