
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that must be implemented in order to create a custom Index Directory Provider for ScrewTurn Wiki search engine.
	/// </summary>
	public interface IIndexDirectoryProviderV40 : IProviderV40 {

		/// <summary>
		/// Gets the directory object to be used by the Search engine.
		/// </summary>
		/// <returns>The directory object.</returns>
		Directory GetDirectory();

	}
}
