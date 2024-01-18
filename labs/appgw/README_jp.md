# アプリケーションゲートウェイとウェブアプリケーションファイアウォール

アプリケーションゲートウェイは[レイヤー7ロードバランサー](https://www.nginx.com/resources/glossary/layer-7-load-balancing/)です - これはドメイン名とURLパスを使用して、受信HTTPリクエストに基づいてトラフィックをルーティングします。バックエンドは健全であることが確認され、健全なインスタンス間でトラフィックが共有されます。

ウェブアプリケーションファイアウォール（WAF）はApp Gatewayのオプション機能であり、強力なセキュリティツールです。WAFはHTTPコールのヘッダーとボディを検査し、悪意のあるペイロードを探します。攻撃はWAFレイヤーで防がれるため、バックエンドサービスには到達しません。

このラボでは、WAFを備えたApp Gatewayをデプロイし、Azureコンテナインスタンスで実行されているいくつかのウェブアプリケーションのフロントエンドとして使用します。

## 参照

- [App Gateway 概要](https://learn.microsoft.com/en-us/azure/application-gateway/overview)

- [ウェブアプリケーションファイアウォール概要](https://learn.microsoft.com/en-us/azure/web-application-firewall/overview)

- [アプリケーションゲートウェイでAPI管理をフロントエンドする](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-integrate-internal-vnet-appgateway)

- [502 Bad Gatewayエラーのトラブルシューティング](https://learn.microsoft.com/en-us/azure/application-gateway/application-gateway-troubleshooting-502)


## アプリケーションゲートウェイの作成

[AKS Ingressラボ](/labs/aks-ingress/README_jp.md)でApp Gatewayを探索しましたので、CLIで作成に直行します。

RGとネットワークの事前条件から始めます：



```
# rg:
az group create -n labs-appgw --tags courselabs=azure -l eastus

# pip:
az network public-ip create -g labs-appgw -n appgw-pip --sku Standard -l eastus --dns-name <unique-dns-name>

# vnet:
az network vnet create -g labs-appgw -n vnet --address-prefix 10.4.0.0/16 -l eastus

# subnet:
az network vnet subnet create -g labs-appgw --vnet-name vnet -n appgw --address-prefixes 10.4.10.0/24
```


> アプリケーションゲートウェイはvnet内にデプロイする必要があります

現在、アプリケーションゲートウェイ（AppGW）を作成しましょう - 私たちはWebアプリケーションファイアウォール（WAF）機能を使用するつもりですので、[OWASPトップ10](https://owasp.org/www-project-top-ten/)の背後にある組織からのOWASPルールを実装するWAFポリシーから始めます。


```
# 最新の3.2ルールセットを使用してWAFポリシーを作成
az network application-gateway waf-policy create -n appg-waf  -g labs-appgw  --type OWASP --version 3.2 -l eastus

# ポリシーを有効にし、予防モードに設定
az network application-gateway waf-policy policy-setting update --mode Prevention --policy-name appg-waf -g labs-appgw --request-body-check  true --state Enabled 
```

> WAFは_検出_モードまたは_予防_モードで動作可能です。

予防モードは、WAFのルールに基づいて怪しいと見なされるすべての着信コールをブロックします。場合によっては誤検知が発生することがあるため、WAFがアプリケーションを中断させ、いくつかのルールを緩和する必要があります。通常、予防モードで開始し、違反を監視します。

次に、WAF SKUを使用してアプリケーションゲートウェイを作成します：



```
az network application-gateway create -g labs-appgw -n appgw  --public-ip-address appgw-pip --vnet-name vnet --subnet appgw --capacity 1 --sku WAF_v2 --priority "1" --waf-policy appg-waf -l eastus
```


これには時間がかかります。ポータルで進行状況を確認してくださいが、作成中にアプリケーションゲートウェイのフロントとなるバックエンドサービスをデプロイできます。

## バックエンドACIコンテナの作成

ウェブアプリを迅速にデプロイする方法としてAzureコンテナインスタンスを使用します（これは[ACIラボ](/labs/aci/README_jp.md)でカバーしました）。

ACIは水平方向にスケールしませんので、単純なウェブアプリ用に2つの別々のコンテナを実行します：



```
az container create -g labs-appgw --name simple-web-1 --image courselabs/simple-web:6.0 --ports 80 --ip-address Public --no-wait

az container create -g labs-appgw --name simple-web-2 --image courselabs/simple-web:6.0 --ports 80 --ip-address Public --no-wait
```


そして、Piウェブアプリ用のもう一つ：


```
az container create -g labs-appgw --name pi-0 --image  kiamol/ch05-pi --ports 80 --ip-address Public --command-line "dotnet Pi.Web.dll -m web" --no-wait
```

コンテナが実行されたら、各ウェブアプリをテストし、IPアドレスをメモしておきます。

## アプリケーションルーティングの設定

ポータルでAppGWを開きます。PIPからの公開URLにアクセスすると、`502`エラーが表示されます。これはルーティングの問題であり、リクエストを処理できるバックエンドへのパスがないことを意味します。

ウェブアプリケーションの設定を行います - これには正しい値でいくつかの設定が必要です。偽のドメイン名を使用します。

_単純なウェブアプリのために作成する設定：_

- ドメイン名`simple.appgw.azure.azureauthority.in`のリスナー
- 2つのsimple-web ACI IPアドレスを含むバックエンドプール
- リスナーとプールをリンクするルール

_Piアプリのために作成する設定：_

- ドメイン名`pi.appgw.azure.azureauthority.in`のリスナー
- Pi ACI IPアドレスを含むバックエンドプール
- リスナーとプールをリンクするルール

_デフォルトルールの優先度を下げる：_

- デフォルトルールは単一サイトルールです
- これは複数サイトのルールが評価されないようにします
- 他のルールよりも高い番号にデフォルトの`rule1`の優先度を変更します

偽のドメインをホストファイルに追加します **AppGWのIPアドレスを指すようにします**：



```
# WindowsでPowershellを使用する場合 - 管理者として実行する必要があります：
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force
./scripts/add-to-hosts.ps1 pi.appgw.azure.azureauthority.in <appgw-ip>
./scripts/add-to-hosts.ps1 simple.appgw.azure.azureauthority.in <appgw-ip>

# macOSまたはLinuxで - sudoパスワードを聞かれます：
sudo chmod +x ./scripts/add-to-hosts.sh
./scripts/add-to-hosts.sh pi.appgw.azure.azureauthority.in <appgw-ip>
./scripts/add-to-hosts.sh simple.appgw.azure.azureauthority.in <appgw-ip>
```


> または*nixでは`/etc/hosts`、Windowsでは`C:\windows\system32\drivers\etc\hosts`を編集します

両方のウェブアドレスを試してみてください - アプリが表示され、simple-webアプリはACIコンテナ間で複数のリクエストをロードバランスするはずです：

- http://simple.appgw.azure.azureauthority.in
- http://pi.appgw.azure.azureauthority.in

## ウェブアプリケーションファイアウォールのテスト

WAFのルールをいくつかテストし、悪意のあるリクエストがブロックされることを確認します。アプリから直接のレスポンスとAppGW WAFからのレスポンスを比較します。

これは[SQLインジェクション](https://owasp.org/www-community/attacks/SQL_Injection)攻撃の試みです：



```
# simple-webコンテナのIPアドレスで試してみてください：
curl "http://<container-ip>/?id=1;select+1,2,3+from+users+where+id=1--"
```


標準的な200レスポンスとHTML出力が表示されますが、これはハッカーにSQLインジェクションがチェックされていないことを示し、攻撃の手段を提供します。

AppGW WAFを通じて同じアプリで同じ攻撃を試してみてください：



```
curl "http://simple.appgw.azure.azureauthority.in/?id=1;select+1,2,3+from+users+where+id=1--"
```


今回は403禁止レスポンスが表示されます。WAFがバックエンドへのリクエストをブロックします。

より徹底的なテストを行いたい場合は、コンテナで実行できる[GoTestWAF](https://github.com/wallarm/gotestwaf)ツールを試してみてください。これはドメインに対して何百もの攻撃を試みます - **自分が所有するドメインでのみ使用してください**。

Docker Desktopを起動し、AppGWのIPアドレスを使用してこのコマンドを実行します：



```
docker run --add-host simple.appgw.azure.azureauthority.in:<app-gw-ip> sixeyed/gotestwaf:2211 --noEmailReport --url http://simple.appgw.azure.azureauthority.in --skipWAFIdentification --skipWAFBlockCheck  --testSet owasp
```


> しばらく時間がかかりますが、500以上のテストケースがWAFによって正常にブロックされるのを見ることができるはずです。

GitHubリポジトリからツールが試みる攻撃について詳しく調べることができます。

## ラボ

AppGWはURLパスに基づいてルーティングすることができるため、同じドメイン内のルートは異なるバックエンドに送信されます。そのためのルーティング設定のどの部分を使用しますか？

> 詰まったら、[提案](suggestions_jp.md)を試してみてください。
___

## クリーンアップ

このラボからRGを削除することができます：



```
az group delete -y --no-wait -n labs-appgw
```
