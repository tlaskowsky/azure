# 実験の解答

まず、リポジトリを追加し、ローカルのパッケージリストを更新します：



```
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx

helm repo update
```


チャートを探します - バージョン `4.3.0` はアプリバージョン `1.4.0` をインストールし、デフォルト値を確認できます：


```
helm search repo ingress-nginx --versions

helm show values ingress-nginx/ingress-nginx --version 4.3.0
```


Helmが存在しない場合に作成するカスタム名前空間にチャートをインストールします。`f` フラグはローカルの値ファイルへのパスです：


```
helm install -n ingress --create-namespace -f labs/kubernetes/helm/ingress-nginx/dev.yaml ingress-nginx ingress-nginx/ingress-nginx --version 4.3.0

# 出力ドキュメントにはサンプルのIngress仕様が含まれています
```


Helmリリースは名前空間に紐づいています - デフォルトの名前空間ではイングレスコントローラを見ることはできません：


```
helm ls

helm ls -A
```


サービスを確認してタイプとポートを確認します：


```
kubectl get svc -n ingress
```

> Browse to http://localhost:30040

And that's a production-grade 404 you're seeing :)

> Back to the [exercises](README.md).
