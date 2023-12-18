# ラボ解決策

SQL VM上のSSMSを使用して、[CREATE LOGIN](https://learn.microsoft.com/ja-jp/sql/t-sql/statements/create-login-transact-sql?view=sql-server-ver16) ステートメントで強力なパスワードを持つ新しいログインを作成します：



```
CREATE LOGIN labs2   
   WITH PASSWORD = '00234$$$jhjhj' 
GO  
```


SQL VMは既にパブリックアクセス用に設定されているので、接続できます。UDFを使用しようとすると：



```
SELECT dbo.LegacyDate() 
```


次のようなエラーが表示されます：

_オブジェクト 'LegacyDate'、データベース 'master'、スキーマ 'dbo' に対するEXECUTE権限が拒否されました_

したがって、VMで[オブジェクトの権限を付与](https://learn.microsoft.com/ja-jp/sql/t-sql/statements/grant-object-permissions-transact-sql?view=sql-server-ver16)する必要がありますが、権限はユーザーに付与されるため、まずログイン用のユーザーを作成する必要があります：


```
CREATE USER labs2 FOR LOGIN labs2

GRANT EXECUTE ON LegacyDate TO labs2
```


これで、リモートセッションでUDFを実行できます。
