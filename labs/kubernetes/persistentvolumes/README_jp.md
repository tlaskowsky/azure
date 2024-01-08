# PersistentVolumesを使ったアプリケーションデータの保存

Kubernetesはコンテナファイルシステムを作成し、複数のソースをマウントすることができます。ConfigMapsやSecretsなど、通常は読み取り専用のマウントを見てきましたが、今回は書き込み可能な[volumes](https://kubernetes.io/docs/concepts/storage/volumes/)を使用します。

Kubernetesのストレージはプラグイン可能であり、ローカルディスクから共有ネットワークファイルシステムまで、さまざまなタイプをサポートしています。

これらの詳細はアプリケーションモデルから隠されており、ストレージを要求するためにアプリが使用する抽象化 - [PersistentVolumeClaim](https://kubernetes.io/docs/concepts/storage/persistent-volumes/#introduction)によって隠されています。

## API 仕様

- [PersistentVolumeClaim](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.20/#persistentvolumeclaim-v1-core)

<details>
  <summary>YAML 概要</summary>

最も単純なPersistentVolumeClaim (PVC) は以下のようになります：



```
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: small-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 100Mi
```


ConfigMapsやSecretsと同様に、Podの仕様でボリュームを参照するためにPVC名を使用します。PVC仕様ではその要件が定義されます：

* `accessModes` - ストレージが読み取り専用か読み書き可能か、そしてそれが一つのノードに限定されるか多くのノードでアクセス可能かを記述します
* `resources` - PVCが必要とするストレージの量

Pod仕様には、コンテナファイルシステムにマウントするPVCボリュームを含めることができます：



```
volumes:
  - name: cache-volume
    persistentVolumeClaim:
      claimName: small-pvc
```


</details><br />

## コンテナの書き込み可能な層のデータ

PVCに取り組む前に、Kubernetesでアプリケーションデータを書き込む他のオプションを見てみましょう。

各コンテナには、ファイルを作成および更新するために使用できる書き込み可能な層があります。

このラボのデモアプリは、Piを計算するウェブサイトで、Nginxプロキシによってフロントエンドされます。プロキシは、ウェブサイトからの応答をキャッシュしてパフォーマンスを向上させます。

アプリをデプロイして試してみましょう：


```
kubectl apply -f labs/kubernetes/persistentvolumes/specs/pi
```


> http://localhost:30010/pi?dp=30000 または http://localhost:8010/pi?dp=30000 にアクセスすると、応答を計算して送信するのに1秒以上かかることがわかります

📋 再度アクセスすると、応答は即時になります - Nginxの応答キャッシュを確認することができます。これは `/tmp` フォルダーで見ることができます。

<details>
  <summary>わからない場合は？</summary>



```
kubectl exec deploy/pi-proxy -- ls /tmp
```


</details><br />

次に、コンテナプロセスを停止してPodを再起動します：



```
kubectl exec deploy/pi-proxy -- kill 1

kubectl get po -l app=pi-proxy
```


新しいコンテナの `/tmp` フォルダーを確認すると、空になっていることがわかります。Piアプリを再度リフレッシュすると、キャッシュが空なので再度計算され、ロードに再び1秒かかります。

> ℹ コンテナの書き込み可能層にあるデータはコンテナと同じライフサイクルを持っています。コンテナが置き換えられると、データも失われます。

## EmptyDirボリュームによるPodストレージ

ボリュームは外部ソースからコンテナファイルシステムにストレージをマウントします。最も単純なタイプのボリュームは`EmptyDir`と呼ばれ、Podレベルで空のディレクトリを作成し、Podのコンテナがマウントできます。

永続的でないデータに使用できますが、再起動を生き延びるようにしたい場合には適しています。データのローカルキャッシュを保持するのに最適です。

- [caching-proxy-emptydir/nginx.yaml](specs/caching-proxy-emptydir/nginx.yaml) - `/tmp`ディレクトリにマウントされるEmptyDirボリュームを使用します

これはPod仕様の変更なので、新しい空のディレクトリボリュームを持つ新しいPodが得られます：



```
kubectl apply -f labs/kubernetes/persistentvolumes/specs/caching-proxy-emptydir

kubectl wait --for=condition=Ready pod -l app=pi-proxy,storage=emptydir
```


ページをリフレッシュして、Pi計算が再び行われるのを見てください - 結果がキャッシュされ、`/tmp`フォルダーが埋まっていくのを見ることができます。

> コンテナは同じファイルシステム構造を見ますが、今は`/tmp`フォルダーがEmptyDirボリュームからマウントされています

📋 Nginxプロセスを停止し、Podが再起動されることを確認します。新しいコンテナの`tmp`フォルダーに古いデータがまだ利用可能かどうかを確認します。

<details>
  <summary>わからない場合は？</summary>



```
kubectl exec deploy/pi-proxy -- kill 1

kubectl get pods -l app=pi-proxy,storage=emptydir 

kubectl exec deploy/pi-proxy -- ls /tmp
```


</details><br />

新しいコンテナでサイトをリフレッシュすると、即座にロードされます。

Podを削除すると、Deploymentは新しいEmptyDirボリュームを持つ代替品を作成しますが、それは空になります。

> ℹ EmptyDirボリュームのデータはPodと同じライフサイクルを持っています。Podが置き換えられると、データも失われます。

## PersistentVolumeClaimsを使った外部ストレージ

永続ストレージは、アプリが置き換えられてもデータが持続する、アプリから独立したライフサイクルを持つボリュームを使用することに関連しています。

Kubernetesのストレージはプラグイン可能であり、本番クラスタでは通常、[Storage Classes](https://kubernetes.io/docs/concepts/storage/storage-classes/)として定義された複数のタイプが提供されます：


```
kubectl get storageclass
```


Docker Desktopやk3dでは単一のStorageClassが表示されますが、AKSのようなクラウドサービスでは、多くの異なるプロパティを持つ多数のものが表示されます（例：1つのノードにアタッチできる高速SSD、または多数のノードで使用できる共有ネットワークストレージ位置）。

名前付きStorageClassを持つPersistentVolumeClaimを作成することも、クラスを省略してデフォルトを使用することもできます。

- [caching-proxy-pvc/pvc.yaml](specs/caching-proxy-pvc/pvc.yaml)は、100MBのストレージを要求し、単一のノードが読み書きアクセスのためにマウントできます


```
kubectl apply -f labs/kubernetes/persistentvolumes/specs/caching-proxy-pvc/pvc.yaml
```


各StorageClassには、オンデマンドでストレージユニットを作成できるプロビジョナーがあります。


📋 永続ボリュームとクレームをリストします。

<details>
  <summary>わからない場合は？</summary>



```
kubectl get pvc

kubectl get persistentvolumes
```


> 一部のプロビジョナーはPVCが作成されるとすぐにストレージを作成します - 他のものはPVCがPodによって要求されるのを待ちます

</details><br />


この[Deployment spec](specs/caching-proxy-pvc/nginx.yaml)は、NginxプロキシをPVCを使用するように更新します：



```
kubectl apply -f labs/kubernetes/persistentvolumes/specs/caching-proxy-pvc/

kubectl wait --for=condition=Ready pod -l app=pi-proxy,storage=pvc

kubectl get pvc,pv
```


> これでPVCはバインドされ、PVCで要求されたサイズとアクセスモードを持つPersistentVolumeが存在します

PVCは最初は空です。アプリをリフレッシュすると、`/tmp`フォルダーが埋まっていくのを見ることができます。

📋 Podを再起動し、その後置き換えて、PVCのデータがどちらも生き残ることを確認します。

<details>
  <summary>わからない場合は？</summary>



```
# コンテナを終了させる
kubectl exec deploy/pi-proxy -- kill 1

kubectl get pods -l app=pi-proxy,storage=pvc

kubectl exec deploy/pi-proxy -- ls /tmp
```

```
# Podを置き換えるためにロールアウトを強制する
kubectl rollout restart deploy/pi-proxy

kubectl get pods -l app=pi-proxy,storage=pvc

kubectl exec deploy/pi-proxy -- ls /tmp
```

再度アプリを試してみてください。新しいPodでもキャッシュからのレスポンスが提供されるため、非常に高速になります。

</details><br />

> ℹ 永続ボリュームには独自のライフサイクルがあります。PVが削除されるまでデータは保持されます。

## 実験

永続ストレージを取得する方法はもっと簡単ですが、PVCを使用するほど柔軟ではなく、いくつかのセキュリティ上の懸念があります。

異なるタイプのボリュームを持つシンプルなスリープPodを実行し、そのPodが実行されているホストノードのルートドライブにアクセスできるようにします。

Nginx Podからキャッシュファイルを見つけるためにスリープPodを使用できますか？

> 詰まったら[hints](hints_jp.md)を試すか、[solution](solution_jp.md)を確認してください。

___

## クリーンアップ



```
kubectl delete all,cm,pvc,pv -l kubernetes.azureauthority.in=persistentvolumes
```
