# ラボの解決策

これが私の解決策です：

- [deployment-productionized.yaml](solution/deployment-productionized.yaml) - (非常に積極的な)コンテナプローブを追加します


```
kubectl apply -f labs/kubernetes/containerprobes/solution
```


2つのターミナルを開いて、修復とスケーリングの動作を監視できます：



```
kubectl get pods -l app=rngapi --watch

kubectl get endpoints rngapi-lb --watch
```


> curlで再びAPIを呼び出します。速すぎると失敗を見ることになりますが、リトライの間に数秒間待つと、アプリはオンラインに戻ります。

繰り返し呼び出すと、最終的にはすべてのPodがCrashLoopBackOffになります。これは、Kubernetesがアプリが不安定だと判断するためです。


> [練習問題](README_jp.md)に戻る
