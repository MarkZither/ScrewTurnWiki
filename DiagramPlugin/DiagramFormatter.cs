using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ScrewTurn.Wiki.PluginFramework;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ScrewTurn.Wiki.Plugins.Diagram
{
    public class DiagramFormatter : IFormatterProviderV30 
    {
	    private IHostV30 _host;
	    private bool _enableLogging = true;

		const string defaultDirectoryName = "/__DiagramPlugin/";
		const string cssFileName = "diagram.css";
		const string jsFileName = "five.js";
		const string gridImageFileName = "grid.gif";
		private static readonly ComponentInformation Info = new ComponentInformation("Diagram Plugin", "Ben Kalegin", "0.1", "http://www.kalegin.com", "");
		private static readonly Regex DiagramRegex = new Regex(@"{diagram(\|(.+?))?}",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		private void LogWarning(string message)
		{
			if (_enableLogging)
			{
				_host.LogEntry(message, LogEntryType.Warning, null, this);
			}
		}

		private void LogError(string message)
		{
			if (_enableLogging)
			{
				_host.LogEntry(message, LogEntryType.Error, null, this);
			}
		}	
		
		private void LogDebug(string message)
		{
			if (_enableLogging)
			{
				_host.LogEntry(message, LogEntryType.General, null, this);
			}
		}

		private IFilesStorageProviderV30 GetDefaultFilesStorageProvider()
		{
			string defaultFilesStorageProviderName = _host.GetSettingValue(SettingName.DefaultFilesStorageProvider);
			return _host.GetFilesStorageProviders(true).First(p => p.GetType().FullName == defaultFilesStorageProviderName);
		}

		private static bool DirectoryExists(IFilesStorageProviderV30 filesStorageProvider, string directoryName)
		{
			string[] directoryList = filesStorageProvider.ListDirectories("/");
			foreach (string dir in directoryList)
			{
				if (dir == directoryName) return true;
			}
			return false;
		}

		private static bool FileExists(IFilesStorageProviderV30 filesStorageProvider, string directory, string fileName)
		{
			string[] filesList = filesStorageProvider.ListFiles(directory);
			foreach (string file in filesList)
			{
				if (file == directory + fileName) return true;
			}
			return false;
		}

	    void IProviderV30.Init(IHostV30 host, string config)
	    {
		    _host = host;

		    try
		    {
			    if (config != null)
			    {
				    string[] configEntries = config.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
				    for (int i = 0; i < configEntries.Length; i++)
				    {
					    string[] configEntryDetails = configEntries[i].Split(new[] {"="}, 2, StringSplitOptions.None);
					    switch (configEntryDetails[0].ToLowerInvariant())
					    {
						    case "logoptions":
							    if (configEntryDetails[1] == "nolog")
							    {
								    _enableLogging = false;
							    }
							    else
							    {
								    LogWarning(@"Unknown value in ""logOptions"" configuration string: " + configEntries[i] +
								               "Supported values are: nolog.");
							    }
							    break;
						    default:
							    LogWarning("Unknown value in configuration string: " + configEntries[i]);
							    break;
					    }
				    }
			    }

			    IFilesStorageProviderV30 filesStorageProvider = GetDefaultFilesStorageProvider();

			    if (!DirectoryExists(filesStorageProvider, defaultDirectoryName))
			    {
				    var dir = defaultDirectoryName.Trim('/');
				    LogDebug("Dia> Creating directory: " + dir);
				    filesStorageProvider.CreateDirectory("/", dir);
				    LogDebug("Dia> Dir created");
			    }
			    if (true || !FileExists(filesStorageProvider, defaultDirectoryName, cssFileName))
			    {
				    LogDebug("Dia> Extracting css");
				    filesStorageProvider.StoreFile(defaultDirectoryName + cssFileName,
					    Assembly.GetExecutingAssembly()
						    .GetManifestResourceStream("ScrewTurn.Wiki.Plugins.Diagram.Resources.diagram.css"), true);
				    LogDebug("Dia> Css extracted");
			    }
			    if (true || !FileExists(filesStorageProvider, defaultDirectoryName, gridImageFileName))
			    {
				    LogDebug("Dia> Extracting grid image");
				    filesStorageProvider.StoreFile(defaultDirectoryName + gridImageFileName,
					    Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.Diagram.Resources.grid.gif"),
					    true);
				    LogDebug("Dia> Grid image extracted");
			    }

			    if (true || !FileExists(filesStorageProvider, defaultDirectoryName, jsFileName))
			    {
				    LogDebug("Dia> Extracting javascrpt");
				    const string jsZipPath = "ScrewTurn.Wiki.Plugins.Diagram.Resources.diagram.js.zip";
				    Stream zippedJs = Assembly.GetExecutingAssembly().GetManifestResourceStream(jsZipPath);


				    if (zippedJs == null)
				    {
					    LogWarning("Cannot find resource " + jsZipPath);
					    return;
				    }
				    var js = new DeflateStream(zippedJs, CompressionMode.Decompress);
				    filesStorageProvider.StoreFile(defaultDirectoryName + jsFileName, js, true);
				    LogDebug("Dia> javascrpt Extracted");
			    }
		    }
		    catch (Exception e)
		    {
			    LogError(e.ToString());
		    }
	    }

		private static void StreamCopy(Stream source, Stream destination)
		{
			// 16 KB buffer used in the StreamCopy method
			// 16 KB seems to be the best break-even between performance and memory usage
			const int BufferSize = 16384;
			
			byte[] buff = new byte[BufferSize];
			int copied = 0;
			do
			{
				copied = source.Read(buff, 0, buff.Length);
				if (copied > 0)
				{
					destination.Write(buff, 0, copied);
				}
			} while (copied > 0);
		}
	    void IProviderV30.Shutdown()
	    {
	    }

	    ComponentInformation IProviderV30.Information
	    {
			get { return Info; }
		}

	    string IProviderV30.ConfigHelpHtml
	    {
			get { return "Specify <i>logooptions=nolog</i> for disabling warning log messages for exceptions."; }
	    }

	    bool IFormatterProviderV30.PerformPhase1
	    {
			get { return false; }
		}

	    bool IFormatterProviderV30.PerformPhase2
	    {
			get { return false; }
		}

	    bool IFormatterProviderV30.PerformPhase3
	    {
			get { return true; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		int IFormatterProviderV30.ExecutionPriority
		{
			get { return 50; }
		}

	    string IFormatterProviderV30.Format(string raw, ContextInformation context, FormattingPhase phase)
	    {
			var buffer = new StringBuilder(raw);
		    var anyDiagramsOnPage = false;
			try
			{
				if (context.Page != null)
				{
					anyDiagramsOnPage = ReplaceTags(context, buffer, context.Page.FullName);
				}
				else
				{
					return raw;
				}
			}
			catch (Exception ex)
			{
				LogWarning(string.Format("Exception occurred: {0}", ex.StackTrace));
			}
			if (anyDiagramsOnPage)
			{
				InjectStyleAndScript(buffer);
			}
			return buffer.ToString();
		}

	    private bool	ReplaceTags(ContextInformation context, StringBuilder buffer, string fullName)
	    {
			bool anyReplaced = false;
			int index;
			Match match;
		    while (FindAndRemoveFirstOccurrence(buffer, out index, out match))
		    {
				buffer.Insert(index, CreateContent(match.Groups[2].Value, context.Page));
			    anyReplaced = true;
		    }
		    return anyReplaced;
	    }

	    private static string StreamToString(Stream stream)
		{
			stream.Position = 0;
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}
	    private string CreateContent(string name, PageInfo currentPage)
	    {
			var format = new FileFormat(ReadAttachment(name, currentPage));
		    if (format.State == FormatState.Error)
		    {
			    return "Error parsing content: " + format.Error;
		    }
			
		    var builder = new StringBuilder();
		    builder.AppendLine(string.Format(@"<div id=""diagram-{0}"" class=""diagramBox"" init=""{1}""></div>", name, HttpUtility.HtmlEncode(format.FileContent)));
     	    return builder.ToString();
	    }

	    private string ReadAttachment(string name, PageInfo currentPage)
	    {
		    IFilesStorageProviderV30 filesStorageProvider = GetDefaultFilesStorageProvider();
		    string content;
		    using (var stream = new MemoryStream())
		    {
			    filesStorageProvider.RetrievePageAttachment(currentPage, name + ".htm", stream, false);
			    content = StreamToString(stream);
		    }
		    return content;
	    }

	    private static bool FindAndRemoveFirstOccurrence(StringBuilder buffer, out int index, out Match match)
		{
			match = DiagramRegex.Match(buffer.ToString());
			index = 0;
			if (match.Success)
			{
				buffer.Remove(match.Index, match.Length);
				index = match.Index;
			}
			return match.Success;
		}

	    private static void InjectStyleAndScript(StringBuilder buffer)
	    {
		    buffer.Append(@"<script type=""text/javascript"" src=""GetFile.aspx?file=" + defaultDirectoryName + jsFileName +
		                  @"""></script>");
		    buffer.Append(@"<link rel=""StyleSheet"" href=""GetFile.aspx?file=" + defaultDirectoryName + cssFileName +
		                  @""" type=""text/css"" />");
		    buffer.Append(@"<script type=""text/javascript""> <!--
window.onload = function () {
	var divs = document.getElementsByClassName('diagramBox');
	for(i =0; i < divs.length; i++) {
		var div = divs[i];
		var graph = new Five.Graph(div);
		var model = graph.getModel();
		var decoded = div.getAttribute('init');
		var mm = Five.Mindmap.MmFileReader.read(decoded);
		model.beginUpdate();
		try
		{
			new Five.Mindmap.Adaptor(graph, mm).render();
		}
		finally
		{
			// Updates the display
			model.endUpdate();
		}
	}
};
//--> </script>");
	    }

	    public string PrepareTitle(string title, ContextInformation context)
	    {
			return title;
		}
    }
}
