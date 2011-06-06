﻿
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki;

namespace ScrewTurn.Wiki {

	public partial class ProviderSelector : System.Web.UI.UserControl {

		private ProviderType providerType = ProviderType.Users;
		private bool excludeReadOnly = false;
		private UsersProviderIntendedUse usersProviderIntendedUse = UsersProviderIntendedUse.AccountsManagement;

		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = Tools.DetectCurrentWiki();
			
			object t = ViewState["ProviderType"];
			if(t != null) providerType = (ProviderType)t;

			t = ViewState["ExcludeReadOnly"];
			if(t != null) excludeReadOnly = (bool)t;

			t = ViewState["UsersProviderIntendedUse"];
			if(t != null) usersProviderIntendedUse = (UsersProviderIntendedUse)t;

			if(!Page.IsPostBack) {
				Reload();
			}
		}

		/// <summary>
		/// Reloads the providers list.
		/// </summary>
		public void Reload() {
			IProviderV40[] allProviders = null;
			string defaultProvider = null;
			switch(providerType) {
				case ProviderType.Users:
					allProviders = Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki);
					defaultProvider = GlobalSettings.DefaultUsersProvider;
					break;
				case ProviderType.Pages:
					allProviders = Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki);
					defaultProvider = GlobalSettings.DefaultPagesProvider;
					break;
				case ProviderType.Themes:
					allProviders = Collectors.CollectorsBox.ThemeProviderCollector.GetAllProviders(currentWiki);
					break;
				case ProviderType.Files:
					allProviders = Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki);
					defaultProvider = GlobalSettings.DefaultFilesProvider;
					break;
				default:
					throw new NotSupportedException();
			}

			lstProviders.Items.Clear();

			int count = 0;
			if(providerType == ProviderType.Themes) lstProviders.Items.Add(new ListItem("standard", "standard"));
			foreach(IProviderV40 prov in allProviders) {
				if(IsProviderIncludedInList(prov)) {
					string typeName = prov.GetType().FullName;
					lstProviders.Items.Add(new ListItem(prov.Information.Name, typeName));
					if(typeName == defaultProvider) lstProviders.Items[count].Selected = true;
					count++;
				}
			}
		}

		/// <summary>
		/// Detectes whether a provider is included in the list.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns><c>true</c> if the provider is included, <c>false</c> otherwise.</returns>
		private bool IsProviderIncludedInList(IProviderV40 provider) {
			IStorageProviderV40 storageProvider = provider as IStorageProviderV40;
			IUsersStorageProviderV40 usersProvider = provider as IUsersStorageProviderV40;

			switch(providerType) {
				case ProviderType.Users:
					return IsUsersProviderIncludedInList(usersProvider);
				case ProviderType.Pages:
					return storageProvider == null || (!storageProvider.ReadOnly || storageProvider.ReadOnly && !excludeReadOnly);
				case ProviderType.Files:
					return storageProvider == null || (!storageProvider.ReadOnly || storageProvider.ReadOnly && !excludeReadOnly);
				case ProviderType.Themes:
					return storageProvider == null || (!storageProvider.ReadOnly || storageProvider.ReadOnly && !excludeReadOnly);
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Detects whether a users provider is included in the list.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns><c>true</c> if the provider is included, <c>false</c> otherwise.</returns>
		private bool IsUsersProviderIncludedInList(IUsersStorageProviderV40 provider) {
			switch(usersProviderIntendedUse) {
				case UsersProviderIntendedUse.AccountsManagement:
					return !provider.UserAccountsReadOnly || (provider.UserAccountsReadOnly && !excludeReadOnly);
				case UsersProviderIntendedUse.GroupsManagement:
					return !provider.UserGroupsReadOnly || (provider.UserGroupsReadOnly && !excludeReadOnly);
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets or sets the provider type.
		/// </summary>
		public ProviderType ProviderType {
			get { return providerType; }
			set {
				providerType = value;
				ViewState["ProviderType"] = value;
			}
		}

		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public string SelectedProvider {
			get { return lstProviders.SelectedValue; }
			set {
				lstProviders.SelectedIndex = -1;
				foreach(ListItem itm in lstProviders.Items) {
					if(itm.Value == value) {
						itm.Selected = true;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to exclude read-only providers.
		/// </summary>
		public bool ExcludeReadOnly {
			get { return excludeReadOnly; }
			set {
				excludeReadOnly = value;
				ViewState["ExcludeReadOnly"] = value;
			}
		}

		/// <summary>
		/// Gets or sets the intended use of users storage provider, if applicable.
		/// </summary>
		public UsersProviderIntendedUse UsersProviderIntendedUse {
			get { return usersProviderIntendedUse; }
			set {
				usersProviderIntendedUse = value;
				ViewState["UsersProviderIntendedUse"] = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control is enabled.
		/// </summary>
		public bool Enabled {
			get { return lstProviders.Enabled; }
			set { lstProviders.Enabled = value; }
		}

		/// <summary>
		/// Gets a value indicating whether the control has providers.
		/// </summary>
		public bool HasProviders {
			get {
				return lstProviders.Items.Count > 0;
			}
		}

		/// <summary>
		/// Event fired when the selected provider changes.
		/// </summary>
		public event EventHandler<EventArgs> SelectedProviderChanged;

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			if(SelectedProviderChanged != null) {
				Reload();
				SelectedProviderChanged(sender, e);
			}

		}

	}

	/// <summary>
	/// Lists legal provider types.
	/// </summary>
	public enum ProviderType {
		/// <summary>
		/// Users storage providers.
		/// </summary>
		Users,
		/// <summary>
		/// Pages storage providers.
		/// </summary>
		Pages,
		/// <summary>
		/// Files storage providers.
		/// </summary>
		Files,
		/// <summary>
		///  Theme providers.
		/// </summary>
		Themes
	}

	/// <summary>
	/// Lists valid uses for users storage providers.
	/// </summary>
	public enum UsersProviderIntendedUse {
		/// <summary>
		/// Accounts management.
		/// </summary>
		AccountsManagement,
		/// <summary>
		/// Groups management.
		/// </summary>
		GroupsManagement
	}

}
