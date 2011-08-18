
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Configuration utilities.
	/// </summary>
	public static class Config {

		/// <summary>
		/// Gets the Azure Storage connection string.
		/// </summary>
		/// <returns>The connection string.</returns>
		public static string GetConnectionString() {
			return RoleEnvironment.GetConfigurationSettingValue("ConnectionString");
		}

	}

}
