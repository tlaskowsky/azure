# プロジェクト 4: サーバーレスアプリ

プロジェクトは、Azure を活用し、独自の解決策を設計および展開する機会です。

これまで学んだ主要なスキルをすべて活用し、以下のことを行います。

- 😣 途中で行き詰まることがあります
- 💥 エラーや動作しないアプリが発生することがあります
- 📑 調査とトラブルシューティングが必要になります

**それがプロジェクトが非常に有用である理由です！**

これにより、どの分野で快適に作業できるか、どこでさらに時間を費やす必要があるかを理解するのに役立ちます。

この第4プロジェクトは「サーバーレスアプリケーション」です。ソリューションは完全にサーバーレスではありませんが、サーバーレス関数を使用して機能を追加し、機能を向上させています。このアプリケーションは、[プロジェクト 2](/projects/distributed/README.md) の進化バージョンで、フロントエンド、データベース、メッセージングに対して同じアーキテクチャを使用していますが、バックエンドは関数に置き換えられています。

## アプリケーションアーキテクチャ

アプリケーションのユーザーインターフェース（UI）は同じです：

![プロジェクト 4 アプリ](/img/project-1-app.png)

このバージョンのアプリケーションは、[プロジェクト 2](/projects/distributed/README_jp.md) と同様の分散アーキテクチャを使用していますが、メッセージハンドラはサーバーレス関数に置き換えられ、REST API および通知ハブとして機能する追加の関数があります：

![プロジェクト 4 アーキテクチャ](/img/project-4-arch.png)

- ユーザー向けのウェブアプリケーション (.NET 6)
- トランザクションデータベース (SQL Server)
- メッセージキュー (Azure Service Bus)
- 通知をブロードキャストするための SignalR サービス (Azure SignalR Service)
- 新しい To-Do アイテムを作成するためのサーバーレス関数 (.NET 6)
- データを SQL Server に保存するためのサーバーレス関数 (.NET 6)
- 新しいアイテムの通知をブロードキャストするためのサーバーレス関数 (.NET 6)

完全なアプリケーションをローカルで実行できません。キーの依存関係である Service Bus および SignalR をローカルで実行できないためです。しかし、ウェブサイトと関数をローカルで実行し、ほとんどの機能を確認できます。

## 🥅 ゴール

このアプリを Azure にデプロイすることです。

ソースコードと設定設定に関するドキュメンテーションがあるだけで、それで十分です。プロジェクト 2 を完了した場合、それを起点にしてください。

アプリが Azure で実行され、テストされたら、さらに 2 つのステージがあります。

1. 新しい REST API を API マネージメントツールを介して公開し、開発者が自分自身でオンボードできるようにします。
2. ウェブアプリケーションとサーバーレス関数の集中型モニタリングを追加し、ログとメトリックを表示できる単一の場所を提供します。

アプリケーションは完全に自動化されたデプロイメントを持つべきです。

**ここにはたくさんのことがありますが、実際のプロジェクトのように扱って、常に何かを示せる状態に近くなるようにしましょう。**

## 覚えておくこと...

_探索 | デプロイ | 自動化_

ここには多くの要素が含まれています。アプリを完全に Azure で実行してから、デプロイメントをスクリプト化し、次のステージに進む前に進めることが合理的です。

## 開発環境

新しいアーキテクチャに慣れ親しむために、まずアプリケーションをローカルで実行することが良いアイデアです。Azure のコンポーネントに依存しているため、事前に作成する必要があります。

- SQL Server データベース
- 2 つの Service Bus キュー
    - `events.todo.newitem`
    - `events.todo.itemsaved`
- SignalR サービス インスタンス

また、Storage Account エミュレーターのためのローカルコンテナも必要です。



```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


関数を構成するには、`functions/TodoList.Functions` フォルダ内に `local.settings.json` ファイルを作成し、以下の設定と接続文字列を設定します。


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "ServiceBusConnectionString" : "<sb-connection-string>",
        "SqlServerConnectionString" : "<sql-connection-string>",
        "SignalRConnectionString" : "<sr-connection-string>"
    },
    "Host": {
      "CORS": "http://localhost:5000/",
      "CORSCredentials": true
    }
}
```


次に、`functions/TodoList.Functions` フォルダから関数を実行します。


```
func start
```


ウェブサイトを構成するには、`src/web` フォルダ内の `appsettings.json` ファイルを編集し、接続文字列を設定します。


```
"ConnectionStrings": {    
    "ToDoDb": "<sql-connection-string>",
    "ServiceBus": "<sb-connection-string>",
    "Functions": "http://localhost:7071/api"
  }
```


別のターミナルで、`src/web` フォルダからウェブサイトを実行します。


```
dotnet run
```


http://localhost:5000 に移動します。To-Do ウェブサイトが表示されると、新しいアイテムを追加します。

---
🤔 **リストに新しいアイテムが表示されないことがあります** 😟

関数エミュレーターは SignalR ネゴシエーションを完全にサポートしていないため、ブラウザの開発者ツールで CORS の問題が表示されます。しかし、リフレッシュしてデータが表示される場合、関数は Service Bus を介して正しく動作し、データを SQL Server に保存しています。

---

新しい REST API を使用して、ToDo リストアイテムを追加できます。



```
# Windows では curl.exe を使用します
curl -XPOST http://localhost:7071/api/items --header 'Content-Type: text/plain' --data-raw 'a new item'
```


ウェブサイトをリフレッシュすると、新しいアイテムが表示されます。

> Azure にデプロイする場合、CORS でウェブサイトの URL を許可し、リクエスト資格情報を有効にすると、関数を介した完全な SignalR フローがサポートされます。

## 設定

Azure にデプロイする場合、ローカルで使用した設定項目を同じように設定する必要があります。ウェブアプリケーションと関数の両方に適用されます。設定キーの形式は、各コンポーネントごとに異なります。

|| Web App の設定名 | Functions アプリの設定名 | 
|-|-|-|
|SQL Server | `ConnectionStrings__ToDoDb` | `SqlServerConnectionString`|
|Service Bus | `ConnectionStrings__ServiceBus` | `ServiceBusConnectionString`|
|SignalR サービス| なし | `SignalRConnectionString`|
|Functions| `ConnectionStrings__Functions` | なし |

## ソースコード

このアプリのコードは、このリポジトリにあります。

- `projects/serverless/src` - ウェブサイトとサポートプロジェクト
- `projects/serverless/functions` - サーバーレス関数

## ストレッチ

アプリをデプロイし、API Management を設定し、集中型モニタリングも完了し、余裕がある場合、セキュリティについて考える必要があります。

- インフラストラクチャコンポーネント（データベースとメッセージキュー）は、ウェブアプリと関数からのみアクセスできるようにする必要があります。

- API 関数は、API Management からのみアクセスできるようにする必要があります。

- ウェブアプリは KeyVault から設定設定を読み取る必要があります（アプリはこれを行うことができます - [プロジェクト 2](/projects/distributed/README_jp.md) と同じ方法で設定できます）。
