
using System;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains the interface of the Providers Collectors Box.
	/// </summary>
	public interface ICollectorsBox {

		/// <summary>
		/// The global settings storage provider.
		/// </summary>
		/// <returns>The globalSettingsProvider.</returns>
		IGlobalSettingsStorageProviderV30 GlobalSettingsProvider { get; }

		/// <summary>
		/// Gets the settings provider.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The settingsProvider initialized for the given wiki.</returns>
		ISettingsStorageProviderV30 GetSettingsProvider(string wiki);

		/// <summary>
		/// Gets the files provider collector.
		/// </summary>
		ProviderCollector<IFilesStorageProviderV30> FilesProviderCollector { get; }

		/// <summary>
		/// Gets the formatter provider collector.
		/// </summary>
		ProviderCollector<IFormatterProviderV30> FormatterProviderCollector { get; }

		/// <summary>
		/// Gets the pages provider collector.
		/// </summary>
		ProviderCollector<IPagesStorageProviderV30> PagesProviderCollector { get; }

		/// <summary>
		/// Gets the theme provider collector.
		/// </summary>
		ProviderCollector<IThemeStorageProviderV30> ThemeProviderCollector { get; }

		/// <summary>
		/// Gets the users provider collector.
		/// </summary>
		ProviderCollector<IUsersStorageProviderV30> UsersProviderCollector { get; }

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		void Dispose();
		
	}
}
