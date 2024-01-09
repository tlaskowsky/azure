# 名前空間を使ったワークロードの分離

Kubernetesの素晴らしい機能の一つは、あらゆるタイプのアプリケーションを実行できることです。多くの組織が、アプリケーションランドスケープ全体をKubernetesに移行しようとしています。クラスターを分割できなければ、操作が難しくなる可能性があります。そこでKubernetesには[名前空間](https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/)があります。

名前空間は、他のリソースを含むコンテナであるKubernetesリソースです。ワークロードを分離するために使用でき、分離の方法はあなた次第です。本番クラスターでは、各アプリケーションごとに異なる名前空間を持ち、非本番クラスターでは、各環境（開発、テスト、UAT）ごとに名前空間を持つことができます。

名前空間を使用すると複雑さが増しますが、単一のクラスターで複数のワークロードを安心して実行できるような多くのセーフガードが得られます。

## API仕様

- [名前空間](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.28/#namespace-v1-core)

<details>
  <summary>YAML概要</summary>

名前空間の基本的なYAMLは非常にシンプルです：





```
apiVersion: v1
kind: Namespace
metadata:
  name: whoami
```



これだけです :) 名前空間には名前が必要で、名前空間内に作成したい各リソースのメタデータに名前空間名を追加します：



```
apiVersion: v1
kind: Pod
metadata:
  name: whoami
  namespace: whoami
```


名前空間はネストできません。クラスターを分割するために使用される単一レベルの階層です。

</details><br />

## 名前空間の作成と使用

Kubernetes自体のコアコンポーネントはPodとサービスで動作しますが、別の名前空間にあるためkubectlで見ることはできません：


```
kubectl get pods

kubectl get namespaces

kubectl get pods -n kube-system
```


> `-n`フラグはkubectlにどの名前空間を使用するかを指示します。含まれていない場合、コマンドはデフォルトの名前空間を使用します

これまでにデプロイしたものはすべて`default`名前空間に作成されています。

`kube-system`で見ることができるものはKubernetesのディストリビューションに依存しますが、DNSサーバーのPodが含まれているはずです。

システムリソースは自分のアプリと同じ方法で操作できますが、kubectlコマンドに名前空間を含める必要があります。

📋 システムDNSサーバーのログを表示します。

<details>
  <summary>どうやるかわからない場合は？</summary>



```
kubectl logs -l k8s-app=kube-dns

kubectl logs -l k8s-app=kube-dns -n kube-system
```


</details><br />

