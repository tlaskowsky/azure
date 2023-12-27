# Service Bus メッセージング

Service Bus は、高スループットで信頼性の高いメッセージキューサービスです。メッセージは読み取られるまで保存され、配信されなかったメッセージや処理に失敗したメッセージのためのデッドメッセージキューなどの高度な機能があります。標準的なメッセージングパターンをサポートするために Service Bus キューを使用できます。

このラボでは、パブリッシャーが戻りを期待せず、どのコンポーネントがそれらを処理するかさえ知らないでメッセージを送信する、ファイア・アンド・フォーゲットのメッセージングパターンを使用します。

## 参照

- [Service bus 概要](https://learn.microsoft.com/ja-jp/azure/service-bus-messaging/service-bus-messaging-overview)

- [Microsoft Learn: Azure Service Bus を使ったメッセージベースの通信ワークフローの実装](https://learn.microsoft.com/ja-jp/training/modules/implement-message-workflows-with-service-bus/)

- [`az servicebus` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/servicebus?view=azure-cli-latest)

## Service Bus 名前空間およびキューの作成

Service Bus リソースを作成する際、_名前空間_ を作ります。これは複数のキューのためのグルーピング構造です。ポータルで新しいリソースを作成し、'service bus' を検索して、_Service Bus_ リソースを作成します。これは実際には名前空間です。オプションを探索してください:

- 名前空間名は、`.servicebus.windows.net` でユニークなサブドメインを提供します
- 価格帯は、最大メッセージサイズ、機能、および操作数を定義します
- 消費者のための最小 TLS レベルを設定できます

CLI で名前空間を作成します:

ラボ用のリソースグループを作成してCLIに切り替えます:



```
az group create -n labs-servicebus --tags courselabs=azure -l southeastasia
```


📋 `servicebus namespace` コマンドを使用して名前空間を作成します。Basic SKU を使用します。

<details>
  <summary>方法がわからない場合</summary>

ヘルプテキストを確認してください:



```
az servicebus namespace create --help
```


名前とRGは必須ですが、デフォルトのSKUはStandardなのでそれを設定する必要があります:


```
az servicebus namespace create -g labs-servicebus --location southeastasia --sku Basic -n <sb-name>
```


</details><br/>

> 出力には service bus エンドポイントが含まれています - 通信は HTTPS 経由

ポータルで Service Bus 名前空間を開きます。通常のブレードに加えて、_キュー_ と _共有アクセスポリシー_ が表示されます。共有アクセス トークンは認証と承認に使用され、ストレージアカウントと同様ですが、ポリシーとトークンの間には一対一の関係があります。

Basic SKU では、キューのみがメッセージングオプションです - 送信と受信のメッセージ用に1つ作成します:



```
az servicebus queue create -g labs-servicebus --name echo --namespace-name <sb-name>
```


> ポータルでキューを開くと、メッセージ数に関するメトリクスが表示されます。

また、キュー レベルで _共有アクセス ポリシー_ タブがあるため、1つのキューに送信し、別のキューから読み取る必要があるアプリのためのきめ細かい権限を作成できます。

## .NET サブスクライバーを実行

サブスクライバーはキューで無限ループを聞き、メッセージを受信するとそれを処理します。分散アプリケーションでは、異なるキューにサブスクライブする複数のコンポーネントがあり、各コンポーネントには複数のインスタンスがある場合があります。

Service Bus は、Advanced Message Queuing Protocol [AMQP](http://docs.oasis-open.org/amqp/core/v1.0/amqp-core-overview-v1.0.html) という標準プロトコルを使用します。他のキュー技術も同じプロトコルを実装しているため、Service Bus を RabbitMQ などのドロップイン代替品として使用できます。

キューを使用してメッセージを購読し、受信した各メッセージの内容を印刷し、メッセージが処理されたことを認めるシンプルなアプリがあります:

- [subscriber/Program.cs](/src/servicebus/subscriber/Program.cs) - キューにサブスクライブし、受信したメッセージの内容を印刷し、メッセージが処理されたことを認めます

ローカルでアプリを実行します（[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download)が必要です）。接続文字列を設定するパラメーターを使用します:


```
# キューの接続文字列を取得します:
az servicebus namespace authorization-rule keys list -n RootManageSharedAccessKey -g labs-servicebus  --query primaryConnectionString -o tsv --namespace-name <sb-name>

# アプリを実行します:
dotnet run --project src/servicebus/subscriber -cs '<connection-string>'
```


アプリはメッセージを聞いて、シャットダウンするまで続きます。

別のシンプルなアプリがあり、ループ内でキューにメッセージをパブリッシュします - バッチをパブリッシュし、待機してから別のバッチをパブリッシュします:

- [publisher/Program.cs](/src/servicebus/publisher/Program.cs) - バッチのメッセージを送信します。これは、個別の接続を作成して個々のメッセージを送信するよりも通常効率的です

別のコンソールでパブリッシャーアプリを実行します:



```
dotnet run --project src/servicebus/publisher -cs '<connection-string>'
```


パブリッシャーがメッセージを送信し、サブスクライバーがそれらを受信するのを見ることができます。ポータルでメトリクスを確認できますが、現時点では処理されるメッセージはあまりありません。

## 信頼性とスケーラビリティのあるメッセージング

サブスクライバーによって処理された最後のバッチの番号を確認し、**サブスクライバー**を終了します（サブスクライバーウィンドウでCtrl-CまたはCmd-C）。パブリッシャーアプリは実行されたままにします。

パブリッシャーはメッセージを送信し続けます。もう少しバッチをパブリッシュするのを待ちます。

サブスクライバーを再実行すると、次の3つのことのいずれかを行うかもしれません:

- バッチ1からすべてのメッセージを処理します
- 聞き始めたときからの新しいメッセージのみを処理します
- サブスクライバーを停止したときからのすべてのバッチを処理します

📋 サブスクライバーを実行します。実際には何をしますか？

<details>
  <summary>方法がわからない場合</summary>

同じコマンドです:



```
dotnet run --project src/servicebus/subscriber -cs '<connection-string>'
```


サブスクライバーが停止したところから続けて、閉じた前のサブスクライバーインスタンスがパブリッシュした新しいバッチを処理するのを見るはずです。

</details>

> Service bus キューは、完了の確認を受けるまでメッセージを保存します。

新しいサブスクライバーは完了とマークされたメッセージを受け取りませんが、キューにある未完了のすべてのメッセージを受け取ります。このようにして、リクエストは失われることなく、またサブスクライバーが失敗した場合に2回処理されることもありません。

別のコンソールを開いて、サブスクライバーの別のインスタンスを実行します:


```
dotnet run --project src/servicebus/subscriber -cs '<connection-string>'
```


これで、サブスクライバーがメッセージを共有します - 彼らはメッセージを受け取るために交代します（おおよそ）。重複はないので、インスタンスは自分のメッセージセットを処理できます。

## ラボ

メッセージングは、スケールでの作業分配についてです。複数のパブリッシャーがある場合、サブスクライバーはどのように作業を分割しますか？

信頼性も重要な要素です。`-ack False` フラグを使用して、メッセージ完了を認めずにサブスクライバーを実行することができます。メッセージを認めずに、そのサブスクライバーを終了して置き換えた場合、処理されたメッセージはどうなりますか？

> 困ったら[ヒント](hints_jp.md)を試してみるか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボ RG を削除します:


```
az group delete -y --no-wait -n labs-servicebus 
```
