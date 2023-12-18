# データベーススキーマのデプロイ

アプリがORMを使用して独自のスキーマを作成できる場合は、空のSQLデータベースで問題ありませんが、すべてのアプリがそれを行うわけではありません。Microsoftは、データベーススキーマのパッケージング形式を提供しており、AzureにアップロードしてAzure SQLインスタンスにデプロイできます。

このラボでは、既存のデータベースバックアップファイルを取り、新しいデータベースにデプロイして、アプリが使用できるようにします。

## 参照

- [VS Code内のSQL Serverプロジェクト](https://learn.microsoft.com/ja-jp/sql/azure-data-studio/extensions/sql-database-project-extension?view=sql-server-ver16)
- [データ層アプリケーション - BacpacおよびDacpacファイル](https://learn.microsoft.com/ja-jp/sql/relational-databases/data-tier-applications/data-tier-applications?view=sql-server-ver16)
- [`az sql db` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/sql/db?view=azure-cli-latest)

## SQL Serverの作成

まず、新しいデータベースに使用するSQL Serverを作成します。何度か使用するパラメータがあるので、変数に保存しておきます。



```
# PowerShellで変数を設定：
$location='southeastasia'
$rg='labs-sql-schema'
$server='<unique-server-dns-name>'
$database='assets-db'

# Bashでの場合：
location='southeastasia'
rg='labs-sql-schema'
server='<unique-server-dns-name>'
database='assets-db'

# RG、サーバー、データベースを作成：
az group create -n $rg  -l $location --tags courselabs=azure

az sql server create -g $rg -l $location -n $server -u sqladmin -p <admin-password>
```


ポータルでサーバーを開きます：

- 上部メニューから _データベースのインポート_ オプションをクリックします。
- 設定の選択肢を探索します。
- 必要な入力と期待される出力は何ですか？

> Bacpac（データベースバックアップ）ファイルからデータベースをインポートできます。

インポートするファイルはAzureに格納されている必要があるため、まずBacpacファイルをアップロードします。

## Bacpacファイルのアップロード

Azureストレージアカウントにファイルをアップロードできます。

ストレージに関する詳細なラボが後にありますが、これで始められます：



```
# ストレージアカウントを作成します - 一意で小文字のみの名前が必要です：
az storage account create  -g $rg -l $location --sku Standard_LRS -n <storage-account-name>

# ファイルをアップロードするためのコンテナを作成します：
az storage container create -n databases --account-name <storage-account-name>

# ローカルファイルをコンテナにアップロードします - AzureではファイルをBLOB（バイナリ大容量オブジェクト）と呼びます：

az storage blob upload -f ./labs/sql-schema/assets-db.bacpac -c databases -n assets-db.bacpac --account-name <storage-account-name>
```

📋 ポータルでリソースグループを開き、アップロードしたBacpacの詳細を見つけます。URLからダウンロードできますか？

<details>
  <summary>わからない場合</summary>

リソースグループをリフレッシュして、ストレージアカウントがリストされているのを見つけます。そのリソースを開きます：

- 左メニューの _データストレージ_ の下に _コンテナ_ があります。
- それを開くと、すべてのblobコンテナがリストされています。
- _databases_ コンテナを開くと、アップロードされたファイルが表示されます。
- ファイルをクリックすると、URLを含む詳細が表示されます。

</details><br/>

Blobストレージはパブリックファイル共有として使用できます。あなたのblobには `https://labssqlschemaes.blob.core.windows.net/databases/assets-db.bacpac` のようなURLがあります - アドレスの最初の部分はストレージアカウント名で、これが一意である必要があります。

しかし、そのアドレスからはダウンロードできません。デフォルトでは新しいblobコンテナはプライベートに設定されており、Azure内でのみアクセス可能です。これは私たちがやりたいことには問題ありません。

## 新しいデータベースにBacpacをインポートする

CLIではまず新しいデータベースを作成し、ローカルマシンからのアクセスを許可してから、アップロードしたBacpacをそのデータベースにインポートします：



```
az sql db create -g $rg -n $database -s $server
```


> 実行中に、インポートの準備のために続けて詳細を取得できます。

データベースのインポートは、バックアップから新しいデータベースを作成する迅速な方法です。オンプレミスデータベースをBacpacファイルにエクスポートし、Azureにアップロードして、データを含むAzure SQLデータベースにインポートするプロセスはシンプルです。

- Bacpacはデータベーススキーマとデータのエクスポートです。
- Dacpacはデータなしのデータベーススキーマのモデルです。Dacpacからのインポートはできません。

📋 アップロードしたBacpacファイルを使用して、`sql db import` コマンドでデータベースを作成します。

<details>
  <summary>わからない場合</summary>

ヘルプテキストを確認します：



```
az sql db import --help

# 内部Azureサービスのアクセスを許可：
az sql server firewall-rule create -g $rg -s $server -n azure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# 自分のパブリックIPアドレスを見つけます（またはhttps://www.whatsmyip.orgにアクセス）
curl ifconfig.me

az sql server firewall-rule create -g $rg -s $server -n client --start-ip-address <ip-address> --end-ip-address <ip-address> 
```

必要なもの:

- SQL Serverの管理者資格情報
- ストレージアカウントからインポートするファイルのURL
- ストレージアカウントキー
- 新しいデータベースと使用するサーバーの名前

まず、blobストレージのアクセスキーを取得する必要があります。ヘルプテキストには、ファイルにアクセスするための認証トークンである共有アクセスキー（SAS）を取得する例が示されています：



```
# 認証トークンを生成して表示：
az storage blob generate-sas  -c databases -n assets-db.bacpac --permissions r --expiry 2030-01-01T00:00:00Z --account-name <storage-account-name>
```


出力でキーを取得し、`import` コマンドに挿入します：



```
az sql db import -s $server -n $database -g $rg --storage-key-type SharedAccessKey -u sqladmin -p <server-password> --storage-uri <bacpac-url> --storage-key <sas-key>
```


</details><br/>

ここではいくつかの部品を組み合わせる必要がありますが、問題があればCLIの出力が修正の手助けになるはずです。

> データベースの作成には[長い時間がかかる](https://docs.microsoft.com/ja-jp/azure/azure-sql/database/database-import-export-hang?view=azuresql)ことがあります。ポータルで _Import/Export history_ タブを開いて進捗状況を確認できます。

## 新しいデータベースの使用

ポータルでデータベースを開き、_Query Editor_ ブレードを開きます。これにより、ブラウザからデータベースに接続してSQLコマンドを実行できます。`import` コマンドで設定した管理者の資格情報でログインし、スキーマがデプロイされているかを確認します。

📋 Bacpacにはインポート時に挿入されるいくつかの参照データが含まれています。イギリスのロケーションの郵便番号を見つけることができますか？

<details>
  <summary>わからない場合</summary>

クエリエディタウィンドウには左側にオブジェクトエクスプローラがあります。ここでスキーマをナビゲートして、テーブルと列の名前を見つけることができます。

それから、エディタ内で実行できる標準的なSQLステートメントです：



```
SELECT * FROM Locations

SELECT PostalCode FROM Locations WHERE Country='UK'
```


</details><br/>


## ラボ

assetsテーブルにいくつかのデータを挿入します：



```
INSERT INTO [dbo].[Assets] (AssetTypeId, LocationId, AssetDescription)
VALUES (1, 1, 'Siddhesh''s MacBook Air')

INSERT INTO [dbo].[Assets] (AssetTypeId, LocationId, AssetDescription)
VALUES (2, 2, 'Siddhesh''s Mac Studio')

INSERT INTO [dbo].[Assets] (AssetTypeId, LocationId, AssetDescription)
VALUES (3, 2, 'Siddhesh''s iPhone')
```


これは元のBacpacには含まれていない追加データです。AzureデータベースからBacpacをエクスポートします。そのファイルを使用して、別のインスタンスでデータを再作成するにはどうすればよいでしょうか。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドでRGを削除し、すべてのリソースを削除できます：



```
az group delete -y -n labs-sql-schema
```
