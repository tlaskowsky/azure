# Podでコンテナを実行する

[Kubernetesの基本的な計算単位であるPod](https://kubernetes.io/docs/concepts/workloads/pods/)は、コンテナを実行します - コンテナが継続して稼働することを保証するのが彼らの役割です。

Podの仕様は非常にシンプルです。最小限のYAMLにはいくつかのメタデータと、実行するコンテナイメージの名前が必要です。

## API仕様

- [Pod](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#pod-v1-core)

<details>
  <summary>YAML概要</summary>

これがPodの最もシンプルな形です：



```
apiVersion: v1
kind: Pod
metadata:
  name: whoami
spec:
  containers:
    - name: app
      image: sixeyed/whoami:21.04
```


Kubernetesリソースにはこれら4つのフィールドが必要です：

* `apiVersion` - リソースはバックワード互換性を支援するためにバージョン管理されます
* `kind` - オブジェクトのタイプです
* `metadata` - 追加のオブジェクトデータの集まりです
* `name` - オブジェクトの名前です

`spec`フィールドの形式はオブジェクトタイプごとに異なります。Podにとってこれが最低限必要なものです：

* `containers`- Pod内で実行するコンテナのリスト
* `name` - コンテナの名前
* `image` - 実行するDockerイメージ

> YAMLではインデントが重要です - オブジェクトフィールドはスペースでネストされます。

</details><br/>

## シンプルなPodを実行する

オブジェクトを管理するツールはKubectlです。YAMLを使って任意のオブジェクトを`apply`コマンドで作成できます。

- [whoami-pod.yaml](specs/whoami-pod.yaml)はシンプルなWebアプリのためのPod仕様です

コースのリポジトリのローカルコピーからアプリをデプロイします：



```
kubectl apply -f labs/kubernetes/pods/specs/whoami-pod.yaml
```


または、YAMLファイルのパスはWebアドレスでもかまいません：


```
kubectl apply -f https://raw.githubusercontent.com/azureauthority/azure/main/labs/kubernetes/pods/specs/whoami-pod.yaml
```


> 出力は何も変更されていないことを示します。Kubernetesは**望ましい状態**のデプロイメントを行います

今、慣れ親しんだコマンドを使って情報を出力できます：



```
kubectl get pods

kubectl get po -o wide
```


> 2番目のコマンドは`po`という短縮名を使い、PodのIPアドレスを含む追加の列を表示します

2番目の出力でどのような追加情報を見ますか、また全てのPod情報を読みやすい形式でどのように出力しますか？


## Podを使った作業

本番環境のクラスターでは、Podは任意のノード上で実行される可能性があります。サーバーに直接アクセスする必要はなく、Kubectlを使ってそれを管理します。

📋 コンテナのログを出力します。

<details>
  <summary>わからない場合は？</summary>


```
kubectl logs whoami
```
</details><br/>

Pod内のコンテナに接続します：



```
# これは失敗します:
kubectl exec -it whoami -- sh
```


> このコンテナイメージにはシェルがインストールされていません！

別のアプリを試してみましょう：

- [sleep-pod.yaml](specs/sleep-pod.yaml)は何もしないアプリを実行します

📋 `labs/kubernetes/pods/specs/sleep-pod.yaml`から新しいアプリをデプロイし、実行中であることを確認します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/pods/specs/sleep-pod.yaml

kubectl get pods
```


</details><br/>

このPodのコンテナにはシェルがあり、いくつかの便利なツールがインストールされています。



```
kubectl exec -it sleep -- sh
```


これで、コンテナ内に接続され、コンテナ環境を探索できます：


```
hostname

whoami
```


そして、Kubernetesのネットワークも：


```
nslookup kubernetes

# これは失敗します:
ping kubernetes
```


> KubernetesのAPIサーバーはPodのコンテナが使用できますが、内部アドレスはpingをサポートしていません

## Pod間の接続

sleep Podのシェルセッションを終了します：



```
exit
```


📋 もとのwhoami PodのIPアドレスを出力します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl get pods -o wide whoami
```
</details><br/>

> これがPodの内部IPアドレスです - クラスタ内の他のPodはこのアドレスに接続できます

sleep Podからwhoami PodのHTTPサーバーにリクエストを送信します：



```
kubectl exec sleep -- curl -s <whoami-pod-ip>
```


> 出力はwhoamiサーバーからのレスポンスで、ホスト名とIPアドレスを含みます

## ラボ

Podはコンテナの抽象表現です。コンテナが終了すると、Podは新しいコンテナを作成して再起動し、アプリが稼働し続けるようにします。これはKubernetesが提供する高可用性の最初の層です。

Docker Hubのイメージ`courselabs/bad-sleep`を実行するPodの仕様を書き、それをデプロイしてPodを観察してみてください - 30秒後には何が起こりますか？そして数分後は？

KubernetesはPodが正常に動作するように続けて試みますので、終わったら削除したいと思うでしょう。

> 行き詰まったら[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)を確認してください。


___
## クリーンアップ

次に進む前に、作成した全てのPodを削除してクリーンアップします：



```
kubectl delete pod sleep whoami sleep-lab
```
