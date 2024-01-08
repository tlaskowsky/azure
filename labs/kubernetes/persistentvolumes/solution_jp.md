# 実験の解決策

ノードのディスクにアクセスするには、ルートパス `/` をターゲットにした `HostPath` ボリュームを使用して新しいPodを作成します。

## HostPath ボリュームを使用した Pod の作成

[sleep-with-hostpath.yaml](solution/sleep-with-hostpath.yaml) はPodスペックでHostPathボリュームを使用します。

Podをデプロイします：



```
kubectl apply -f labs/kubernetes/persistentvolumes/solution
```


このPodコンテナは、ノードのディスクのルートをコンテナ内の `/node-root` にマウントし、rootとして実行されます。

これにより、ディスク上でほとんど何でも実行できます：



```
kubectl exec pod/sleep -- ls /node-root

kubectl exec pod/sleep -- mkdir -p /node-root/secret/hacker/tools

kubectl exec pod/sleep -- ls -l /node-root/secret/hacker
```


> それが安全ではない理由です :) ノードのディスクにアクセスする必要がある場合は、ルートドライブ全体ではなく、より制限されたスコープを持つhostPathを使用するべきです。

> [演習に戻る](README_jp.md)。
