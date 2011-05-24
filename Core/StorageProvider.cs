
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// A storage provider object from web.config.
	/// </summary>
	public class StorageProvider {

		/// <summary>
		/// The name of the storage provider Type.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// The name of the storage provider assembly.
		/// </summary>
		public string AssemblyName;

		/// <summary>
		/// The configuration string of the storage provider.
		/// </summary>
		public string ConfigurationString;

		/// <summary>
		/// <c>true</c> if the storage provider is the default one.
		/// </summary>
		public bool IsDefault;
	}
}
