using System;
using System.IO;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.FSProviders.Tests
{
	[TestFixture]
	public class FSIndexDirectoryProviderTests
	{

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		protected IHostV30 MockHost()
		{
			if(!System.IO.Directory.Exists(testDir))
			{
				System.IO.Directory.CreateDirectory(testDir);
			}

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		[SetUp]
		public void SetUp()
		{
			if(!System.IO.Directory.Exists(testDir))
			{
				System.IO.Directory.CreateDirectory(testDir);
			}
		}

		[TearDown]
		public void TearDown()
		{
			System.IO.Directory.Delete(testDir, true);
		}

		[Test]
		public void AddPageTest()
		{
		}
	}
}
