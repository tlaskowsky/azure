# ラボの解決策

設定変更のロールアウト方法は、組織の構造やワークフローに大きく依存します。

この解決策では2つの有効なオプションを示していますが、多くの場合、プロセスは[Kustomize](https://kustomize.io)や[Helm](https://helm.sh)で自動化されます。


## 設定オブジェクトに新しい名前を使用する

設定を変更したときにデプロイメントのロールアウトをトリガーしたい場合は、新しい名前の新しい設定オブジェクトを作成し、デプロイメントのPodスペックを更新して新しいオブジェクトを使用するようにします。

v1の設定セットをデプロイします：



```
kubectl apply -f labs/kubernetes/secrets/solution
```


> アプリを :30020 または :8020 で閲覧

更新は、新しいシークレットとデプロイメントの更新で行われます：

- [secret-v2.yaml](solution/v2-name/secret-v2.yaml) - 新しい設定を持つまったく新しいシークレットオブジェクトです
- [deployment.yaml](solution/v2-name/deployment.yaml) - Podスペック内の新しいシークレット名を使って既存のデプロイメントオブジェクトを更新します


```
kubectl apply -f labs/kubernetes/secrets/solution/v2-name
```


アプリをリフレッシュすると、Podのロールアウトが完了するとすぐに新しい設定が表示されます。

このオプションの利点は、問題があった場合にロールバックできるように古い設定を保存することです：


```
kubectl get secrets -l app=configurable-lab

kubectl rollout undo deploy/configurable-lab
```


## 設定バージョンを使用したアノテーション

アノテーションはもう一つの人気のオプションです。アノテーションはラベルのようなメタデータ項目ですが、Kubernetes内部で使用されるラベルとは異なり、追加情報を記録するために使用されます。

ラボをリセットするために、削除して再デプロイします：



```
kubectl delete deployment,secret -l app=configurable-lab

kubectl apply -f labs/kubernetes/secrets/solution
```


> :30020 または :8020 で閲覧 - 設定はv1に戻っています

次に更新をデプロイします - 既存のシークレットオブジェクトへの変更と、Podスペック内の新しいアノテーションです：

- [secret-v2.yaml](solution/v2-annotation/secret-v2.yaml) - 既存のシークレットオブジェクトのデータを更新します
- [deployment.yaml](solution/v2-annotation/deployment.yaml) - 既存のデプロイメントオブジェクトを更新しますが、同じシークレット名を使用し、設定バージョンを保存するためのアノテーションを追加します


```
kubectl apply -f labs/kubernetes/secrets/solution/v2-annotation
```


Podスペックのメタデータ変更はロールアウトをトリガーします（ただし、デプロイメント自体のメタデータではありません）。リフレッシュすると、v2の設定が表示されます。

このオプションは設定履歴を保存しませんので、変更を元に戻すには、v1のシークレットを再適用してからロールバックする必要があります：



```
kubectl get secrets -l app=configurable-lab

kubectl apply -f labs/kubernetes/secrets/solution/secret.yaml

kubectl rollout undo deploy/configurable-lab
```


> これらのどちらがプロジェクトに適しているかは、組織、設定の保存方法、デプロイメントを完全に自動化するか、設定管理をアプリデプロイメントから切り離す必要があるかによって異なります。

> [練習問題](README_jp.md)に戻る。
