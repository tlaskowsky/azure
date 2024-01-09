# イングレス

イングレスには二つの部分があります:

- コントローラーは、すべての着信トラフィックを受け取るリバースプロキシです。
- イングレスオブジェクトは、コントローラーのためのルーティングルールを設定します。

異なるコントローラーから選択することができます。私たちは[Nginx Ingress Controller](https://kubernetes.github.io/ingress-nginx/)を使用しますが、[Traefik](https://doc.traefik.io/traefik/providers/kubernetes-ingress/)や[Contour - CNCFプロジェクト](https://projectcontour.io)も人気の代替案です。

## API 仕様

- [Ingress (networking.k8s.io/v1)](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#ingress-v1-networking-k8s-io)

<details>
  <summary>YAML 概要</summary>

イングレスルールには複数のマッピングがありますが、かなり直感的です。

通常、アプリケーションごとに1つのオブジェクトがあり、名前空間に配置されているので、アプリと同じ名前空間にデプロイすることができます:



```
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: whoami
spec:
  rules:
  - host: whoami.local
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: whoami-internal
            port: 
              name: http
```

- `rules` - collection of routing rules
- `host` - the DNS host name of the web app
- `http` - ingress routing is only for web traffic
- `paths` - collection of request paths, mapping to Kubernetes Services
- `path` - the HTTP request path, can be generic `/` or specific `/admin`
- `pathType` - whether path matching is as a `Prefix` or `Exact`
- `backend.service` - Service where the controller will fetch content


- `rules` - ルーティングルールのコレクション
- `host` - WebアプリのDNSホスト名
- `http` - イングレスルーティングはWebトラフィックのみ
- `paths` - Kubernetesサービスへのリクエストパスのコレクション
- `path` - HTTPリクエストパス、一般的な`/`または特定の`/admin`が可能
- `pathType` - パスマッチングが`Prefix`または`Exact`として行われるか
- `backend.service` - コントローラーがコンテンツを取得するサービス

</details><br/>

## イングレスコントローラーのデプロイ

あまり良い名前ではありませんが、イングレスコントローラーはKubernetesオブジェクトの特定のタイプではありません - DeploymentがPodコントローラーであるように。

イングレスコントローラーは論理的なもので、サービス、Podコントローラー、RBACルールのセットで構成されています:

- [01_namespace.yaml](specs/ingress-controller/01_namespace.yaml) - イングレスコントローラーはすべてのアプリで共有されるため、通常は独自の名前空間があります
- [02_rbac.yaml](specs/ingress-controller/02_rbac.yaml) - イングレスコントローラーがKubernetes APIからサービスエンドポイント、イングレスオブジェクトなどを問い合わせるためのRBACルール
- [configmap.yaml](specs/ingress-controller/configmap.yaml) - プロキシキャッシングを有効にするためのNginxの追加設定
- [daemonset.yaml](specs/ingress-controller/daemonset.yaml) - イングレスコントローラーポッドを実行するDaemonSet; まだ見たことのないいくつかのフィールドが含まれています
- [services.yaml](specs/ingress-controller/services.yaml) - 外部アクセスのためのサービス

コントローラーのデプロイ:



```
kubectl apply -f labs/kubernetes/ingress/specs/ingress-controller

kubectl get all -n ingress-nginx

kubectl wait --for=condition=Ready pod -n ingress-nginx -l app.kubernetes.io/name=ingress-nginx
```


> http://localhost:8000 または http://localhost:30000 にアクセスしてください。クラスター内でアプリが実行されていなくても、イングレスコントローラーから404レスポンスが返されます。

イングレスコントローラーはNginxによって動力を与えられていますが、Nginx内でルーティングを設定する必要はありません。ブラックボックスとして扱い、イングレスオブジェクトですべての設定を行います。

## イングレスを通じてデフォルトアプリを公開

最初にデフォルトアプリをキャッチオールとして開始し、ユーザーがイングレスコントローラーからの404レスポンスを見ることがないようにします。

- [default/deployment.yaml](specs/default/deployment.yaml) - 標準のNginxイメージを使用したシンプルなNginxデプロイメント、イングレスコントローラーではありません
- [default/configmap.yaml](specs/default/configmap.yaml) - Nginxが表示するHTMLファイルを含む設定
- [default/service.yaml](specs/default/service.yaml) - ClusterIPサービス

📋 `labs/kubernetes/ingress/specs/default`からデフォルトWebアプリをデプロイ:

<details>
  <summary>どうやって?</summary>



```
kubectl apply -f labs/kubernetes/ingress/specs/default
```


</details><br/>

まだ何も起こりません。サービスは自動的にイングレスコントローラーに接続されません。イングレスオブジェクトでルーティングルールを指定することによってそれを行います:

- [ingress/default.yaml](specs/default/ingress/default.yaml) - ホストが指定されていないため、すべてのリクエストがデフォルトでここに行くイングレスルール

📋 `labs/kubernetes/ingress/specs/default/ingress`のイングレスルールをデプロイし、すべてのルールをリストアップ:

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/ingress/specs/default/ingress

kubectl get ingress
```

</details><br/>

どのURLにアクセスしても、デフォルトのレスポンスが表示されます：

> http://localhost:8000/a/bc.php または http://localhost:30000/a/bc.php にアクセスしてみてください。

<details>
  <summary>ℹ イングレスコントローラーには通常、独自のデフォルトバックエンドがあります。</summary>
 
 これがNginxから最初に来た404レスポンスの原因です。独自のデフォルトアプリを実行する代わりに、[デフォルトバックエンドをカスタマイズ](https://kubernetes.github.io/ingress-nginx/user-guide/default-backend/)することもできますが、これは使用しているイングレスコントローラーに特有のものです。

</details><br/>

## 特定のホストアドレスへアプリを公開する

イングレスコントローラーを通じてすべてのアプリを公開するには、同じパターン - アプリケーションのPod上に内部サービスを持ち、ルーティングルールを持つイングレスオブジェクトを使用します。

こちらが特定のホスト名に公開するwhoamiアプリの仕様です：

- [whoami.yaml](specs/whoami/whoami.yaml) - アプリのデプロイメントとClusterIPサービス、イングレスに特有のものはありません
- [whoami/ingress.yaml](specs/whoami/ingress.yaml) - ホストドメイン `whoami.local` のトラフィックをClusterIPサービスにルーティングするイングレス

📋 `labs/kubernetes/ingress/specs/whoami` にあるアプリをデプロイし、イングレスルールを確認してください。

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/ingress/specs/whoami

kubectl get ingress
```


</details><br/>

サイトにローカルでアクセスするには、[ホストファイル](https://en.wikipedia.org/wiki/Hosts_(file))にエントリを追加する必要があります - このスクリプトがそれを行います（リモートクラスターを使用している場合は、IPアドレスをノードのIPに置き換えてください）：


```
# Powershellを使用する場合 - 管理者として実行する必要があります：
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force; ./scripts/add-to-hosts.ps1 whoami.local 127.0.0.1

# macOSまたはLinuxでは - sudoパスワードを求められます：
sudo chmod +x ./scripts/add-to-hosts.sh
./scripts/add-to-hosts.sh whoami.local 127.0.0.1
```


> http://whoami.local:8000 または http://whoami.local:30000 にアクセスすると、サイトが表示されます。複数のレプリカがあるので、更新するとロードバランシングが見られます。

## レスポンスキャッシュを使用したイングレス

Ingress APIは、すべてのイングレスコントローラーの機能をサポートしているわけではないので、カスタム機能を使用するにはアノテーションで設定します。

ホスト名 `pi.local` でPi Webアプリを公開しますが、最初はレスポンスキャッシュなしでシンプルなイングレスを使用します：

- [pi.yaml](specs/pi/pi.yaml) - アプリのデプロイメントとサービス
- [pi/ingress.yaml](specs/pi/ingress.yaml) - `pi.local` へのトラフィックをサービスにルーティングするイングレス

📋 `labs/kubernetes/ingress/specs/pi` にあるアプリをデプロイし、ステータスを確認し、`pi.local` をホストファイルに追加してください。

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/ingress/specs/pi

kubectl get ingress

kubectl get po -l app=pi-web

# Windows:
./scripts/add-to-hosts.ps1 pi.local 127.0.0.1

# *nix:
./scripts/add-to-hosts.sh pi.local 127.0.0.1
```


</details><br/>

> http://pi.local:8000/pi?dp=25000 / http://pi.local:30000/pi?dp=25000 にアクセスすると、レスポンスが表示されるまでに1、2秒かかります。更新すると、リクエストがロードバランスされ、毎回レスポンスが計算されます。

レスポンスキャッシングを使用するようにIngressオブジェクトを更新できます - Nginxイングレスコントローラーはこれをサポートしています：

- [ingress-with-cache.yaml](specs/pi/update/ingress-with-cache.yaml) - サイトの設定を行う際にコントローラーがこれを探す、キャッシュを使用するためのNginxアノテーションを使用します

アプリに変更はありません、Ingressのみ：



```
kubectl apply -f labs/kubernetes/ingress/specs/pi/update
```


> 今度は http://pi.local:8000/pi?dp=25000 / http://pi.local:30000/pi?dp=25000 にアクセスすると、リフレッシュごとにキャッシュされたレスポンスが表示されます。

<details>
  <summary>ℹ 通常、アプリのすべての部分をキャッシュすることはありません。</summary>

静的コンテンツのためにキャッシュアノテーションがあるIngressルールと、動的コンテンツのための別のルールを持つことがあります。

</details><br />

## ラボ

このラボには二つの部分があります。まず、設定可能なWebアプリをイングレスコントローラーを通じて公開したいと思います。

アプリの仕様はすでに用意されており、あなたの仕事はIngressルーティングを構築してデプロイすることです：



```
kubectl apply -f labs/kubernetes/ingress/specs/configurable
```


二つ目の部分は、イングレスコントローラーを標準のポート - HTTPのための80およびHTTPSのための443を使用するように変更したいと思います。LoadBalancerを使用している場合にのみこれが可能です。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ



```
kubectl delete all,secret,ingress,clusterrolebinding,clusterrole,ns,ingressclass -l kubernetes.azureauthority.in=ingress
```
