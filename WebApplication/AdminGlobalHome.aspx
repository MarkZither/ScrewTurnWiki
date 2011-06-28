<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminGlobalHome.aspx.cs" Inherits="ScrewTurn.Wiki.AdminGlobalHome" culture="auto" meta:resourcekey="PageResource2" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnAdminHome" ContentPlaceHolderID="cphAdmin" runat="server">
<div class="leftaligned">
		<script type="text/javascript">
		<!--
			function PreIndexRebuild() {
				if(RequestConfirm()) {
					document.getElementById("ProgressSpan").style["display"] = "";
					return true;
				}
				else return false;
			}

			function PostIndexRebuild() {
				document.getElementById("ProgressSpan").style["display"] = "none";
			}

			function PreLinksRebuild() {
				if(RequestConfirm()) {
					document.getElementById("OrphansProgressSpan").style["display"] = "";
					return true;
				}
				else return false;
			}

			function PostLinksRebuild() {
				document.getElementById("OrphansProgressSpan").style["display"] = "none";
			}
		// -->
		</script>

		<h2 class="sectiontitle"><asp:Literal ID="lblAdminHome" runat="server" Text="Administration Home" EnableViewState="False" meta:resourcekey="lblAdminHomeResource1" /></h2>
	
		<p>
			<asp:Literal ID="lblSystemStatusContent" runat="server" meta:resourcekey="lblSystemStatusContentResource1" />
		</p>

		<br /><br />

		<h2 class="separator"><asp:Literal ID="lblAppShutdown" runat="server" Text="Web Application Shutdown" meta:resourcekey="lblAppShutdownResource1" /></h2>
		<asp:Literal ID="lblAppShutdownInfo" runat="server" 
			Text="You can force a shutdown-and-restart cycle of the Web Application. You will be asked to confirm the restart twice, then the Web Application will restart at the first subsequent request.&lt;br /&gt;&lt;b&gt;Warning&lt;/b&gt;: all the open sessions will be lost, and users may experience errors.&lt;br /&gt;&lt;b&gt;Note&lt;/b&gt;: the restart will affect only this Web Application." 
			meta:resourcekey="lblAppShutdownInfoResource1" />
		<br /><br />
		<div id="ShutdownDiv" class="warning">
			<asp:Button ID="btnShutdownConfirm" runat="server" Text="Shutdown Application" OnClientClick="javascript:return RequestConfirm();"
				OnClick="btnShutdownConfirm_Click" meta:resourcekey="btnShutdownConfirmResource2" />
		</div>
	</div>

	<div class="clear"></div>
</asp:Content>
