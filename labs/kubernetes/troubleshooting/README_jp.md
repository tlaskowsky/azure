# Kubernetesでのアプリのトラブルシューティング

Kubectlで問題をトラブルシューティングする時間が多くなります。

Kubernetesはデプロイ時にAPI仕様の正確さを検証しますが、アプリが実際に動作するかどうかはチェックしません。

サービスやポッドのようなオブジェクトは疎結合なので、仕様にエラーがある場合、アプリケーションを壊しやすくなります。

## ラボ

これはすべて実験です :) このアプリを実行してみて、ポッドが再起動なしで健康状態になるように必要な変更を加えてください。


```
kubectl apply -f labs/kubernetes/troubleshooting/specs/pi-failing
```


> 目標は、http://localhost:8020 または http://localhost:30020 にアクセスして、Piアプリからの応答を見ることです

すぐに解決策に飛ばないでください！これらはあなたが常に直面する種類の問題なので、問題を診断するためのステップを踏み始めることが良いです。

> 行き詰まったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

完了したら、すべてのオブジェクトを削除できます：



```
kubectl delete all -l kubernetes.azureauthority.in=troubleshooting
```
