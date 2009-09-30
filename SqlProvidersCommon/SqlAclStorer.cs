
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.AclEngine;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a SQL ACL Storer.
	/// </summary>
	public class SqlAclStorer : AclStorerBase {

		private LoadData loadData;
		private DeleteEntries deleteEntries;
		private StoreEntries storeEntries;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SqlAclStorer" /> class.
		/// </summary>
		/// <param name="aclManager">The instance of the ACL Manager to handle.</param>
		/// <param name="loadData">The <see cref="T:LoadData" /> delegate.</param>
		/// <param name="deleteEntries">The <see cref="T:DeleteEntries" /> delegate.</param>
		/// <param name="storeEntries">The <see cref="T:StoreEntries" /> delegate.</param>
		public SqlAclStorer(IAclManager aclManager, LoadData loadData, DeleteEntries deleteEntries, StoreEntries storeEntries)
			: base(aclManager) {

			if(loadData == null) throw new ArgumentNullException("loadData");
			if(deleteEntries == null) throw new ArgumentNullException("deleteEntries");
			if(storeEntries == null) throw new ArgumentNullException("storeEntries");

			this.loadData = loadData;
			this.deleteEntries = deleteEntries;
			this.storeEntries = storeEntries;
		}

		/// <summary>
		/// Loads data from storage.
		/// </summary>
		/// <returns>The loaded ACL entries.</returns>
		protected override AclEntry[] LoadDataInternal() {
			return loadData();
		}

		/// <summary>
		/// Deletes some entries.
		/// </summary>
		/// <param name="entries">The entries to delete.</param>
		protected override void DeleteEntries(AclEntry[] entries) {
			deleteEntries(entries);
		}

		/// <summary>
		/// Stores some entries.
		/// </summary>
		/// <param name="entries">The entries to store.</param>
		protected override void StoreEntries(AclEntry[] entries) {
			storeEntries(entries);
		}

	}

	/// <summary>
	/// Defines a delegate for a method that loads ACL data from storage.
	/// </summary>
	/// <returns>The loaded ACL entries.</returns>
	public delegate AclEntry[] LoadData();

	/// <summary>
	/// Defines a delegate for a method that deletes ACL entries in the storage.
	/// </summary>
	/// <param name="entries">The entries to delete.</param>
	public delegate void DeleteEntries(AclEntry[] entries);

	/// <summary>
	/// Defines a delegate for a method that stores ACL entries in the storage.
	/// </summary>
	/// <param name="entries">The entries to store.</param>
	public delegate void StoreEntries(AclEntry[] entries);

}
