# Azureテーブルストレージ

テーブルストレージは、Azureストレージアカウント内でホストできるシンプルでスケーラブルなデータベースです。No-SQLアプローチを採用しているため、データの読み書きに専用のライブラリをコードで使用する必要があります。Azureのストレージスタックの古い部分ですが、Mongoなどの代替手段が登場する前からあり、初期のAzureソリューションで見られます。CosmosDBがより良いオプションですが、テーブルストレージとの互換性があるため、移行パスがあります。

このラボでは、シンプルなアプリでテーブルストレージを使用し、データがどのように保存されアクセスされるかを見ていきます。

## 参照

- [Azureテーブルストレージの概要](https://docs.microsoft.com/ja-jp/azure/storage/tables/table-storage-overview)

- [テーブルストレージ設計ガイドライン](https://docs.microsoft.com/ja-jp/azure/storage/tables/table-storage-design-guidelines)

- [ODataクエリチュートリアル](https://www.odata.org/getting-started/basic-tutorial/#queryData)

- [`az storage table` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/storage/table?view=azure-cli-latest)

## テーブルの作成

テーブルストレージはストレージアカウントの機能の一つですので、まずリソースグループとストレージアカウントを作成します：



```
az group create -n labs-storage-table --tags courselabs=azure -l southeastasia

az storage account create -g labs-storage-table --sku Standard_LRS -l southeastasia -n <sa-name> 
```


📋 `storage table create` コマンドを使用してSA内に`students`というテーブルを作成してください。

<details>
  <summary>わからない場合は？</summary>


```
az storage table create --help
```


必要なのはテーブル名とSA名だけです：



```
az storage table create -n students --account-name <sa-name>
```


</details><br/>

> 出力ではテーブルが作成されたとだけ表示されます。空のテーブルはコストがかかりません - データがテーブル内にある場合にのみ料金がかかります。

ポータルでストレージアカウントを開き、_テーブル_ビューを確認します。あまり見るべきものはありません。代わりに_ストレージブラウザ_を開いてテーブルを参照してください。

テーブルストレージはSQLデータベースとは異なる用語を使用します：

- _エンティティ_ はデータ項目で、SQLの行やMongoのオブジェクトに相当します
- _パーティションキー_ はエンティティの一意なIDの一部で、データが格納される場所を決定するためのグループです
- _行キー_ はエンティティのIDの一意な部分です

ストレージブラウザを使用していくつかのエンティティを追加してください：

|パーティションキー| 行キー | プロパティ|
|-|-|-|
|org1|1023|FirstName=x,LastName=y,Role=z|
|org1|1040|FirstName=a,LastName=b,Role=c|
|org2|aed1895|FullName=a b c,CountryCode=123|
|23124|stonemane||

注意点：

- エンティティは異なるプロパティを持つことができ、テーブルに固定スキーマはありません
- パーティションキーと行キーは異なる形式を持つことができ、文字列と整数を混在させることができます
- プロパティは必須ではなく、空のエンティティを持つことができます

## ODataを使用したテーブルストレージのクエリ

テーブルストレージはOData REST APIを提供しているため、curlを使用してデータをクエリできます。

アカウントのテーブルストレージドメイン名を出力してください：



```
az storage account show --query 'primaryEndpoints.table' -o tsv -n <sa-name>
```


そのURLの末尾にテーブル名と空のクエリ `()` を追加して、すべてのエンティティを取得することができます：



```
curl '<table-storage-url>/students()'
```


> _リソースが見つかりません_ というエラーが表示されます。ODataはサポートされていますが、公開アクセスは有効にされていません

📋 `students`テーブルを読み取るために使用できるSASトークンを生成してください。トークンは2時間有効です。

<details>
  <summary>わからない場合は？</summary>

ポータルでこれを行うことができますが、トークンはテーブルではなくストレージアカウントレベルで作成する必要があります。_共有アクセス署名_ ブレードを開き、テーブルストレージ用のフィールドを入力します。

またはCLIを使用して、特定のテーブルに対してのみトークンを取得し、適切な形式で有効期限日付を生成するためのスクリプトを使用することができます：


```
az storage table generate-sas --help 

# PowerShell:
$expiry=$(Get-Date -Date (Get-Date).AddHours(2) -UFormat +%Y-%m-%dT%H:%MZ)

# 又は zsh:
expiry=$(date -u -v+2H '+%Y-%m-%dT%H:%MZ')

# 又は 手動で上記がうまくいかない場合 :)
expiry='2022-12-31T23:59Z'

az storage table generate-sas -n students --permissions r --expiry $expiry -o tsv --account-name <sa-name>
```


</details><br/>

> CLIを使用すると、特定のパーティションキーと行キーの範囲に制限された詳細なトークンを生成できます

ODataクエリをもう一度試してみてくださいが、今回はSASトークンをURLに追加します。完全なURLは次のようになりますが、ドメイン名とトークンは異なります: `https://labsstoragetablees.table.core.windows.net/students()?se=2022-10-27T19%3A59Z&sp=r&sv=2019-02-02&tn=students&sig=DM6tZRoUcCzO0EVepJF6KF%2BJeMGktbD5vEjvbOqNUAw%3D`

**ODataのURLをダブルクォートで囲んでください！**



```
# すべての学生を返します：
curl "<table-url>/students()?<sas-token>"

# 特定のキーによって学生をフィルタリングします：
curl "<table-url>/students(PartitionKey='org1',RowKey='1040')?<sas-token>"
```


クエリはテーブル名の後の括弧内にあり、応答にはクエリに一致するすべてのエンティティとそのプロパティが含まれます。

XMLがデフォルトの応答形式ですが、HTTPヘッダーでJSONを要求できます：



```
# JSONレスポンスを要求：
curl -H 'Accept: application/json' "<table-url>/students(PartitionKey='org1',RowKey='1023')?<sas-token>"

# ODataメタデータなし：
curl -H 'Accept: application/json;odata=nometadata' "<table-url>/students(PartitionKey='org1',RowKey='1023')?<sas-token>"
```


> データには、行が挿入または更新されたときに自動的に設定される_Timestamp_フィールドがあることがわかります

ODataはあまり一般的に使用されていませんが、Table Storageの機能として存在し、独自のREST APIを構築することなくデータを共有するための良いオプションになることがあります。

より一般的には、コード内でデータを読み書きするためにクライアントライブラリを使用します。

## テーブルストレージをログシンクとして使用

[Serilog](https://serilog.net)というログライブラリを使用するシンプルな.NETアプリケーションを実行します。Serilogは、テーブルストレージを含むさまざまな種類のストレージにログデータを書き込むことができます。

アプリを実行する前に、まずテーブルを作成して認証の詳細を取得する必要があります。

📋 _FulfilmentLogs_という名前の新しいテーブルをストレージアカウントに作成し、アプリが認証するための接続文字列を出力してください。

<details>
  <summary>わからない場合は？</summary>

新しいテーブルを作成します：



```
az storage table create -n FulfilmentLogs --account-name <sa-name>
```


接続文字列を出力します：



```
az storage account show-connection-string -g labs-storage-table -n <sa-name>
```


</details><br/>

ポータルの_アクセスキー_の下で接続文字列も確認できます。完全なストレージアカウント接続文字列の形式は次の通りです：

- `DefaultEndpointsProtocol=https;AccountName=<sa-name>;AccountKey=<key1-or-key2>;EndpointSuffix=core.windows.net;BlobEndpoint=https://<sa-name>.blob.core.windows.net/;FileEndpoint=https://<sa-name>.file.core.windows.net/;QueueEndpoint=https://<sa-name>.queue.core.windows.net/;TableEndpoint=https://<sa-name>.table.core.windows.net/`

ローカルでアプリを実行し、クラウドにログが書き込まれるのを見てみましょう。

> アプリを実行するには[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download/dotnet/6.0)がインストールされている必要があります

アプリ設定ファイルを更新してください：

- [appsettings.json](/src/fulfilment-processor/appsettings.json) - ファイルを開いて`<sa-connection-string>`の値を実際の接続文字列に置き換えてください

アプリを実行します：


```
dotnet run --project src/fulfilment-processor
```

> 出力は見えませんが、すべてのログはテーブルストレージに書き込まれています

アプリを数分間実行させた後、`Ctrl-C` または `Cmd-C` で終了します。

ポータルを開いて、ストレージブラウザーの `FulfilmentLogs` テーブルを確認します - 多くのエントリが表示されます。PartitionKeyとRowKeyはどのように構築されていますか？

## ラボ

エラーイベントのみを見つけるために、fulfilmentログエントリをクエリできますか？これはブラウザまたはODataを使用して行うことができます。問題を解決するために何をする必要があると思いますか？

> 詰まったら[ヒント](hints_jp.md)を試してください、または[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

不要になったリソースグループを削除してクリーンアップします：



```
az group delete -y -n labs-storage-table --no-wait
```
