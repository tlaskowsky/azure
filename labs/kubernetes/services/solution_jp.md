# ラボ解決策

サービスのすべてのポッドをリストするには、以下を使用します：



```
kubectl describe svc whoami

# または
kubectl get endpoints whoami
```


> エンドポイントはKubernetesオブジェクトですが、通常はサービスによって管理され、自分自身で作成することはありません

## エンドポイントのないサービス

ラベルを追加することで、一致するポッドがないサービスを作成できます：

- [whoami-svc-zeromatches.yaml](solution/whoami-svc-zeromatches.yaml)

`version`ラベルがwhoamiポッドにないため、一致するポッドがありません：



```
kubectl apply -f labs/kubernetes/services/solution/whoami-svc-zeromatches.yaml

kubectl get endpoints whoami-zero-matches

kubectl exec sleep -- nslookup whoami-zero-matches

kubectl exec sleep -- curl -v -m 5 http://whoami-zero-matches
```


> サービスにはIPアドレスがありますがエンドポイントがないため、curlコールはタイムアウトします


## 複数のエンドポイントを持つサービス

同じラベルで多くのポッドを実行することができます。最初のポッドと同じ仕様で2番目のwhoamiポッドをデプロイします - 名前だけが変わる必要があります：


```
kubectl apply -f labs/kubernetes/services/solution/whoami-pod-2.yaml

kubectl get po -o wide -l app=whoami

kubectl get endpoints whoami
```


> 両方のポッドIPアドレスがサービスエンドポイントとして登録されます



```
kubectl exec sleep -- curl -v http://whoami
```


> レスポンスのIPはポッドのIPで、リクエストされたIPはサービスです。コールを繰り返すと、レスポンスのポッドIPが変わります - サービスはポッド間でリクエストをロードバランスします。

> [演習に戻る](README_jp.md)。
