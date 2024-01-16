# Azure Front Door

Azure Front Doorは、Application Gatewayのように振る舞いますが、グローバルCDNも提供しています。同じWAF機能を統合し、DDOS保護を提供します。

これは、さまざまなロードバランシングおよびCDNサービスの進化であり、現在はHTTPサービスのフロントエンドに適したオプションです（Web AppsやAPI Managementドメインを含むことができます）。

このラボでは、WAFとともにFront Doorを作成および設定します。

## 参照

- [Front Door 概要](https://learn.microsoft.com/ja-jp/azure/frontdoor/front-door-overview)

- [Azure Load Balancersの選択](https://learn.microsoft.com/ja-jp/azure/architecture/guide/technology-choices/load-balancing-overview?toc=%2Fazure%2Ffrontdoor%2Fstandard-premium%2Ftoc.json)

- [`az afd` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/afd?view=azure-cli-latest)

## Explore & create Front Door

ポータルで新しいリソースを作成し、「front door」と検索します - _Front Door and CDN profiles_ を選択し _Create_ をクリックし、_Quick Create_ を選びます：

- レイヤーの選択肢はパフォーマンス**または**セキュリティを優先します
- _Premium_ レイヤーを選択し、WAFポリシーを選ぶ必要があります
- Front Doorは多くのオリジンをサポートしています - ACI、APIM、AppGWを含む
- チェックボックスをオンにするだけで静的リソースにキャッシングを追加できます

CLIを使用してFront Doorを作成します。

リソースグループとFront Doorプロファイルから始めます：



```
az group create -n labs-frontdoor --tags courselabs=azure -l eastus

az afd profile create --profile-name labs -g labs-frontdoor --sku Premium_AzureFrontDoor
```


Front Doorにはフロントするバックエンドが必要です - 東部と西部の異なるリージョンで同じアプリを実行するACIコンテナを使用します：


```
az container create -g labs-frontdoor --name simple-web-1 --image courselabs/simple-web:6.0 --ports 80 --no-wait -l eastus --dns-name-label <app1-dns-name>

az container create -g labs-frontdoor --name simple-web-2 --image courselabs/simple-web:6.0 --ports 80 --no-wait -l westus --dns-name-label <app2-dns-name>
```

コンテナが起動している間、ポータルでFront Doorプロファイルを開きます：

- _Front Door manager_ は、アプリのエントリーポイントであるサブドメイン（またはカスタムドメイン）を作成する場所です
- _Origin groups_ はバックエンドです - 各エンドポイントはオリジングループを参照し、各オリジングループには複数のオリジン（実際のアプリケーションホスト）を持つことができます
- _Routes_ は、フロントエンドのエンドポイントをバックエンドのオリジングループにリンクします
- _Rule sets_、_Security policies_、_Optimizations_ により、個々のルートの処理動作をカスタマイズできます

## ACIバックエンドをオリジンとして設定

各アプリケーションホスト（ACIコンテナ、Webアプリ、VMなど）は、Front Door内で _origin_ として作成されます。オリジンは _origin group_ に属します - グループはすべてのインスタンスで共有される設定を定義します。

シンプルなウェブアプリ用のオリジングループを作成します：



```
az afd origin-group create -g labs-frontdoor --origin-group-name simple-web --profile-name labs --probe-request-type GET --probe-protocol Http    --probe-interval-in-seconds 30 --probe-path /  --sample-size 4    --successful-samples-required 3 --additional-latency-in-milliseconds 50
```


> これらはすべて必要な設定です

オリジングループレベルでは、健康パラメーターを定義します - どのURLパスでテストを行うか、テストの頻度、健全と判断するための成功した応答の数、許容可能な追加遅延。

次に、最初のACIコンテナをグループにオリジンとして追加します：


```
# コンテナ1のFQDNを表示：
az container show -g labs-frontdoor --name simple-web-1 --query 'ipAddress.fqdn'

# FQDNを使用してコンテナ1をオリジンとして追加：
az afd origin create -g labs-frontdoor --profile-name labs --origin-group-name simple-web --origin-name container1 --priority 1 --weight 300 --enabled-state Enabled  --http-port 80 --origin-host-header <container-1-fqdn> --host-name <container-1-fqdn>
```


そして2番目のコンテナ：



```
# コンテナ2のFQDNを表示：
az container show -g labs-frontdoor --name simple-web-2 --query 'ipAddress.fqdn'

# FQDNを使用してコンテナ2をオリジンとして追加：
az afd origin create -g labs-frontdoor --profile-name labs --origin-group-name simple-web --origin-name container2 --priority 1 --weight 1000 --enabled-state Enabled  --http-port 80 --origin-host-header <container-2-fqdn> --host-name <container-2-fqdn>
```

> 両方のコンテナを優先度1で追加しましたが、最初のコンテナは重みが300、2番目のコンテナは重みが1000です。

優先度と重みはロードバランシングの判断に使用されます。両方のオリジンが健全であれば、重みが使われます - コンテナ1へのトラフィックがコンテナ2の約3倍になるはずです。

ポータルで再びFront Doorプロファイルを開きます。新しいオリジングループが両方のACIコンテナをオリジンとしてリストされているのを見ることができます。これはセットアップのバックエンド部分に過ぎません - `simple-web`はまだどのルートにも関連付けられていないことがオリジングループテーブルで確認できます。

## フロントエンドを設定

Front Doorのエントリーポイントは _エンドポイント_ です - これらはアプリの公開ドメイン名です。

シンプルなウェブアプリのためのエンドポイントを作成します：



```
az afd endpoint create -g labs-frontdoor --profile-name labs --endpoint-name simple-web --enabled-state Enabled
```


全てが準備完了し、最後のステップはフロントエンドのエンドポイントをバックエンドのオリジングループにリンクするために _ルート_ を作成することです：


```
az afd route create -g labs-frontdoor --profile-name labs --endpoint-name simple-web --forwarding-protocol HttpOnly --route-name simple-web-route --origin-group simple-web --supported-protocols Http --https-redirect Disabled --link-to-default-domain Enabled --enable-compression true
```

> ルートの設定は、いくつかの追加機能が適用される場所です

ACIはHTTPSエンドポイントを提供していないため、ここではHTTPへのトラフィックを制限しています。圧縮をリストしていますので、圧縮をサポートするリクエスト（すべてのクライアントブラウザがそうしています）をクライアントブラウザが送信すると、Front Doorは圧縮されたレスポンスを送信します。

再びポータルでFront Doorプロファイルを参照します。_概要_ ページから、以下のための _プロビジョニング成功_ と緑色のチェックが見られるはずです：

- あなたのエンドポイント simple-web
- ルート
- そしてオリジングループ

エンドポイントURLを参照し、数回リフレッシュしてみてください - ロードバランシングは一方のコンテナに有利に働いていますか？

## WAFセキュリティルールを適用

Premium SKUのFront Doorは、App Gatewayと同じWAF機能を実行できます。各エンドポイントに対して異なるWAFセキュリティポリシーを作成でき、それによって各フロントエンド用のルールセットをカスタマイズできます。

予防モードで実行され、有効になっているWAFポリシーの作成から始めます：



```
az network front-door waf-policy create -g labs-frontdoor --name simplewebwaf --sku Premium_AzureFrontDoor --disabled false --mode Prevention
```


> WAFポリシーはFront Doorプロファイルにアタッチされていないことに注意してください - それはリソースグループ内の別のリソースです

ポリシーはルールなしで開始されます - あなたが望む保護を提供するルールセットを選択する必要があります。利用可能なルールセットをすべてリストすることができます：


```
az network front-door waf-policy managed-rule-definition list -o table
```


最も有用なのは、OWASPの脅威をカバーする[Microsoft Defaults](https://learn.microsoft.com/ja-jp/azure/web-application-firewall/afds/waf-front-door-drs?tabs=drs20#drs21)と、ボットへのアクセスをブロックする[Bot Manager](https://learn.microsoft.com/ja-jp/azure/web-application-firewall/ag/bot-protection-overview)です。

これらのルールセットを両方ともWAFポリシーに追加します：



```
# 古いバージョンのデフォルトルールを使用します
# これらはファイアウォールリソースを必要としません
az network front-door waf-policy managed-rules add  -g labs-frontdoor --policy-name simplewebwaf --type Microsoft_DefaultRuleSet --version 1.1

# そして現在のボットルール
az network front-door waf-policy managed-rules add  -g labs-frontdoor --policy-name simplewebwaf --type Microsoft_BotManagerRuleSet --version 1.0
```


ポータルでリソースグループをチェックすると、WAFポリシーがリストされているのが見えます。それを開き、_Managed rules_ メニューを選択して、適用されるすべてのルールを確認します。

これで、Front Doorプロファイル内でセキュリティポリシーを作成することにより、WAFを適用できます。このコマンドは汎用的なので、完全なリソースIDを使用する必要があります：


```
# エンドポイントのリソースIDを表示：
az afd endpoint show -g labs-frontdoor --profile-name labs --endpoint-name simple-web --query id

# WAFポリシーのIDを表示：
az network front-door waf-policy show  -g labs-frontdoor -n simplewebwaf --query id

# Front Doorセキュリティポリシーを作成：
az afd security-policy create -g labs-frontdoor --profile-name labs --security-policy-name simplewebsec --domains <endpoint-id> --waf-policy <policy-id>
```


ポータルでFront Doorをチェックすると、セキュリティポリシーとWAFに対して_Provision succeeded_ が表示されるはずです。

これで、SQLインジェクションのような攻撃がブロックされます：



```
# Windowsではcurl.exeを使用
curl -v "http://<endpoint-url>/?id=1;select+1,2,3+from+users+where+id=1--"
```

されたメッセージが表示されます。

## ラボ

Front Doorの設定にはいくつかのステップが必要ですが、App Gatewayよりも直感的です。次は、プロファイルに新しいアプリケーションを追加する番です。Piアプリケーションを実行する新しいACIコンテナを起動します：


```
az container create -g labs-frontdoor --name pi --image kiamol/ch05-pi --ports 80 --ip-address Public --command-line "dotnet Pi.Web.dll -m web" --no-wait --dns-name-label <pi-dns-name>
```


そして、独自のエンドポイントURLを持つアプリをFront Doorプロファイルを通じて公開するために同様のステップに従います。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

ラボのリソースグループを削除します：



```
az group delete -y --no-wait -n labs-frontdoor
```
