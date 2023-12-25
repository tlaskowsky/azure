# Cosmos DB - Mongo API

CosmosDBのネイティブドライバーは、コード内にカスタムクライアントライブラリを必要としますが、他のドライバーは標準APIを使用します。

[MongoDB](https://www.mongodb.com/) は人気のあるオープンソースのNoSQLデータベースで、Mongoドライバーを使用するCosmosDBインスタンスを作成できます。これは、既存のアプリをAzureに移行するのに最適です - コードを変更する必要はありません。アプリは引き続きMongoデータベースを見ますが、Cosmosのスケールと一貫した管理を得ることができます。

このラボでは、Mongoドライバーを使用してCosmosDBデータベースを作成し、簡単な.NETアプリケーションで使用します。

## 参照

- [CosmosDBのMongo概要](https://learn.microsoft.com/ja-jp/azure/cosmos-db/mongodb/introduction)

- [MongoDB C# ドライバー ドキュメント](https://mongodb.github.io/mongo-csharp-driver/2.17/getting_started/)

- [`az cosmosdb mongodb` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/cosmosdb/mongodb?view=azure-cli-latest)

## Cosmos DB Mongo データベースを作成する

ポータルで新しいCosmos DBリソースを作成し、Mongo APIを選択します。オプションを探索します：

- Mongo APIバージョンを選択できます
- SQLドライバーと同じ容量/プロビジョニングオプション
- 同じ地理冗長性、ネットワーキング、バックアップポリシー

使用するAPIに関係なく、Cosmos DBのストレージエンジンは同じです。サービスの主な機能はすべてのAPIで同じですが、アプリケーションが接続する方法についての選択です。

CLIでCosmos DBアカウントを作成します：



```
az group create -n labs-cosmos-mongo --tags courselabs=azure -l southeastasia

az cosmosdb create -g labs-cosmos-mongo --enable-public-network --kind MongoDB --server-version 4.2 -n <cosmos-db-name> 
```


> `Kind`はアカウントレベルで設定されます - DocumentDBまたはMongoDBです。アカウント内のすべてのデータベースは同じ種類を使用し、同じAPIを使用する必要があります。

これが完了するのにはSQL API用のCosmosアカウントを作成するよりも時間がかかるかもしれません。完了したら、ポータルでCosmosDBアカウントを開きます。ドキュメントDBアカウントとは異なるオプションがあります：

- PowerBI統合はありません
- _データ移行_、_接続文字列_、_コレクション_（_コンテナ_ の代わりに）など、新しい左ナビオプションがあります

📋 `cosmosdb mongodb database create` コマンドを使用して、アカウント内に `AssetsDb` というデータベースを作成します。

<details>
  <summary>わからない場合はこちら</summary>



```
az cosmosdb mongodb database create --help
```


最低限、名前、アカウント名、RGを設定する必要があります：



```
az cosmosdb mongodb database create --name AssetsDb -g labs-cosmos-mongo --account-name <cosmos-db-name>
```


</details><br/>

## Mongo Shellでデータベースに接続する

ポータルで _データエクスプローラー_ を開きます。新しいAssetsDbデータベースがリストされています。それを展開すると、コレクションはありません。これは新しい空のデータベースです。

新しいメニューオプション _Mongo Shellを開く_ があります。これは、データベースに接続するためのコマンドラインインターフェース ([mongosh](https://www.mongodb.com/docs/mongodb-shell/)) を開始します。そのオプションを選択し、端末で以下のコマンドを実行してデータベースを探索します：



```
show dbs

use AssetsDb

show collections

db.help()
```


NoSQL APIは、ポータルでデータを操作するためにSQLのカスタマイズされたバージョンを使用しますが、Mongoコマンドは任意のMongoデータベースで標準です。

データはMongoでドキュメントとしてコレクションに格納されます（概念的にはSQLデータベースのテーブルの行に似ています）。コレクションを作成します：



```
db.createCollection('Students')
```


> 応答はJSONです - Mongoのネイティブデータフォーマット

データを挿入します：



```
db.Students.help()

db.Students.insertOne({ "OrganizationId": "aed1895", "StudentId" : "aed1895", "FullName": "a b c", "CountryCode": 123 })
```


> JSON応答には、データベースが生成したオブジェクトIDが含まれます

一度に複数のドキュメントを挿入できます。そして、ドキュメントは同じスキーマを持つ必要はありません：



```
db.Students.insertMany([{ "OrganizationId": "org1", "StudentId" : "1023", "FirstName": "x", "LastName": "y", "Role": "z" },  {"OrganizationId": "org1", "StudentId" : "1040", "FirstName": "a", "LastName": "b", "Role": "c" }])
```


📋 コレクションの `find` メソッドを使用してデータを照会します。組織 `org1` の学生を見つけて、結果に名と姓のみを含めることはできますか？

<details>
  <summary>わからない場合はこちら</summary>

ヘルプテキストを表示します：



```
db.Students.find().help()
```


すべてそこにありますが、Azure CLIのヘルプテキストほど役に立ちません。

すべてのドキュメントを表示します：



```
db.Students.find().pretty()
```


プロパティでクエリします - クエリはJSONオブジェクトとして表現されます：


```
db.Students.find( {"OrganizationId" : "org1"} )
```


応答でプロパティを投影します - フィールドを含めるには1を使用し、除外するには0を使用します：


```
db.Students.find( {"OrganizationId" : "org1"}, { _id:0, FirstName:1, LastName:1 } )
```


</details><br/>

MongoDBの構文に慣れるのには少し時間がかかりますが、それは非常に一貫しています。シェルで使用できる同じ関数とフォーマットは、コード用のクライアントライブラリにもあります。

## Cosmos DBとMongoを使用してアプリを実行する

Mongoのドキュメントはコード内のオブジェクトに直接マッピングされるため、アプリの表現をデータベース表現に変換するためのORMレイヤーは必要ありません。Mongoクライアントライブラリは、データベースからJSONを効果的に取得し、オブジェクトに[逆シリアライズ](https://learn.microsoft.com/ja-jp/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-6-0)します。

オブジェクトクラスには、Mongoのためのいくつかの情報を含める必要があります：

- [EntityBase.cs](/src/asset-manager/Model/Spec/EntityBase.cs) - これはオブジェクトの基本クラスで、Mongoクライアントライブラリの注釈を使用して、どのプロパティがオブジェクトIDであるかを指定します
- [MongoAssetService.cs](/src/asset-manager/Services/MongoAssetService.cs) - コレクションからドキュメントを読み込み、参照データを挿入するデータアクセスを管理します
- [Dependencies.cs](/src/asset-manager/Dependencies.cs) - アプリが使用できる異なるストレージオプションを管理します。Mongoは標準クライアントライブラリを使用してサポートされており、CosmosDB固有のものはありません

📋 Cosmos DBアカウントのキー一覧から _Primary MongoDB Connection String_ を印刷します。

<details>
  <summary>わからない場合はこちら</summary>

キーリストはデータベースアカウントレベルにあります。すべてのAPIタイプに対して同じコマンドですが、キーの名前はSQL APIとは異なります：



```
az cosmosdb keys list --type connection-strings -g labs-cosmos-mongo  --query "connectionStrings[?description==``Primary MongoDB Connection String``].connectionString" -o tsv -n <cosmos-db-name>
```


</details><br/>

> 接続文字列は `mongodb://<cosmos-db-name>` で始まります - 認証の詳細を含んでいるので、安全なデータとして扱う必要があります

[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download)が必要ですが、ローカルでアプリを実行します。データベースタイプと接続文字列を設定するためのパラメータを使用します：



```
# 接続文字列を 'クォート' してください：
dotnet run --project src/asset-manager --Database:Api=Mongo --ConnectionStrings:AssetsDb='<cosmos-connection-string>'
```

http://localhost:5208 にアクセスしてアプリをブラウズします。実行すると、Mongoにいくつかのコレクションを作成し、ドキュメントを保存します。これは、[Cosmos lab]()で使用したのと同じアプリケーションですが、MongoではオブジェクトIDが異なる形式です（IDにパターンはありますか？）。

## ラボ

Mongoシェル（または [Mongo VS Code]() 拡張機能）を使用して、すべてのロケーションドキュメントを見つけます。データ構造はCosmos SQL APIとどう違いますか？新しいロケーションを挿入してみてください：


```
{
    "AddressLine1": "1 Parliament Place",
    "Country": "Singapore",
    "PostalCode": "178880"
}
```

データベースに保存して、アプリをリフレッシュしたときに表示されるようにできますか？

> 詰まったら [ヒント](hints_jp.md) を試すか、[解答](solution_jp.md) を確認してください。

___

## クリーンアップ

次のコマンドでリソースグループとデータベースを削除し、すべてのデータも削除します：


```
az group delete -y --no-wait -n labs-cosmos-mongo
```
