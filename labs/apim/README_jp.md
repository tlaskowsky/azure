# API Management（API 管理）

API Management（APIM）は、パブリック API またはビジネス パートナー向けの外部 API であれ、消費者に HTTP API を提供するためのフルサービスです。API Management は 4 つのコア機能を提供します：API の操作を記述しバージョン管理する API デザイナー、バックエンド API ホストへの着信トラフィックをルーティングし、リクエストやレスポンスを変更できるゲートウェイ、API のユーザーとして自らを登録できる開発者ポータル、API アクセスを制限する統合セキュリティです。

非常に強力なサービスで、さらに多くの機能を探求します。このラボでは APIM の基本を紹介します。サービスのインスタンスを作成するには 60 分以上かかる場合があるため、事前に行うことをお勧めします。

## 参照

- [API Management 概要](https://learn.microsoft.com/en-us/azure/api-management/api-management-key-concepts)

- [Web Apps & APIM 間の相互 TLS](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-mutual-certificates)

- [APIM のための仮想 IP アドレス](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-ip-addresses)

- [`az apim` コマンド](https://learn.microsoft.com/en-us/cli/azure/apim?view=azure-cli-latest)

## 新しい API Management リソースの作成と探索

APIM の作成には時間がかかるため、CLI を通じて作成します：



```
az group create -n labs-apim --tags courselabs=azure -l westus

az apim create --no-wait --sku-name Developer -g labs-apim -n <apim> --publisher-name <company-name> --publisher-email <real-email-address> 
```

> 開発者向けレベルは安価で運用が可能です（現在は $0.07/時間）し、探索用としては適していますが、実際の環境には使用できません（SLAがありません）

APIMインスタンスが使用可能になったら、メールが送信されます。その後、ポータルで開くことができます：

- 概要にはゲートウェイURL、管理URL、開発者ポータルURLがあります
- 左側のナビゲーションには _APIs（API群）_、_Products（製品）_、_Subscriptions（サブスクリプション）_ があります

一つのAPIMリソースで複数のAPIをホストできます。_Products（製品）_ は、ユーザーが登録できるビジネスユニットであり、製品を通じて一つ以上のAPIへのアクセスを提供します。_Subscriptions（サブスクリプション）_ は、製品にサインアップしたユーザーを表示します。

## バックエンドAPIのデプロイ

APIMはAPIのホスティングサービスではありません。実際のAPIロジックをAzure内外の別のサービスにデプロイする必要があります。APIMにバックエンドとして追加することで、サービスは着信コールのルーティング先を知ることができます。

WebアプリをAPIバックエンドとして使用します - これはFunction App、Logic App、またはカスタムURLでも構いません。

ランダムナンバージェネレータAPIをWebアプリとしてデプロイします：



``` 
# APIソースコードがあるフォルダに切り替えます：
cd src/rng/Numbers.Api

az webapp up -g labs-apim --os-type Linux --sku B1 --runtime dotnetcore:6.0 -l westus -n <webapp-name>
```


そのAPIは独自のドキュメントをホストしており、APIの仕様が含まれています。ドキュメントは `http://<webapp-url>/swagger` で閲覧できます。

> SwaggerはREST APIドキュメント用のオープンソースツールです。

また、標準の [OpenAPI仕様](https://www.openapis.org) を使用してJSONでドキュメントを公開します。JSON仕様は `https://<webapp-name>.azurewebsites.net/swagger/v1/swagger.json` で確認できます。

APIMはOpenAPI仕様をインポートできます：

- ポータル内の _APIs（API群）_ ブレードを開く
- _Add API（APIを追加）_ をクリック
- _Import from（インポート元）_ _OpenAPI（オープンAPI）_ を選択
- Webアプリの `swagger.json` のURLを入力
- 名前と表示名を追加

APIがデザイナーで開き、APIMのさらなる探索が可能になります。

## APIの設定

APIMの探索には時間がかかります。主要な機能の一つは、APIの実際のロジックにインバウンドおよびアウトバウンドの処理ポリシーを追加する機能です。

ランダムナンバージェネレータAPIを以下のように設定します：

- `/rng` 操作のレスポンスは30秒間キャッシュされます - これにより実際のAPIへの負荷が軽減されます
- `/reset` 操作は自分のIPアドレスからのみアクセス可能です（公開IPアドレスを確認するには https://ifconfig.me を参照） - これは制限すべき管理機能です
- `/healthz` 操作は常に 404 を返します - これは内部のヘルスチェックエンドポイントであり、公開すべきではありません

📋 デザイナーを通じて各エンドポイントをテストし、ポリシーが期待通りに機能しているかを確認します。

<details>
  <summary>ヘルプが必要ですか？</summary>

各操作に _入力処理_ ポリシーを追加する必要があります。IPアドレスのフィルタリングやレスポンスのキャッシュは、UIで見つけられる標準的なポリシーです。

APIを呼び出す代わりにカスタムレスポンスを返すには、_その他のポリシー_ にエントリを追加する必要があります - これは少々扱いにくいXMLビューですが、必要な機能を追加するスニペットがあります。XML内の正しい位置にスニペットを追加してください。

</details><br/>

現在、APIは公開されていませんが、初期設定のテストには問題ありません。設定に満足したら、APIを公開できます。

## APIおよび開発者ポータルを公開する

APIを公開するためのいくつかのステップがあります。

API自体は _設定_ で設定する必要があります：

- _WebサービスURL_ はバックエンドWebアプリのURLである必要があります
- APIを _製品_ に追加して利用可能にする必要があります
- APIMには2つのデフォルト製品があり、それらのうちの1つを使用するか、独自のものを作成できます

_開発者ポータル_ リンクはデザイナービューを開きます：

- 自社の会社名を追加
- UIをパーソナライズ

次に、_ポータル概要_ の下で開発者ポータルを設定できます：

- ADアカウントの使用を許可してサインアップ
- CORSを有効にする
- ポータルを公開

これで開発者ポータルが利用可能になり、新しいユーザーがサインアップしてAPIを使用できます。

## 顧客としてサインアップ

プライベートブラウザセッションを使用して開発者ポータルを開き、アカウントにサインアップします - 実際のメールアドレスと強力なパスワードを提供する必要があります。確認メールが送信され、リンクをたどる必要があります。

> これらはすべて、APIMサービスでホストされているデフォルトのポータルロジックです

ユーザーとしてログインし、開発者ポータルを探索します。

ランダムナンバージェネレータAPIの呼び出しを見つけ、テストページで試してみてください - 失敗するでしょう。サブスクリプションキーが必要です。

そのためには、製品に移動してサブスクリプションを追加する必要があります（APIに対して有効にした製品からユーザーは選択できます。例えば _Starter_ や _Unlimited_ など）。

これで、`/rng` 操作を呼び出すために使用できるサブスクリプションキーを取得します。

📋 APIをcurlで呼び出すことができますか？

<details>
  <summary>ヘルプが必要ですか？</summary>

テストページには、curlを含むAPIの呼び出し方法がいくつか示されています。コマンドは次のようになり、サブスクリプションキーが含まれます：


```
curl "https://<apim-name>.net/<api-name>/rng" -H "Ocp-Apim-Subscription-Key: <suscription-key>"
```


</details><br />

エンドポイントを繰り返し呼び出してみてください。キャッシュを正しく設定していれば、30秒間は同じランダムナンバーが得られるはずです。

異なる製品には異なる制限があります。_Starter_ 製品を使用した場合、分間に5回の呼び出しに制限されます。それを超えると、エラーレスポンスが返されます：


```
{ "statusCode": 429, "message": "Rate limit is exceeded. Try again in 51 seconds." }
```

> これらはコードを書くことなく得られる本格的な機能です

これがAPIMの強力な点です。APIをビジネスロジックに集中させ、インフラの懸念をAPIMに任せることができます。

## ラボ

APIMの機能についてほんの少し触れただけですが、考えるべきいくつかの質問があります：

- サインアップするユーザーに送信されるメールのテキストをカスタマイズできますか？
- すべてのクリックとポインティングはエラーが発生しやすいですが、APIM設定を自動化する方法は？
- エンドポイントポリシーはすべてAPIMによって適用されますが、バックエンドWebアプリはまだ公開されていますか？

> 詰まったら、[提案](suggestions_jp.md) を試してみてください。
___

## クリーンアップ

**まだクリーンアップしないでください！**

1つのAPIMインスタンスで複数のAPIをホストでき、次の数回のラボで同じリソースを使用します。削除して別のものを作成するのにまた1時間待つのではなく、そのままにしておきます。
