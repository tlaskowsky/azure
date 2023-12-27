# Service Bus パブリッシュ-サブスクライブ

非同期メッセージングにおける主要なパターンの一つは、_パブリッシュ-サブスクライブ_（pub-sub）です。メッセージを送信するコンポーネントはパブリッシャーと呼ばれ、ゼロ個以上のコンポーネントがメッセージをサブスクライブ（購読）し、それら全員にコピーが配信されます。これは拡張可能なアーキテクチャに適しており、既存のコンポーネントを変更することなく新しいサブスクライバーを追加できます。

このラボでは、Service Bus _トピック_ を使用してpub-subメッセージングを行い、トピックにサブスクライバーを追加すると何が起こるかを見ていきます。

## 参考文献

- [パブリッシュ-サブスクライブ パターン](https://learn.microsoft.com/ja-jp/azure/architecture/patterns/publisher-subscriber)

- [Service Bus キュー、トピック、およびサブスクリプション](https://learn.microsoft.com/ja-jp/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)

- [`az servicebus topic` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/servicebus/topic?view=azure-cli-latest)

- [`az servicebus topic subscription` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/servicebus/topic/subscription?view=azure-cli-latest)

## Service Bus ネームスペースとトピックの作成

まず、Service Bus ネームスペース（これは [Service Bus ラボ]() でカバーしました）から始めますが、トピック機能を使用するためには少なくとも Standard ティアが必要です：


```
az group create -n labs-servicebus-pubsub  --tags courselabs=azure -l southeastasia

# TLS 1.2 と Standard ティアで作成 - トピックにはこれが必要です
az servicebus namespace create -g labs-servicebus-pubsub --sku Standard --min-tls 1.2 -l southeastasia -n <sb-name> 
```


ポータルで名前空間を開きます。名前空間は、複数のキューとトピックのコンテナです。トピックを作成するには、いくつかの興味深いオプションがあります：

- _生存期間_ (TTL) - サブスクライバーがピックアップするのを待っている間、メッセージがどれだけの時間利用可能であるかを定義します。
- 最大トピックサイズ - トピックはメッセージを保存し、サブスクライバーに転送するので、最大ストレージ量を設定できます。

📋 `servicebus topic` コマンドを使用して、TTL を 10 分間、最大サイズを 2GB と指定して `broadcast` という名前のトピックを作成します。

<details>
  <summary>わからない場合は？</summary>

ヘルプテキストを確認してください：



```
az servicebus topic create --help
```


TTL を設定するには、日、時、分、秒の数を設定する [期間形式](https://en.wikipedia.org/wiki/ISO_8601#Durations) を使用できます：


```
az servicebus topic create --max-size 2048 --default-message-time-to-live P0DT0H10M1S -n broadcast -g labs-servicebus-pubsub  --namespace-name <sb-name> 
```


</details><br/>

キューを作成して比較します - キューにも TTL と最大サイズを設定できます：



```
az servicebus queue create --max-size 1024 --default-message-time-to-live P0DT0H1M0S -n command -g labs-servicebus-pubsub  --namespace-name <sb-name> 
```


ポータルで二つを比較します - どちらもパブリッシャーがメッセージを送信できる先です。トピックとキューの違いは何ですか？

> トピックには _サブスクリプション_ があります。キューのようにトピックをリッスンすることはできません。消費者はリッスンするためにサブスクリプションを持つ必要があります。

## サブスクリプションの作成

サブスクリプションはルーティング用のチャネルのようなものです。パブリッシャーはメッセージをトピック全体に送信し、すべてのサブスクリプションがメッセージのコピーを受け取ります。通常、異なる機能用に複数のサブスクリプションを持ちます。

例えば、店舗アプリケーションでは、`order-created` メッセージをトピックに公開するコンポーネントがあり、異なる機能用に複数のサブスクリプションが使用されるかもしれません：

- 配送要求を処理する履行コンポーネント
- データを要約する分析コンポーネント
- 注文の詳細を追跡する監査コンポーネント

📋 トピックに対して `web` と `desktop` という名前の2つのサブスクリプションを作成します。

<details>
  <summary>わからない場合は？</summary>

サブスクリプションにはトピックグループの下に独自のコマンドがあります：



```
az servicebus topic subscription create --help
```


サブスクリプションは特定のトピック用に作成する必要があり、それから名前だけが必要です：


```
az servicebus topic subscription create --name web --topic-name broadcast -g labs-servicebus-pubsub  --namespace-name <sb-name>  

az servicebus topic subscription create --name desktop --topic-name broadcast -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


</details><br/>

サブスクリプションの詳細を印刷すると、何件のメッセージがあるかが含まれます：



```
az servicebus topic subscription show --name desktop --topic-name broadcast -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


メッセージ数だけを問い合わせることもできます - _これは一つのトピックで読むことができるメッセージの数です：_


```
az servicebus topic subscription show --name web --topic-name broadcast --query messageCount -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


今のところメッセージはありません。

## トピックへのメッセージの公開

私たちはトピックにメッセージを公開する簡単な .NET 6 アプリケーションを持っています：

- [publisher/Program.cs](/src/servicebus/publisher/Program.cs) - キューまたはトピックに送信するためのコードはまったく同じで、送信者の視点からはどちらであっても問題ありません。

アプリケーションがトピックに公開するためには、アクセスポリシーが必要です。すべての名前空間には、すべての権限を持つルートポリシーがありますが、必要以上の権限を使用しないように注意する必要があります。

このトピックにメッセージを送信するためだけの権限を持つ送信者ロール用の新しい認可ルールを作成します：



```
az servicebus topic authorization-rule create --help

az servicebus topic authorization-rule create --topic-name broadcast --name publisher --rights Send -g labs-servicebus-pubsub --namespace-name <sb-name>  
```


これでそのロール用の接続文字列を取得し、発行者アプリで使用できます（アプリをローカルで実行するには [.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download) が必要です）：


```
# 送信者ロール用の接続文字列を取得します：
az servicebus topic authorization-rule keys list --topic-name broadcast --name publisher --query primaryConnectionString -o tsv -g labs-servicebus-pubsub  --namespace-name <sb-name>  

# アプリを実行します - 接続文字列を '引用符' で囲んでください：
dotnet run --project src/servicebus/publisher -topic broadcast -cs '<publisher-connection-string>'

# アプリがいくつかのバッチを送信するのを待ってから、終了します
# ctrl-c or cmd-c
```


📋 両方のサブスクリプションが同じメッセージ数を持っているか確認します。

<details>
  <summary>わからない場合は？</summary>



```
az servicebus topic subscription show --name web --topic-name broadcast --query messageCount -g labs-servicebus-pubsub  --namespace-name <sb-name>  

az servicebus topic subscription show --name desktop --topic-name broadcast --query messageCount -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


</details><br/>

両方のサブスクリプションは同じ数のメッセージを持っているはずです。トピックはすべてのメッセージをすべてのサブスクリプションに転送します。

ポータルで確認し、サブスクリプションブレードの _Service Bus Explorer_ を使用してメッセージを調査できます。

## サブスクリプションからメッセージを受信

アクセスポリシーは名前空間全体に適用することも、個々のキューやトピックに適用することもできます。サブスクリプションは独立してセキュリティが保護されていないため、トピック内の任意のサブスクリプションへの読み取りアクセスを与えるアクセスポリシーを作成します：


```
az servicebus topic authorization-rule create --topic-name broadcast --name subscriber --rights Listen -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


メッセージを読むアプリケーションコードは、発行者プログラムとは別のプログラムにあります：

- [subscriber/Program.cs](/src/servicebus/subscriber/Program.cs) - キューとトピックのためのロジックは同じですが、プロセッサはトピック名とサブスクリプション名で初期化する必要があります。


```
# 接続文字列を表示します：
az servicebus topic authorization-rule keys list --topic-name broadcast --name subscriber --query primaryConnectionString -o tsv -g labs-servicebus-pubsub  --namespace-name <sb-name>  

# web サブスクリプションの購読者を実行します：
dotnet run --project src/servicebus/subscriber  -topic broadcast -subscription web -cs '<subscriber-connection-string>'
```

> メッセージは見えますか？トピックにはデフォルトの有効期限が10分ありますので、その時間が経過するとサブスクライバーはメッセージを受け取れません。

サブスクライバーを実行し続け、別のコンソールでパブリッシャーを再開してください:



```
dotnet run --project src/servicebus/publisher -topic broadcast -cs '<publisher-connection-string>'
```


バッチを送信するときにパブリッシャーがログを記録しているのを見るべきですし、サブスクライバーは受け取ったすべてのメッセージを印刷します。

## 両方のサブスクリプションからメッセージを受信する

アプリケーションをモデル化するために必要なだけ多くのサブスクリプションを持つことができます。

一つのサブスクライバーは `web` サブスクリプションからメッセージを消費していますが、`desktop` サブスクリプションには消費者がいません。

再びサブスクリプション内のメッセージ数を比較してください:



```
az servicebus topic subscription show --name web --topic-name broadcast --query messageCount -g labs-servicebus-pubsub  --namespace-name <sb-name>  

az servicebus topic subscription show --name desktop --topic-name broadcast --query messageCount -g labs-servicebus-pubsub  --namespace-name <sb-name>  
```


> `web` サブスクリプションはメッセージが0個であるべきです。なぜなら、すべてのメッセージが消費者に配信されているからです。`desktop` サブスクリプションは公開されていてまだ期限切れになっていないすべてのメッセージのコピーを持っています。

新しいコンソールで `desktop` サブスクリプションのためのサブスクライバーを開始してください:


```
dotnet run --project src/servicebus/subscriber -topic broadcast -subscription desktop -cs '<subscriber-connection-string>'
```


新しいサブスクライバーはすべての未期限のメッセージのコピーを受け取りますので、たくさんのログを印刷しますが、`web` サブスクライバーはメッセージを待っています。古いメッセージをすべて受け取ると、バックログに追いついて新しいメッセージを待つようになります。

## ラボ

Service Busは信頼性が高く、スケーラブルなメッセージングソリューションです。さまざまなスピードで動作する異なるプロセスをモデル化するためにサブスクリプションを使用します。しかし、各サブスクリプションには一つの消費者を持つ必要はありません。

同じサブスクリプションに複数のサブスクライバーがリスニングしている場合はどうなりますか？そして、同じトピックに複数のパブリッシャーが公開している場合はどうなりますか？

> 行き詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除してください:


```
az group delete -y --no-wait -n labs-servicebus-pubsub
```
