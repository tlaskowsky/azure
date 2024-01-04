# 実験解答

ブルーグリーンアップデートには、アプリの異なるバージョンのポッドを管理する2つのデプロイメントオブジェクトが必要です。

- [solution/whoami-deployments.yaml](./solution/whoami-deployments.yaml) には同じYAMLで定義された2つのデプロイメントがあり、簡単に比較できます。

Kubectlもこれをサポートしており、`---`を使用してオブジェクトを分離します。



```
kubectl apply -f labs/kubernetes/deployments/solution/whoami-deployments.yaml

kubectl get pods -l app=whoami-lab,version=v1

kubectl get pods -l app=whoami-lab,version=v2
```


> 4つのポッドが実行中ですが、それらのラベルをターゲットにするサービスはありません

## v1サービスをデプロイする

ブルーグリーンスイッチはサービスのラベルセレクターを変更することで行われます。

- [solution/whoami-service-v1.yaml](./solution/whoami-service-v1.yaml) にはLoadBalancerとNodePortサービスが定義されており、それぞれv1ポッドを選択するための同じセレクターを使用します。

v1をデプロイしてテストします：



```
kubectl apply -f labs/kubernetes/deployments/solution/whoami-service-v1.yaml

kubectl get endpoints whoami-lab-np whoami-lab-lb

curl localhost:8020 # OR curl localhost:30020
```


## v2に切り替える

- [whoami-service-v2.yaml](./solution/whoami-service-v2.yaml) には、セレクターを変更しただけの同じサービス定義があります。

Kubernetesはこれを既存のサービスへの更新としてデプロイするため、IPアドレスは変わらず、サービスが見つけるエンドポイントだけが変わります：



```
kubectl apply -f labs/kubernetes/deployments/solution/whoami-service-v2.yaml

kubectl get endpoints whoami-lab-np whoami-lab-lb

curl localhost:8020 # OR curl localhost:30020
```


> サービス仕様を変更することでデプロイメント間を切り替えることができます

> [演習](README_jp.md)に戻る。
