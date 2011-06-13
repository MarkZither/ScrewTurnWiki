
if (select count([Name]) from [Namespace] where [Name] = '' and [Wiki] = @wiki) = 0
begin
	insert into [Namespace] ([Wiki], [Name], [DefaultPage]) values (@wiki, '', null)
end
