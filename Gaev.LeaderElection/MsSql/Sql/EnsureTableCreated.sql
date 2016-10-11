IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Leaders]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[Leaders](
		[app] [nvarchar](50) NOT NULL,
		[node] [nvarchar](50) NOT NULL,
		[expired] [datetime] NOT NULL,
	 CONSTRAINT [PK_Leaders] PRIMARY KEY CLUSTERED 
	(
		[app] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
END