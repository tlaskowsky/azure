# Functions: RabbitMQからBlob Storageへ

Azure Functionsは、Azureにネイティブでないいくつかのサービスをサポートしており、インフラストラクチャに移行できない場合のシナリオに使用できます。[RabbitMQ](https://www.rabbitmq.com)はその1つで、AzureにはRabbitMQを管理するサービスはありませんが、非常に人気のあるオープンソースのメッセージキューシステムで、Azure Functionsと組み合わせて使用できます。

このラボでは、RabbitMQトリガーを使用し、すべての依存関係をコンテナ内でローカルに実行し、次にRabbitMQのマーケットプレイスVMイメージを使用してAzureにデプロイします。

## 参考

- [RabbitMQチュートリアル](https://www.rabbitmq.com/getstarted.html)

- [RabbitMQトリガー＆バインディングリファレンス](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-rabbitmq?tabs=in-process&pivots=programming-language-csharp)

- [Blobストレージ出力バインディングリファレンス](hhttps://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-output?tabs=in-process%2Cextensionv5&pivots=programming-language-csharp)

- [Azurite接続文字列](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json&tabs=visual-studio#http-connection-strings)

## Blob Storageへの書き込みを行うRabbitMQ関数

シナリオは、顧客イベントを表すメッセージの着信ストリームで、重要なイベントのみを保存するためにフィルタリングしたいものです。コードは`RabbitToBlob`ディレクトリにあります。

- [RabbitToBlob/PriorityMessageArchive.cs](/labs/functions/rabbitmq/RabbitToBlob/PriorityMessageArchive.cs) - 受信メッセージを受信し、内容をチェックしてクレームメッセージをBlobストレージにコピーします。

これらの属性は関数をワイヤアップします：

- `[RabbitMQTrigger]`は`customerevents`という名前のRabbitMQキューでリッスンします。
- `[Blob]`は文字列をBlobに書き込む出力バインディングです。

RabbitMQトリガーはJSONを.NETオブジェクトに逆シリアル化できるため、関数は`CustomerEvent`メッセージで始まり、フォーマット作業は不要です。

<details>
  <summary>参考情報</summary>

関数の作成方法は次のとおりです：



```
func init RabbitToBlob --dotnet 

cd RabbitToBlob

dotnet add package Microsoft.Azure.WebJobs.Extensions.RabbitMQ --version 2.0.3

dotnet add package Microsoft.Azure.WebJobs.Extensions.Storage.Blobs --version 4.0.5

# RabbitMQのテンプレートは存在しません
```


</details><br/>

## ローカルで関数をテスト

Azure StorageエミュレーターとRabbitMQをDockerコンテナで起動します：



```
docker compose -f labs/functions/rabbitmq/docker-compose.yml up -d
```


> 関数がリッスンするRabbitMQキューを作成する必要があります

RabbitMQ UIをhttp://localhost:15672で開き、次でサインインします：

- ユーザー名 `guest`
- パスワード `guest`

_キュー_ タブを開き、_新しいキューを追加_ の下で `customerevents` という名前のキューを作成します：

![RabbitMQ新しいキュー](/img/rabbitmq-create-queue.png)

次に、これらの接続詳細を持つファイル `labs/functions/rabbitmq/RabbitToBlob/local.settings.json` を作成します：



```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "CustomerOutputStorageConnectionString" : "UseDevelopmentStorage=true",
        "InputRabbitMQConnectionString" : "amqp://localhost:5672"
    }
}
```


関数を実行し、関数がコンテナ内のRabbitMQに接続します：



```
cd labs/functions/rabbitmq/RabbitToBlob

func start
```


RabbitMQ UIの_キュー_ リストで `customerevents` キューを開き、_メッセージを発行_ の下で以下のようなメッセージを送信します：



```
{
  "CustomerId" : 297844,
  "EventType" : "Order"
}
```

```
{
  "CustomerId" : 113435,
  "EventType" : "Complaint"
}
```


![RabbitMQメッセージを発行](/img/rabbitmq-publish-message.png)

> 関数の出力で次のような出力を見るはずです： 関数での出力は、`Complaint` メッセージのみがアーカイブされます。


```
[2022-11-08T02:58:32.148Z] Received customer message with event type: Complaint
[2022-11-08T02:58:32.166Z] Archiving complaint message for customer: 113435
[2022-11-08T02:58:32.291Z] Executed 'PriorityMessageArchive' (Succeeded, Id=44d45f7c-91b4-445d-9c6c-ea604fa0c614, Duration=145ms)
```


Blobストレージで確認できます：



```
az storage blob list -c complaints -o table --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```


クレームメッセージごとにJSON Blobが表示されるはずです。

## Azureにデプロイ

Azureでの初期設定は通常通りです：



```
az group create -n labs-functions-rabbitmq --tags courselabs=azure -l eastus

az storage account create -g labs-functions-rabbitmq --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-rabbitmq  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


事前に必要なものは、RabbitMQサービスを管理できないため、ポータルを使用するかもしれません：

- リソースを作成し、「rabbitmq bitnami」と検索します。VMオプションが最も簡単で、dev/testプリセットを使用できます。次に通常のVM作成画面に移動します。SSHキーの代わりにパスワードを使用してください。

- 作成されたら、NSGを設定して、ポート `15672` と `22` へのアクセスを自分のIPアドレスから許可し、ポート `5672` へのアクセスをすべてのアドレスから許可する必要があります。

- [これらの手順](https://docs.bitnami.com/azure/faq/get-started/find-credentials/#option-2-find-credentials-by-connecting-to-your-application-through-ssh)に従ってRabbitMQのユーザー名とパスワードを取得します。

- VMのIPアドレスとポート `15672` でRabbitMQ UIを開きます。ローカルで行った手順と同じ手順で `customerevents` キューを作成します。

- RabbitMQの接続文字列の形式は `amqp://<user>:<password>@<ip-address>:5672` です。これをappsetting `InputRabbitMQConnectionString` に保存します。

- 出力用のストレージアカウントも必要です。ストレージアカウントの接続文字列をappsetting `CustomerOutputStorageConnectionString` に設定します。

すべて実行されているとき、関数を公開します：



```
func azure functionapp publish <function-name>
```


RabbitMQ UIの_Connections_タブに戻り、関数トリガーのリスナーである接続が表示されるはずです。

異なるイベントタイプのメッセージをいくつか発行し、苦情がBlob Storageにアーカイブされていることを確認してください。

## Lab

RabbitMQ VMのセットアップを自動化する方法は何ですか？

> 行き詰まったら、[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Azure StorageエミュレーターとRabbitMQコンテナを停止します：



```
docker compose -f ../docker-compose.yml down
```


ラボリソース グループを削除します：



```
az group delete -y --no-wait -n labs-functions-rabbitmq
```
