# 関数: サービスバスから複数の出力へ

非同期イベントパブリッシングを使用してアプリを構築する際、関数とメッセージングは非常にうまく連携します。メインアプリがサービスバスのトピックにメッセージをプッシュし、それが関数をトリガーして新しい機能を追加することができます。

このラボでは、サービスバスのトリガーを使用し、関数が複数の出力を持つ様子を見てみましょう - このケースでは、テーブルストレージに書き込み、サービスバスキューにメッセージを公開します。

## 参考文献

- [Azure サービスバス トリガーおよびバインディング リファレンス](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-service-bus?tabs=in-process%2Cextensionv5%2Cextensionv3&pivots=programming-language-csharp)

- [テーブルストレージ バインディング リファレンス](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-storage-table?tabs=in-process%2Ctable-api%2Cextensionv3&pivots=programming-language-csharp)

- [テーブルストレージ Functions .NET SDK](https://github.com/Azure/azure-sdk-for-net/blob/Microsoft.Azure.WebJobs.Extensions.Tables_1.0.0/sdk/tables/Microsoft.Azure.WebJobs.Extensions.Tables/README.md)

- [サービスバス Functions .NET SDK](https://github.com/Azure/azure-functions-servicebus-extension)

## サービスバス トピック関数、テーブルストレージとキューへの書き込み

シナリオは、複数のサプライヤーが製品の提供について見積もりを出すことができるアプリケーションです。`TopicToTableAndQueue` ディレクトリには2つの関数があります：

- [TopicToTableAndQueue/Supplier1Quote.cs](/labs/functions/servicebus/TopicToTableAndQueue/Supplier1Quote.cs) - トピックサブスクリプション上の着信リクエストをリッスンし、レスポンスをテーブルストレージに保存し、キューにメッセージを投稿します。

- [TopicToTableAndQueue/Supplier2Quote.cs](/labs/functions/servicebus/TopicToTableAndQueue/Supplier2Quote.cs) - `Supplier1Quote`と同様の機能を持ちますが、異なる価格エンジンを使用し、意図的な遅延があります。

これらの属性は、トリガーとバインディングを担当します：

- `[ServiceBusTrigger]` は、`QuoteRequestTopic` へのメッセージ配信時、または `Supplier1Subscription` または `Supplier2Subscription` が発生した際に関数を実行するように設定します。
- `[Table]` は出力バインディングであり、テーブル `quotes` のテーブルストレージにエンティティを作成します。
- `[ServiceBus]` は別の出力バインディングであり、`QuoteStoredQueue` というキューにメッセージを送信して、見積もりが保存されたことを通知します。

サービスバスのトピックは複数のサブスクリプションを持つことができ、それぞれがすべてのメッセージのコピーを受け取ります。このシナリオは、実際の世界では異なるサプライヤーAPIを呼び出す2つの別個のプロセス（異なる待ち時間を持つ）をモデル化しています。各プロセスは独自のサブスクリプションを持つ関数で、それぞれのペースで作業することができます。

<details>
  <summary>参照用</summary>

関数の作成方法は以下の通りです：



```
func init TopicToTableAndQueue --dotnet 

cd TopicToTableAndQueue

dotnet add package Microsoft.Azure.WebJobs.Extensions.ServiceBus --version 5.8.0
dotnet add package Microsoft.Azure.WebJobs.Extensions.Tables --version 1.0.0

func new --name Supplier1Quote --template ServiceBusTopicTrigger
func new --name Supplier2Quote --template ServiceBusTopicTrigger
```


</details><br/>

## ローカルでの関数テスト

サービスバスのエミュレーターは存在しないため、関数を完全にローカルで実行することはできません。まずAzureでこの依存関係を作成する必要があります：

- サービスバスネームスペース（スタンダードSKU）
- ネームスペース内の `QuoteStoredQueue` というキュー
- ネームスペース内の `QuoteRequestTopic` というトピック
- トピック内の `Supplier1Subscription` と `Supplier2Subscription` という2つのサブスクリプション

Azureストレージエミュレータを開始します。これは関数が使用します：



```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


そして、自分のサービスバス接続文字列を含むファイル `labs/functions/servicebus/TopicToTableAndQueue/local.settings.json` を作成します：


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "ServiceBusInputConnectionString" : "<your-connection-string>",
        "ServiceBusOutputConnectionString" : "<your-connection-string>",
        "OutputTableStorageConnectionString": "UseDevelopmentStorage=true"
    }
}
```


すべての依存関係が整ったら、関数をローカルで実行できます：


```
cd labs/functions/servicebus/TopicToTableAndQueue

func start
```


Azureポータルでトピックにメッセージを送信します：


```
{
    "QuoteId" : "42bf48b5-8531-48b3-82e0-91af19df6351", 
    "ProductCode": "PR-123",
    "Quantity" : 19
}
```


> 関数の出力は以下のようになります：


```
[2022-11-07T21:56:55.436Z] Supplier1 saved quote response for ID: 42bf48b5-8531-48b3-82e0-91af19df6351
[2022-11-07T21:56:55.436Z] Supplier2 saved quote response for ID: 42bf48b5-8531-48b3-82e0-91af19df6351
[2022-11-07T21:56:55.505Z] Executed 'Supplier1Quote' (Succeeded, Id=58895544-27e9-4a52-99a2-7cf0ff3596c9, Duration=117ms)
[2022-11-07T21:56:55.505Z] Executed 'Supplier2Quote' (Succeeded, Id=31a6f011-0cdf-4941-90d8-92e42031c8f7, Duration=117ms)
```


そして、`quotes`テーブルがエミュレーターに作成されたことを確認できます（またはStorage Explorerを使用してブラウズできます）：


```

az storage table list --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
```


また、Service Bus **キュー**からメッセージを確認してみてください。両方のサプライヤーからの応答が表示されるはずです。

AzureデプロイメントにはAzureサービスバス名前空間を使用できます（または新しいものを作成する方が良いかもしれません）。

## Azureにデプロイ

以下は、開始するためのセットアップです：



```
az group create -n labs-functions-servicebus --tags courselabs=azure -l eastus

az storage account create -g labs-functions-servicebus --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-servicebus  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


関数の前提条件は次のとおりです：

- トピック`QuoteRequestTopic`と2つのサブスクリプション`Supplier1Subscription`および`Supplier2Subscription`を持つService Bus名前空間
- Service Busの接続文字列をアプリ設定`ServiceBusInputConnectionString`として設定

- 名前が`QuoteStoredQueue`のサービスバスキュー
- Service Busの接続文字列をアプリ設定`ServiceBusOutputConnectionString`として設定

- 出力用のストレージアカウント
- ストレージアカウントの接続文字列をアプリ設定`OutputTableStorageConnectionString`として設定

それから、次のコマンドで公開する準備が整います：



```
func azure functionapp publish <function-name>
```


## ラボ

これを大規模にテストしたい場合、どのようにして関数を使用できるでしょうか？

> 行き詰まっていますか？[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Azure Storageエミュレーターを停止します：



```
docker rm -f azurite
```


ラボリソースグループを削除します：


```
az group delete -y --no-wait -n labs-functions-servicebus
```
