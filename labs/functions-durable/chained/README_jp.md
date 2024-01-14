# Durable Functions: チェーン化されたFunctions

出力が次の関数のトリガーとして機能する場合、関数を連鎖させることができます。例えば、一つの関数でBlobストレージに書き込み、その出力を次の関数のBlobトリガーとして使用することができます。これにより、複数ステップのワークフローをモデル化することができますが、必ずしも実行順序を保証したり、次のトリガーに使用する出力を常に持つわけではありません。

このような長期間にわたるシナリオには、Azureの_durable functions_が適しています。これらは、ワークフローの各ステップ間で状態を共有する必要がある場合に使用されます。これらは通常のAzure Functionsとしてデプロイされますが、トリガーは_orchestrator_を起動し、実際のコードがそこで実行されます。オーケストレーターのコードは、他のすべてのアクティビティを呼び出し、追加のトリガーなしで入出力を管理します。

このラボでは、順序通りに実行する必要があるいくつかのアクティビティを含むワークフローにdurable functionを使用します。

## 参考

- [Durable functionsの概要](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=csharp)

- [Orchestrations - durable functionのコード化されたワークフロー](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-orchestrations?tabs=csharp)

- [Activity functions - ワークフローの個別ステップ](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-types-features-overview#activity-functions)


## タイマートリガーとオーケストレーション

このシナリオは、[functions CI/CDラボ](/labs/functions/cicd/README.md)でのチェーン化された関数の代替実装です。ロジックは同じですが、複数の関数が互いにトリガーする代わりに、一つの耐久性のある関数を使用しています。コードは`DurableChained`フォルダーにあります：

- [DurableChained/TimedOrchestratorStart.cs](/labs/functions-durable/chained/DurableChained/TimedOrchestratorStart.cs) - タイマートリガーを使用し、`DurableClient`デコレーターがあります。これはオーケストレーターを開始するために使用され、ダミーのアプリケーションステータスオブジェクトを渡します。

- [DurableChained/ChainedOrchestrator.cs](/labs/functions-durable/chained/DurableChained/ChainedOrchestrator.cs) - これは他のすべてのアクティビティのオーケストレーターです。最初のアクティビティの出力を使用して、3つのアクティビティを順番に実行します。

耐久性のある関数でデータがどのように交換されるかがわかります - トリガーはオーケストレーターにオブジェクトを渡し、オーケストレーターはアクティビティからオブジェクトを受け取ったり渡したりすることができます。

これらはオーケストレーターによって呼び出されるアクティビティで、`ActivityTrigger`を使用するため、耐久性のある関数内でのみアクティブにすることができます：

- [Activities/WriteBlob.cs](/labs/functions-durable/chained/DurableChained/Activities/WriteBlob.cs) - アクティビティステータスオブジェクトをBlobに保存します。コードでバインディングを作成し、Blob名を動的に指定できます。

- [Activities/NotifySubscribers.cs](/labs/functions-durable/chained/DurableChained/Activities/NotifySubscribers.cs) - Service Busバインディングを使用してキューにメッセージを書き込みます。

- [Activities/WriteLog.cs](/labs/functions-durable/chained/DurableChained/Activities/WriteLog.cs) - Tableバインディングを使用してTable Storageにエンティティを書き込みます。

アクティビティのロジックは通常の関数とほぼ同じですが、オーケストレーターによって制御されます。

## ローカルでの関数テスト

Service Busエミュレーターはないので、Azureで以下を作成する必要があります：

- Service Bus Namespace
- `HeartbeatCreated`という名前のQueue
- 接続文字列をメモしておく

Docker Desktopを実行し、Azure Storageエミュレーターを開始します：



```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


次に、`labs/functions-durable/chained/DurableChained/local.settings.json`にローカル設定ファイルを作成し、接続設定を追加します：


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "StorageConnectionString": "UseDevelopmentStorage=true",
        "ServiceBusConnectionString" : "<sb-connection-string>"
    }
}
```


関数をローカルで実行します：


```
func start
```


これはタイマートリガーを使用するので、数分以内にオーケストレーターが開始されるはずです。

> 関数は多くの出力を生成するはずです。その中には以下のような行が見られます：


```
[2022-11-14T02:24:00.053Z] Executing 'TimedOrchestratorStart' (Reason='Timer fired at 2022-11-14T02:24:00.0336490+00:00', Id=56d226b5-ba43-46dc-8adf-713b23dd7b45)
[2022-11-14T02:24:00.061Z] Starting orchestration for: save-handler; at: 14/11/2022 02:24:00 (UTC)
...
[2022-11-14T02:24:00.246Z] Executing 'WriteBlob' (Reason='(null)', Id=557d0579-ea38-41c6-8fd5-f3bb8d4ece42)
[2022-11-14T02:24:00.356Z] Created blob: heartbeat/20221114022400
...
[2022-11-14T02:24:00.405Z] Executing 'NotifySubscribers' (Reason='(null)', Id=664c8c69-3f8b-4487-9f5d-7daa3d89865c)
[2022-11-14T02:24:00.845Z] Published heartbeat message
...
[2022-11-14T02:24:00.972Z] Orchestrator completed.
[2022-11-14T02:24:00.973Z] Executed 'ChainedOrchestrator' (Succeeded, Id=54945bb0-80a0-4dbb-9a30-3025404142b2, Duration=1ms)
```


Table Storageをチェックします - `heartbeats` という名前のテーブルが見られるはずです（またはStorage

```

az storage table list --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"

```

ポータルでService Bus Queueを開き、_Service Bus Explorer_に移動します。Explorerで _Peek from start_ をクリックすると、以下のような本文を含むメッセージが表示されるはずです：


```
{
    "BlobName": "heartbeat/20221114022400"
}
```


すべてが順調に見える場合、Azureにデプロイする準備ができています。

## Azureへのデプロイ

Function Appの基本セットアップは以下の通りです：



```
az group create -n labs-functions-durable-chained --tags courselabs=azure -l eastus

az storage account create -g labs-functions-durable-chained --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-durable-chained  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


依存関係には以下が必要です：

- Service Bus Namespace（既存のものを使用できます）
- `HeartbeatCreated` という名前のService Bus Queue
- アプリ設定に保存されたService Busの接続文字列 `ServiceBusConnectionString`

- 出力用のストレージアカウント
- アプリ設定に設定されたストレージアカウントの接続文字列 `StorageConnectionString`



```
func azure functionapp publish <function-name>
```


ポータルの _Functions_ リストをチェックします - すべての関数が表示されるか、それとも外部トリガー（タイマートリガーなど）を持つものだけが表示されるかを確認してください。

## ラボ

ポータルで関数は _Disabled_ に設定することができ、これはトリガーが発火しないことを意味します。タイマートリガーを無効にすることで、このワークフロー全体を停止することができますが、アクティビティトリガーの一つを無効にすることはできますか？もしできたらどうなるでしょうか？

> 詰まった場合は、[提案](suggestions_jp.md) を試してみてください。

___

## クリーンアップ

Azure Storageエミュレーターを停止します：



```
docker rm -f azurite
```


ラボのリソースグループを削除します：


```
az group delete -y --no-wait -n labs-functions-durable-chained
```
