
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Reflection;
using System.IO;

namespace ScrewTurn.Wiki.PluginFramework {
	/// <summary>
	/// The handler for custom StorageProvider section in WebConfig.
	/// </summary>
	public class WikiListHandler : IConfigurationSectionHandler {

		/// <summary>
		/// Creates a configuration section handler.
		/// </summary>
		/// <param name="parent">Parent object.</param>
		/// <param name="configContext">Configuration context object.</param>
		/// <param name="section">Section XML node.</param>
		/// <returns>The created section handler object.</returns>
		public object Create(object parent, object configContext, XmlNode section) {
			List<Wiki> storageProviders = new List<Wiki>();

			foreach(XmlNode provider in section) {
				XmlAttributeCollection attributeCollection = provider.Attributes;
				if(attributeCollection == null) continue;

				string wikiName = attributeCollection["name"].Value;
				string hostList = attributeCollection["host"].Value;
				storageProviders.Add(new PluginFramework.Wiki(wikiName,	hostList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>()));
			}
			return storageProviders;
		}

	}
}

