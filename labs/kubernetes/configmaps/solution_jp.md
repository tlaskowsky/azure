# ラボ解答

`kubectl create` コマンドを使って命令的にオブジェクトを作成します。PodやDeploymentには使わないでください - YAMLがはるかに良いオプションです - しかしConfigMapsにはうまく機能します。

## リテラルからConfigMapを作成

最も簡単なオプションは、環境変数設定のキーと値をリテラルとして指定することです:



```
kubectl create configmap configurable-env-lab --from-literal=Configurable__Release='21.04-lab'

kubectl describe cm configurable-env-lab
```


## envファイルからConfigMapを作成

代わりに、値を.envファイルに保存します - 例えば [configurable.env](solution/configurable.env) のように。これはYAMLに設定を保存するのとは異なります。なぜならそれはネイティブフォーマットであり、Kubernetesの外部で使用できるからです。

この方法を試すにはリテラルConfigMapを削除する必要があります - それがYAMLの望ましい状態がより良いオプションである理由です:



```
kubectl delete configmap configurable-env-lab

kubectl create configmap configurable-env-lab --from-env-file=labs/kubernetes/configmaps/solution/configurable.env

kubectl describe cm configurable-env-lab
```


## 設定ファイルからConfigMapを作成

- [override.json](solution/override.json) には必要なJSON設定が含まれています。ファイル名はアプリが読む予定のファイル名と同じです。



```
kubectl create configmap configurable-override-lab --from-file=labs/kubernetes/configmaps/solution/override.json

kubectl describe cm configurable-override-lab
```


## アプリをデプロイ

- [deployment-lab.yaml](specs/configurable/lab/deployment-lab.yaml) は、私たちが使っているのと同じConfigMap名を期待していますので、デプロイできます:


```
kubectl apply -f labs/kubernetes/configmaps/specs/configurable/lab/
```


> サービスにアクセスすると、期待されるソースからの設定が表示されるはずです

> [演習](README_jp.md) に戻ります。
