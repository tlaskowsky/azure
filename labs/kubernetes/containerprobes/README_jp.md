# コンテナプローブ

Kubernetesでアプリをモデル化し、実行することは通常簡単ですが、本番環境に移行する前に行うべき作業がまだあります。

Kubernetesの主要な機能の一つは、一時的な障害があるアプリを修正できることで、コンポーネントを常にテストし、期待通りに応答しない場合は対応を行います。それを実現するためには、Kubernetesにアプリのテスト方法を伝える必要があり、これは _コンテナプローブ_ を使って行います。

## API 仕様

- [ContainerProbe](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#probe-v1-core)

<details>
  <summary>YAML 概要</summary>

コンテナプローブは、Pod仕様内のコンテナ仕様の一部です：


```
spec:
  containers:
    - # normal container spec
      readinessProbe:
        httpGet:
          path: /health
          port: 80
        periodSeconds: 5
```


- `readinessProbe` - プローブにはいくつかの種類があり、このプローブはアプリがネットワークリクエストを受け取る準備ができているかをチェックします
- `httpGet` - Kubernetesがアプリをテストするために行うHTTPコールの詳細 - OK以外のレスポンスコードはアプリが準備ができていないことを意味します
- `periodSeconds` - プローブを実行する頻度

</details><br/>

## レディネスプローブを使用した自己修復アプリ

Kubernetesはコンテナが終了したときにPodを再起動することはわかっていますが、コンテナ内のアプリが実行されているが応答していない場合（例えば、ウェブアプリが `503` を返す場合）Kubernetesはそれを知りません。

whoamiアプリには、そのような障害をトリガーするのに使える素晴らしい機能があります。

📋 `labs/kubernetes/containerprobes/specs/whoami` からアプリをデプロイして始めます。

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/containerprobes/specs/whoami
```


</details><br/>

2つのwhoami Podsがあります - POSTコマンドを実行すると、そのうちの1つが障害状態に切り替わります：



```
# Windowsを使用している場合、正しいcurlを使用するにはこれを実行します：
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force; . ./scripts/windows-tools.ps1

curl http://localhost:8010

curl --data '503' http://localhost:8010/health

curl -i http://localhost:8010
```


> 最後のcurlコマンドを繰り返すと、OKのレスポンスと503のレスポンスがいくつか得られます - 壊れたアプリのPodは自分自身を修正しません。

Kubernetesにアプリが健康であることをテストする方法を伝えることができます。[コンテナプローブ](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)を使用してプローブのアクションを定義し、Kubernetesはそれを繰り返し実行してアプリが健康であることを確認します：

- [whoami/update/deployment-with-readiness.yaml](specs/whoami/update/deployment-with-readiness.yaml) - アプリの/healthエンドポイントに5秒ごとにHTTPコールを行うレディネスプローブを追加します

📋 `labs/kubernetes/containerprobes/specs/whoami/update` にある更新をデプロイし、ラベル `update=readiness` が付いたPodが準備完了になるまで待ちます。

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/containerprobes/specs/whoami/update

kubectl wait --for=condition=Ready pod -l app=whoami,update=readiness
```


</details><br/>

> Podの説明を表示すると、出力にレディネスチェックがリストされています

これらは新しいPodなので、アプリはどちらも健康です。1つのPodを不健康な状態に移行させると、ステータスが変わるのがわかります：



```
curl --data '503' http://localhost:8010/health

kubectl get po -l app=whoami --watch
```


> Ready列の1つのPodが変わります - 今は0/1のコンテナが準備完了です。

レディネスチェックに失敗すると、PodはServiceから削除され、トラフィックは受信しません。

📋 ServiceにPodのIPが1つだけあることを確認し、アプリをテストします。

<details>
  <summary>方法がわからない場合は？</summary>



```
# watchを終了するにはCtrl-C

kubectl get endpoints whoami-np

curl http://localhost:8010
```


</details><br/>

> 健康なPodだけがServiceに登録されているため、常にOKのレスポンスが得られます。

これが実際のアプリであれば、`503`はアプリが過負荷の場合に発生するかもしれません。Serviceから削除することで、回復する時間を与えることができます。

## 自己修復アプリと生存プローブ

レディネスプローブは失敗したPodをServiceのロードバランサーから隔離しますが、アプリの修復を行うわけではありません。

そのためには、プローブが失敗した場合に新しいコンテナでPodを再起動する生存プローブを使用できます：

- [deployment-with-liveness.yaml](specs/whoami/update2/deployment-with-liveness.yaml) - レディネスプローブと同じテストを使用する生存チェックを追加します

レディネスと生存の両方のテストはしばしば同じですが、生存チェックはより重大な結果をもたらすため、頻度が少なく、失敗の閾値が高いことが望ましいです。

📋 `labs/kubernetes/containerprobes/specs/whoami/update2` にある更新をデプロイし、ラベル `update=liveness` が付いたPodが準備完了になるまで待ちます。

<details>
  <summary>方法がわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/containerprobes/specs/whoami/update2

kubectl wait --for=condition=Ready pod -l app=whoami,update=liveness
```


</details><br/>

📋 今度は、あるPodで障害を引き起こし、それが再起動されるか監視してください。

<details>
  <summary>方法がわからない場合</summary>



```
curl --data '503' http://localhost:8010/health

kubectl get po -l app=whoami --watch
```


</details><br/>

> 1つのPodが準備完了 0/1 になった後、再起動し、再び準備完了 1/1 になります。

エンドポイントを確認すると、両方のPodのIPがサービスリストにあるのがわかります。再起動されたPodが準備完了チェックを通過した後、それが追加されました。

他の種類のプローブも存在するので、これはHTTPアプリに限定されたものではありません。このPostgres Podの仕様はTCPプローブとコマンドプローブを使用しています：

- [products-db.yaml](specs/products-db/products-db.yaml) - Postgresがリスニングしているかをテストするレディネスプローブと、データベースが使用可能かをテストする生存プローブがあります

___

## ラボ

プロダクションの懸念事項を追加することは、初期モデリングを行いアプリを稼働させた後に行うことが多いです。したがって、あなたのタスクは、Random Number Generator APIにコンテナプローブを追加することです。基本的な仕様で実行してみてください：


```
kubectl apply -f labs/kubernetes/containerprobes/specs/rngapi
```


アプリを試してみてください：



```
curl -i http://localhost:8040/rng
```


数回試した後、アプリは失敗し、オンラインに戻ることはありません。その状況をチェックするための `/healthz` エンドポイントがあります。あなたの目標は：

- 5つのレプリカを実行し、トラフィックが健康なPodにのみ送られることを確認する
- コンテナ内のアプリが失敗した場合、Podを再起動する

> 詰まったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___
## クリーンアップ



```
kubectl delete all -l kubernetes.azureauthority.in=containerprobes
```
