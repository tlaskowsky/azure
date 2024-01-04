# Azure Kubernetes Service (AKS)

Kubernetesはオープンソースプラットフォームですが、多くのベンダーがそれぞれにパッケージ化されたバージョンを提供しています。Azure Kubernetes Service（AKS）は、管理されたKubernetesサービスです。AKSクラスタを作成し、Kubernetesモデルを使用してアプリをデプロイします。AzureはクラスタノードのVMのプロビジョニングとKubernetesのインストールおよび設定を行います。また、クラスタをスケーリングしてノードを追加または削除したり、Kubernetesのバージョンをアップグレードしたり、他のAzureサービスと統合するなどの作業を簡素化します。

## 参照

- [Kubernetesサービスドキュメント](https://docs.microsoft.com/ja-jp/azure/aks/)

- [`az aks` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/aks?view=azure-cli-latest)

- [デプロイメント - Kubernetes API仕様](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#deployment-v1-apps)

## ポータルで探る

ポータルを開いて、新しいKubernetesサービスリソースを作成するために検索します。AKSには多くの設定が可能です：

- ノードの数とVMサイズ
- _プリセット_ は良い初期設定を提供します
- AKSには同じセットアップを共有するノードのグループである _ノードプール_ があります - 例えば、同じクラスタ内に10のLinuxノードを持つプール、GPUを備えた5のLinuxサーバーを持つ別のプール、そして2のWindowsサーバーを持つ3つ目のプールがあるかもしれません
- クラスタは標準のKubernetesロールベースアクセス制御（RBAC）を使用してAzureアカウントにリンクされてセキュリティを確保できます
- AKSはACRと統合されているため、追加の設定なしでプライベートACRイメージからコンテナを実行できます

AKSの本番環境でのデプロイは複雑になりますが、CLIで簡単なものから始めましょう。

## CLIでAKSクラスタを作成する

ラボ用の新しいリソースグループをお好みのリージョンで始めます：



```
az group create -n labs-aks --tags courselabs=azure -l eastus
```


新しいクラスタを作成するには、`az aks create` コマンドを使用します。

📋 `Standard_D2s_v5` VMサイズ（または選択したリージョンで有効なサイズ）を使用して2ノードで `aks01` というクラスタを作成します。

<details>
  <summary>どうすれば良いか分からない場合</summary>

`az aks create --help` を実行すると、多くのオプションが表示されます。ほとんどは任意ですが、これによりセットアップが作成されます：



```
az aks create -g labs-aks -n aks01 --node-count 2 --node-vm-size Standard_D2s_v5 --location eastus
```


</details><br/>

クラスタの作成には時間がかかります。CLIが実行されている間に、ポータルに戻ってリソースグループを見てみましょう。`labs-aks` にAKSクラスタが表示されますが、`MC_` で始まる名前の別のRGも表示されます。そこに何があるか見てみましょう - そのRGは何だと思いますか？

## クラスタの使用

AKSは多くの他のAzureリソースをまとめ、それらすべての管理を行います。これらのリソースは別のRGに保持されており、AKSリソースを介して管理するべきです。

クラスタが準備でき次第、アプリをデプロイできます。Docker Desktopや他のKubernetesクラスタと同じYAMLモデルと同じKubectlコマンドラインをAKSで使用します。KubectlにはDocker CLIのように_コンテキスト_の概念があります。Azureコマンドラインを使用してAKSクラスタのコンテキストを追加できます。

📋 `az aks` コマンドでクラスタ認証情報をダウンロードします。

<details>
  <summary>どうすれば良いか分からない場合</summary>

AKSコマンドをリストアウトしてください：



```
az aks --help
```


`get-credentials` を見つけることができます。これはKubectlを使用してAKSクラスタにアクセスするために必要な詳細をダウンロードします：


```
az aks get-credentials -g labs-aks -n aks01 --overwrite-existing 
```


</details><br/>

Azureコマンドラインは詳細を処理しますが、接続可能なクラスタを表示するためにKubectlを使用できます：


```
kubectl config get-contexts
```


AKSクラスタの隣にアスタリスクが表示され、それが現在のコンテキストであることを意味します。これで、Kubectlコマンドを実行するときはAzureのKubernetesクラスタに話しかけています：


```
kubectl get nodes
```


## アプリケーションのデプロイ

Docker Desktopで使用したのと全く同じKubernetesアプリケーションモデルをAKSにデプロイできます。`labs/aks/specs` フォルダのYAML仕様は、任意のKubernetesクラスタで実行されるアプリを定義しています：

- [configmap.yaml](./specs/configmap.yaml) - 環境名をPRODに設定します
- [deployment.yaml](./specs/deployment.yaml) - [Kubernetesラボ](/labs/kubernetes/README.md)と同じです
- [service.yaml](./specs/service.yaml) - 外部トラフィックをポート80からアプリケーションPodにルーティングします

📋 AKSクラスタでアプリを実行します - 一つのフォルダにあるすべてのYAMLファイルを単一のコマンドでデプロイできます - その後、Podとサービスを確認します。

<details>
  <summary>どうすれば良いか分からない場合</summary>

それは同じ `kubectl apply` コマンドです - パスは単一のファイル、フォルダ、またはWebアドレスにすることができます：



```
kubectl apply -f labs/aks/specs
```


次にリソースをリストします：


```
kubectl get pods,services
```


</details><br/>

> Podはかなり早く _Running state_ になり、サービスには外部IPアドレスが付与されるはずです。

外部IPアドレスが `<pending>` と表示された場合は、このコマンドを実行してリソースの更新を監視できます：



```
kubectl get service simple-web --watch
```


IPアドレスが設定されたら、Ctrl-C/Cmd-Cでウォッチを終了できます。

IPアドレスはアプリケーションの公開IPアドレスです。それをブラウズしてアプリを見てみてください。ポータルでIPアドレスを提供し、クラスタVMにトラフィックをルーティングするリソースを見つけることができますか。

## ラボ

AKSクラスタには余剰容量がありますので、より多くのユーザーにサービスを提供するために、より多くのPodを実行できます。Webアプリケーションのデプロイメント仕様を変更して4つのPodを実行する方法を調査します。YAMLを編集して変更を適用する必要があります。複数のPodを実行している場合、ブラウザでサイトを繰り返し更新するとどうなりますか？設定から環境名を `PROD` に変更して再デプロイすると、サイトはすぐに更新されますか？

> 詰まったら [ヒント](hints_jp.md) を見るか、[解決策](solution_jp.md) を確認してください。

___

## クリーンアップ

このラボのためのRGを削除して、すべてのリソースを削除します。AKSクラスタが削除されると、管理された `MC_` RGも削除されます：



```
az group delete -y --no-wait -n labs-aks
```


KubernetesコンテキストをローカルのDocker Desktopに戻します：


```
kubectl config use-context docker-desktop
```
