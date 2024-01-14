# デュラブルファンクション: 人間とのインタラクション

デュラブルファンクションの利点の一つは、アクティビティが完了するのを長期間待つことができ、そのアクティビティに機密データが含まれている場合、そのデータはファンクションの外部に保存されないため、攻撃者がそれにアクセスするのが非常に困難になります。これは、ワークフローがあるポイントまで実行され、その後、人間の入力を待つ状態で停止する、人間とのインタラクションに最適です。これにより、完全に自動化されたワークフローを構築することができますが、人間による承認ステップが含まれます。

このラボでは、Twilioバインディングを使用したデュラブルファンクションを使用して、ユーザーにSMSテキストメッセージを送信します。このファンクションは、ユーザーが返信するまで待機します。

## 参照

- [人間とのインタラクションのためのデュラブルファンクション](https://learn.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-overview?tabs=csharp#human)

- [Twilio ファンクションバインディング](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-twilio?tabs=in-process%2Cfunctionsv2&pivots=programming-language-csharp)

## 前提条件

TwilioはSMSメッセージを送信するためのサービスです。開発やプロトタイピングに十分な無料枠があります。いくつかの設定ステップが必要です：

> Twilioアカウントの設定から始めます - https://www.twilio.com/try-twilio

以下を使用する必要があります：

- 本当のメールアドレス - これは検証されます
- 本当の携帯電話番号 - これも検証されます
- しかし、支払い情報を入力する必要はありません。

アカウントページから _API キー & トークン_ を開き、認証情報をメモしてください - **これらを安全に保管してください**：

- あなたのTwilioアカウントのSID
- あなたのTwilioアカウントの認証トークン

![Twilio 認証トークン](/img/twilio-auth-token.png)

次に、Twilioの電話番号を作成する必要があります - これは、Azureファンクションからメッセージを送信するときに表示される送信者の番号になります。

- 上部メニューのジャンプボックスに「番号を購入」と入力します
- _機能_ のすべてのチェックを外して「SMS」のみを選択し、「検索」をクリックします
- いずれかの番号を選び、「購入」をクリックします

> この料金は無料クレジットから差し引かれます

![Twilio 番号購入](/img/twilio-buy-number.png)

新しい番号をメモして準備完了です！



## HTTPトリガーとオーケストレーション

シナリオはシンプルな2要素認証で、コードを含むテキストメッセージがユーザーに送信され、続行するためにはそのコードを入力する必要があります。私たちはオーケストレーショントリガーで得られる標準のHTTPコールを使用しますが、これらは簡単に良いWeb UIでラップアップすることができます。

コードは `2FA` フォルダにあります：

- [2FA/Authenticate.cs](/labs/functions-durable/human/2FA/Authenticate.cs) - URLのパラメーターとして検証する電話番号を含むPOSTを期待するHTTPトリガーを使用します。

- [2FA/SmsVerify.cs](/labs/functions-durable/human/2FA/SmsVerify.cs) - Twilioアクティビティを呼び出してSMSメッセージを送信し、タイマーを開始するオーケストレーターです。ユーザーは2分以内に返信する必要があり、そうでない場合は認証が失敗します。

- [2FA/SmsChallenge.cs](labs/functions-durable/human/2FA/SmsChallenge.cs) - 実際にTwilioを呼び出し、ユーザーにランダムなコードを生成してSMSメッセージを送信するアクティビティファンクションです。

Twilioは電話番号にメッセージを送信するためだけに使用されます。ファンクションはユーザーの応答コードを含むステータス更新を送信する必要があり、ユーザーが認証されたかどうかを決定するのはファンクションロジックです。

## ローカルでのファンクションテスト

このファンクションの依存関係は標準のストレージアカウントのみです。

Docker Desktopを実行し、Azureストレージエミュレーターを開始します：



```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


Twilioの詳細を含むローカル設定ファイルが必要ですので、`labs/functions-durable/human/2FA/local.settings.json` にテキストファイルを作成し、標準の設定を追加します。**[E.164形式](https://support.twilio.com/hc/en-us/articles/223183008-Formatting-International-Phone-Numbers)を使用します - つまり、番号はプラス記号で始まり、次に国コード、次に番号が続きます - 例：+447412972480**。


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "TwilioAccountSid": "<your-twilio-account-SID>",
        "TwilioAuthToken": "<your-twilio-auth-token>",
        "TwilioPhoneNumber": "<the-twilio-phone-number-you-bought>"
    }
}
```


> GitHubに誤ってJSONファイルをプッシュしないように注意してください。Twilioはすべての公開リポジトリを監視しており、どこかで認証情報を見つけた場合、認証トークンを再生成して恥ずかしいメールを送ります。

ローカルでファンクションを実行します：



```
cd labs/functions-durable/human/2FA

func start
```


通常のスタートアップログとリストされたファンクションが表示されます。

ワークフローを開始するためにHTTP POSTリクエストを行います - **自分の携帯番号を使用してください** これによりTwilioは、あなたが購入した番号からあなた自身の番号にSMSメッセージを送信します **E.164形式を使用してください**。

例えば、イギリスの国際ダイヤルコードは44なので、私の番号が07654 123123だった場合、`+447654123123`を使用します：


```
# Windowsでは curl.exe を使用
curl -XPOST http://localhost:7071/api/Authenticate?number=+447654123123
```


4桁のコードを含むテキストメッセージが届きます（ワクワクしますね！）、そして次のようなログが表示されます：


```
[2022-11-14T16:13:49.544Z] Starting SmsChallenge for: + 44xxx
[2022-11-14T16:13:49.549Z] Executed 'SmsVerify' (Succeeded, Id=8feb5637-523c-46c4-9fae-262beab6da05, Duration=14ms)
[2022-11-14T16:13:49.586Z] Executing 'SmsChallenge' (Reason='(null)', Id=3d9aa8a2-8c3a-4ef0-807f-6f853e44c90c)
[2022-11-14T16:13:49.587Z] Sending verification code 4091 to + 44xxx.
```


次に、取得したコードを確認するために、ファンクションにステータス更新を送信する必要があります。HTTPトリガーへのcurlリクエストの応答には、オーケストレーションインスタンスへのイベント送信用のURLを含むフィールドが含まれていました：


```
"sendEventPostUri":"http://localhost:7071/runtime/webhooks/durabletask/instances/eb9fa85442254eb8af7de25efaca5dda/raiseEvent/{eventName}?taskHub=TestHubName&connection=Storage&code=_umg_d2m6RKVVzHbDKM9xmWQjIkhVazcg01c5nKIlMxGAzFulTbm8Q=="
```


`eventName` を `SmsChallengeResponse` に設定して、SMSメッセージからのコードを確認するためのcurlリクエストを使用してください：


```
# Windowsでは curl.exe を使用

curl -XPOST -d <your-sms-code>  -H 'Content-Type: application/json' "http://localhost:7071/runtime/webhooks/durabletask/instances/<id-from-url>/raiseEvent/SmsChallengeResponse?taskHub=TestHubName&connection=Storage&code=<code-from-url>"
```


> これは少し手間がかかるかもしれませんが、構文を正しく取得するまでにいくつかの試行が必要です。あなたには5分の時間があります :)

