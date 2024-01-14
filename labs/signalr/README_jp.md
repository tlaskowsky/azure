# Azure SignalR サービス

SignalRはインターネット上での双方向通信のための技術です。これは、Webアプリケーションがブラウザに更新をプッシュする方法であり、フロントエンドへの非同期配信をサポートしています。SignalRは、独自のアプリケーションで実行できるサーバー技術です（これはMicrosoftによる.NET用[WebSockets](https://en.wikipedia.org/wiki/WebSocket)のカスタマイズです）が、各サーバーは接続されているクライアントのリストを独自に保持しているため、自分で実行する場合はスケーリングが難しいです。

Azure SignalR サービスは、SignalRを独自のコンポーネントとして移動させ、Webアプリケーションが直接クライアントを扱うことなく、更新通知をSignalR サービスに送信し、それがすべてのクライアントにブロードキャストされるようにします。

このラボでは、ローカルで簡単なSignalRアプリケーションを実行し、それをAzureのSignalRサービスと統合する方法を見ていきます。

## 参照

- [SignalR 概要](https://learn.microsoft.com/ja-jp/aspnet/core/signalr/introduction?view=aspnetcore-7.0)

- [Azure SignalR サービス概要](https://learn.microsoft.com/ja-jp/azure/azure-signalr/signalr-overview)

- [`az signalr` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/signalr?view=azure-cli-latest)

## ローカルSignalRウェブサイトを実行

基本的なチャットアプリケーションがあります。これはSignalRを使用して接続されたクライアントにメッセージをブロードキャストします。コードはそれほど面白くないので、すぐにローカルで実行してみましょう（[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download)が必要です）：


```
dotnet run --project src/signalr/chat --urls=http://localhost:5005/
```


次に、http://localhost:5005/ に**2つの**ブラウザウィンドウを開きます。アプリはユーザー名を尋ね、ランダムなデフォルトを生成します。メッセージを交換すると、クライアント側の更新なしに両方のブラウザが更新されます。新しいメッセージが投稿されると、サーバーはSignalRを使用してそれをブロードキャストします。

> さて、Ctrl-C/Cmd-Cでサーバーアプリを終了すると、ブラウザーで何が起こるか？

同じコマンドでアプリを再起動します：



```
dotnet run --project src/signalr/chat --urls=http://localhost:5005/
```


ブラウザをF5で再接続できますが、以前のメッセージは失われます。アプリにはデタの永続化層がなく、SignalRはそれを提供しません。

大規模に実行する場合はどうでしょうか？異なるポートを使用してウェブサイトの別のコピーを実行できますが、それは完全に別のインスタンスです：




```
dotnet run --project src/signalr/chat --urls=http://localhost:5006/
```


http://localhost:5005/ と http://localhost:5006/ の各サイトに1つずつブラウザページを開いてメッセージを送信してみてください。

メッセージは2つのサーバー間で共有されず、ユーザーは接続しているサーバーに応じて異なるメッセージセットを見ます。これがAzure SignalRサービスの使用例です - クライアント接続を管理することなく、アプリケーションが必要とするだけのウェブホストを実行できます。

## SignalRサービスの作成

Azureポータルを開き、新しいリソースを作成し、'signalr'を検索します。_SignalRサービス_ を選択して作成します：

- 通常のDNS名、リージョン、リソースグループの要件があります
- 価格層はデフォルトでプレミアムです - フリーとスタンダードもあります（スケールと信頼性の異なるレベル）
- _ユニット_ と _サービスモード_ についてはリンクをたどって確認してください

> SignalRはあまり文書化されていないサービスの一つです :)

SignalRサービスは共有プラットフォーム（Appサービスのような）上で実行されるため、VNet統合はありません。

CLIを使用して新しいインスタンスを作成しましょう：



```
az group create -n labs-signalr -l eastus --tags courselabs=azure 

az signalr create -g labs-signalr --sku Free_F1 -l eastus -n <signalr-name>
```

> メッセージが表示されるかもしれません：_この操作に使用されるリソースプロバイダー「Microsoft.SignalRService」は登録されていません。私たちがあなたのために登録しています。_

SignalRはあまり一般的でないサービスなので、デフォルトではサブスクリプションで有効になっていない可能性があります。CLIがそれを設定します。

作成したら、ポータルで新しいサービスを確認してください：

- _キー_ の下では、アクセスキーを使用する接続文字列を確認できます
- _接続文字列_ では、マネージドIDを使用する異なる接続文字列を見つけることができます

ウェブサーバーからSignalRサービスへの異なる認証方法を使用できます。

_Access Key用の接続文字列_ を取得し、ローカルで実行中のアプリとホストされたSignalRサービスを使用します。今回は、SignalRサービスの設定を渡して再び2つのインスタンスを実行します：


```
dotnet run --project src/signalr/chat --urls=http://localhost:5005/ --Azure:SignalR:Enabled=true --Azure:SignalR:ConnectionString='<signalr-connection-string>'

dotnet run --project src/signalr/chat --urls=http://localhost:5006/ --Azure:SignalR:Enabled=true --Azure:SignalR:ConnectionString='<signalr-connection-string>'
```


それぞれのサーバーに1つずつブラウザを開いてください：

- http://localhost:5005/ 
- http://localhost:5006/

今回はメッセージの交換ができますか？1つのサーバーを停止して再起動すると、メッセージは今回保存されますか？

> SignalRはリアルタイムブロードキャスト用です。アプリが状態を維持する必要がある場合は、自分のコードでそれを処理する必要があります。

## AzureにSignalRウェブサイトをデプロイ

SignalRサービスはマネージドIDでの認証をサポートしています。システム管理されたIDを持つAppサービスでウェブサイトを実行し、接続文字列に機密データを必要とせずにSignalRに接続できます。

📋 `az webapp up` コマンドを使用して、`src/signalr/chat`フォルダからアプリケーションをデプロイします。

<details>
  <summary>わからない場合は？</summary>

ヘルプから始めます：



```
cd src/signalr/chat

az webapp up -g labs-signalr --os-type Linux --sku B1 --runtime dotnetcore:6.0 -n <app-name>
```


</details><br/>

次に、SignalR接続文字列をアプリの設定に設定します。マネージドIDを使用するので、接続文字列にはSignalRドメイン名のみが必要で、キーは必要ありません：


```
az webapp config appsettings set --settings Azure__SignalR__Enabled='true' Azure__SignalR__ConnectionString='Endpoint=https://<signalr-name>.service.signalr.net;AuthType=azure.msi;Version=1.0;' -g labs-signalr -n <app-name>
```

> アプリにアクセスしてください。動作しません - ログで問題がわかりますか？

ページは読み込まれますが、メッセージの送信に失敗します - ブラウザの開発者ツールを開くと、500エラーが表示されます。Webアプリの_Log stream_を確認すると、次のようなエラーが表示されます：



```
2022-11-11T15:10:37.631828591Z info: Microsoft.Azure.SignalR.Connections.Client.Internal.WebSocketsTransport[6]
2022-11-11T15:10:37.632038589Z       Transport is stopping.
2022-11-11T15:10:37.634772560Z fail: Microsoft.Azure.SignalR.ServiceConnection[2]
2022-11-11T15:10:37.634793760Z       Failed to connect to '(Primary)https://clabsazes221111.service.signalr.net(hub=ChatSampleHub)', will retry after the back off period. Error detail: ManagedIdentityCredential authentication unavailable. No Managed Identity endpoint found.. ManagedIdentityCredential authentication unavailable. No Managed Identity endpoint found.. Id: fdc941cd-4e53-4139-96df-6a1e21f8b80f
```


> App ServiceアプリはデフォルトではManaged Identityで作成されません。

Webアプリをシステム生成のManaged Identityを使用するように設定してください：


```
az webapp identity assign -g labs-signalr -n <app-name>
```


もう一度試してみてください。それでも動作しないことに気付くでしょう :) ログからは問題が明確になるはずです。

> WebアプリはManaged Identityを使用してSignalRに認証できるようになりましたが、Identityはサービスを使用するために承認されていません。

Identityは[ロール割り当て](https://learn.microsoft.com/en-gb/azure/azure-signalr/signalr-howto-authorize-managed-identity)で承認する必要があります。

SignalRサービスのIDを取得します - これはロールの_scope_です：



```
az signalr show -g labs-signalr --query id -n <signalr-name>
```


次に、App ServiceのManaged Identity IDに、サービススコープを持つ`SignalR App Server`ロールを付与するロール割り当てを作成します：


```
# アプリサービスのプリンシパルIDを取得します：
az webapp identity show --query principalId -o tsv -g labs-signalr -n <app-name>

# ロール割り当てを作成します：
az role assignment create  --role 'SignalR App Server' --assignee-object-id <principalId> --scope "<signalr-id>"
```


📋 ポータルでSignalRサービスを確認してください。ロールの割り当てが表示されますか？

<details>
  <summary>わからない場合は？</summary>
  
ロール割り当てはAzureの一般的な承認システムです。ポータルで：

- _アクセス制御 (IAM)_ を開きます
- _Roles_ に移動し、_SignalR App Server_ を選択して _View_ を選択します
- _Assignments_ の下にWebアプリが表示されるはずです

</details><br/>

ロール割り当てには数分かかる場合がありますが、再起動を必要とせずにアプリが動作し始めます。

## Lab

SignalRには他にはあまりない便利なデバッグツールがあります。Portalでメッセージのトレースを設定し、トレースツールを開いてみてください。さらにブラウザウィンドウでチャットサイトを開いてメッセージを送信します。トレースには何が表示されますか？SignalR接続の詳細を持っていれば、サイトをハッキングしてすべてのユーザーにメッセージをブロードキャストできるでしょうか？

> 行き詰まっていますか？[ヒント](hints_jp.md)を試すか、[ソリューション](solution_jp.md)を確認してみてください。

___

## クリーンアップ

すべてのリソースを削除するには、このラボ用のRGを削除できます：



```
az group delete -y --no-wait n labs-signalr
```
