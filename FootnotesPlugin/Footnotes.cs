
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a footnotes plugin.
	/// </summary>
	public class Footnotes : IFormatterProviderV40 {

		// Kindly contributed by Jens Felsner

		private static readonly ComponentInformation info = new ComponentInformation("Footnotes Plugin", "Threeplicate Srl", "4.0.5.143", "http://www.screwturn.eu", "http://www.screwturn.eu/Version4.0/PluginPack/Footnotes.txt");

		private static readonly Regex ReferencesRegex = new Regex("(<[ ]*references[ ]*/[ ]*>|<[ ]*references[ ]*>.*?<[ ]*/[ ]*references[ ]*>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RefRegex = new Regex("<[ ]*ref[ ]*>.*?<[ ]*/[ ]*ref[ ]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RefRemovalRegex = new Regex("(<[ ]*ref[ ]*>|<[ ]*/[ ]*ref[ ]*>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private IHostV40 host = null;
		private string config = "";
		private string wiki;

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return wiki; }
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
			this.host = host;
			this.config = config != null ? config : "";
			this.wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki;
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) { }

		// Replaces the first occurence of 'find' in 'input' with 'replace'
		private static string ReplaceFirst(string input, string find, string replace) {
			return input.Substring(0, input.IndexOf(find)) + replace + input.Substring(input.IndexOf(find) + find.Length);
		}

		/// <summary>
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			// Match all <ref>*</ref>
			MatchCollection mc = RefRegex.Matches(raw);

			// No ref-tag found, nothing to do
			if(mc.Count == 0) return raw;

			// No references tag
			if(ReferencesRegex.Matches(raw).Count == 0) {
				return raw + "<br/><span style=\"color: #FF0000;\">Reference Error! Missing element &lt;references/&gt;</span>";
			}

			string output = raw;
			string ref_string = "<table class=\"footnotes\">";

			int footnoteCounter = 0;

			// For each <ref>...</ref> replace it with Footnote, append it to ref-section
			foreach(Match m in mc) {
				footnoteCounter++;
				output = ReplaceFirst(output, m.Value, "<a id=\"refnote" + footnoteCounter.ToString() + "\" href=\"#footnote" + footnoteCounter.ToString() + "\"><sup>" + footnoteCounter.ToString() + "</sup></a>");

				ref_string += "<tr><td><a id=\"footnote" + footnoteCounter.ToString() + "\" href=\"#refnote" + footnoteCounter.ToString() + "\"><sup>" + footnoteCounter.ToString() + "</sup></a></td><td>" + RefRemovalRegex.Replace(m.Value, "") + "</td></tr>";
			}
			ref_string += "</table>";

			// Replace <reference/> with ref-section
			output = ReferencesRegex.Replace(output, ref_string);

			return output;
		}

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
			get { return true; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 3.
		/// </summary>
		public bool PerformPhase3 {
			get { return false; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		public int ExecutionPriority {
			get { return 50; }
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
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() { }

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
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
			return false;
		}

	}

}
