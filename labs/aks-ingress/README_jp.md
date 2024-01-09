# IngressとApplication Gateway

Kubernetesクラスターにネットワークトラフィックをルーティングするために、LoadBalancerサービスを使用できます。AKSではこれによりパブリックIPアドレスが割り当てられますが、Kubernetesを使用し始めると、単一のクラスターで多くのアプリを実行することになり、多くのランダムIPアドレスを望まないでしょう。

代わりに、単一のIPアドレスを使用してHTTPドメイン名によって受信トラフィックをルーティングし、`myapp.com`、`api.myapp.com`、`otherapp.co.uk`を同じパブリックIPアドレスでサービスできるようにしたいと思います。これはDNSサービスで設定します。Kubernetesは_Ingress_オブジェクトでこれをサポートし、Azure Application Gatewayサービスとうまく統合されます。

## 参考

- [Azure Application Gatewayドキュメント](https://docs.microsoft.com/ja-jp/azure/application-gateway/)
- [KubernetesのIngress](https://kubernetes.io/docs/concepts/services-networking/ingress/) および [Ingress API仕様](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#ingress-v1-networking-k8s-io)
- [AGIC - Application Gateway Ingress Controller](https://docs.microsoft.com/ja-jp/azure/application-gateway/ingress-controller-overview)

## Application Gatewayの作成

ポータルを開き、新しいリソースの作成に移動し、「application gateway」を検索して、作成をクリックします。これは通常とは異なるリソースです - 各ページを完了する必要があります。ページを進むと以下の項目が表示されます：

- _基本_ - 固定スケールまたはオートスケーリングオプションを選択でき、ティアが必要で、VNetが必要です
- _フロントエンド_ - AppGWへのIPアドレスのルーティング、通常はPIP
- _バックエンド_ - トラフィックがルーティングされるバックエンドプール、これはAzure Load Balancerと同じコンセプトです
- _設定_ - 受信リクエストをバックエンドにマッチングするためのルーティングルール

AppGWを手動で設定し、すべてのルーティングルール（例えば、mydomain.comのリクエストが特定のVMSSにルーティングされる）を指定できます。AKSでは、AppGWが_ingress controller_（[Ingress lab](/labs/kubernetes/ingress/README.md)でカバーしたコンセプト）として機能するため、これは自動的に行われます。

ラボ用に新しいリソースグループをお好みのリージョンで作成します：





```
az group create -n labs-aks-ingress --tags courselabs=azure -l eastus
```

AGICアドオンを使用してAKSクラスターを作成することができ、それにより必要なすべてが作成されますが、Application Gatewayを最初に作成して、好きなように設定し、AKSクラスターを削除してもAppGWを実行し続けることができるため、その方が良いでしょう。

App Gatewayはあなたのすべてのアプリのエントリーポイントになりますので、パブリックIPアドレスが必要です。また、AKSクラスターで使用するのと同じVNetにデプロイする必要があります。

📋 デプロイメントに使用するPIP、VNet、および2つのサブネットを作成します。

<details>
  <summary>どうすれば良いかわからない場合</summary>



```
# PIPを作成：
az network public-ip create -g labs-aks-ingress -n appgw-pip --sku Standard -l eastus --dns-name <unique-dns-name>

# VNet：
az network vnet create -g labs-aks-ingress -n vnet --address-prefix 10.2.0.0/16 -l eastus

# そしてサブネット：
az network vnet subnet create -g labs-aks-ingress --vnet-name vnet -n aks --address-prefixes 10.2.8.0/21

az network vnet subnet create -g labs-aks-ingress --vnet-name vnet -n appgw --address-prefixes 10.2.3.0/24
```


</details><br/>

次にアプリケーションゲートウェイを作成します。すべてのネットワークコンポーネントは同じリージョンにある必要があります。さもなければエラーになります：



```
# AKSと連携するためにv2 SKUが必要です：
az network application-gateway create -g labs-aks-ingress -n appgw  --public-ip-address appgw-pip --vnet-name vnet --subnet appgw --capacity 1 --sku Standard_v2 --priority "1" -l eastus
```


これを作成するにはしばらく時間がかかります。ポータルで進捗状況を確認してくださいが、作成中にAKSクラスターの作成に進むことができます。

## AKSアドオン

AKSには、既存のクラスターに新しい機能を追加できる[アドオン](https://learn.microsoft.com/ja-jp/azure/aks/integrations)の概念があります。クラスターを作成し、クラスターとAppGWの両方が準備できたら、アドオンを使用して統合します。

📋 `azure`ネットワークプラグインを使用してAKSクラスターを作成し、VNetの別のサブネットを使用します（サブネットIDが必要です）。

<details>
  <summary>どうすれば良いかわからない場合</summary>

サブネットIDを取得：



```
az network vnet subnet show  -g labs-aks-ingress -n aks --vnet-name vnet --query id -o tsv
```


クラスターを作成：



```
az aks create -g labs-aks-ingress -n aks04 --network-plugin azure --vnet-subnet-id '<subnet-id>' -l eastus
```


</details><br/>

> AKSをVNetと統合する場合、クラスターはネットワークを管理する権限が必要です。

_AADロールが伝播するのを待っています_ というメッセージと独自の進行バーが表示されます。役割を作成するためには、サブスクリプションでアカウントに昇格した権限が必要です。これは自分のサブスクリプションでは問題ありませんが、企業のサブスクリプションでは制限されている可能性があります。

AppGWが作成されたら、ポータルで確認してください。UXはロードバランサーリソースに非常に似ています - AppGWはロードバランサーの強化版です - いくつかの追加機能があります。Web Application Firewall（WAF）は、本番環境のデプロイメントで絶対に見ておきたい機能です。

AKSクラスターも準備ができたら、AppGWアドオンをデプロイできます：


```
# AppGWのIDを取得：
az network application-gateway show -n appgw -g labs-aks-ingress --query id -o tsv

# アドオンを有効にする：
az aks enable-addons -n aks04 -g labs-aks-ingress -a ingress-appgw --appgw-id '<appgw-id>' 
```


この設定にはしばらく時間がかかりますが、これはスケーラブルで信頼性が高く、パブリックフェイシングなアプリをKubernetesで実行するための本番グレードのデプロイメントです。

## AKS上のApplication Gatewayでデプロイ

すべてが準備できたので、パブリックURLを使用してアクセスできるシンプルなアプリをデプロイします。

📋 AKSクラスターを使用するためのKubectl接続資格情報をダウンロードします。

<details>
  <summary>どうすれば良いかわからない場合</summary>

このコマンドはKubectlコンテキストを作成し、現在のものとして設定します：



```
az aks get-credentials -g labs-aks-ingress -n aks04 --overwrite
```


常にノードをリストアップして、正しいクラスターを使用していることを確認するのが良いアイデアです：


```
kubectl get nodes
```


</details><br/>

Docker Desktopでローカルに使用した同様の仕様を使用してwhoamiアプリケーションをデプロイします：[whoami.yaml](/labs/aks-ingress/specs/whoami.yaml)。これにより、内部のClusterIPサービスとデプロイメント（今回は10レプリカで）が作成されます。


```
kubectl apply -f labs/aks-ingress/specs/whoami.yaml
```


Ingressオブジェクトには、PIPに一致するDNS名を設定する必要があります。PIPの完全修飾DNS名（FQDN）を表示するには、次のコマンドを実行します：


```
az network public-ip show -g labs-aks-ingress -n appgw-pip --query 'dnsSettings.fqdn' -o tsv
```
> **ファイルを編集** [ingress-aks.yaml](/labs/aks-ingress/specs/ingress-aks.yaml)、プレースホルダー<pip-fqdn>を実際のFQDNに置き換えてください。

📋 `labs/aks-ingress/specs/ingress-aks.yaml`からIngressオブジェクトを作成し、詳細を表示します。ドメイン名にアクセスして、アプリが表示されるか確認してください。

<details>
  <summary>どうすれば良いかわからない場合</summary>

YAMLに自分のDNS名を設定してから適用してください：



```
kubectl apply -f labs/aks-ingress/specs/ingress-aks.yaml
```

IngressオブジェクトはDNS名とパブリックIPアドレスを表示するはずです：



```
kubectl get ingress
```


</details><br/>

ホスト名にアクセスすると、Application Gateway経由でルーティングされたアプリケーションが表示されるはずです。リフレッシュすると、すべてのポッド間でロードバランシングがうまく機能します。

## ラボ

Application Gatewayが実際にコンテナにトラフィックをどのようにルーティングするかを理解することは非常に有用です。ポータルでApplication Gatewayの設定をナビゲートしてください。ルーティングがどのように機能しているか、そしてトラフィックが健康なポッドにのみルーティングされることを確認するためのヘルスチェックがあるかどうかを確認できますか？

> 困っていますか？[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボのリソースグループを削除して、ストレージを含むすべてのリソースを削除できます。


```
az group delete -y --no-wait -n labs-aks-ingress
```


次に、KubernetesのコンテキストをローカルのDocker Desktopに戻します：


```
kubectl config use-context docker-desktop
```
