<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AclActionsSelector.ascx.cs" Inherits="ScrewTurn.Wiki.AclActionsSelector" %>

<table>
	<tr>
		<td style="text-align: right;">
			<b><asp:Literal ID="lblGrant" runat="server" Text="Grant" EnableViewState="False" meta:resourcekey="lblGrantResource1" /></b>
		</td>
		<td>
			<b><asp:Literal ID="lblDeny" runat="server" Text="Deny" EnableViewState="False" meta:resourcekey="lblDenyResource1" /></b>
		</td>
	</tr>
	<tr>
		<td style="text-align: right;">
			<asp:CheckBoxList ID="lstActionsGrant" runat="server" TextAlign="Left" OnSelectedIndexChanged="lstActions_SelectedIndexChanged" meta:resourcekey="lstActionsGrantResource1" />
		</td>
		<td>
			<asp:CheckBoxList ID="lstActionsDeny" runat="server" 
				OnSelectedIndexChanged="lstActions_SelectedIndexChanged" 
				meta:resourcekey="lstActionsDenyResource1" />
		</td>
	</tr>
</table>
