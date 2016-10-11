BEGIN TRY
MERGE Leaders
AS dst
USING (SELECT @app, @node, DATEADD(ms, @renewperiod, GETUTCDATE()))
AS src (app, node, expired)
ON (dst.app = src.app AND (dst.node = src.node OR dst.expired < GETUTCDATE()))
	WHEN MATCHED 
		THEN UPDATE SET dst.node = src.node, dst.expired = src.expired
	WHEN NOT MATCHED
		THEN INSERT VALUES (src.app, src.node, src.expired);
	END TRY
BEGIN CATCH
END CATCH

SELECT * FROM Leaders WHERE app = @app