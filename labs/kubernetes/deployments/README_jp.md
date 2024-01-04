# デプロイメントによるポッドのスケーリングと管理

ポッドを直接作成することはあまりありません。それは柔軟性がないからです - ポッドを更新してアプリケーションの更新をリリースすることはできず、新しいポッドを手動でデプロイしてスケールするしかありません。

代わりに、他のオブジェクトを管理するKubernetesオブジェクトである[コントローラー](https://kubernetes.io/docs/concepts/architecture/controller/)を使用します。ポッドに最も使用されるコントローラーはデプロイメントであり、アップグレードとスケールをサポートする機能があります。

デプロイメントはテンプレートを使用してポッドを作成し、ラベルセレクターを使用して所有するポッドを識別します。

## API 仕様

- [デプロイメント](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#deployment-v1-apps)

<details>
  <summary>YAML 概要</summary>

デプロイメント定義には通常のメタデータがあります。

仕様はもっと興味深いものです - ラベルセレクターを含むだけでなく、ポッド仕様も含まれます。ポッド仕様はYAMLでポッドを定義するのと同じ形式ですが、名前を含めません。


```
apiVersion: apps/v1
kind: Deployment
metadata:
  name: whoami
spec:
  selector:
    matchLabels:
      app: whoami
  template:
    metadata:
      labels:
        app: whoami
    spec:
      containers:
        - name: app
          image: sixeyed/whoami:21.04.01
```


デプロイメントのセレクターでラベルが含まれていない場合、YAMLを適用しようとするとエラーになります。

* `spec.selector`- ポッドを見つけるためのラベルリスト
* `spec.template` - ポッドを作成するためのテンプレート
* `spec.template.metadata` - ポッドのメタデータ - `name` フィールドはありません
* `spec.template.metadata.labels` - ポッドに適用するラベル、セレクターに含まれるものを含む必要があります
* `spec.template.spec` - 完全なポッド仕様

</details><br/>

## whoami アプリのデプロイメントを作成する

前回のラボをクリアした場合、クラスターは空であるはずです。この仕様はwhoamiポッドを作成するデプロイメントを記述します：

- [whoami-v1.yaml](specs/deployments/whoami-v1.yaml) - 以前に見たのと同じポッド仕様、デプロイメントでラップされています

デプロイメントを作成すると、ポッドが作成されます：


```
kubectl apply -f labs/kubernetes/deployments/specs/deployments/whoami-v1.yaml

kubectl get pods -l app=whoami 
```


> デプロイメントはポッドを作成する際に独自の命名システムを適用し、ランダムな文字列で終わります

デプロイメントは一級のオブジェクトであり、通常の方法でKubectlで作業します。

📋 デプロイメントの詳細を印刷します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl get deployments

kubectl get deployments -o wide

kubectl describe deploy whoami
```


</details><br/>

> イベントはReplicaSetという別のオブジェクトについて話しています - それについてはまもなく取り組みます。

## デプロイメントのスケーリング

デプロイメントは仕様のテンプレートからポッドを作成する方法を知っています。クラスターが処理できる限り、多くのレプリカ - 同じポッド仕様から作成された異なるポッド - を作成できます。

Kubectlで **命令的に** スケールできます：



```
kubectl scale deploy whoami --replicas 3

kubectl get pods -l app=whoami
```


しかし、これで実行中のデプロイメントオブジェクトはソースコントロールにある仕様と異なります。これは良くないです。

<details>
  <summary>なぜ？</summary>
ソースコントロールはアプリケーションの真の説明であるべきです - 本番環境では、すべてのデプロイメントがソースコントロールにあるYAMLから自動化され、誰かがKubectlで手動で行った変更は上書きされます。

したがって、YAMLで**宣言的に変更**する方が良いです。

</details><br />

- [whoami-v1-scale.yaml](specs/deployments/whoami-v1-scale.yaml) はレプリカレベルを2に設定します

📋 その仕様を使用してデプロイメントを更新し、再度ポッドを確認します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/deployments/specs/deployments/whoami-v1-scale.yaml

kubectl get pods -l app=whoami
```


</details><br/>

> デプロイメントは1つのポッドを削除します。なぜなら現在の状態（3レプリカ）がYAMLの望ましい状態（2レプリカ）と一致しないからです

## 管理されたポッドの操作

ポッド名はランダムであるため、Kubectlでラベルを使用して管理します。これは`get`で行いましたが、`logs`にも同様に機能します：


```
kubectl logs -l app=whoami 
```


ポッドでコマンドを実行する必要がある場合、デプロイメントレベルでexecを使用できます：


```
# これは失敗します
kubectl exec deploy/whoami -- hostname
```


> このコンテナイメージにはシェルがありません :)

デプロイメントテンプレートのポッド仕様にはラベルが適用されます。

📋 ラベル`app=whoami`が付いたすべてのポッドの詳細 - IPアドレスとラベルを含む - を印刷します。

<details>
  <summary>わからない場合は？</summary>
 app=whoami ラベルで：



```
kubectl get pods -o wide --show-labels -l app=whoami
```


</details><br/>

これらのサービスのラベルセレクターもそのラベルと一致します：

- [whoami-loadbalancer.yaml](specs/services/whoami-loadbalancer.yaml)
- [whoami-nodeport.yaml](specs/services/whoami-nodeport.yaml)

サービスをデプロイし、Pod IPエンドポイントを確認します：



```
kubectl apply -f labs/kubernetes/deployments/specs/services/

kubectl get endpoints whoami-np whoami-lb
```


そうすることで、マシンからアプリにまだアクセスできます：


```
# どちらか
curl http://localhost:8080

# または
curl http://localhost:30010
```


## アプリケーションの更新

アプリケーションの更新は通常、コンテナイメージの変更、または設定の変更という形でポッド仕様の変更を意味します。実行中のポッドの仕様を変更することはできませんが、デプロイメントのポッド仕様を変更することはできます。新しいポッドを起動し、古いものを終了することで変更を行います。

- [whoami-v2.yaml](specs/deployments/whoami-v2.yaml) はアプリの設定を変更します。これは環境変数の更新ですが、それらはポッドコンテナの寿命に固定されているため、この変更は新しいポッドを意味します。


```
# ポッドを監視するために新しいターミナルを開きます：
kubectl get po -l app=whoami --watch

# 変更を適用します：
kubectl apply -f labs/kubernetes/deployments/specs/deployments/whoami-v2.yaml
```


アプリをもう一度試してみてください - 出力が小さくなり、リクエストを繰り返すと負荷分散されます。

デプロイメントは以前の仕様をKubernetesデータベースに保存し、リリースが壊れている場合は簡単にロールバックできます：



```
kubectl rollout history deploy/whoami

kubectl rollout undo deploy/whoami

kubectl get po -l app=whoami
```


> もう一度アプリを試してみてください、元のフル出力に戻ります

## 実験

ローリングアップデートは常に望ましいわけではありません - それは古いバージョンと新しいバージョンのアプリが同時に実行され、両方ともリクエストを処理していることを意味します。

代わりに、どちらか一方だけがトラフィックを受け取る状態で、両バージョンが実行されるブルーグリーンデプロイメントを望むかもしれません。

whoamiアプリのブルーグリーンアップデートを作成するために、デプロイメントとサービスを書いてください。v1の2レプリカとv2の2レプリカを実行し始めますが、トラフィックを受け取るのはv1ポッドだけです。

次に、デプロイメントに変更を加えずにv2ポッドにトラフィックを切り替えるアップデートを行います。

> 詰まったら[hints](hints_jp.md)を試してみるか、[solution](solution_jp.md)をチェックしてください。

___
## クリーンアップ

このラボのラベルが付いたオブジェクトを削除してクリーンアップします：


```
kubectl delete deploy,svc -l kubernetes.azureauthority.in=deployments
```
