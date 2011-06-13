
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
			bool defaultFound = false;
			foreach(XmlNode provider in section) {
				XmlAttributeCollection attributeCollection = provider.Attributes;
				if(attributeCollection == null) continue;

				string typeName = attributeCollection["name"].Value;
				string assemblyName = attributeCollection["assembly"].Value;
				string config = attributeCollection["config"].Value;
				string isDefaultString = attributeCollection["isDefault"] != null ? attributeCollection["isDefault"].Value : null;
				bool isDefault = false;
				bool.TryParse(isDefaultString, out isDefault);
				storageProviders.Add(new StorageProvider() {
					TypeName = typeName,
					AssemblyName = assemblyName,
					ConfigurationString = config,
					IsDefault = defaultFound ? false : isDefault
				});
			}

			if(!defaultFound) storageProviders[0].IsDefault = true;

			return storageProviders;
		}

	}
}

