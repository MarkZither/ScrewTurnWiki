using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ScrewTurn.Wiki {
	public partial class ThemesProviderSelector : System.Web.UI.UserControl {
		protected void Page_Load(object sender, EventArgs e) {

		}

		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public string SelectedProvider {
			get { return lstThemesProviders.SelectedValue; }
			set {
				lstThemesProviders.SelectedIndex = -1;
				foreach(ListItem itm in lstThemesProviders.Items) {
					if(itm.Value == value) {
						itm.Selected = true;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Determines whether this instance is default.
		/// </summary>
		public bool IsDefault() {
			bool result = false;
			if(lstThemesProviders.SelectedValue == "default") result = true;
			return result;
		}
		/// <summary>
		/// Event fired when the selected provider changes.
		/// </summary>
		public event EventHandler<EventArgs> SelectedThemesProviderChanged;

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			if(SelectedThemesProviderChanged != null) SelectedThemesProviderChanged(sender, e);
		}
	}
}