
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.RatingManagerPlugin {

	/// <summary>
	/// A plugin for assigning a rating to pages.
	/// </summary>
	public class RatingManager : IFormatterProviderV40 {

		private const string _defaultDirectoryName = "/__RatingManagerPlugin/";
		private const string ratingFileName = "RatingManagerPluginRatingFile.dat";
		private const string CookieNamePrefix = "RatingManagerPlugin_";

		private static byte[] JsContent = null;
		private static byte[] Js2Content = null;
		private static byte[] CssContent = null;
		private static byte[] GifContent = null;

		private IHostV40 _host;
		private string _wiki;
		private bool _enableLogging = true;
		private static readonly ComponentInformation Info = new ComponentInformation("Rating Manager Plugin", "Threeplicate Srl", "4.0.4.122", "http://www.screwturn.eu", "http://www.screwturn.eu/Version4.0/PluginPack/RatingManager.txt");

		private string DefaultDirectoryName() {
			return Path.Combine(_wiki, _defaultDirectoryName);
		}

		private static readonly Regex VotesRegex = new Regex(@"{rating(\|(.+?))?}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		static RatingManager() {
			Func<Stream, byte[]> readStream = s => {
				List<byte> result = new List<byte>(65536);

				byte[] buffer = new byte[65536];
				int read = 0;
				do {
					read = s.Read(buffer, 0, buffer.Length);
					if(read > 0) {
						result.AddRange(buffer.Take(read));
					}
				} while(read > 0);

				return result.ToArray();
			};

			CssContent = readStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.jquery.rating.css"));
			JsContent = readStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.jquery.rating.pack.js"));
			Js2Content = readStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.rating.js"));
			GifContent = readStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.star.gif"));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RatingManager"/> class.
		/// </summary>
		public RatingManager() { }

		/// <summary>
		/// Specifies whether or not to execute Phase 1.
		/// </summary>
		public bool PerformPhase1 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 2.
		/// </summary>
		public bool PerformPhase2 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 3.
		/// </summary>
		public bool PerformPhase3 {
			get { return true; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		public int ExecutionPriority {
			get { return 50; }
		}

		/// <summary>
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			// {rating}
			// _backendpage not found -> ignored

			StringBuilder buffer = new StringBuilder(raw);
			try {
				if(context.Page != null) {
					ComputeRating(context, buffer, context.Page);
				}
				else {
					return raw;
				}
			}
			catch(Exception ex) {
				LogWarning(string.Format("Exception occurred: {0}", ex.ToString()));
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Gets the rating of the plugin from the backendpage and display it to the user.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="buffer">The page content.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		private void ComputeRating(ContextInformation context, StringBuilder buffer, string fullPageName) {
			KeyValuePair<int, Match> block = FindAndRemoveFirstOccurrence(buffer);
			int numRatings = 0;
			while(block.Key != -1) {
				numRatings++;

				string currentVote = null;
				if(context.HttpContext.Request.Cookies[CookieNamePrefix + fullPageName] != null) {
					currentVote = context.HttpContext.Request.Cookies[CookieNamePrefix + fullPageName].Value;
				}

				string result = "";

				if(block.Value.Groups[2].Value != "") {
					int average = (int)Math.Round((decimal)GetCurrentAverage(block.Value.Groups[2].Value), 0, MidpointRounding.AwayFromZero);

					result += @"<span id=""staticStar" + numRatings + @""" class=""rating""></span>";

					if(currentVote != null) {
						result += @"<span id=""average" + numRatings + @""" class=""ui-rating-side-message"">You voted " + currentVote + "</span>";
					}

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function() {
$('#staticStar" + numRatings + @"').html(GenerateStaticStars(" + average + @", 'ui-rating-full'));
});
//--> </script>";
				}
				else if(currentVote != null) {
					int average = (int)Math.Round((decimal)GetCurrentAverage(fullPageName), 0, MidpointRounding.AwayFromZero);

					result += @"<span id=""staticStar" + numRatings + @""" class=""rating""></span>";
					result += @"<span id=""average" + numRatings + @""" class=""ui-rating-side-message"">You voted " + currentVote + "</span>";

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function() {
$('#staticStar" + numRatings + @"').html(GenerateStaticStars(" + average + @", 'ui-rating-full'));
});
//--> </script>";
				}
				else {
					int average = (int)Math.Round((decimal)GetCurrentAverage(fullPageName), 0, MidpointRounding.AwayFromZero);

					result += @"<select name=""myRating"" class=""rating"" id=""serialStar" + numRatings + @""">  
									<option value=""1"">Awful</option>
									<option value=""2"">Below average</option>
									<option value=""3"">Good</option>
									<option value=""4"">Above average</option>
									<option value=""5"">Awesome</option>
								</select>
								<span id=""staticStar" + numRatings + @""" style=""vertical-align: middle;""></span> <span id=""average" + numRatings + @""" class=""ui-rating-side-message"">&nbsp;</span>";

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function() { SetupVoteTool(" + numRatings + @", '" + fullPageName + @"', " + average + @"); });
//--> </script>";

				}

				buffer.Insert(block.Key, result);

				block = FindAndRemoveFirstOccurrence(buffer);
			}
		}
		
		private float GetCurrentAverage(string fullPageName) {
			float average = 0;
			try {
				IFilesStorageProviderV40 filesStorageProvider = GetDefaultFilesStorageProvider();

				MemoryStream stream = new MemoryStream();
				string fileContent = "";

				if(FileExists(filesStorageProvider, DefaultDirectoryName(), ratingFileName)) {
					filesStorageProvider.RetrieveFile(DefaultDirectoryName() + ratingFileName, stream);
					stream.Seek(0, SeekOrigin.Begin);
					fileContent = Encoding.UTF8.GetString(stream.ToArray());
				}

				string[] plugins = fileContent.Split(new String[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

				// If the plugin is found return the posizion in the plugins array
				// otherwise return -1
				int pluginIndex = SearchPlugin(plugins, fullPageName);
				if(pluginIndex != -1) {
					string[] pluginDetails = plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					average = (float)int.Parse(pluginDetails[2]) / (float)100;
				}
			}
			catch(Exception ex) {
				LogWarning(String.Format("Exception occurred {0}", ex.ToString()));
			}
			return average;
		}

		private void AddRating(string fullPageName, int rate) {
			IFilesStorageProviderV40 filesStorageProvider = GetDefaultFilesStorageProvider();

			MemoryStream stream = new MemoryStream();

			if(FileExists(filesStorageProvider, DefaultDirectoryName(), ratingFileName)) {
				filesStorageProvider.RetrieveFile(DefaultDirectoryName() + ratingFileName, stream);
				stream.Seek(0, SeekOrigin.Begin);
			}
			string fileContent = Encoding.UTF8.GetString(stream.ToArray());

			string[] plugins = fileContent.Split(new String[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

			StringBuilder sb = new StringBuilder();

			// If the plugin is found return the posizion in the plugins array
			// otherwise return -1
			int pluginIndex = SearchPlugin(plugins, fullPageName);
			if(pluginIndex != -1) {
				int numRates = int.Parse(plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[1]);
				int average = int.Parse(plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[2]);
				int newAverage = ((average * numRates) + (rate * 100)) / (numRates + 1);
				numRates++;
				plugins[pluginIndex] = fullPageName + "|" + numRates + "|" + newAverage;
				foreach(string plugin in plugins) {
					sb.Append(plugin + "||");
				}
			}
			else {
				foreach(string plugin in plugins) {
					sb.Append(plugin + "||");
				}
				sb.Append(fullPageName + "|1|" + (rate * 100));
			}

			stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));

			filesStorageProvider.StoreFile(DefaultDirectoryName() + ratingFileName, stream, true);
		}

		/// <summary>
		/// Searches the plugin.
		/// </summary>
		/// <param name="plugins">The plugins array.</param>
		/// <param name="currentPlugin">The current plugin.</param>
		/// <returns>
		/// The position of the plugin in the <paramref name="plugins"/> array, otherwise -1
		/// </returns>
		private int SearchPlugin(string[] plugins, string currentPlugin) {
			for(int i = 0; i < plugins.Length; i++) {
				if(plugins[i].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[0] == currentPlugin) {
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Finds the and remove first occurrence.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The index->content data.</returns>
		private KeyValuePair<int, Match> FindAndRemoveFirstOccurrence(StringBuilder buffer) {
			Match match = VotesRegex.Match(buffer.ToString());

			if(match.Success) {
				buffer.Remove(match.Index, match.Length);
				return new KeyValuePair<int, Match>(match.Index, match);
			}

			return new KeyValuePair<int, Match>(-1, null);
		}

		/// <summary>
		/// Logs the warning.
		/// </summary>
		/// <param name="message">The message.</param>
		private void LogWarning(string message) {
			if(_enableLogging) {
				_host.LogEntry(message, LogEntryType.Warning, null, this, _wiki);
			}
		}

		/// <summary>
		/// Prepares the title of an item for display (always during phase 3).
		/// </summary>
		/// <param name="title">The input title.</param>
		/// <param name="context">The context information.</param>
		/// <returns>The prepared title (no markup allowed).</returns>
		public string PrepareTitle(string title, ContextInformation context) {
			return title;
		}

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return _wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;
			_wiki = wiki;

			IFilesStorageProviderV40 filesStorageProvider = GetDefaultFilesStorageProvider();

			if(!DirectoryExists(filesStorageProvider, DefaultDirectoryName())) {
				filesStorageProvider.CreateDirectory("/", DefaultDirectoryName().Trim('/'));
			}

			string[] configEntries = config.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			for(int i = 0; i < configEntries.Length; i++) {
				string[] configEntryDetails = configEntries[i].Split(new string[] { "=" }, 2, StringSplitOptions.None);
				switch(configEntryDetails[0].ToLowerInvariant()) {
					case "logoptions":
						if(configEntryDetails[1] == "nolog") {
							_enableLogging = false;
						}
						else {
							LogWarning("Unknown value in 'logOptions' configuration string: " + configEntries[i] + "; supported values are: 'nolog'.");
						}
						break;
					default:
						LogWarning("Unknown value in configuration string: " + configEntries[i]);
						break;
				}
			}
		}

		private IFilesStorageProviderV40 GetDefaultFilesStorageProvider() {
			string defaultFilesStorageProviderName = _host.GetGlobalSettingValue(GlobalSettingName.DefaultFilesStorageProvider);
			return _host.GetFilesStorageProviders(_wiki).First(p => p.GetType().FullName == defaultFilesStorageProviderName);
		}

		private bool DirectoryExists(IFilesStorageProviderV40 filesStorageProvider, string directoryName) {
			string[] directoryList = filesStorageProvider.ListDirectories("/");
			foreach(string dir in directoryList) {
				if(dir == directoryName) return true;
			}
			return false;
		}

		private bool FileExists(IFilesStorageProviderV40 filesStorageProvider, string directory, string fileName) {
			string[] filesList = filesStorageProvider.ListFiles(directory);
			foreach(string file in filesList) {
				if(file == directory + fileName) return true;
			}
			return false;
		}

		/// <summary>
		/// Method called when the plugin must handle a HTTP request.
		/// </summary>
		/// <param name="context">The HTTP context.</param>
		/// <param name="urlMatch">The URL match.</param>
		/// <returns><c>true</c> if the request was handled, <c>false</c> otherwise.</returns>
		/// <remarks>This method is called only when a request matches the 
		/// parameters configured by calling <see cref="IHostV40.RegisterRequestHandler"/> during <see cref="IProviderV40.SetUp"/>. 
		/// If the plugin <b>did not</b> call <see cref="IHostV40.RegisterRequestHandler"/>, this method is never called.</remarks>
		public bool HandleRequest(HttpContext context, Match urlMatch) {
			if(urlMatch.Value == "_setrating.ashx") {
				string vote = context.Request["vote"];
				string page = context.Request["page"];

				if(!string.IsNullOrEmpty(vote) && !string.IsNullOrEmpty(page)) {
					AddRating(page, int.Parse(vote));
					System.Web.HttpCookie cookie = new System.Web.HttpCookie(CookieNamePrefix + page, vote);
					cookie.Expires = DateTime.Now.AddYears(10);

					context.Response.Clear();
					context.Response.Cookies.Add(cookie);
					return true;
				}
				else return false;
			}
			else {
				string mime = null;
				byte[] content = null;

				switch(urlMatch.Value) {
					case "star.gif.ashx":
						mime = "image/gif";
						content = GifContent;
						break;
					case "javascript.js.ashx":
						mime = "application/javascript";
						content = JsContent;
						break;
					case "styles.css.ashx":
						mime = "text/css";
						content = CssContent;
						break;
					case "rating.js.ashx":
						mime = "application/javascript";
						content = Js2Content;
						break;
					default:
						return false;
				}

				context.Response.Clear();
				context.Response.ContentType = mime;
				context.Response.ContentEncoding = Encoding.UTF8;
				context.Response.OutputStream.Write(content, 0, content.Length);
				context.Response.CacheControl = "private";

				return true;
			}
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			// Setup request handlers

			host.RegisterRequestHandler(this, @"(star\.gif\.ashx)|(javascript\.js\.ashx)|(styles\.css\.ashx)|(rating\.js\.ashx)", new[] { "GET", "HEAD" });

			host.RegisterRequestHandler(this, @"_setrating\.ashx", new[] { "POST" });

			host.AddHtmlHeadContent(this,
@"<script type=""text/javascript"" src=""javascript.js.ashx""></script>
<link rel=""StyleSheet"" href=""styles.css.ashx"" type=""text/css"" />
<script type=""text/javascript"" src=""rating.js.ashx""></script>");
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			// Nothing to do
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return Info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return "Specify <i>logooptions=nolog</i> for disabling warning log messages for exceptions."; }
		}

	}

}
