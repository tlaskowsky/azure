# ラボ解決策

## 複数のレプリカ

フィールドは `replicas` と呼ばれており、Deploymentの仕様の一部として設定します（テンプレート内のPodの仕様の一部としてではありません）：



```
apiVersion: apps/v1
kind: Deployment
metadata:
  name: simple-web
spec:
  replicas: 2
  selector:
    # ...
  template:
    # ...
```


[labs/aks/lab/deployment.yaml](./lab/deployment.yaml) のサンプルファイルでは、レプリカ数を4に設定しています。

このファイルを使用してデプロイメントを更新すると、Podを監視すると3つ追加で作成されるのを見ることができます：



```
kubectl apply -f labs/aks/lab/deployment.yaml

kubectl get pods --watch
```


すべてのPodが実行中になったら、`-o wide` フラグを使用してリストを出力し、Podがどのノードを使用しているかを確認します：



```
kubectl get pods -o wide
```


2つのノードにPodが分散しているのが見られるはずです。

公開IPアドレスに戻ってください - サービスIPアドレスは変わっていません。ブラウザのキャッシュを使用せずに更新します（例：WindowsではCtrl-F5）と、異なるPodからの応答が見られます。表示されない場合はcurlを試してみてください：


```
curl <service-ip-address>
```


Podはすべて `app=simple-web` ラベルを持っており、サービスはそれらの間でリクエストをロードバランスします。

## 設定の変更

ConfigMapはYAMLで定義されているので、値を更新して再デプロイできます - [lab/configmap.yaml](./lab/configmap.yaml) のサンプルでは環境名を "UAT" に設定しています：


```
kubectl apply -f labs/aks/lab/configmap.yaml
```


Podをチェックすると、それらは再起動しないことがわかります。サイトを試すと、まだ古い値が表示されます。設定は環境変数としてロードされ、Podの寿命中に変更することはできません。ConfigMapを更新しても、データを使用するデプロイメントのロールアウトはトリガーされません - これは手動で行う必要があります：


```
kubectl rollout restart deploy/simple-web

kubectl get pods --watch
```


> Podは置き換えられ、新しいPodは新しい設定をロードします - アプリを再度試すと、更新された値が表示されます。
