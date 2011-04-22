
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Reflection;
using System.IO;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// The handler for custom StorageProvider section in WebConfig.
	/// </summary>
	public class StorageProviderHandler : IConfigurationSectionHandler {

		/// <summary>
		/// Creates a configuration section handler.
		/// </summary>
		/// <param name="parent">Parent object.</param>
		/// <param name="configContext">Configuration context object.</param>
		/// <param name="section">Section XML node.</param>
		/// <returns>The created section handler object.</returns>
		public object Create(object parent, object configContext, XmlNode section) {
			List<StorageProvider> storageProviders = new List<StorageProvider>();

			foreach(XmlNode provider in section) {
				XmlAttributeCollection attributeCollection = provider.Attributes;
				string typeName = attributeCollection["name"].Value;
				string assemblyName = attributeCollection["assembly"].Value;
				string config = attributeCollection["config"].Value;
				storageProviders.Add(new StorageProvider() {
					TypeName = typeName,
					AssemblyName = assemblyName,
					ConfigurationString = config
				});
			}
			return storageProviders;
		}

	}
}

