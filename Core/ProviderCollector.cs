
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

		private Dictionary<Type, Assembly> dictionary;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public ProviderCollector() {
			dictionary = new Dictionary<Type, Assembly>(3);
		}

		/// <summary>
		/// Adds a Provider to the Collector.
		/// </summary>
		/// <param name="provider">The Provider to add.</param>
		/// <param name="assembly">The assembly.</param>
		public void AddProvider(Type provider, Assembly assembly) {
			lock(this) {
				dictionary[provider] = assembly;
			}
		}

		/// <summary>
		/// Removes a Provider from the Collector.
		/// </summary>
		/// <param name="provider">The Provider to remove.</param>
		public void RemoveProvider(Type provider) {
			lock(this) {
				dictionary.Remove(provider);
			}
		}

		/// <summary>
		/// Gets all the Providers (copied array).
		/// </summary>
		public T[] AllProviders {
			get {
				lock(this) {
					List<T> providers = new List<T>(dictionary.Count);
					foreach(Type key in dictionary.Keys) {
						T provider = ProviderLoader.CreateInstance<T>(dictionary[key], key);
						ProviderLoader.Initialize<T>(provider);
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
			return dictionary[typeName];
		}

		/// <summary>
		/// Gets a Provider, searching for its Type Name.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <returns>The Provider, or null if the Provider was not found.</returns>
		public T GetProvider(string typeName) {
			lock(this) {
				foreach(Type type in dictionary.Keys) {
					if(type.FullName.Equals(typeName)) {
						T provider = ProviderLoader.CreateInstance<T>(dictionary[type], type);
						ProviderLoader.Initialize<T>(provider);
						return provider;
					}
				}
				return default(T);
			}
		}
	}

}