kubectlで毎回名前空間を追加するのは時間がかかるため、kubectlには[コンテキスト](https://kubernetes.io/docs/reference/kubectl/cheatsheet/#kubectl-context-and-configuration)があり、コマンドのデフォルトの名前空間を設定できます：


```
kubectl config get-contexts

cat ~/.kube/config
```


> コンテキストはクラスター間の切り替えにも使用されます。クラスターAPIサーバーの詳細はkubeconfigファイルに保存されています

リモートクラスター、またはクラスター上の特定の名前空間を指す新しいコンテキストを作成できます。コンテキストには認証詳細が含まれているため、慎重に管理する必要があります。

コンテキストの設定を更新して名前空間を変更できます：


```
kubectl config set-context --current --namespace kube-system
```


すべてのkubectlコマンドは現在のコンテキストのクラスターと名前空間に対して動作します。

📋 システム名前空間とデフォルト名前空間からPodの詳細を表示します。

<details>
  <summary>どうやるかわからない場合は？</summary>



```
kubectl get po

kubectl logs -l k8s-app=kube-dns 

kubectl get po -n default
```


</details><br />

📋 コンテキストを`default`名前空間に戻して、誤って危険な操作をしないようにします。

<details>
  <summary>どうやるかわからない場合は？</summary>



```
kubectl config set-context --current --namespace default
```


</details><br />

## 名前空間へのオブジェクトのデプロイ

オブジェクト仕様には、YAML内にターゲット名前空間を含めることができます。指定されていない場合、kubectlで名前空間を設定できます。

- [sleep-pod.yaml](specs/sleep-pod.yaml)は名前空間を指定せずにPodを定義しているため、kubectlが名前空間を決定します。コンテキストのデフォルトまたは明示的な名前空間を使用します

📋 `labs/kubernetes/namespaces/specs/sleep-pod.yaml`のPod仕様をデフォルト名前空間とシステム名前空間にデプロイします。

<details>
  <summary>どうやるかわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/namespaces/specs/sleep-pod.yaml -n default

kubectl apply -f labs/kubernetes/namespaces/specs/sleep-pod.yaml -n kube-system

kubectl get pods -l app=sleep --all-namespaces
```


</details><br />

> 名前空間へのアクセスはアクセスコントロールで制限できますが、開発環境ではクラスター管理者の権限を持つため、すべてを見ることができます。

アプリケーションを分離するために名前空間を使用している場合、モデルに名前空間仕様を含め、すべてのオブジェクトで名前空間を指定します：

- [whoami/01-namespace.yaml](specs/whoami/01-namespace.yaml) - 名前空間を定義します
- [whoami/deployment.yaml](specs/whoami/deployment.yaml) - 名前空間のデプロイメントを定義します
- [whoami/services.yaml](specs/whoami/services.yaml) - サービスを定義します。ラベルセレクタはサービスと同じ名前空間のPodにのみ適用されます

kubectlはフォルダ内のすべてのYAMLをデプロイできますが、オブジェクトの依存関係をチェックして正しい順序で作成するわけではありません。通常、疎結合アーキテクチャのため、サービスはデプロイメントの前後に作成できます。

しかし、名前空間はそれらに含まれるオブジェクトが作成される前に存在する必要があるため、名前空間YAMLは`01_namespaces.yaml`と呼ばれ、最初に作成されることが保証されます（kubectlはファイル名の順に処理します）。


```
kubectl apply -f labs/kubernetes/namespaces/specs/whoami

kubectl get svc -n whoami
```


アプリケーションや環境をグループ化するために名前空間を使用すると、トップレベルのオブジェクト（デプロイメント、サービス、ConfigMaps）に多くのラベルが不要になります。名前空間内で操作するため、フィルタリングのためのラベルは必要ありません。

こちらはすべてのコンポーネントが独自の名前空間に隔離される別のアプリです：

- [configurable/01-namespace.yaml](specs/configurable/01-namespace.yaml) - 新しい名前空間
- [configurable/configmap.yaml](specs/configurable/configmap.yaml) - アプリ設定のConfigMap
- [configurable/deployment.yaml](specs/configurable/deployment.yaml) - ConfigMapを参照するデプロイメント。ConfigオブジェクトはPodと同じ名前空間にある必要があります。

📋 アプリをデプロイし、kubectlを使用してすべての名前空間のデプロイメントをリストします。

<details>
  <summary>どうやるかわからない場合は？</summary>



```
kubectl apply -f labs/kubernetes/namespaces/specs/configurable

kubectl get deploy -A --show-labels
```


</details><br />

kubectlは一つの名前空間またはすべての名前空間でのみ使用できるため、サービスなどのオブジェクトには追加のラベルが必要になる場合があります。これにより、すべての名前空間を横断してラベルでフィルタリングしながらリストできます：


```
kubectl get svc -A -l kubernetes.azureauthority.in=namespaces
```

## 名前空間とサービスDNS

Kubernetesのネットワーキングはフラットなので、任意の名前空間の任意のPodは別のPodのIPアドレスにアクセスできます。

サービスは名前空間スコープなので、DNS名を使用してサービスのIPアドレスを解決したい場合、名前空間を含めることができます：

- `whoami-np`はローカルドメイン名であり、実行される同じ名前空間でのサービスwhoami-npのみを探します
- `whoami-np.whoami.svc.cluster.local`は完全修飾ドメイン名（FQDN）であり、whoami名前空間のサービスwhoami-npを探します

sleep Pod内でいくつかのDNSクエリを実行します：

```
# this won't return an address - the Service is in a different namespace:
kubectl exec pod/sleep -- nslookup whoami-np

# this includes the namespace, so it will return an IP address:
kubectl exec pod/sleep -- nslookup whoami-np.whoami.svc.cluster.local
```

> ベストプラクティスとして、コンポーネント間の通信にはFQDN（完全修飾ドメイン名）を使用すべきです。これによりデプロイメントの柔軟性は低下しますが、名前空間を変更する際にアプリの設定も変更する必要があるためです。しかし、これにより混乱を招く可能性のある故障点を除去できます。

## ラボ

`set-context`コマンドを使用したKubernetesクラスターと名前空間間の切り替えは面倒です。`kubens`と`kubectx`というツールのペアを探してインストールしてください。これらを使用すると、複数のクラスターやアプリケーションを扱う際に便利です。

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ



```
# 名前空間を削除すると、その中のすべてが削除されます:
kubectl delete ns -l kubernetes.azureauthority.in=namespaces

# これによりsleep Podsだけが残ります:
kubectl delete po -A -l kubernetes.azureauthority.in=namespaces
```
