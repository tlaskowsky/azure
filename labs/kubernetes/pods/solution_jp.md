# ラボの解決策

あなたのPodの仕様はこのようになります - [solution/lab.yaml](./solution/lab.yaml)のサンプルを使用できます：



```
apiVersion: v1
kind: Pod
metadata:
  name: sleep-lab
spec:
  containers:
    - name: app
      image: courselabs/bad-sleep
```


Kubectlで通常通りデプロイします：



```
kubectl apply -f labs/kubernetes/pods/solution/lab.yaml
```


次に、Podの状態を監視します：



```
kubectl get pod sleep-lab --watch
```


約30秒後、コンテナ内のアプリケーションが終了するため、コンテナは終了します - そしてKubernetesはPodを再起動します。watch出力に新しい行が表示され、再起動回数が1に増えたことが確認できます：


```
NAME        READY   STATUS    RESTARTS   AGE
sleep-lab   1/1     Running   0          3s
sleep-lab   0/1     Completed   0          33s
sleep-lab   1/1     Running     1          35s
```


> Podは既存のコンテナを再起動する**のではなく**新しいコンテナを作成して再起動します

新しいコンテナはアプリが30秒後に終了するまで実行されます。KubernetesはPodを再起動します - しかし、Podのコンテナが続けて終了する場合、Kubernetesは再起動する前に遅延時間を増やします。

> 状態が`Completed`から`Running`に再び変わりますが、Podは`CrashLoopBackOff`状態に入ります：


```
NAME        READY   STATUS    RESTARTS   AGE
sleep-lab   1/1     Running   0          3s
sleep-lab   0/1     Completed   0          33s
sleep-lab   1/1     Running     1          35s
sleep-lab   0/1     Completed   1          64s
sleep-lab   0/1     CrashLoopBackOff   1          79s
sleep-lab   1/1     Running            2          80s
sleep-lab   0/1     Completed          2          110s
sleep-lab   0/1     CrashLoopBackOff   2          2m4s
sleep-lab   1/1     Running            3          2m17s
```


Podを名前で削除できます：


```
kubectl delete pod sleep-lab
```


または、YAMLファイルを使ってdeleteコマンドを使用して削除します：


```
kubectl delete -f labs/kubernetes/pods/solution/lab.yaml
```


> [演習](README_jp.md)に戻る。
