# ラボ解決策

PVCにアクセスモードとストレージクラス名を設定する必要があります：


```
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: pvc-azurefile
spec:
  accessModes:
    - ReadWriteMany
  storageClassName: azurefile
  resources:
    # ...
```


[lab/pvc-azurefile.yaml](./lab/pvc-azurefile.yaml)のサンプルファイルはこれを行っています。更新された[lab/deployment.yaml](./lab/deployment.yaml)は新しいPVCを使用します。

デプロイメントを更新します：



```
kubectl apply -f labs/aks-persistentvolumes/lab/

kubectl get pods --watch
```


新しいAzureファイルストレージがプロビジョニングされるまで1、2分かかる場合があります。新しいPodが動作している時、Podの1つを削除することができます：



```
kubectl delete pod <pod-name>
```


その後、どのPodでもデータベースファイルを確認します：



```
kubectl exec -it deploy/queue-worker -- cat /mnt/database/app.db
```


ポータルで自動生成された`MC_`リソースグループを開くと、ランダムな名前のストレージアカウントリソースが表示されます。それを開き、左ナビで_ファイル共有_を選択します。ランダムな名前で始まる`pvc-`という共有が表示されます。それを開くと、Podが書き込んでいる`app.db`ファイルが表示されます（ポータルで編集して、Podで変更を確認することもできます）。

ファイル共有は複数のノード上の複数のPodで利用可能です。
