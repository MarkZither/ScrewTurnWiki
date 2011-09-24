
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests {

	public class FilesStorageProviderTests : FilesStorageProviderTestScaffolding {

		public override IFilesStorageProviderV40 GetProvider() {
			FilesStorageProvider prov = new FilesStorageProvider();
			prov.SetUp(MockHost(), "");
			prov.Init(MockHost(), "", null);

			return prov;
		}

		[Test]
		public void Init() {
			IFilesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), "", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
