
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	public class UsersStorageProviderTests : UsersStorageProviderTestScaffolding {

		public override IUsersStorageProviderV40 GetProvider() {
			UsersStorageProvider prov = new UsersStorageProvider();
			prov.SetUp(MockHost(), "");
			prov.Init(MockHost(), "", null);
			return prov;
		}

		[Test]
		public void Init() {
			IUsersStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), "", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
