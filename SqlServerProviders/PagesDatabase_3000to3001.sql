
drop table [IndexWordMapping]
drop table [IndexWord]
drop table [IndexDocument]

create table [IndexDocument] (
	[Wiki] varchar(100) not null,
	[Id] int not null,
	[Name] nvarchar(200) not null
		constraint [UQ_IndexDocument] unique,
	[Title] nvarchar(200) not null,
	[TypeTag] varchar(10) not null,
	[DateTime] datetime not null,
	constraint [PK_IndexDocument] primary key clustered ([Wiki], [Id])
)

create table [IndexWord] (
	[Wiki] varchar(100) not null,
	[Id] int not null,
	[Text] nvarchar(200) not null
		constraint [UQ_IndexWord] unique,
	constraint [PK_IndexWord] primary key clustered ([Wiki], [Id])
)

create table [IndexWordMapping] (
	[Wiki] varchar(100) not null,
	[Word] int not null,
	[Document] int not null,
	[FirstCharIndex] smallint not null,
	[WordIndex] smallint not null,
	[Location] tinyint not null,
	constraint [FK_IndexWordMapping_IndexWord] foreign key ([Wiki], [Word]) references [IndexWord]([Wiki], [Id])
		on delete cascade on update cascade,
	constraint [FK_IndexWordMapping_IndexDocument] foreign key ([Wiki], [Document]) references [IndexDocument]([Wiki], [Id])
		on delete cascade on update cascade,
	constraint [PK_IndexWordMapping] primary key clustered ([Wiki], [Word], [Document], [FirstCharIndex], [WordIndex], [Location])
)

update [Version] set [Version] = 3001 where [Component] = 'Pages'
