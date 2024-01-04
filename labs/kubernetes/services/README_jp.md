# サービスを使用したネットワーキングポッド

すべてのPodには他のクラスタ内のPodが到達できるIPアドレスがありますが、そのIPアドレスはPodの寿命にのみ適用されます。

[サービス](https://kubernetes.io/docs/concepts/services-networking/service/)はDNS名にリンクされた一貫したIPアドレスを提供し、内部および外部のトラフィックをPodにルーティングするために常にサービスを使用します。

サービスとポッドは疎結合です：サービスは[ラベルセレクター](https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/)を使用して対象のポッドを見つけます。

## API 仕様

- [サービス](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#service-v1-core)

<details>
  <summary>YAML 概要</summary>

サービス定義には通常のメタデータが含まれています。仕様にはネットワークポートとラベルセレクタが含まれている必要があります：



```
apiVersion: v1
kind: Service
metadata:
  name: whoami
spec:
  selector:
    app: whoami
  ports:
    - name: http
      port: 80
      targetPort: 80
```


ポートはサービスがリッスンする場所であり、ラベルセレクタはゼロから多数のポッドに一致する可能性があります。

* `selector` - 対象のポッドを見つけるためのラベルリスト
* `ports` - リッスンするポートのリスト
* `name` - Kubernetes内のポート名
* `port` - サービスがリッスンするポート
* `targetPort` - トラフィックが送信されるポッドのポート

## ポッド YAML

ポッドはサービスからのトラフィックを受信するために一致するラベルを含める必要があります。

ラベルはメタデータで指定されます：



```
apiVersion: v1
kind: Pod
metadata:
  name: whoami
  labels:
    app: whoami
spec:
  # ...
```


> ラベルは任意のキー値ペアです。アプリケーションポッドには通常、`app`、`component`、`version`が使用されます。

</details><br/>

## サンプルポッドを実行

ラベルが含まれている定義からいくつかのシンプルなポッドを作成することから始めます：

* [whoami.yaml](specs/pods/whoami.yaml)
* [sleep.yaml](specs/pods/sleep.yaml)



```
kubectl apply -f labs/kubernetes/services/specs/pods
```


> 複数のオブジェクトを操作し、複数のYAMLマニフェストをKubectlでデプロイできます

📋 すべてのポッドのステータスを確認し、すべてのIPアドレスとラベルを表示します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl get pods -o wide --show-labels
```


</details><br/>

ポッド名はネットワーキングに影響しません。ポッドは名前でお互いを見つけることはできません：



```
kubectl exec sleep -- nslookup whoami
```


## 内部サービスをデプロイ

Kubernetesは内部および外部のポッドへのアクセス用に異なるタイプのサービスを提供します。

[ClusterIP](https://kubernetes.io/docs/concepts/services-networking/connect-applications-service/)はデフォルトであり、サービスにはクラスタ内でのみアクセス可能なIPアドレスが割り当てられます。これはコンポーネントが内部的に通信するためのものです。

* [whoami-clusterip.yaml](specs/services/whoami-clusterip.yaml)はwhoamiポッドにトラフィックをルーティングするClusterIPサービスを定義します

📋 `labs/kubernetes/services/specs/services/whoami-clusterip.yaml`からサービスをデプロイし、その詳細を表示します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/services/specs/services/whoami-clusterip.yaml
```


詳細を表示：


```
kubectl get service whoami

kubectl describe svc whoami
```


> `get`と`describe`コマンドはすべてのオブジェクトに対して同じです。サービスは`svc`のエイリアスを持ちます

</details><br/>

サービスには独自のIPアドレスがあり、それはサービスの寿命にわたって静的です。

## DNSを使用してサービスを見つける

Kubernetesはクラスタ内にDNSサーバーを実行し、すべてのサービスにエントリがあります。これにより、IPアドレスがサービス名にリンクされます。


```
kubectl exec sleep -- nslookup whoami
```


> これは、サービスのDNS名からIPアドレスを取得します。最初の行はKubernetes DNSサーバー自体のIPアドレスです。

これでポッドはDNS名を使用して通信できます：



```
kubectl exec sleep -- curl -s http://whoami
```


📋 whoamiポッドを再作成し、交換が新しいIPアドレスを持つようにしますが、DNSを使用したサービス解決は引き続き機能します。

<details>
  <summary>わからない場合は？</summary>

現在のIPアドレスを確認してからポッドを削除します：



```
kubectl get pods -o wide -l app=whoami

kubectl delete pods -l app=whoami
```


> ラベルセレクタもKubectlで使用できます。ラベルは強力な管理ツールです

交換用のポッドを作成し、そのIPアドレスを確認します：



```
kubectl apply -f labs/kubernetes/services/specs/pods

kubectl get pods -o wide -l app=whoami
```


</details><br/>

サービスのIPアドレスは変更されていませんので、クライアントがそのIPをキャッシュしていれば、引き続き機能します。今やサービスは新しいポッドへトラフィックをルーティングします：


```
kubectl exec sleep -- nslookup whoami

kubectl exec sleep -- curl -s http://whoami
```


## 外部サービスをデプロイ

クラスター外からアクセス可能なサービスのタイプには、[LoadBalancer](https://kubernetes.io/docs/tasks/access-application-cluster/create-external-load-balancer/)と[NodePort](https://kubernetes.io/docs/concepts/services-networking/service/#nodeport)の2種類があります。

どちらもクラスターに入ってくるトラフィックをリッスンし、ポッドにルーティングしますが、動作方法が異なります。LoadBalancerは扱いやすく、Docker DesktopやAKS（Azure Kubernetes Service）などの管理されたKubernetesクラスターでサポートされています。

whoamiアプリをクラスター外からアクセス可能にするための2つのサービス定義があります：

* [whoami-nodeport.yaml](specs/services/whoami-nodeport.yaml) - LoadBalancerサービスをサポートしていないクラスター用
* [whoami-loadbalancer.yaml](specs/services/whoami-loadbalancer.yaml) - LoadBalancerサービスをサポートしているクラスター用

両方をデプロイできます：



```
kubectl apply -f labs/kubernetes/services/specs/services/whoami-nodeport.yaml -f labs/kubernetes/services/specs/services/whoami-loadbalancer.yaml
```


📋 `app=whoami`ラベルが付いたサービスの詳細を表示します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl get svc -l app=whoami
```


</details><br/>

> クラスターにLoadBalancerサポートがない場合、`EXTERNAL-IP`フィールドは永遠に`<pending>`のままになります

これで、ローカルマシンからwhoamiアプリを呼び出すことができます：



```
# どちらか
curl http://localhost:8080

# または
curl http://localhost:30010
```


## ラボ

サービスはネットワーキングの抽象化です。入ってくるトラフィックをリッスンしてポッドに直接ルーティングするルーターのようなものです。

ターゲットポッドはラベルセレクターで識別され、ゼロまたはそれ以上の一致があり得ます。

これらのシナリオをテストするために新しいサービスとwhoamiポッドを作成します：

* ラベル仕様に一致するポッドがゼロ
* 複数のポッドがラベル仕様に一致

何が起こりますか？サービスの対象ポッドをどのように見つけますか？

> 困ったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___
## クリーンアップ

このラボのすべてのYAML仕様にはラベル`kubernetes.azureauthority.in=services`が追加されています。

これにより、それらのリソースをすべて削除することが非常に簡単になります：



```
kubectl delete pod,svc -l kubernetes.azureauthority.in=services
```
