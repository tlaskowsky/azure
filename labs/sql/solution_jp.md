# ラボ解決策

削除コマンドのヘルプを表示します：



```
az sql db delete --help
```


データベース名、サーバー名、リソースグループを指定する必要があります。例えば：



```
az sql db delete --name db01 --resource-group labs-sql --server <server-name>
```


> 確認を求められます。

コマンドが完了したら、ポータルでSQL Serverインスタンスを開き、_削除されたデータベース_ セクションを開きます。

[ポータルで削除されたデータベースを復元](https://docs.microsoft.com/ja-jp/azure/azure-sql/database/recovery-using-backups#deleted-database-restore-by-using-the-azure-portal)することができますが、新しく削除されたデータベースが表示されるまでに数分かかります。

リソースグループを削除すると、SQL Serverも削除されます：



```
az group delete -n labs-sql -y
```


これにより、すべてのデータベースとバックアップが削除され、削除されたデータベースを復元することはできなくなります。
