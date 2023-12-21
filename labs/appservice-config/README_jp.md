# App Service の設定と管理

App Service は PaaS であり、IaaS デプロイメントで実装するのに多大な労力が必要な機能を提供します。アプリケーションには異なる設定が必要であり、App Service では手動でログインしてファイルを編集することなく設定を行うことができます。また、App Service はアプリケーションのヘルスを監視し、不健康なインスタンスを再起動することができます。

このラボでは、繰り返し失敗を引き起こす設定でランダム数生成器 REST API をデプロイし、App Service がアプリをオンラインに保つ方法を確認します。

## 参考文献

- [App Service ヘルスチェック](https://docs.microsoft.com/ja-jp/azure/app-service/monitor-instances-health-check?tabs=dotnet)

- [Web アプリの設定と環境変数](https://docs.microsoft.com/ja-jp/azure/app-service/reference-app-settings?tabs=kudu%2Cdotnet)

- [`az webapp config` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/webapp/config?view=azure-cli-latest)

## 失敗するアプリケーションのデプロイ

📋 まず、`src/rng/Numbers.Api` フォルダ内の .NET 6.0 アプリケーションを新しいリソースグループの新しい App Service プランにデプロイします。単一ワーカーで基本 SKU を使用します。

<details>
  <summary>方法がわからない場合はこちら</summary>

RG に新しいものはありません：



```
az group create -n labs-appservice-config --tags courselabs=azure
```


`webapp up` ショートカットコマンドで App Service プランを作成できますが、SKU は指定できますが、ワーカー数は指定できません：


```
az webapp up --help
```


コマンドはソースコードフォルダから実行する必要があります：


```
cd src/rng/Numbers.Api

az webapp up -g labs-appservice-config --plan app-plan-01 --os-type Linux --sku B1 --runtime dotnetcore:6.0 -n <api-dns-name>
```

</details><br/>

App Service プランの詳細を確認します：


```
az appservice plan show -g labs-appservice-config -n app-plan-01
```


現在のワーカー数が 1（デフォルト）で、最大ワーカー数が 3 であることが確認できるはずです。

curl でアプリをテストします：



```
curl https://<api-fqdn>/rng
```


> 最初の応答はアプリがウォームアップする間に 1 分かかるかもしれませんが、その後の呼び出しは速くなるはずです。

ソースコードからデプロイされたデフォルトのアプリ設定を確認してください：

- [src/rng/Numbers.Api/appsettings.json](/src/rng/Numbers.Api/appsettings.json) - ランダム数生成の範囲を設定できます。また、使用後に API が失敗するようにする設定もあります

📋 Azure でデフォルトの設定を上書きします。設定キーは `Rng__FailAfter__CallCount`、値は `3` です。

<details>
  <summary>方法がわからない場合はこちら</summary>

ポータルの App Service の _設定_ ページでこれを行うことができます。

または CLI を使用します：



```
az webapp config appsettings set --settings Rng__FailAfter__CallCount='3' -g labs-appservice-config -n <api-dns-name>
```

</details><br/>

> CLI を使用すると、デプロイ時に作成されたアプリ設定と一緒に、新しい設定が出力に表示されます。

curl リクエストを繰り返し、アプリが失敗するまで何回呼び出す必要があるかを確認します。



```
curl https://<api-fqdn>/rng
```


3回目の呼び出し後にエラーメッセージが表示されます。このアプリのインスタンスは現在失敗状態にあり、自己修復することはありません。API にはヘルスエンドポイント（非常に便利な機能）もあり、正常に動作しているかどうかを確認するために使用できます：


```
# Windows で使用している場合は curl.exe を使うべきです
curl -v https://<api-fqdn>/healthz
```


`-v` フラグは追加の出力を表示し、HTTP ステータスコードを含みます。コード 500 は _Internal Server Error_ を意味し、これを使用して Azure でアプリのヘルスをチェックできます。

## App Service ヘルスチェックの追加

これをポータルで行いますので、設定する必要がある内容を簡単に確認できます。App Service を開き、_ヘルスチェック_ タブを開きます（_監視セクション_ の下にあります）：

- ヘルスチェックを有効にするためにクリックします
- パスとして `/healthz` を入力します - これは API のヘルスエンドポイントです
- ロードバランシングの閾値を最小値に減らします

変更を保存します - アプリが再起動されるため、確認が求められます。今度は _メトリックス_ タブを開きます - HTTP 応答コードやヘルスチェックのステータスのグラフを選択できます。

これは新しいアプリのインスタンスなので、今は健全です。もう一度ヘルスエンドポイントを試してみてください。今回は 200 の応答が表示されるはずです：



```
# Windows で実行する場合は curl.exe を使用する必要があります
curl -v https://<api-fqdn>/healthz
```

ランダム数をいくつか取得します。新しいインスタンスは同じ設定を使用しているため、3回の呼び出し後、アプリは再び失敗します：


```
curl https://<api-fqdn>/rng
```


> ポータルを確認してください - _Http Server Errors_ メトリックスにピークが見られます

_概要_ タブでは、アプリが不健全であるという赤いバーが上部に表示され、単一インスタンスのため再起動されないことが示されています。Azure は慎重なアプローチを取り、不健全であっても単一のインスタンスを交換しません。交換すると、新しいインスタンスがオンラインになる間ダウンタイムが発生する可能性があります。

## アプリのスケールアップ

複数のインスタンスを実行すると、アプリが処理できるリクエストの量が増え、可用性も向上します - 一つのインスタンスが失敗した場合、すべての着信トラフィックが他のインスタンスに向けられます。

📋 App Service プランをスケールアップして、2つのインスタンスを持つようにします

<details>
  <summary>方法がわからない場合はこちら</summary>

ポータルの App Service プランの _スケールアウト_ タブで、または CLI でこれを行うことができます：



```
az appservice plan update -g labs-appservice-config -n app-plan-01 --sku B1 --number-of-workers 2
```


</details><br/>

新しいインスタンスがオンラインになるまでに数分かかります。準備ができると、それは健全であり、以前のインスタンスは不健全になります。ヘルスエンドポイントを確認してください。新しいインスタンスが準備ができると、すべてのトラフィックがそこに向けられます：


```
curl https://<api-fqdn>/healthz
```


今、ランダム数のいくつかの呼び出しを行うと、唯一健全なインスタンスもすぐに不健全になります：


```
curl https://<api-fqdn>/rng
```


両方のインスタンスが不健全になりますが、最も最近失敗したものがすべてのトラフィックを受け取ります。App Service は失敗したインスタンスを再起動しますが、それには 1 時間待ちます...

## ラボ

待つ代わりに、失敗したインスタンスの再起動をトリガーする自動ヒーリングを設定できます。30秒以内に 500 エラーが発生した場合に、API インスタンスが交換されるように App Service を更新します。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

RG を削除してクリーンアップします：



```
az group delete -y -n labs-appservice-config
```
