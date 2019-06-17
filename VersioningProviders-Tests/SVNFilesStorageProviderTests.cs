using System;
using System.IO;
using System.Linq;
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
			IVersionedFilesStorageProviderV30 prov = GetProvider();

			ClearDirsRecursive(prov, "", "");

			base.TearDown();
		}

		private void ClearDirsRecursive(IVersionedFilesStorageProviderV30 prov, string directory, string path)
		{
			var dirs = prov.ListDirectories(path).OrderByDescending(x => x);
			try
			{
				foreach(var dir in dirs)
				{
					string fullPath = path + (path.Equals("") || dir.Equals("") ? "" : "\\") + dir;
					if(!dir.Equals(""))
					{
						ClearDirsRecursive(prov, dir, fullPath);
					}
					var files = prov.ListFiles(fullPath);
					using(SvnClient svnClient = new SvnClient())
					{
						svnClient.Authentication.Clear(); // Prevents saving/loading config to/from disk
						svnClient.Authentication.DefaultCredentials = new System.Net.NetworkCredential("wiki", testRepoPassword);
						svnClient.Authentication.SslServerTrustHandlers += delegate (object sender, SvnSslServerTrustEventArgs e)
						{
							e.AcceptedFailures = e.Failures;
							e.Save = false; // Save acceptance to authentication store
						};
						svnClient.Update(Path.Combine(testDir, "SVN", "wiki", fullPath), out SvnUpdateResult svnUpdateResult);
						foreach(string file in files)
						{
							svnClient.Delete(Path.Combine(testDir, "SVN", "wiki", fullPath, file));
						}
						if(!fullPath.Equals("") && !dir.Equals(""))
						{
							svnClient.Delete(Path.Combine(testDir, "SVN", "wiki", fullPath));
						}
						svnClient.Commit(Path.Combine(testDir, "SVN", "wiki", fullPath),
							new SvnCommitArgs() { LogMessage = "unit testing" },
							out SvnCommitResult resultDir);
					}
				}
			}
			catch(SvnException sex)
			{
				Console.WriteLine("Test: could not delete temp directory");
			}
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
	}
}
