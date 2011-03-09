using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki;

namespace ScrewTurn.Wiki.PluginFramework {


	/// <summary>
	/// The interface that must be implemented in order to create a custom Theme Storage Provider for ScrewTurn Wiki.
	/// </summary>
	public interface IThemeStorageProviderV30 : IProviderV30 {

		/// <summary>
		/// Retrives the lists of all theme saved.
		/// </summary>
		/// <returns>list contenent all theme</returns>
		List<string> GetListTheme();

		/// <summary>
		/// Retrieve all files present in the selected theme 
		/// </summary>
		/// <param name="name">The name of theme selected</param>
		/// <returns>list contenet all files for the selected theme</returns>
		List<string> GetListThemeFiles(string name);


		/// <summary>
		/// Stores the theme.
		/// </summary>
		/// <param name="filename">The name.</param>
		/// <param name="assembly">The zipFile contenent the theme</param>
		/// <returns><c>true</c> if the theme is saved, <c>false</c> otherwise.</returns>
		bool storeTheme(string filename, byte[] assembly);


		/// <summary>
		/// Deletes the theme.
		/// </summary>
		/// <param name="themename">The name.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		bool DeleteTheme(string themename);
	}
}
