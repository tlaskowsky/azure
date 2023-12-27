# プロジェクト2: 分散アプリケーション

プロジェクトは、Azureを使って自分自身のソリューションを設計しデプロイするために、専念する時間です。

あなたはこれまでに学んだすべての重要なスキルを使います、そして:

- 😣 行き詰まることがあります
- 💥 エラーや壊れたアプリが出ることがあります
- 📑 調査とトラブルシューティングが必要になります

**それがプロジェクトがとても有用な理由です！**

これらは、どのエリアで快適で、どのエリアでより多くの時間を費やす必要があるかを理解するのに役立ちます。

この2番目のプロジェクトは、_分散アプリケーション_です。それは、完全な機能を提供するために連携して動作する複数のコンポーネントを持つアーキテクチャです。このアプリは、クラウドをより有効に活用するためのより進んだアーキテクチャを使用して[プロジェクト1](/projects/lift-and-shift/README.md)の進化版です。

## アプリケーションアーキテクチャ

アプリケーションのUIは同じです:

![プロジェクト2アプリ](/img/project-1-app.png)

このバージョンのアプリには、複数のコンポーネントがあります:

![プロジェクト2アーキテクチャ](/img/project-2-arch.png)

- ユーザー向けWebアプリケーション (.NET 6)
- トランザクションデータベース (SQL Server)
- メッセージキュー (Azure Service Bus)
- メッセージハンドラ (.NET 6)
- 集中ログ用のドキュメントデータベース (Azure Table Storage)

## 🥅 目標

目標は、**セキュアなデプロイメントで** Azureでアプリを実行することです。

私たちが持っているのはソースコードといくつかの設定に関する文書だけですが、それで十分です。しかし、プロジェクト1を完了した場合は、それを出発点として使用することができます。

アプリが分散していると、ハッカーの攻撃対象が大きくなります。アプリが実行されている次の段階は、それをロックダウンすることです:

- 機密設定データ用のセキュアなストレージコンポーネントを使用する
- インフラストラクチャ（ストレージとキュー）が私たち自身のアプリコンポーネントの外で使用されないようにアクセスを制限する

セキュアなアプリケーションは、完全に自動化されたデプロイメントを持っているべきです。

**単一のコマンドを実行することによって何もないところからアプリケーションを起動できるはずです。**

## 覚えておくこと...

_探求する | デプロイする | 自動化する_

ここには多くの動く部分があります。セキュリティ設定を追加する前に、Azureでアプリを完全に動作させることが賢明でしょう。

## 開発環境

まず、新しいアーキテクチャに慣れるために、ローカルでアプリケーションを実行することも良い考えです。それは作成する必要があるAzureコンポーネントに依存しています:

- SQL Serverデータベース
- `events.todo.newitem`という名前のService Busキュー
- Table Storageサービス

また、マシンには[.NET 6 SDK](https://dotnet.microsoft.com/en-us/download)がインストールされている必要があります。

それらのサービスの接続文字列を取得し、次にバックエンドメッセージハンドラを実行します:



```
cd projects/distributed/src/save-handler

dotnet run `
 --ConnectionStrings:ToDoDb='<sqlserver-connection-string>' `
 --ConnectionStrings:ServiceBus='<servicebus-connection-string>' `
 --Serilog:WriteTo:0:Args:connectionString='<tablestorage-connection-string>'
```


> これはメッセージハンドラを実行します。それはキューでメッセージを待っているリスナです。次に、ウェブサイトを実行する必要があります。

メッセージハンドラの端末には、いくつかの設定ログエントリーが表示されるだけです。その後、Table Storageをチェックして、ハンドラがキューに登録されたことを示すログがあるかどうかを確認してください。

次に、ウェブサイトを実行するために新しい端末を開きます:



```
cd projects/distributed/src/web

dotnet run `
 --ConnectionStrings:ToDoDb='<sqlserver-connection-string>' `
 --ConnectionStrings:ServiceBus='<servicebus-connection-string>' `
 --Serilog:WriteTo:0:Args:connectionString='<tablestorage-connection-string>'
```

アプリを確認するには、http://localhost:5000 にアクセスしてください。アプリが表示されたら、新しいアイテムを追加してください。

---
🤔 **アイテムはリストに表示されません** 😟

アイテムはバックグラウンドで追加され、ハンドラがメッセージを受信し新しいデータを挿入する前にウェブページがリストを読み込みます。リストをリフレッシュすると、新しいアイテムが表示されるはずです。

---

再度、テーブル ストレージを確認し、より多くのログが表示されるはずです。両コンポーネントは中央のストアにログを記録しており、これによりデバッグが容易になります。

## 設定

このアプリケーションは標準の .NET 設定モデルを使用しています。デフォルトの設定は、[web/appsettings.json](/projects/distributed/src/web/appsettings.json) および [save-handler/appsettings.json](/projects/distributed/src/web/appsettings.json) にあります。**これらのファイルを変更しないでください**。これらは開発環境で実行するための正しい設定を持っており、ソースコードリポジトリで使用したいものです。

Azure にデプロイする場合、ローカルで使用したと同じ設定項目を設定する必要があります。ウェブサイトとメッセージ ハンドラの両方は KeyVault から接続文字列を読み取ることができますが、次の 2 つのアプリケーション設定を有効にする必要があります。

- `KeyVault__Enabled` を `True` に設定
- `KeyVault__Name` を KeyVault の名前に設定

接続文字列キーの形式は、それらをどこに保存するかによって異なります:

|| アプリケーション設定名 | KeyVault シークレット名 | 
|-|-|-|
|SQL Server | `ConnectionStrings__ToDoDb` | `ConnectionStrings--ToDoDb`|
|Service Bus | `ConnectionStrings__ServiceBus` | `ConnectionStrings--ServiceBus`|
|Table Storage | `Serilog__WriteTo__0__Args__connectionString` | `Serilog--WriteTo--0--Args--connectionString`|

## ソースコード

このアプリのコードはこのリポジトリにあります:

- `projects/distributed/src/web` - ウェブサイト 
- `projects/distributed/src/save-handler` - バックグラウンド メッセージ ハンドラ

**メッセージ ハンドラはデプロイ パッケージにコンパイルされています:**

- `projects/distributed/src/save-handler/deploy.zip` - これは、Azure の Windows App Service でウェブジョブとして実行する予定の場合の正しいフォーマットです

## ストレッチ

このアーキテクチャは信頼性とスケーラビリティに関するものです。メッセージ ハンドラの複数のインスタンスを実行する場合はどうなるでしょうか？メッセージ ハンドラが実行されていない場合、アイテムはいつ挿入されるのでしょうか？

新しいアイテムを追加する際の遅延があるのは残念です。これは「最終的な整合性 (eventual consistency)」と呼ばれ、分散アーキテクチャの欠点の1つです。これは Azure では解決できず、コードの変更が必要です。
