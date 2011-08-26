create table [SearchIndex] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Size] bigint not null,
	[LastModified] datetime not null,
	[Data] varbinary(max) not null,
	constraint [PK_SearchIndex] primary key clustered ([Wiki], [Name])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'SearchIndex') = 0
begin
	insert into [Version] ([Component], [Version]) values ('SearchIndex', 4000)
end