ファンクションログに、認証コードが正しいか、または時間内に応答しなかった場合が表示されます：



```
[2022-11-14T16:13:50.184Z] Executed 'SmsVerify' (Succeeded, Id=eda56e78-a907-496f-81b3-fab326cd0785, Duration=6ms)
[2022-11-14T16:14:25.583Z] Executing 'SmsVerify' (Reason='(null)', Id=51088b64-54f3-40b7-9069-61b48ea596a8)
[2022-11-14T16:14:25.584Z] Starting SmsChallenge for: + 44xxx
[2022-11-14T16:14:25.585Z] Authorized! User responded correctly to SmsChallenge for: + 44xxx
```


また、`statusQueryGetUri` フィールドにあるURLを呼び出してステータスを確認することができます。出力は、あなたが正しく認証された場合に `true` と表示されます。

オーケストレーションとの作業に関してUXが期待に沿わないかもしれませんが、Azureでこれをデプロイしてテストする価値があります :)

## Azureへのデプロイ

これがあなたのFunction Appのセットアップです：



```
az group create -n labs-functions-durable-human --tags courselabs=azure -l eastus

az storage account create -g labs-functions-durable-human --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-durable-human  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


Function Appの設定にTwilioの詳細を設定する必要があります（ローカルの設定JSONから同じ値を使用）：

- `TwilioAccountSid`
- `TwilioAuthToken`
- `TwilioPhoneNumber`

**依存関係はありません** - トリガーやバインディングに外部サービスは使用されていないので、すぐにデプロイを進めることができます：



```
func azure functionapp publish <function-name>
```


HTTPトリガーを使用して機能をテストします - パラメータとして電話番号を追加する必要があります。応答には、状態を確認しイベントを送信するための通常のURLが含まれていますが、これらの呼び出しをポータルで行うための便利なものは何もありません。URLを構築してcurlを使用する必要があります。

## ラボ

人間のインタラクション機能のためにHTTPトリガーを使用する必要があります。これにより、消費者がイベントを投稿できるステータスワークフロー（ユーザーがコードを入力したときなど）が得られます。通常、これはすべてWeb UIで処理されますが、ウェブサイトがユーザーが認証されたかどうかを確認するために状態エンドポイントをポーリングし続ける必要がないようにする設計方法は？

> 行き詰まったら、[私の提案](suggestions_jp.md)を試してください。

___

## クリーンアップ

Azure Storageエミュレータを停止します：



```
docker rm -f azurite
```


ラボのリソースグループを削除します：


```
az group delete -y --no-wait -n labs-functions-durable-human
```
