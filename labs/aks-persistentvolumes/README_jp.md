# Kubernetesのストレージ

Kubernetesには、アプリケーションをモデル化するための多くの抽象概念があり、すべてのクラスターで機能する一般的な方法でそれを記述できます。ストレージに関しては、ストレージユニットを表すさまざまな種類の_ボリューム_を定義し、それらをアプリケーションPodにマウントすることができます。

ストレージマウントはコンテナファイルシステムの一部として表示されますが、実際にはコンテナの外部に保存されています - AKSは標準のAzureリソース、ディスクおよびファイル共有を使用します。これにより、設定をコンテナに読み取り専用ファイルとしてプッシュし、アプリケーションの状態をコンテナの外部に保存できます。

## 参照

- [Kubernetesのボリューム](https://kubernetes.io/docs/concepts/storage/volumes/)

- [AKSのストレージ](https://learn.microsoft.com/en-us/azure/aks/concepts-storage)

- [PersistentVolumeClaim - Kubernetes API 仕様](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#persistentvolumeclaim-v1-core)


## AKSクラスターの作成

まず、ラボ用の新しいリソースグループをお好みのリージョンで作成します：



```
az group create -n labs-aks-persistentvolumes --tags courselabs=azure -l eastus
```


次に、ラボ用の小規模なクラスターを作成します：



```
az aks create -g labs-aks-persistentvolumes -n aks02 --node-count 1 --node-vm-size Standard_D2s_v5 --no-wait --location eastus
```


> `no-wait`フラグは、コマンドが返され、クラスターがバックグラウンドで作成されることを意味します。

それが作成されている間、ローカルのクラスターで作業を行います。Docker Desktopが実行されていて、CLIがそれを指していることを確認してください：



```
kubectl config use-context docker-desktop

# これがローカルクラスターであることを確認します：
kubectl get nodes
```


## ボリュームとVolumeMounts

私たちは、設定ファイルを読み込み、さまざまな場所にファイルを書き込む単純なアプリを使用します。これは、.NET 6.0のバックグラウンドワーカーアプリで、主要なコードは[Worker.cs](/src/queue-worker/src/Worker.cs)にあります。このアプリはDocker Hubで[courselabs/queue-worker:6.0](https://hub.docker.com/r/courselabs/queue-worker/tags)として利用可能です。

最初に使用するバージョンは、ConfigMapをマウントして設定を読み込みます：

- [v1/configmap.yaml](./specs/v1/configmap.yaml) - ログとアプリケーション設定を含む完全なappsettings.jsonファイルを含みます
- [v1/deployment.yaml](./specs/v1/deployment.yaml) - ConfigMapをボリュームマウントとして読み込み、/appディレクトリにappsettings.jsonファイルを読み込むPodをモデル化します

> このモデルで定義されていることを確認してください。ConfigMapはJSONファイルを保存します。PodはConfigMapをボリュームとして読み込み、コンテナは実際にファイルをボリュームマウントとして読み込みます。

📋 このアプリをローカルクラスターで実行し、アプリが動作していることを確認するためにPodのログを出力してください。

<details>
  <summary>方法がわからない場合</summary>

`v1`フォルダのすべてのスペックを適用します：



```
kubectl apply -f labs/aks-persistentvolumes/specs/v1
```


Pod名を検索します：



```
kubectl get pods
```


ログを出力します：



```
kubectl logs -f <pod-name>
```


</details><br/>

アプリケーションのログから、いくつかの作業を行った後で20秒間隔でループすることがわかります。

コンテナ内で実行中のファイルの内容を表示するには、Kubernetesの`exec`コマンドを使用してコンテナ内でコマンドを実行できます。Kubernetesはコマンドを実行し、返された出力を出力します。

アプリがコンテナ内で書き込んでいるファイルの内容を表示するには、これを実行します：


```
kubectl exec -it deploy/queue-worker -- cat /mnt/cache/app.cache

kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


> アプリは各ファイルに20秒ごとに1行を書き込みます - その行にはホスト名が含まれ、KubernetesではPod名です

これまでのところ、すべてがうまく機能しています。

## コンテナの書き込み可能なストレージ

コンテナでデータを書き込むと、そのストレージはコンテナと同じライフサイクルを持ちます。Podが置き換えられると、新しいファイルシステムを持つ新しいコンテナが得られ、以前のコンテナによって書かれたデータは失われます。新しいコンテナイメージを使用するために更新するたび、またはPodの仕様の他の部分を変更するたびに、これが発生します。

📋 あなたのqueue-worker Podを削除し、デプロイメントが代替のPodを作成するのを待ってください。それが実行中のとき、新しいPodの`app.db`ファイルの内容を出力してください。

<details>
  <summary>方法がわからない場合</summary>

デプロイメントの仕事は、あなたのアプリのために1つのPodがあることを確認することです。Podを削除すると、デプロイメントは代替を作成します：



```
kubectl delete pod <pod-name>
```


Podの代替が起動するのを見るためにPodを監視します：



```
kubectl get pods --watch
```


新しいPodが実行中のとき、dbファイルをチェックします：



```
kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


</details><br/>

新しいPodからの書き込みのみがデータベースファイルに含まれていることがわかります - 以前のPodのデータファイルはコンテナのファイルシステム内にあり、そのコンテナは削除されて置き換えられました。

## EmptyDirsとPersistentVolumeClaims

アプリケーションモデルの新しいバージョンは、同じコンテナイメージとConfigMapを使用しますが、Pod仕様に2つの書き込み可能なボリュームを追加します：

- [v2/deployment.yaml](./specs/v2/deployment.yaml) - キャッシュディレクトリをEmptyDirボリュームに、データベースディレクトリをPersistentVolumeClaimボリュームにマウントします

- [v2/pvc.yaml](./specs/v2/pvc.yaml) - PersistentVolumeClaim（PVC）をモデル化します

これらのKubernetesのストレージの詳細は、[PersistentVolumes lab](/labs/kubernetes/persistentvolumes/README.md)でカバーしました。リマインダーとして - _EmptyDir_ は、Podが再起動する必要がある場合にデータが生き残る、Podのライフサイクルを持つストレージの一部です。_PersistentVolumeClaim_ は、クラスタがPodにアタッチできるストレージを提供するよう要求するもので、ここではストレージのタイプを指定せず、必要な量だけを指定します。

📋 新しいバージョンのアプリをDocker Desktopにデプロイします。Podがしばらく実行された後、それを削除します。代替Podのデータベースとキャッシュファイルをチェックしてください。以前のPodからのデータが引き継がれていますか？

<details>
  <summary>方法がわからない場合</summary>

v2の仕様をデプロイします：



```
kubectl apply -f ./labs/aks-persistentvolumes/specs/v2
```


データファイルをチェックします：



```
kubectl exec -it deploy/queue-worker -- cat /mnt/cache/app.cache

kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


次に、Podを削除します：



```
kubectl delete pod <pod-name>
```


Podの代替が起動するのを見るためにPodを監視します：



```
kubectl get pods --watch
```


新しいPodが実行中のとき、もう一度ファイルをチェックします：



```
kubectl exec -it deploy/queue-worker -- cat /mnt/cache/app.cache

kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


</details><br/>

キャッシュファイルは新しくなっているはずです - このPodのEmptyDirボリュームは古いものを置き換えるため、データは失われます。しかし、データベースファイルは保持されているはずです。それはPersistentVolumeに保存されており、Podとは別のライフサイクルを持つため、ボリュームが削除されるまでデータは保持されます。

## AKSのPVCとストレージクラス

queue-workerモデルは、クラスター固有の設定を使用していませんので、新しいAKSクラスターでも同じ方法で機能します。

Kubernetes CLIを新しいAKSクラスターに接続します：



```
az aks get-credentials -g labs-aks-persistentvolumes -n aks02 --overwrite
```

📋 AKSクラスターで同じステップを繰り返してください - v2アプリケーション仕様をデプロイし、Podが開始されるのを待ってから削除します。置換されたPodのデータベースファイルを確認し、Pod間でデータが保持されていることを確認してください。

<details>
  <summary>わからない場合</summary>

v2の仕様をデプロイし、Podが開始するのを待ちます：



```
kubectl apply -f ./labs/aks-persistentvolumes/specs/v2

kubectl get pods --watch
```


数秒間実行されたら、Podを削除します：



```
kubectl delete pod <pod-name>

kubectl get pods --watch
```


新しいPodが動作している時に、dbファイルを確認します：



```
kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


</details><br/>

両方のPodが同じファイルに書き込んでいるのを見るでしょう。データは実際にどこに保存されていますか？選べるいくつかのオプションがあります - Kubernetesはそれらを_ストレージクラス_と呼びます：


```
kubectl get storageclass
```


これらはプラットフォーム固有です。AKSはAzureストレージサービスを提供し、Docker Desktopはマシンのディスクを使用します。しかし、両方ともPVCのためのデフォルトのストレージクラスがあります、それはなぜ同じアプリケーションモデルを使用できる理由です。

## ラボ

AKSのデフォルトのストレージクラスは仮想ディスクを使用し、良好なI/O性能を提供しますが、一度に一つのノードにのみアタッチできます。時には共有ストレージが必要になり、もう一つのオプションは`azurefile`と呼ばれるストレージクラスを使用することです。これはAzureファイル共有サービスを使用し、多くのPodが多くのノードにアクセスできます。

Azure Files ストレージクラスを指定する新しいPVCを書き、データベースボリュームが新しいPVCを使用するようにデプロイメント仕様を修正し、3つのレプリカにスケールアップしてください。アプリは同じように動作しますが、Azureポータルで探索すればデータベースファイルにもアクセスできるはずです。

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボ用のリソースグループを削除して、すべてのリソース、ストレージを含む、を削除します。



```
az group delete -y --no-wait -n labs-aks-persistentvolumes
```

次に、KubernetesのコンテキストをローカルのDocker Desktopに戻してください：


```
kubectl config use-context docker-desktop
```
