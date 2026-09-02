BEGIN TRANSACTION;

                UPDATE [Initiatives]
                SET [Priority] = CASE
                        WHEN [Priority] IS NULL OR LTRIM(RTRIM([Priority])) = '' THEN 'Medium'
                        ELSE REPLACE([Priority], ' ', '')
                    END;


                UPDATE [Initiatives]
                SET [Status] = CASE
                        WHEN [Status] IS NULL OR LTRIM(RTRIM([Status])) = '' THEN 'Planning'
                        ELSE REPLACE([Status], ' ', '')
                    END;


                UPDATE [Initiatives]
                SET [Visibility] = CASE
                        WHEN [Visibility] IS NULL OR LTRIM(RTRIM([Visibility])) = ''
                            THEN 'InitiativeMembers'
                        ELSE REPLACE([Visibility], ' ', '')
                    END;


                UPDATE [Initiatives] SET [Priority] = 'Medium'
                WHERE [Priority] NOT IN ('High', 'Medium', 'Low');


                UPDATE [Initiatives] SET [Status] = 'Planning'
                WHERE [Status] NOT IN
                    ('Draft', 'Planning', 'OnTrack', 'AtRisk', 'OnHold', 'Completed', 'Cancelled');


                UPDATE [Initiatives] SET [Visibility] = 'InitiativeMembers'
                WHERE [Visibility] NOT IN
                    ('InitiativeMembers', 'Leadership', 'Organization', 'Private');

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Initiatives]') AND [c].[name] = N'Visibility');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Initiatives] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Initiatives] ALTER COLUMN [Visibility] nvarchar(50) NOT NULL;

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Initiatives]') AND [c].[name] = N'Status');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Initiatives] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Initiatives] ALTER COLUMN [Status] nvarchar(50) NOT NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Initiatives]') AND [c].[name] = N'Priority');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Initiatives] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Initiatives] ALTER COLUMN [Priority] nvarchar(50) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818114851_InitiativeEnumColumns', N'9.0.0');

COMMIT;
GO

