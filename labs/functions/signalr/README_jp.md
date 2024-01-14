# Functions: SignalR ホスト

SignalR は非常にシンプルなワークフローを持っています - クライアントは、JavaScript 関数を実行し、SignalR _hub_ に接続するウェブページを取得します。ハブはクライアントの接続を管理し、すべてまたは一部の接続にメッセージを送信するコンポーネントです。

Azure Functions を使用して、このワークフロー全体をモデル化し、HTTP トリガーから HTML ページを提供し、SignalR Hub 接続を設定し、接続されたクライアントにデータをプッシュするトリガーを使用できます。

このラボでは、SignalR を使用してタイムドアップデートを実行する単純なウェブアプリケーションを実行し、アプリケーション全体を Azure Functions でホストします。

## 参考

- [SignalR サービス トリガーおよびバインディング リファレンス](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-signalr-service?tabs=in-process&pivots=programming-language-csharp)

- [地球から月までの距離](https://spaceplace.nasa.gov/moon-distance/en/)

## タイマーで SignalR サービスにプッシュ

シナリオは、地球から月までの距離を監視し、SignalR を介してクライアントに更新をブロードキャストするアプリケーションです。Azure Functions はウェブページの静的な HTML を提供し、Azure SignalR サービスとの統合も提供します。

コードは `MoonDistance` フォルダにあります：

- [MoonDistance/Index.cs](/labs/functions/signalr/MoonDistance/Index.cs) - HTTP トリガーで、[index.html](/labs/functions/signalr/MoonDistance/content/index.html) ファイルのコンテンツを返します。これはウェブアプリのエントリポイントであり、HTML には SignalR 接続用の JavaScript が含まれています。

- [MoonDistance/Negotiate.cs](/labs/functions/signalr/MoonDistance/Negotiate.cs) - SignalR 接続のセットアップを処理し、新しいクライアントがウェブアプリをロードしたときに呼び出されるメソッドです。コードには接続の詳細が不要です。なぜなら、`SignalRConnectionInfo` はそれを提供する入力バインディングであるからです。

- [MoonDistance/Broadcast.cs](/labs/functions/signalr/MoonDistance/Broadcast.cs) - 10秒ごとに発生するタイマー トリガーで、現在の（偽の）月までの距離で SignalR 更新を送信します。Negotiate 関数と同じ SignalR サービスにバインドされているため、すべての接続されたクライアントに送信されます。

SignalR サービスはさまざまなモードで動作でき、Azure Functions では [サーバーレスモード](https://learn.microsoft.com/en-us/azure/azure-signalr/concept-service-mode#serverless-mode) を使用する必要があります。

## ローカルで関数をテスト

SignalR で多くの作業を行う場合は、[SignalR サービスエミュレータ](https://github.com/Azure/azure-signalr/blob/dev/docs/emulator.md) を使用しますが、実際の SignalR サービスを利用するために起動します：


```
az group create -n labs-functions-signalr --tags courselabs=azure 

az signalr create -g labs-functions-signalr --service-mode Serverless --sku Free_F1  -n <signalr-name>
```


それを作成している間に、ローカルの Azure Storage エミュレータを実行します：


```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


そして、ファイル `labs/functions/signalr/MoonDistance/local.settings.json` を作成します。**あなた自身の SignalR 接続文字列を使用してください**：


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "AzureSignalRConnectionString" : "<signalr-connection-string>"
    }
}
```


関数をローカルで実行します：


```
cd labs/functions/signalr/MoonDistance

func start
```


ブラウザでインデックス URL を開きます：http://localhost:7071/api/index

月からの定期的な更新を受け取るはずです。

## Azure にデプロイ

リソースグループと SignalR サービスは既に用意されていますので、Function App セットアップのみが必要です：


```
az storage account create -g labs-functions-signalr --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-signalr  --runtime dotnet --functions-version 4 --consumption-plan-location eastus  --storage-account <sa-name> -n <function-name> 
```


これ以上作成するサービスはありませんが、すべてを接続する必要があります。Function App は Managed Identity で実行できますが、SignalR バインディングは現在それをサポートしていないため、SignalR 接続文字列をキーで使用する必要があります：

- SignalR の接続文字列は Function アプリ設定 `AzureSignalRConnectionString` に設定する必要があります

その後、関数をデプロイできます：



```
func azure functionapp publish <function-name>
```


Azure Function URL にアクセスしてください：`https://<function-name>.azurewebsites.net/api/index` で更新を確認できます。

## ラボ

10秒ごとのタイマーはかなり頻繁です。開発中に関数をテストするときはそれが必要かもしれませんが、Azure では異なるスケジュールで実行したいかもしれません。スケジュールは現在、タイマー トリガーの属性にハードコードされています。どのようにして柔軟にできるでしょうか？

> 行き詰まっていますか？[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Azure Storage エミュレータを停止します：



```
docker rm -f azurite
```


ラボ RG を削除します：


```
az group delete -y --no-wait -n labs-functions-signalr
```
