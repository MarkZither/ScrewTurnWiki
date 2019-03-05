<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="KeepAlive.ascx.cs" Inherits="ScrewTurn.Wiki.KeepAlive" %>

<script type="text/javascript">
// <![CDATA[

	function Refresh() {
		$.ajax({
			url: 'SessionRefresh.aspx?Page=' + encodeURIComponent('<%= CurrentPage != null ? CurrentPage : "" %>'),
			success: function () {
				setTimeout('Refresh()', <%= ScrewTurn.Wiki.Collisions.EditingSessionTimeout * 1000 %>);
			}
		});
	}

	$(function () {
		Refresh();
	});

// ]]>
</script>
