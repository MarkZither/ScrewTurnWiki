
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ScrewTurn.Wiki {

	public class WebRole : RoleEntryPoint {

		public override bool OnStart() {
			RoleEnvironment.Changing += new EventHandler<RoleEnvironmentChangingEventArgs>((s, e) => {
				e.Cancel = true;
			});

			return base.OnStart();
		}

	}

}
