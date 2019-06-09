using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using ScrewTurn.Wiki.PluginFramework;
using SharpSvn;
using SharpSvn.Security;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace ScrewTurn.Wiki.Plugins.Versioning
{
	public class SVNFilesStorageProvider : IVersionedFilesStorageProviderV30
	{
		private readonly ComponentInformation info = new ComponentInformation("SVN Files Storage Provider", "Mark Burton", "3.3.0.0", "https://github.com/MarkZither/ScrewTurnWiki/tree/gh-pages", "http://www.screwturn.eu/Version/SQLServerProv/Files.txt");
		// The following strings MUST terminate with DirectorySeparatorPath in order to properly work
		// in BuildFullPath method
		private readonly string SVNRootDirectory = "SVN";// + Path.DirectorySeparatorChar;
		// 16 KB buffer used in the StreamCopy method
		// 16 KB seems to be the best break-even between performance and memory usage
		private const int BufferSize = 16384;

		private IHostV30 host;
		private Config m_Config;

		private class Repo
		{
			public string Name;
			public string RepoUrl;
			public string Username;
			public string Password;
		}

		private class Config
		{
			public List<Repo> Repos
			{
				get;set;
			}

			/// <summary>
			/// Returns true if the group map contains at least one entry.
			/// </summary>
			/// <value>
			/// 	<c>true</c> if this instance is group map set; otherwise, <c>false</c>.
			/// </value>
			public bool IsRepoSet
			{
				get
				{
					return Repos.Count > 0;
				}
			}
		}

		public bool ReadOnly
		{
			get
			{
				return true;
			}
		}

		public int CountRepos()
		{
			return m_Config?.Repos?.Count ?? 0;
		}

		/// <summary>
		/// Checks the path.
		/// </summary>
		/// <param name="path">The path to be checked.</param>
		/// <param name="begin">The expected beginning of the path.</param>
		/// <exception cref="InvalidOperationException">If <paramref name="path"/> does not begin with <paramref name="begin"/> or contains "\.." or "..\".</exception>
		private string CheckPath(string path, string begin)
		{
			// TODO: check why relative paths are blocked
			//if(!path.StartsWith(begin) || path.Contains(Path.DirectorySeparatorChar + "..") || path.Contains(".." + Path.DirectorySeparatorChar))
			if(!path.StartsWith(begin))
			{
				throw new InvalidOperationException();
			}

			return path;
		}

		/// <summary>
		/// Builds a full path from a provider-specific partial path.
		/// </summary>
		/// <param name="partialPath">The partial path.</param>
		/// <returns>The full path.</returns>
		/// <remarks>For example: if <b>partialPath</b> is "/my/directory", the method returns 
		/// "C:\Inetpub\wwwroot\Wiki\public\Upload\my\directory", assuming the Wiki resides in "C:\Inetpub\wwwroot\Wiki".</remarks>
		private string BuildFullPath(string repoName, string partialPath)
		{
			if(partialPath == null)
			{
				partialPath = "";
			}

			partialPath = partialPath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
			string up = Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), SVNRootDirectory, repoName);
			return CheckPath(Path.Combine(up, partialPath), up); // partialPath CANNOT start with "\" -> Path.Combine does not work
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information
		{
			get
			{
				return info;
			}
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml
		{
			get
			{
				return "";
			}
		}

		public FileDetails GetFileDetails(string fullName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config)
		{
			if(host == null)
			{
				throw new ArgumentNullException("host");
			}

			if(config == null)
			{
				throw new ArgumentNullException("config");
			}

			this.host = host;	
			InitConfig(config);
			
		}

		public string[] ListDirectories(string directory)
		{
			List<string> files = new List<string>();
			foreach(var repo in m_Config.Repos)
			{
				using(SvnClient svnClient = GetSvnClient(repo))
				{
					Collection<SvnListEventArgs> contents;
					if(svnClient.GetList(new Uri(repo.RepoUrl), out contents))
					{
						foreach(SvnListEventArgs item in contents.Where(x => x.Entry.NodeKind == SvnNodeKind.Directory))
						{
							files.Add(item.Path);
						}
					}
				}
			}
			return files.ToArray();
		}

		/// <summary>
		/// Copies data from a Stream to another.
		/// </summary>
		/// <param name="source">The Source stream.</param>
		/// <param name="destination">The destination Stream.</param>
		private static void StreamCopy(Stream source, Stream destination)
		{
			byte[] buff = new byte[BufferSize];
			int copied = 0;
			do
			{
				copied = source.Read(buff, 0, buff.Length);
				if(copied > 0)
				{
					destination.Write(buff, 0, copied);
				}
			} while(copied > 0);
		}

		public string[] ListFiles(string directory)
		{
			List<string> files = new List<string>();
			foreach(var repo in m_Config.Repos)
			{
				using(SvnClient svnClient = GetSvnClient(repo))
				{
					Collection<SvnListEventArgs> contents;
					if(svnClient.GetList(new Uri(repo.RepoUrl), out contents))
					{
						foreach(SvnListEventArgs item in contents.Where(x => x.Entry.NodeKind == SvnNodeKind.File))
						{
							files.Add(item.Path);
						}
					}
				}
			}
			return files.ToArray();
		}

		public bool RetrieveFile(string fullName, Stream destinationStream, bool countHit)
		{
			throw new NotImplementedException();
		}

		public void SetFileRetrievalCount(string fullName, int count)
		{
			throw new NotImplementedException();
		}

		public void SetUp(IHostV30 host, string config)
		{
			throw new NotImplementedException();
		}

		public void Shutdown()
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Logs a message from this plugin.
		/// </summary>
		/// <param name="entryType">Type of the entry.</param>
		/// <param name="message">The message.</param>
		/// <param name="args">The args.</param>
		private void LogEntry(LogEntryType entryType, string message, params object[] args)
		{
			string entry = String.Format(message, args);
			host.LogEntry(entry, entryType, null, this);
		}


		/// <summary>
		/// Configures the plugin based on the configuration settings.
		/// </summary>
		/// <param name="config">The config.</param>
		private void InitConfig(string config)
		{
			Config newConfig = ParseConfig(config);

			if(!newConfig.IsRepoSet)
			{
				LogEntry(LogEntryType.Error, "No Repo entries found. Please make sure at least one valid Repo configuration entry exists.");
				throw new InvalidConfigurationException("No Repo entries found. Please make sure at least one valid Repo configuration entry exists.");
			}

			foreach(var item in newConfig.Repos)
			{
				LogEntry(LogEntryType.General,
					"Configured to use server \"{0}\".",
					item.RepoUrl);
			}			

			m_Config = newConfig;
		}

		/// <summary>
		/// Parses the plugin configuration string.
		/// </summary>
		/// <param name="config">The config.</param>
		/// <returns>A Config object representig the configuration string.</returns>
		private Config ParseConfig(string config)
		{
			// Setup the input
			var input = new StringReader(config);
			
			try
			{
				// Load the stream
				var yaml = new YamlStream();
				yaml.Load(input);

				// Examine the stream
				var mapping =
					(YamlMappingNode)yaml.Documents[0].RootNode;

				// List all the items
				var items = (YamlSequenceNode)mapping.Children[new YamlScalarNode("Repos")];

				string[] configLines = config.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				// Then we wrap the existing ObjectNodeDeserializer
				// with our ValidatingNodeDeserializer:
				//var deserializer = new DeserializerBuilder()
				//	.WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner), s => s.InsteadOf<ObjectNodeDeserializer>())
				//	.Build();
				//deserializer.Deserialize<Config>(new StringReader(@"Name: ~"));
				var deserializer = new DeserializerBuilder().Build();
				var parsedConfig = deserializer.Deserialize<Config>(config);

				foreach(var item in parsedConfig.Repos)
				{
					if(!Uri.TryCreate(item.RepoUrl, UriKind.Absolute, out _))
					{
						LogEntry(LogEntryType.Error,
						"Invalid config key, see help for valid options. " +
						"The Username is empty, checkout will be done anonymously.",
						item.RepoUrl);
					}
					if(string.IsNullOrEmpty(item.Name))
					{
						LogEntry(LogEntryType.Error,
						"Invalid config key, see help for valid options. " +
						"The Name is empty, checkout will be done in the root and fail due to directory not being empty.",
						item.Name);
					}
					if(string.IsNullOrEmpty(item.Username))
					{
						LogEntry(LogEntryType.Error,
						"Invalid config key, see help for valid options. " +
						"The Username is empty, checkout will be done anonymously.",
						item.Username);
					}

					if(!string.IsNullOrEmpty(item.Username) && string.IsNullOrEmpty(item.Password))
					{
						LogEntry(LogEntryType.Error,
						"Invalid config key, see help for valid options. " +
						"The Password should not be empty if the Username is set.",
						item.Password);
					}
				}

				return parsedConfig;
			}

			catch(Exception ex)
			{
				LogEntry(LogEntryType.Error, "Error parsing the configuration: {0}", ex);
				throw new InvalidConfigurationException("Error parsing the configuration.", ex);
			}
		}

		/// <summary>
		/// Stores a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream, 
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing file.</param>
		/// <returns><c>true</c> if the File is stored, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>overwrite</b> is <c>false</c> and File already exists, the method returns <c>false</c>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> os <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StoreFile(string fullName, Stream sourceStream, bool overwrite)
		{
			if(fullName == null)
			{
				throw new ArgumentNullException("fullName");
			}

			if(fullName.Length == 0)
			{
				throw new ArgumentException("Full Name cannot be empty", "fullName");
			}

			if(sourceStream == null)
			{
				throw new ArgumentNullException("sourceStream");
			}

			if(!sourceStream.CanRead)
			{
				throw new ArgumentException("Cannot read from Source Stream", "sourceStream");
			}

			string filename = BuildFullPath(m_Config.Repos.First().Name, fullName);

			// Abort if the file already exists and overwrite is false
			if(File.Exists(filename) && !overwrite)
			{
				return false;
			}


			FileStream fs = null;

			bool done = false;

			try
			{
				fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

				// StreamCopy content (throws exception in case of error)
				StreamCopy(sourceStream, fs);
			}
			catch(IOException ioex)
			{
				done = false;
			}
			finally
			{
				try
				{
					fs.Close();
				}
				catch { }
			}
			try
			{
				using(SvnClient svnClient = GetSvnClient(m_Config.Repos.First()))
				{
					var root = svnClient.GetWorkingCopyRoot(BuildFullPath(m_Config.Repos.First().Name, null));
					if(root != null)
					{

					}
					svnClient.Add(filename);
					svnClient.Commit(filename, new SvnCommitArgs() { LogMessage = "unit testing"}, out SvnCommitResult result);
				}
				done = true;
			}
			catch(SvnException sex)
			{
				host.LogEntry(sex.Message, LogEntryType.Error, null, this);
			}

			return done;
		}

		private SvnClient GetSvnClient(Repo repo)
		{
			SvnClient client = new SvnClient();
			//client.Configuration.SetOption("servers", "global", "http-auth-types", "basic;digest");
			client.Authentication.Clear(); // Prevents saving/loading config to/from disk
			client.Authentication.DefaultCredentials = new NetworkCredential(repo.Username, repo.Password);
			client.Authentication.SslServerTrustHandlers += delegate (object sender, SvnSslServerTrustEventArgs e) {
				e.AcceptedFailures = e.Failures;
				e.Save = false; // Save acceptance to authentication store
			};
			return client;
		}

		public bool CheckoutRepos(string repoDir)
		{
			foreach(var repo in m_Config.Repos)
			{
				using(SvnClient client = GetSvnClient(repo))
				{
					// Checkout the code to the specified directory
					client.CheckOut(new Uri(repo.RepoUrl), Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), SVNRootDirectory, repo.Name));
					client.Update(Path.Combine(host.GetSettingValue(SettingName.PublicDirectory), SVNRootDirectory, repo.Name));
				}
			}
			
			return true;
		}
	}
}
