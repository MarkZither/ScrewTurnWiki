
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represent a Wiki object containing info about the name of a wiki and its associated hosts.
	/// </summary>
	public class Wiki {

		private string _wikiName;
		private List<string> _wikiHosts;

		/// <summary>
		/// Initializes a new instance of the <see cref="Wiki"/> class.
		/// </summary>
		/// <param name="wikiName">The name of the wiki.</param>
		/// <param name="wikiHosts">The hosts associated with the wiki name.</param>
		public Wiki(string wikiName, List<string> wikiHosts) {
			_wikiName = wikiName;
			_wikiHosts = wikiHosts;
		}

		/// <summary>
		/// Gets the name of the wiki.
		/// </summary>
		public string WikiName {
			get {
				return _wikiName;
			}
		}

		/// <summary>
		/// Gets the hosts.
		/// </summary>
		public List<string> Hosts {
			get {
				return _wikiHosts;
			}
		}
	}
}
