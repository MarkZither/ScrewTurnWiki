
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

		private Dictionary<Type, Assembly> assembliesDictionary;
		private Dictionary<Type, T> instancesDictionary;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public ProviderCollector() {
			assembliesDictionary = new Dictionary<Type, Assembly>(3);
			instancesDictionary = new Dictionary<Type, T>(3);
		}

		/// <summary>
		/// Adds a Provider to the Collector.
		/// </summary>
		/// <param name="provider">The Provider to add.</param>
		/// <param name="assembly">The assembly.</param>
		public void AddProvider(Type provider, Assembly assembly) {
			lock(this) {
				assembliesDictionary[provider] = assembly;
			}
		}

		/// <summary>
		/// Removes a Provider from the Collector.
		/// </summary>
		/// <param name="provider">The Provider to remove.</param>
		public void RemoveProvider(Type provider) {
			lock(this) {
				if(instancesDictionary.ContainsKey(provider)) {
					instancesDictionary[provider].Dispose();
					instancesDictionary.Remove(provider);
				}
				assembliesDictionary.Remove(provider);
			}
		}

		/// <summary>
		/// Gets all the Providers (copied array).
		/// </summary>
		public T[] AllProviders {
			get {
				lock(this) {
					List<T> providers = new List<T>(assembliesDictionary.Count);
					foreach(Type key in assembliesDictionary.Keys) {
						T provider = null;
						if(instancesDictionary.ContainsKey(key)) {
							provider = instancesDictionary[key];
						}
						else {
							provider = ProviderLoader.CreateInstance<T>(assembliesDictionary[key], key);
							ProviderLoader.Initialize<T>(provider);
							instancesDictionary[key] = provider;
						}
						providers.Add(provider);
					}
					return providers.ToArray();
				}
			}
		}

		/// <summary>
		/// Gets the assembly associated with the given type.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <returns>The assembly</returns>
		public Assembly GetAssembly(Type typeName) {
			return assembliesDictionary[typeName];
		}

		/// <summary>
		/// Gets a Provider, searching for its Type Name.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <returns>The Provider, or null if the Provider was not found.</returns>
		public T GetProvider(string typeName) {
			lock(this) {
				foreach(Type type in assembliesDictionary.Keys) {
					if(type.FullName.Equals(typeName)) {
						T provider = null;
						if(instancesDictionary.ContainsKey(type)) {
							provider = instancesDictionary[type];
						}
						else {
							provider = ProviderLoader.CreateInstance<T>(assembliesDictionary[type], type);
							ProviderLoader.Initialize<T>(provider);
							instancesDictionary[type] = provider;
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
			foreach(Type key in instancesDictionary.Keys) {
				instancesDictionary[key].Dispose();
			}
			instancesDictionary.Clear();
		}
	}

}
