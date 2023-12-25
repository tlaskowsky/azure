# Cosmos DB - Table API

Cosmos DB Table APIはAzure Table Storageの直接的な置き換えです。それは、Table Storageで構築された古いアプリケーションのための簡単な移行パスを提供します。これらのアプリを変更せずにCosmos Table APIを使用し、Cosmosのスケールと機能を備えた現代的なストレージオプションに移行できます。

このラボでは、Table APIを探索し、Table Storageに書き込むアプリケーションを実行して、設定の変更だけでCosmosDBに切り替えます。

## 参照

- [Cosmos 容量計算機](https://cosmos.azure.com/capacitycalculator/)

- [Table StorageをCosmosに移行する](https://learn.microsoft.com/en-us/azure/cosmos-db/table/import) - 第三者ツール

- [Cosmos Table APIのクエリ](https://learn.microsoft.com/en-us/azure/cosmos-db/table/tutorial-query)

- [`az cosmosdb table` コマンド](https://docs.microsoft.com/en-us/cli/azure/cosmosdb/table?view=azure-cli-latest)


## Cosmos DB Table データベースを作成する

ポータルで新しいリソースを作成 - "cosmos"を検索し、新しいCosmosDBを作成し、Table APIを選択します：

- オプションはSQL APIと同じです
- どちらの下層データベースエンジンもドキュメントDBです

CLIでCosmos DBアカウントを作成します：



```
az group create -n labs-cosmos-table  --tags courselabs=azure -l southeastasia

az cosmosdb create --help

az cosmosdb create -g labs-cosmos-table --enable-public-network --kind GlobalDocumentDB --capabilities EnableTable -n <cosmos-db-name>
```


> ここで使っている_GlobalDocumentDB_種類はNoSQL APIに使うものと同じですが、追加のcapabilityフラグでTable APIを有効にします

ポータルで新しいデータベースを開きます。他のCosmosのフレーバーといくつか異なるオプションがあります：

- _Collections_ や _Containers_ がなく、Table APIには構造の1レベルだけがあります：アカウント -> テーブル
- データ変更時にトリガーされるAzure Functionを追加できる_Integrations_セクションがあります

📋 `FulfilmentLogs`という名前のデータベースをTable APIを使用して作成します

<details>
  <summary>わからない場合はこちら</summary>

`cosmosdb table create` コマンドが必要です：


```
az cosmosdb table create --help

az cosmosdb table create --name FulfilmentLogs -g labs-cosmos-table --account-name <cosmos-db-name>
```


</details><br/>

ポータルの_Data Explorer_でCosmosDBを開きます。新しいテーブルが見えますが、展開してもエンティティはありません。

## Cosmos Table APIをログシンクとして使用する

[Table Storage lab](/labs/storage-table/README.md)で使用したアプリは、Table Storageにログエントリを書き込みました。最初はTable Storageを使用してAzureにそのアプリをデプロイし、設定の変更だけでCosmosに切り替えます。

アプリのログ設定はここで設定されます：

- [appsettings.json](/src/fulfilment-processor/appsettings.json) - ストレージアカウント接続文字列はプレースホルダーです；実際の値はAzureでアプリケーション設定として設定します

まずは「レガシー」データストアとなるストレージアカウントとテーブルを作成します：



```
az storage account create -g labs-cosmos-table --sku Standard_LRS -l southeastasia -n <sa-name>

az storage table create -n FulfilmentLogs --account-name <sa-name>
```


これで、テーブルにログを書き込むアプリをデプロイできます。

📋 lab RGにBasic SKUの新しいApp Serviceプランと.NET 6 Webアプリを作成します。

<details>
  <summary>わからない場合はこちら</summary>


```
az appservice plan create -g labs-cosmos-table -n app-plan-01 --sku B1 --number-of-workers 1

az webapp create -g labs-cosmos-table --plan app-plan-01 --runtime dotnet:6 -n <web-app-name>
```


</details><br/>

WebアプリはHTTPアプリケーション向けですが、同じホスティング環境でバックグラウンドプロセスも実行できます。ポータルでWebアプリを開いて_WebJobs_ページを確認します - 今はありませんが、デモアプリをバックグラウンドで実行するためのWebジョブをアップロードできます。

まず、ホスティング環境がウェブサイトを実行していないと判断してシャットダウンしないようにAlways Onフラグを設定します：



```
az webapp config set --always-on true   -g labs-cosmos-table -n <web-app-name>
```


次に、ストレージテーブルの接続文字列を取得し、Webアプリの設定項目として設定します：



```
az storage account show-connection-string -g labs-cosmos-table --query connectionString -o tsv -n <sa-name>

az webapp config appsettings set --settings Serilog__WriteTo__0__Args__connectionString='<sa-connection-string>' -g labs-cosmos-table -n <web-app-name> 
```


次にアプリをデプロイします - Webジョブのデプロイメントは通常のApp Serviceのデプロイメントオプションには当てはまらず、コンパイル済みアプリケーションを含むZIPファイルをアップロードする必要があります：


```
az webapp deployment source config-zip -g labs-cosmos-table --src src/fulfilment-processor/deploy.zip -n <web-app-name>
```


ポータルを開くと、デプロイメントが完了するとアプリサービスアプリの下に_Running_状態の_WebJob_が表示されます。

バックグラウンドワーカーがStorage Tableにログを書き込んでいるので、ストレージブラウザーで確認するとたくさんのデータが入っているのが見られます。

## Cosmos Table APIにログシンクを切り替える

アプリは引き続き実行され、ログを生成しています。テーブルのためのCosmosDBがドロップイン代替であることを証明するために必要なのは、アプリがCosmosに書き込み始めるように接続文字列を変更することだけです。

📋 Cosmos DBのキーリストから _Primary Table Connection String_ を出力します。

<details>
  <summary>わからない場合はこちら</summary>

データベースアカウントレベルのキーリストです。すべてのAPIタイプに対して同じコマンドですが、SQLとTableの接続文字列が両方とも表示されます：



```
az cosmosdb keys list --type connection-strings -g labs-cosmos-table  --query "connectionStrings[?description==``Primary Table Connection String``].connectionString" -o tsv -n <cosmos-db-name>
```


</details><br/>

> テーブル接続文字列はストレージアカウント接続文字列と同じ形式です - クライアントは接続データを読んで接続するための変更を必要としません

アプリの設定を変更します：



```
az webapp config appsettings set --settings Serilog__WriteTo__0__Args__connectionString='<cosmos-connection-string>' -g labs-cosmos-table -n <web-app-name>
```


設定を変更するとWebジョブが再起動され、ポータルでリフレッシュすると、状態が _Stopped_ から _Running_ に変わるのを見るかもしれません。

Webジョブが起動すると、データはCosmosに書き込まれます。

## ラボ

Data Explorerを使用してCosmosDBをクエリし、最後の1時間のエラーログだけを見つけます。このクエリアプローチは元のTable Storage、またはSQL APIを使用したCosmosと比較してどうですか？

> 詰まったら [ヒント](hints.md) を試すか、[解答](solution.md) を確認してください。

___

## クリーンアップ

リソースグループを削除してクリーンアップします：



```
az group delete -y -n labs-cosmos-table --no-wait
```
