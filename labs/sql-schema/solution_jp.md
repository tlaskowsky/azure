# ラボ解決策

ポータルを使用して：

- Azure SQLデータベースを開きます
- _エクスポート_ オプションはトップメニューにあります
- Bacpacのファイル名を入力します
- ラボで作成したAzureストレージアカウントを選択します
- SQL認証の詳細を入力します

またはCLIを使用して：



```
# ヘルプを表示：
az sql db export --help

# ファイル書き込み用のSASトークンを生成：
az storage blob generate-sas  -c databases -n assets-db.bacpac --permissions w --expiry 2030-01-01T00:00:00Z --account-name <storage-account-name>

# Bacpacをエクスポート：
az sql db export -s $server -n $database -g $rg -p <sql-password> -u sqladmin --storage-key-type SharedAccessKey --storage-key <sas-token> --storage-uri https://<storage-account-name>.blob.core.windows.net/databases/assets-db.bacpac
```


これで、AzureストレージにアップデートされたBacpacを、別のAzure SQLインスタンスのインポートコマンドで使用できます。
