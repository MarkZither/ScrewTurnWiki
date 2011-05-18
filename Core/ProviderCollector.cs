
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Reflection;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a generic Provider Collector.
	/// </summary>
	/// <typeparam name="T">The type of the Collector.</typeparam>
	public class ProviderCollector<T> where T : class, IProviderV30 {

		// TypeName -> Assembly
		private Dictionary<Type, Assembly> assembliesDictionary;
		// wiki -> (providerType -> providerInstance)
		private Dictionary<string, Dictionary<Type, T>> instancesDictionary;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public ProviderCollector() {
			assembliesDictionary = new Dictionary<Type, Assembly>(3);
			instancesDictionary = new Dictionary<string, Dictionary<Type, T>>();
		}

		/// <summary>
		/// Adds a Provider to the Collector.
		/// </summary>
		/// <param name="provider">The Provider to add.</param>
		/// <param name="assembly">The provider assembly.</param>
		public void AddProvider(Type provider, Assembly assembly) {
			lock(this) {
				assembliesDictionary[provider] = assembly;
			}
		}

		/// <summary>
		/// Removes a Provider from the Collector.
		/// </summary>
		/// <param name="typeName">The Provider to remove.</param>
		public void RemoveProvider(string typeName) {
			lock(this) {
				foreach(Type type in assembliesDictionary.Keys) {
					if(type.FullName.Equals(typeName)) {
						foreach(string wiki in instancesDictionary.Keys) {
							if(instancesDictionary[wiki].ContainsKey(type)) {
								instancesDictionary[wiki][type].Dispose();
								instancesDictionary[wiki].Remove(type);
							}
						}
						assembliesDictionary.Remove(type);
					}
				}
			}
		}

		/// <summary>
		/// Gets all the Providers (copied array).
		/// </summary>
		public T[] GetAllProviders(string wiki) {
			string wikiKey = wiki != null ? wiki : "-";
			lock(this) {
				List<T> providers = new List<T>(assembliesDictionary.Count);
				foreach(Type key in assembliesDictionary.Keys) {
					T provider = null;
					if(!instancesDictionary.ContainsKey(wikiKey)) {
						instancesDictionary[wikiKey] = new Dictionary<Type, T>(3);
					}
					if(instancesDictionary[wikiKey].ContainsKey(key)) {
						provider = instancesDictionary[wikiKey][key];
					}
					else {
						provider = ProviderLoader.CreateInstance<T>(assembliesDictionary[key], key);
						ProviderLoader.Initialize<T>(provider, ProviderLoader.LoadProviderConfiguration(key.FullName, typeof(T)), wiki);
						instancesDictionary[wikiKey][key] = provider;
					}
					providers.Add(provider);
				}
				return providers.ToArray();
			}
		}
		
		/// <summary>
		/// Gets a Provider, searching for its Type Name.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The Provider, or null if the Provider was not found.</returns>
		public T GetProvider(string typeName, string wiki) {
			string wikiKey = wiki != null ? wiki : "-";
			lock(this) {
				foreach(Type type in assembliesDictionary.Keys) {
					if(type.FullName.Equals(typeName)) {
						T provider = null;
						if(!instancesDictionary.ContainsKey(wikiKey)) {
							instancesDictionary[wikiKey] = new Dictionary<Type, T>(3);
						}
						if(instancesDictionary[wikiKey].ContainsKey(type)) {
							provider = instancesDictionary[wikiKey][type];
						}
						else {
							provider = ProviderLoader.CreateInstance<T>(assembliesDictionary[type], type);
							ProviderLoader.Initialize<T>(provider, ProviderLoader.LoadProviderConfiguration(type.FullName, typeof(T)), wiki);
							instancesDictionary[wikiKey][type] = provider;
						}
						return provider;
					}
				}
				return default(T);
			}
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <returns>A clone of this instance.</returns>
		public ProviderCollector<T> Clone() {
			ProviderCollector<T> ret = new ProviderCollector<T>();
			foreach(var provider in assembliesDictionary) {
				ret.AddProvider(provider.Key, provider.Value);
			}
			return ret;
		}

		/// <summary>
		/// Releases resources
		/// </summary>
		public void Dispose() {
			foreach(string wiki in instancesDictionary.Keys) {
				foreach(Type key in instancesDictionary[wiki].Keys) {
					instancesDictionary[wiki][key].Dispose();
				}
			}
			instancesDictionary.Clear();
		}
	}

}
