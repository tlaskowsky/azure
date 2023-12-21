# 分散アプリ用の App Service

一つの App Service プランでは、複数の App Service を実行できます。これはコンピューティングリソースを共有する良い方法で、一つのインフラストラクチャセットに対して支払いを行い、分散アプリケーションの複数のコンポーネントを実行するために使用できます。コンポーネントを連携させることが興味深い点であり、各環境で使用するアドレスは変わります。App Service は、アプリケーションにプッシュされる App Service の設定値を管理する良い方法を提供します。

このラボでは、同じサービスプラン内の2つのアプリケーションとして、WebフロントエンドとバックエンドREST APIをデプロイし、それらが互いに通信できるように設定します。

## 参考資料

- [App Service 設定設定](https://learn.microsoft.com/ja-jp/azure/app-service/configure-common?tabs=portal)

- [.NET 6.0 での設定ソース](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0)

- [`az webapp up` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/webapp?view=azure-cli-latest#az-webapp-up)

- [`az appservice plan update` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/appservice/plan?view=azure-cli-latest#az-appservice-plan-update)

## API のデプロイ

まずは REST API から始めます。これはランダム数生成器なので、実行できれば HTTP GET リクエストを送信して応答でランダム数を見ることができるはずです。

📋 リソースグループと App Service プランを作成します。プランは Linux を使用し、B1 SKU と 2 つのワーカーを設定します。

<details>
  <summary>方法がわからない場合はこちら</summary>

通常のコマンドで RG を作成します：

```
az group create -n labs-appservice-api --tags courselabs=azure -l southeastasia 
```

App Service プランでは、ヘルプテキストを表示してオプションを確認します：



```
az appservice plan create --help
```


必要な設定で作成します：



```
az appservice plan create -g labs-appservice-api -n app-plan-01 --is-linux --sku B1 --number-of-workers 2  -l westus
```


</details><br/>

> すべての地域がすべての App Service プランの SKU をサポートしているわけではありません。選択した地域が B1 SKU をサポートしていない場合、`az` の出力で _Plan with requested features is not supported in current region_ というエラーが表示されます。

App Service プランを作成したら、`az webapp up` を使用してアプリをデプロイします。これは、ローカルデプロイメントでの Web アプリの作成を簡略化するショートカットで、App Service の作成とコードのアップロードを一つのコマンドで行います。


```
# API ソースコードがあるフォルダに移動：
cd src/rng/Numbers.Api

# 必要なランタイムを見つけます - これは .NET 6.0 アプリです：
az webapp list-runtimes --os-type=linux
```


📋 `az webapp up` を使用して、App Service プランに API コードをデプロイします。正しいアプリランタイムと OS を指定してください。

<details>
  <summary>方法がわからない場合はこちら</summary>

`webapp up` コマンドは、指定されたプランがない場合に App Service プランを作成します。既存のプランを指定する場合でも、OS とランタイムを設定する必要があります。また、ユニークな DNS 名も指定する必要があります：



```
az webapp up -g labs-appservice-api --plan app-plan-01 --os-type Linux --runtime dotnetcore:6.0 -l westus -n <api-dns-name>
```

</details><br/>

出力では、コマンドがソースコードフォルダの ZIP ファイルを作成し、アップロードする様子が表示されます。

新しい App Service をポータルで参照してください。_デプロイメントセンター_ を開き、現在のデプロイメントのログを確認します。「Oryx build」ログの下で .NET コンパイラの出力を見ることができます。

CLI からデプロイメントログの概要を出力することもできます：



```
az webapp log deployment show -g labs-appservice-api -n <api-dns-name>
```


完全な DNS 名を見つけ、`https://<api-fqdn>/swagger` の App Service にアクセスします。そこではランダム数生成器の API ドキュメントが見られます（これは [Swagger](https://swagger.io) を使用しており、REST API の文書化に標準的なツールです）。

Swagger ドキュメントをナビゲートして `rng` サービスを呼び出すことができます。これはランダム数を返します。コマンドラインから curl を使用してこれを行うこともできます：



```
curl https://<api-fqdn>/rng
```


> 各呼び出しで異なるランダム数が表示されるはずです

## ウェブサイトのデプロイ

API を使用する Web フロントエンド（.NET 6.0）もあります。これを同じアプリサービス内の別のアプリとしてデプロイできます：



```
cd ../Numbers.Web

az webapp up -g labs-appservice-api --plan app-plan-01 --os-type Linux --runtime dotnetcore:6.0 -l westus -n <web-dns-name> 
```

これもデプロイに数分かかります。実行中に、ポータルで API の App Service を確認してください。API の呼び出しを示すログを見つけることができますか？

Web アプリのデプロイが完了したら、ブラウザで Web URL を開きます。ランダム数アプリが表示されるはずです：

![ランダム数生成器の Web UI](/img/rng-web.png)

_Go!_ ボタンをクリックしてランダム数を取得すると、ウェブサイトにエラーが表示されます - _RNG サービス利用不可_。ウェブサイトは API と通信するために使用している URL も表示しており、問題の原因がここにあります。開発者設定でデプロイされているため、設定を更新する必要があります。

ポータルでウェブサイトの App Service を開き、_設定_ を開きます。デプロイに使用されるいくつかの設定が既に存在していますが、これらはアプリケーションには使用されていません。

API のデフォルト URL を上書きするために新しい _アプリケーション設定_ を追加します：

- キー: `RngApi__Url`
- 値: `https://<api-fqdn>/rng` (これは curl で使用した URL です、例えば私の場合は https://rng-api-es.azurewebsites.net/rng)

_保存_ をクリックして設定を更新します - アプリが再起動し、新しい設定を適用することを確認するための警告が表示されます。

もう一度 Web アプリにアクセスして、_Go!_ ボタンをクリックします - 今度は、Web ページに API からのランダム数が表示されるはずです。（.NET 開発者であれば、[設定ファイル](/src/rng/Numbers.Web/appsettings.json) でデフォルトの API URL を確認できます。App Service の新しい設定がアプリにどのように読み込まれるかを考えてみてください。）

## App Service プランのスケール

基本 B1 SKU と 2 つのインスタンスを使用して App Service プランを作成しました。この価格帯では最大 3 つのインスタンスまでスケールアップできるはずです：



```
# これを実行するとエラーが表示されます：
az appservice plan update -g labs-appservice-api -n app-plan-01 --number-of-workers 3
```

それは奇妙です。

📋 `az ... show` コマンドを使って App Service プランの詳細を出力し、SKU と現在のワーカー数を確認します。

<details>
  <summary>方法がわからない場合はこちら</summary>



```
az appservice plan show -g labs-appservice-api -n app-plan-01 
```


</details><br/>

表示される情報によると、プランは無料の F1 ティアにあり、最大で 1 つのワーカーが許可されています。しかし、間違いなく B1 SKU で 2 つのワーカーを持つように作成したはずです。プランが変更されたのは何故でしょうか？

プランを B1 SKU に戻し、3 つのワーカーを使用するように更新します：



```
az appservice plan update -g labs-appservice-api -n app-plan-01 --sku B1 --number-of-workers 3
```


> SKU の更新が先に行われ、最大ワーカー数が増えるため、その後でプランが 3 へスケールされます。

もう一度ブラウザで Web アプリを開き、ランダム数を取得します。API ログを確認してください - 異なるインスタンスがリクエストに応答していますか？プライベートブラウザウィンドウで履歴なしにウェブサイトを開いた場合、別の API サーバーからの応答が見られますか？curl を使用して直接 API を呼び出した場合はどうでしょうか？


## ラボ

ウェブサイトの別バージョンを静的 Web アプリとしてデプロイできます。これは `labs/appservice-api/spa` にある HTML ファイルです。`index.html` ファイルを更新してランダム数 API の URL を設定し、アプリを静的 Web アプリとしてデプロイします。実行できたら、アプリにアクセスしてボタンをクリックしますが、何も起こりません。

ブラウザの開発者ツールを開いて再試行すると、コンソールログにこのようなエラーが表示されます：

_「https://rng-api-es.azurewebsites.net/rng?=1659970485522」への XMLHttpRequest アクセスは、「https://rng-web-spa-es.azurewebsites.net」からのもので、CORS ポリシーによってブロックされています：プリフライトリクエストへの応答がアクセス制御チェックを通過していません：リクエストされたリソースに 'Access-Control-Allow-Origin' ヘッダーが存在していません。_

これはセキュリティ機能です - API は外部ドメインからの直接の呼び出しを許可しません。API App Service で設定する必要がある設定を見つけて、静的 Web アプリのドメインからの呼び出しを許可します。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

RG を削除してクリーンアップします：



```
az group delete -y -n labs-appservice-api --no-wait
```
