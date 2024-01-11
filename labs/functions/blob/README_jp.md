# 関数: Blob ストレージから SQL サーバーへ

関数は統合コンポーネントとして非常に優れており、直接的に接続する方法がないシステムをつなぎ合わせることができます。_データレベルの統合_ は、一方のシステムがデータを書き込むときにトリガーされる関数がそのデータを読み取り、適応させたり豊かにしてから別のシステムに書き込むオプションです。

このラボでは、Blob ストレージへの書き込みからトリガーされる関数を使用し、SQL サーバーにテーブルを書き込むことで出力を生成します。依存関係のあるコンテナを使用してローカルで関数をテストし、その後 Azure にデプロイします。

## 参照

- [Blob ストレージ トリガーおよびバインディング リファレンス](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-storage-blob-trigger?tabs=in-process%2Cextensionv5&pivots=programming-language-csharp)

- [SQL サーバー バインディング リファレンス](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-azure-sql?tabs=in-process%2Cextensionv3&pivots=programming-language-csharp)

- [SQL サーバー バインディング サンプル](https://github.com/Azure/azure-functions-sql-extension)

## Blob ストレージ関数が SQL サーバーに書き込む

関数コードは `BlobToSql` ディレクトリにあります:

- [BlobToSql/UploadLog.cs](/labs/functions/blob/BlobToSql/UploadLog.cs) - Blob が保存された時に実行され、SQL サーバーにレコードを書き込みます

これらの属性はトリガーとバインディングを担当します:

- `[BlobTrigger]` は、`uploads` コンテナに Blob が作成されたときに関数を実行するように設定します
- `[Sql]` は出力バインディングで、データベーステーブル `UploadLogItems` にレコードを作成します

<details>
  <summary>参考までに</summary>

こちらは関数の作成方法です:


```
func init BlobToSql --dotnet 

cd BlobToSql

func new --name UploadLog --template BlobTrigger

dotnet add package Microsoft.Azure.WebJobs.Extensions.Sql
```


</details><br/>

トリガーはアップロードされた Blob に関する重要な詳細を提供します - 名前と完全な内容。SQL サーバーバインディングはテーブル名と接続文字列を指定します。オブジェクトをデータベーススキーマにマッピングする作業を行いますが、関数が実行される前にテーブルが存在する必要があります。

## 関数をローカルでテストする

Azure ストレージ エミュレーターと SQL サーバーデータベースをコンテナで起動します:

- [docker-compose.yml](/labs/functions/blob/docker-compose.yml) - 依存する各コンテナを定義します



```
docker compose -f labs/functions/blob/docker-compose.yml up -d
```


準備ができたら、SQL サーバーに接続してデータベースを作成します。ここではコマンドラインを使用しますが、任意の SQL クライアントを使用できます（Compose ファイルにあるパスワードで `localhost:1433` に接続）。

コンテナに接続します:



```
docker exec -it blob-mssql-1 "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P AzureD3v!!!"
```


> Arm64 マシン（例: Apple Silicon）を使用している場合、データベースエンジンは問題なく動作しますが、Docker イメージには SQL ツールがインストールされていません。_no such file or directory_ のエラーが表示された場合は、[SqlEctron](TODO) のような SQL クライアントを使用する必要があります。

データベーススキーマを作成します:



```
CREATE DATABASE func
GO

USE func
GO

CREATE TABLE dbo.UploadLogItems (Id uniqueidentifier primary key, BlobName nvarchar(200) not null, Size int null)
GO

SELECT * FROM UploadLogItems
GO
```


Ctrl-C/Cmd-C でコンテナシェルセッションを終了します。

設定ファイル `labs/functions/blob/BlobToSql/local.settings.json` を以下の内容で作成します:



```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "UploadInputStorageConnectionString": "UseDevelopmentStorage=true",
        "UploadSqlServerConnectionString": "Data Source=localhost;Initial Catalog=func;Integrated Security=False;User Id=sa;Password=AzureD3v!!!;MultipleActiveResultSets=True"
    }
}
```


これで、すべての依存関係と設定が整いました。ローカルで関数を実行できます:


```
cd labs/functions/blob/BlobToSql

func start
```


> ホストの出力が表示され、`blobTrigger` 関数がリストにあることがわかります

別のターミナルで、ストレージエミュレーターに blob コンテナを作成し、ファイルをアップロードします - **これが正しいアカウントキーです** - エミュレーターにはハードコーディングされています:


```
az storage container create --connection-string 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;' -n uploads

az storage blob upload --connection-string 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;' --file labs/functions/blob/document.txt --container uploads --name document1.txt
```


関数の出力に次のような行が表示されます:


```
[2022-11-07T17:11:19.263Z] New blob uploaded:document1.txt
[2022-11-07T17:11:20.643Z] Stored blob upload item in SQL Server
[2022-11-07T17:11:20.653Z] Executed 'UploadLog' (Succeeded, Id=986759b8-a91e-4a0a-a8ec-694cf315f972, Duration=1415ms)
```


もう一度 SQL サーバーコンテナに接続し、データが存在するか確認します:


```
USE func
GO

SELECT * FROM UploadLogItems
GO
```

Azure への展開にを行いましょう

## Azure へのデプロイ

基本的な関数設定を開始するための手順です：



```
az group create -n labs-functions-blob --tags courselabs=azure -l eastus

az storage account create -g labs-functions-blob --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-blob  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


次に、関数のための事前条件が必要です：

- `uploads` という blob コンテナを含む入力用のストレージアカウント
- このストレージアカウントの接続文字列を `UploadInputStorageConnectionString` としてアプリ設定に設定

- 上記と同じデータベーススキーマをデプロイした SQL Azure インスタンス (ポータルのデータベースエクスプローラーを使用できます)
- SQL の接続文字列を `UploadSqlServerConnectionString` としてアプリ設定に設定
- Function App は SQL サーバーへのネットワークアクセスが必要です

これらが準備できたら、関数をデプロイできます：


```
func azure functionapp publish <function-name>
```


blob ストレージにいくつかのファイルをアップロードしてテストしてください。

## ラボ

SQL スキーマ作成をどのように自動化しますか？

> 詰まったら、[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Docker コンテナを停止：



```
docker compose -f ../docker-compose.yml down
```


ラボのリソースグループを削除：


```
az group delete -y --no-wait -n labs-functions-blob
```
