# Cosmos DB

Cosmos DBは、_惑星規模のデータベース_ として請求されています。ローカライズされたインスタンスから始めて、グローバルレプリケーションを拡張し、投げかけることができるほぼあらゆる負荷を処理する能力を持っています。Cosmos DBは単一の製品ですが、NoSQL、Mongo、テーブルストレージなど、異なるストレージドライバーをサポートするデータベースがあります。各データベースは単一のドライバーを使用するように固定されていますが、異なるストレージアプローチを異なるアプリケーションに使用し、それらすべてを一貫して管理することができます。

このラボでは、CosmosDBアカウントを作成し、NoSQLドライバーを使用したデータベースを使用します。

---
**NoSQLはCosmosDBのネイティブドライバーです**

_ただし、以前はSQLドライバーと呼ばれていました_ 😃

ポータルでは"NoSQL"として参照されますが、CLIおよびドキュメントでは依然として"SQL"ドライバーと呼ばれています。

---

## 参考資料

- [Cosmos DB概要](https://docs.microsoft.com/ja-jp/azure/cosmos-db/introduction)

- [Microsoft Learn: Cosmos DBを探る](https://docs.microsoft.com/ja-jp/learn/modules/explore-azure-cosmos-db/)

- [`az cosmosdb` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/cosmosdb?view=azure-cli-latest)

- [`az cosmosdb sql` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/cosmosdb/sql?view=azure-cli-latest)

## Cosmos DBデータベースを作成する

ポータルを開いて、新しいリソースページに移動します。"cosmos"を検索し、_Azure Cosmos DB_ を選択します。APIを選択できます - NoSQLを選択してください:

- キャパシティモード - プロビジョニングまたはサーバーレス
- プロビジョニングはフリーティアと価格キャップを許可します
- ジオ冗長性、リージョン間のデータ同期、およびオプションのマルチリージョンライト
- 自動データバックアップとストレージのためのバックアップポリシー

> CosmosDBはエンタープライズグレードのデータベースです。価格モデルを理解することが重要です

CLIを使用してCosmos DBアカウントを作成します：



```
az group create -n labs-cosmos --tags courselabs=azure -l southeastasia

az cosmosdb create --help

az cosmosdb create -g labs-cosmos --enable-public-network --kind GlobalDocumentDB -n <cosmos-db-name>
```


ポータルで新しいリソースを開きます - これは単なるCosmosDB _アカウント_であり、データベースのグルーピングメカニズムです。リソースページには_クイックスタート_ウィザードがあります。

SQL APIを使用してデータベースを作成します：



```
az cosmosdb sql database create --help

az cosmosdb sql database create --name AssetsDb -g labs-cosmos --account-name <cosmos-db-name>
```


> ポータルで確認してください - Cosmos DBアカウントの下のデータベースは、アプリサービスプラン内のアプリのように別々のリソースとして表示されません

アカウントの_データエクスプローラー_を開くと、新しいデータベースが表示されます。今は空ですが、使用する準備ができています。クライアントアプリケーション用の接続文字列を確認するために_キー_を開きます。プライマリ接続文字列をメモしてください。

CLIから接続文字列も取得できます：



```
az cosmosdb keys list --type connection-strings -g labs-cosmos -n <cosmos-db-name>
```


📋 CLIコマンドにクエリを追加し、出力形式を変更して、_プライマリSQL接続文字列_の値だけが表示されるようにします。

<details>
  <summary>わからない場合は？</summary>

このクエリには、配列を含むconnectStringsフィールドを選択し、配列内でdescriptionフィールドが入力と一致するオブジェクトを検索し、そのオブジェクトからconnectionStringフィールドを選択し、JSONマーカーなしで印刷するためにTSV形式を使用する必要があります：



```
az cosmosdb keys list --type connection-strings -g labs-cosmos  --query "connectionStrings[?description==``Primary SQL Connection String``].connectionString" -o tsv -n <cosmos-db-name>
```


</details><br/>

> これは、アプリ設定に注入する必要がある値を取得するためにスクリプトを記述する際に行うタスクの種類です

## Cosmos DBを使用したアプリの実行

CosmosDBが非常によくスケールするのは、データを複数のストレージロケーションに分散させる方法によるものです。これらのロケーションはすべて同時に読み書きが可能であり、CosmosDBはパーティションを追加することで容量を増やすことができます。

そのプロセスはすべて自動的に管理されますが、これはCosmosDB NoSQLドライバーのデータが標準的なSQLデータベースのデータとは異なる形式を持っていることを意味します。

私たちはCosmosDBをストレージに使用できるシンプルな.NETアプリケーションを持っています。それはCosmosDB NoSQLライブラリで構築されていますが、データモデルはCosmos特有のものではありません：

- [Asset.cs](/src/asset-manager/Model/Asset.cs) - これはPOCO（Plain-Old C# Object）で、データフィールドと関係性があります
- [AssetContext.cs](/src/asset-manager/Sql/AssetContext.cs) - EFコンテキストオブジェクトで、エンティティオブジェクトへのアクセスを提供します
- [Dependencies.cs](/src/asset-manager/Dependencies.cs) - アプリが使用できる異なるストレージオプションを管理します。SqlモードではCosmosDBを使用します

次のパラメータを使用して、ローカルでアプリを実行します（[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download)が必要です）:



```
# 接続文字列を引用してください：
dotnet run --project src/asset-manager --Database:Api=Sql --ConnectionStrings:AssetsDb='<cosmos-connection-string>'
```


http://localhost:5208 にアクセスすると、ランダムなIDを持つ一連の参照データアイテムが表示されます：

![Asset Manager with CosmosDB](/img/asset-manager-cosmos.png)

> アプリケーションはORMを使用してデータベーススキーマを設定し、その後この参照データを挿入します。

ターミナルのログを確認すると、多くのSQLステートメントがあります。これらはORMが生成するクエリです。

## データの探索

Cosmos Dataブラウザでは、_コンテナー_を見ることができます - コンテナーはテーブルのようなものですが、コンテナ内のアイテムは同じスキーマを持つ必要はありません。

このアプリでは、すべてのオブジェクトタイプに1つのコンテナが使用されます。アイテムをチェックすると、_Location_ オブジェクトと _AssetType_ オブジェクトが表示されます。

Data Explorerを使用して新しいロケーションを追加します：



```
{
    "Id": "64eb3e9f-e92d-4a63-b234-08da7b01d0d6",
    "AddressLine1": "Parliament House",
    "Country": "Australia",
    "Discriminator": "Location",
    "PostalCode": "2600",
    "id": "Location|64eb3e9f-e92d-4a63-b234-08da7b01d0d6"
}
```


- `Discriminator`はオブジェクトタイプを識別するためのORMメカニズムです
- `Id`はオブジェクトプロパティであり、`id`はアイテム識別子です（識別子にオブジェクトタイプが含まれます）

アセットマネージャーWebサイトをリフレッシュします。ロードに時間がかかる場合がありますが、新しいロケーションが表示されるでしょうか？

ID列なしで新しいアイテムを挿入するとどうなりますか？



```
{
    "AddressLine1": "1 Parliament Place",
    "Country": "Singapore",
    "Discriminator": "Location",
    "PostalCode": "178880"
}
```

📋 ウェブサイトを再読み込みするとエラーが表示されます。データを修正して、ウェブサイトが再び読み込まれ、新しいロケーションが表示されるようにしてください。

<details>
  <summary>わからない場合はこちら</summary>

CosmosDBは、新しいアイテムに対して `id` 列を指定しない場合、自動的に生成しますが、アプリが期待する規約を知りません。

アプリは `Id` フィールドに一意の識別子を求め、`id` フィールドはオブジェクトタイプでプレフィックスする必要があります。

Cosmosはプロパティの変更を許可しています - データエクスプローラーでアイテムを選択してください：

- `id` フィールドを `Id` という新しいフィールドにコピーします。
- 実際の識別子の前に `Location|` を挿入して `id` フィールドを編集します。

変更を保存し、ウェブサイトをリフレッシュすると、4つのロケーションが表示されるはずです。

</details>

アプリのテストが終わったら、`Ctrl-c` または `Cmd-c` を実行して終了します。

## ラボ

SQLかNoSQLか？NoSQLドライバは実際にはSQLクエリをサポートしていますが（完全なSQL構文はサポートしていません）。データエクスプローラーでいくつかのクエリを実行してみてください：

- IDと説明だけを表示するすべてのアセットタイプ
- 郵便番号に '1' が含まれるロケーションの数

> 詰まったら [ヒント](hints_jp.md) を試すか、[解答](solution_jp.md) を確認してください。

___

## クリーンアップ

RGとデータベースを削除すれば、すべてのデータも削除されます：



```
az group delete -y --no-wait -n labs-cosmos
```
