
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// A Lock implementation for Sql.
	/// </summary>
	public class SqlLock : Lock{

		/// <summary>
		/// Determines whether this instance is locked.
		/// </summary>
		/// <returns><c>true</c> if this instance is locked; otherwise, <c>false</c>.</returns>
		public override bool IsLocked() {
			return false;
		}

		/// <summary>
		/// Obtains the lock.
		/// </summary>
		/// <returns><c>true</c> if the lock has been obtained, <c>false</c> otherwise.</returns>
		public override bool Obtain() {
			return true;
		}

		/// <summary>
		/// Releases the lock.
		/// </summary>
		public override void Release() {
			return;
		}
	}
}
