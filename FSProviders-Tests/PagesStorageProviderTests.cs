
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Tests {

	public class PagesStorageProviderTests : PagesStorageProviderTestScaffolding {

		public override IPagesStorageProviderV40 GetProvider() {
			PagesStorageProvider prov = new PagesStorageProvider();
			prov.SetUp(MockHost(), "");
			prov.Init(MockHost(), "", null);
			return prov;
		}

		[Test]
		public void Init() {
			IPagesStorageProviderV40 prov = GetProvider();

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
