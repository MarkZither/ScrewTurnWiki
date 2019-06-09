using System;
using System.IO;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;
using SharpSvn;
using SharpSvn.Security;

namespace ScrewTurn.Wiki.Plugins.Versioning.Tests
{
	[TestFixture]
	public class SVNFilesStorageProviderTests : VersionedFilesStorageProviderTestScaffolding
	{
		private string repoUrl = "https://ZITHERLDWX01.ad.mark-burton.com:8443/svn/TestWiki/";
		private string testRepoPassword;
		private string config;

		public SVNFilesStorageProviderTests()
		{
		}

		[OneTimeSetUp]
		public void FixtureSetUp()
		{
			bool.TryParse(Environment.GetEnvironmentVariable("APPVEYOR"), out bool isAppveyor);
			testRepoPassword = Environment.GetEnvironmentVariable("TESTREPOPASSWORD") ?? "password";
			if(isAppveyor)
			{
				repoUrl = "https://svn.riouxsvn.com/markburtonwiki";
			}
			config =
$@"--- 
Repos: 
  - 
    Name: wiki
    Password: 'password'
    RepoUrl: '{repoUrl}'
    Username: 'wiki'";
			Console.WriteLine($"isAppveyor is {isAppveyor.ToString()}");
			Console.WriteLine($"repoUrl is {repoUrl}");
		}

		[TearDown]
		public new void TearDown()
		{
			try
			{
				IVersionedFilesStorageProviderV30 prov = GetProvider();
				var files = prov.ListFiles("\\");
				using(SvnClient svnClient = new SvnClient())
				{
					svnClient.Authentication.Clear(); // Prevents saving/loading config to/from disk
					svnClient.Authentication.DefaultCredentials = new System.Net.NetworkCredential("wiki", testRepoPassword);
					svnClient.Authentication.SslServerTrustHandlers += delegate (object sender, SvnSslServerTrustEventArgs e) {
						e.AcceptedFailures = e.Failures;
						e.Save = false; // Save acceptance to authentication store
					};
					foreach(string file in files)
					{
						svnClient.Delete(Path.Combine(testDir, "SVN", "wiki", file));
						svnClient.Commit(Path.Combine(testDir, "SVN", "wiki", file), 
							new SvnCommitArgs() { LogMessage = "unit testing" }, 
							out SvnCommitResult result);
					}
				}
			}
			catch(SvnException sex)
			{
				Console.WriteLine("Test: could not delete temp directory");
			}
			base.TearDown();
		}

		public override IVersionedFilesStorageProviderV30 SetupProvider()
		{
			testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			IVersionedFilesStorageProviderV30 prov = GetProvider();
			// Checkout the repo
			prov.CheckoutRepos(testDir);
			return prov;
		}
		public override IVersionedFilesStorageProviderV30 GetProvider()
		{
			IVersionedFilesStorageProviderV30 prov = new SVNFilesStorageProvider();
			prov.Init(MockHost(), config);

			return prov;
		}

		[Test]
		public void Test1()
		{
			IVersionedFilesStorageProviderV30 prov = SetupProvider();

			Assert.That(true);
		}
	}
}
